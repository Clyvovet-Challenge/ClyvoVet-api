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

    public async Task<IEnumerable<LembreteResponse>> GetAllAsync(int page, int pageSize, string? animalId, StatusLembreteEnum? status, TipoLembreteEnum? tipo)
    {
        var lembretes = await _repository.GetAllAsync(page, pageSize, animalId, tipo, status);
        return lembretes.Select(MapToResponse);
    }

    public async Task<LembreteResponse> GetByIdAsync(string id)
    {
        var lembrete = await _repository.GetByIdAsync(id);
        if (lembrete is null)
            throw new NotFoundException($"Lembrete com id {id} não encontrado.");
        return MapToResponse(lembrete);
    }

    public async Task<LembreteResponse> CreateAsync(LembreteRequest request)
    {
        var animal = await _animalRepository.GetByIdAsync(request.AnimalId);
        if (animal is null)
            throw new NotFoundException($"Animal com id {request.AnimalId} não encontrado.");

        if (request.AgendadoEm < DateTime.Now)
            throw new BadRequestException("A data do lembrete não pode ser no passado.");

        var lembrete = new Lembrete
        {
            AnimalId = request.AnimalId,
            Titulo = request.Titulo,
            Descricao = request.Descricao,
            Tipo = request.Tipo,
            AgendadoEm = request.AgendadoEm,
            Recorrente = request.Recorrente,
            Status = request.Status
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

        var lembrete = new Lembrete
        {
            AnimalId = request.AnimalId,
            Titulo = request.Titulo,
            Descricao = request.Descricao,
            Tipo = request.Tipo,
            AgendadoEm = request.AgendadoEm,
            Recorrente = request.Recorrente,
            Status = request.Status
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
        Status = lembrete.Status,
        CriadoEm = lembrete.CriadoEm
    };
}
