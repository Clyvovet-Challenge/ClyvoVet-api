using ClyvoVet.Api.DTOs.Request;
using ClyvoVet.Api.DTOs.Response;
using ClyvoVet.Api.Enums;
using ClyvoVet.Api.Exceptions;
using ClyvoVet.Api.Models;
using ClyvoVet.Api.Repositories.Interfaces;
using ClyvoVet.Api.Services.Interfaces;

namespace ClyvoVet.Api.Services;

public class EventoPetService : IEventoPetService
{
    private readonly IEventoPetRepository _repository;

    public EventoPetService(IEventoPetRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<EventoPetResponse>> GetAllAsync(int page, int pageSize, string? cidade, TipoEventoPetEnum? tipo, EspecieEnum? especieAlvo)
    {
        var eventos = await _repository.GetAllAsync(page, pageSize, cidade, tipo, especieAlvo);
        return eventos.Select(MapToResponse);
    }

    public async Task<EventoPetResponse> GetByIdAsync(string id)
    {
        var evento = await _repository.GetByIdAsync(id);
        if (evento is null)
            throw new NotFoundException($"Evento com id {id} não encontrado.");
        return MapToResponse(evento);
    }

    public async Task<EventoPetResponse> CreateAsync(EventoPetRequest request)
    {
        if (request.DataInicio < DateOnly.FromDateTime(DateTime.Today))
            throw new BadRequestException("A data de início do evento não pode ser no passado.");

        if (request.DataFim.HasValue && request.DataFim.Value < request.DataInicio)
            throw new BadRequestException("A data de fim não pode ser anterior à data de início.");

        var evento = MapToEntity(request);
        var created = await _repository.CreateAsync(evento);
        return MapToResponse(created);
    }

    public async Task<EventoPetResponse> UpdateAsync(string id, EventoPetRequest request)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing is null)
            throw new NotFoundException($"Evento com id {id} não encontrado.");

        if (request.DataInicio < DateOnly.FromDateTime(DateTime.Today))
            throw new BadRequestException("A data de início do evento não pode ser no passado.");

        if (request.DataFim.HasValue && request.DataFim.Value < request.DataInicio)
            throw new BadRequestException("A data de fim não pode ser anterior à data de início.");

        var evento = MapToEntity(request);
        var updated = await _repository.UpdateAsync(id, evento);
        return MapToResponse(updated!);
    }

    public async Task DeleteAsync(string id)
    {
        var deleted = await _repository.DeleteAsync(id);
        if (!deleted)
            throw new NotFoundException($"Evento com id {id} não encontrado.");
    }

    private static EventoPet MapToEntity(EventoPetRequest request) => new()
    {
        Titulo = request.Titulo,
        Descricao = request.Descricao,
        Tipo = request.Tipo,
        Rua = request.Rua,
        Numero = request.Numero,
        Bairro = request.Bairro,
        Cidade = request.Cidade,
        Estado = request.Estado,
        Cep = request.Cep,
        DataInicio = request.DataInicio,
        DataFim = request.DataFim,
        EspecieAlvo = request.EspecieAlvo,
        Organizador = request.Organizador,
        Gratuito = request.Gratuito,
        LinkInscricao = request.LinkInscricao,
        Ativo = request.Ativo
    };

    private static EventoPetResponse MapToResponse(EventoPet evento) => new()
    {
        Id = evento.Id,
        Titulo = evento.Titulo,
        Descricao = evento.Descricao,
        Tipo = evento.Tipo,
        Rua = evento.Rua,
        Numero = evento.Numero,
        Bairro = evento.Bairro,
        Cidade = evento.Cidade,
        Estado = evento.Estado,
        Cep = evento.Cep,
        DataInicio = evento.DataInicio,
        DataFim = evento.DataFim,
        EspecieAlvo = evento.EspecieAlvo,
        Organizador = evento.Organizador,
        Gratuito = evento.Gratuito,
        LinkInscricao = evento.LinkInscricao,
        Ativo = evento.Ativo,
        CriadoEm = evento.CriadoEm
    };
}
