using ClyvoVet.Api.DTOs.Request;
using ClyvoVet.Api.DTOs.Response;
using ClyvoVet.Api.Enums;
using ClyvoVet.Api.Exceptions;
using ClyvoVet.Api.Models;
using ClyvoVet.Api.Repositories.Interfaces;
using ClyvoVet.Api.Services.Interfaces;

namespace ClyvoVet.Api.Services;

public class LembreteService : ILembreteService
{
    private readonly ILembreteRepository _repository;
    private readonly IAnimalRepository _animalRepository;

    public LembreteService(ILembreteRepository repository, IAnimalRepository animalRepository)
    {
        _repository = repository;
        _animalRepository = animalRepository;
    }

    public async Task<IEnumerable<LembreteResponse>> GetAllAsync(int page, int pageSize, string? animalId, StatusLembreteEnum? status, TipoLembreteEnum? tipo, string? tutorId = null)
    {
        var lembretes = await _repository.GetAllAsync(page, pageSize, animalId, tipo, status, tutorId);
        return lembretes.Select(MapToResponse);
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

    public async Task<LembreteResponse> CreateAsync(LembreteRequest request)
    {
        var animal = await _animalRepository.GetByIdAsync(request.AnimalId);
        if (animal is null)
            throw new NotFoundException($"Animal com id {request.AnimalId} não encontrado.");

        if (DataValidationHelper.EhDataNoPassado(request.AgendadoEm))
            throw new BadRequestException("A data do lembrete não pode ser no passado.");

        ValidarSerie(request);

        var lembrete = new Lembrete
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
            Status = StatusLembreteEnum.Pendente
        };

        var created = await _repository.CreateAsync(lembrete);
        var full = await _repository.GetByIdAsync(created.Id);
        return MapToResponse(full!);
    }

    public async Task<LembreteResponse> UpdateAsync(string id, LembreteRequest request)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing is null)
            throw new NotFoundException($"Lembrete com id {id} não encontrado.");

        var animal = await _animalRepository.GetByIdAsync(request.AnimalId);
        if (animal is null)
            throw new NotFoundException($"Animal com id {request.AnimalId} não encontrado.");

        if (DataValidationHelper.EhDataNoPassado(request.AgendadoEm))
            throw new BadRequestException("A data do lembrete não pode ser no passado.");

        ValidarSerie(request);

        var lembrete = new Lembrete
        {
            AnimalId = request.AnimalId,
            Titulo = request.Titulo,
            Descricao = request.Descricao,
            Tipo = request.Tipo,
            AgendadoEm = request.AgendadoEm,
            Recorrente = request.IntervaloDias.HasValue,   // derivado; ver CreateAsync
            IntervaloDias = request.IntervaloDias,
            RepetirAte = request.RepetirAte,
            Status = existing.Status
        };

        await _repository.UpdateAsync(id, lembrete);
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
