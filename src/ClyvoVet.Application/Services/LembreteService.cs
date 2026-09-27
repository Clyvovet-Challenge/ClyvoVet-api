using ClyvoVet.Application.Common;
using ClyvoVet.Application.DTOs.Request;
using ClyvoVet.Application.DTOs.Response;
using ClyvoVet.Domain.Enums;
using ClyvoVet.Domain.Exceptions;
using ClyvoVet.Domain.Entities;
using ClyvoVet.Application.Abstractions.Repositories;
using ClyvoVet.Application.Services.Interfaces;

namespace ClyvoVet.Application.Services;

public class LembreteService : ILembreteService
{
    private readonly ILembreteRepository _repository;
    private readonly IAnimalRepository _animalRepository;

    public LembreteService(ILembreteRepository repository, IAnimalRepository animalRepository)
    {
        _repository = repository;
        _animalRepository = animalRepository;
    }

    public async Task<PaginaDeResultados<LembreteResponse>> GetAllAsync(ConsultaPaginada consulta, string? animalId, StatusLembreteEnum? status, TipoLembreteEnum? tipo, string? tutorId = null)
    {
        var pagina = await _repository.GetAllAsync(consulta, animalId, tipo, status, tutorId);
        return pagina.Mapear(MapToResponse);
    }

    public async Task<LembreteResponse> GetByIdAsync(string id)
    {
        var lembrete = await _repository.GetByIdAsync(id);
        if (lembrete is null)
            throw new NotFoundException($"Lembrete com id {id} não encontrado.");
        return MapToResponse(lembrete);
    }

    /// <summary>
    /// A serie precisa fazer sentido antes de ser gravada.
    /// </summary>
    /// <remarks>
    /// Duas recusas, e as duas evitam um lembrete que nunca dispararia como o
    /// usuario espera:
    ///
    /// <para>1. <c>RepetirAte</c> antes de <c>AgendadoEm</c> descreve uma serie
    /// que termina antes de comecar. O motor a encerraria no primeiro disparo, e
    /// o tutor teria pedido "de 10/09 ate 05/09" sem ouvir que isso e vazio.</para>
    ///
    /// <para>2. <c>RepetirAte</c> sem <c>IntervaloDias</c> e um fim para uma
    /// serie que nao existe. Aceitar em silencio gravaria uma data que nada le —
    /// e o usuario acharia que configurou uma repeticao.</para>
    /// </remarks>
    private static void ValidarSerie(LembreteRequest request)
    {
        if (request.RepetirAte.HasValue && !request.IntervaloDias.HasValue)
            throw new BadRequestException(
                "Para repetir até uma data, escolha de quantos em quantos dias o lembrete volta.");

        if (request.RepetirAte.HasValue && request.RepetirAte.Value < request.AgendadoEm)
            throw new BadRequestException(
                "A data final da repetição não pode ser anterior à data do lembrete.");
    }

    /// <summary>Criar e atualizar aceitam o mesmo corpo, então validam do mesmo jeito.</summary>
    private async Task ValidarAsync(LembreteRequest request)
    {
        var animal = await _animalRepository.GetByIdAsync(request.AnimalId);
        if (animal is null)
            throw new NotFoundException($"Animal com id {request.AnimalId} não encontrado.");

        if (DataValidationHelper.EhDataNoPassado(request.AgendadoEm))
            throw new BadRequestException("A data do lembrete não pode ser no passado.");

        ValidarSerie(request);
    }

    private static Lembrete Montar(LembreteRequest request, StatusLembreteEnum status) => new()
    {
        AnimalId = request.AnimalId,
        Titulo = request.Titulo,
        Descricao = request.Descricao,
        Tipo = request.Tipo,
        AgendadoEm = request.AgendadoEm,
        // DERIVADO, e nao copiado do request: com duas fontes para a mesma
        // verdade, um corpo com {"recorrente": true} sem intervalo gravaria
        // um lembrete que se diz recorrente e nao repete -- exatamente o
        // defeito que a V18 veio consertar.
        Recorrente = request.IntervaloDias.HasValue,
        IntervaloDias = request.IntervaloDias,
        RepetirAte = request.RepetirAte,
        Status = status
    };

    public async Task<LembreteResponse> CreateAsync(LembreteRequest request)
    {
        await ValidarAsync(request);

        var created = await _repository.CreateAsync(Montar(request, StatusLembreteEnum.Pendente));
        var full = await _repository.GetByIdAsync(created.Id);
        return MapToResponse(full!);
    }

    public async Task<LembreteResponse> UpdateAsync(string id, LembreteRequest request)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing is null)
            throw new NotFoundException($"Lembrete com id {id} não encontrado.");

        await ValidarAsync(request);

        // O status não vem do corpo: editar um lembrete não o reenvia nem o dá por enviado.
        // Só a data nova o reabre: o worker só dispara Pendente, então um Enviado
        // reagendado ficaria mudo para sempre. A data já foi validada como futura.
        var status = request.AgendadoEm != existing.AgendadoEm
            ? StatusLembreteEnum.Pendente
            : existing.Status;
        await _repository.UpdateAsync(id, Montar(request, status));
        var full = await _repository.GetByIdAsync(id);
        return MapToResponse(full!);
    }

    public async Task DeleteAsync(string id)
    {
        var deleted = await _repository.DeleteAsync(id);
        if (!deleted)
            throw new NotFoundException($"Lembrete com id {id} não encontrado.");
    }

    private static LembreteResponse MapToResponse(Lembrete lembrete) => new()
    {
        Id = lembrete.Id,
        AnimalId = lembrete.AnimalId,
        NomeAnimal = lembrete.Animal?.Nome ?? string.Empty,
        Titulo = lembrete.Titulo,
        Descricao = lembrete.Descricao,
        Tipo = lembrete.Tipo,
        AgendadoEm = lembrete.AgendadoEm,
        Recorrente = lembrete.Recorrente,
        IntervaloDias = lembrete.IntervaloDias,
        RepetirAte = lembrete.RepetirAte,
        Status = lembrete.Status,
        CriadoEm = lembrete.CriadoEm
    };
}
