using ClyvoVet.Api.Data;
using ClyvoVet.Api.Enums;
using ClyvoVet.Api.Models;
using ClyvoVet.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClyvoVet.Api.Repositories;

public class EventoPetRepository : IEventoPetRepository
{
    private readonly AppDbContext _context;

    public EventoPetRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<EventoPet>> GetAllAsync(int page, int pageSize, string? cidade, TipoEventoPetEnum? tipo, EspecieEnum? especieAlvo)
    {
        var query = _context.EventosPet.AsQueryable();

        if (!string.IsNullOrWhiteSpace(cidade))
            query = query.Where(e => e.Cidade != null && e.Cidade.ToUpper() == cidade.ToUpper());

        if (tipo.HasValue)
            query = query.Where(e => e.Tipo == tipo.Value);

        if (especieAlvo.HasValue)
            query = query.Where(e => e.EspecieAlvo == especieAlvo.Value);

        return await query
            .OrderBy(e => e.DataInicio)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<EventoPet?> GetByIdAsync(string id)
    {
        return await _context.EventosPet.FindAsync(id);
    }

    public async Task<EventoPet> CreateAsync(EventoPet evento)
    {
        evento.Id = Guid.NewGuid().ToString();
        evento.CriadoEm = DateTime.UtcNow;
        _context.EventosPet.Add(evento);
        await _context.SaveChangesAsync();
        return evento;
    }

    public async Task<EventoPet?> UpdateAsync(string id, EventoPet evento)
    {
        var existing = await _context.EventosPet.FindAsync(id);
        if (existing is null) return null;

        existing.Titulo = evento.Titulo;
        existing.Descricao = evento.Descricao;
        existing.Tipo = evento.Tipo;
        existing.Rua = evento.Rua;
        existing.Numero = evento.Numero;
        existing.Bairro = evento.Bairro;
        existing.Cidade = evento.Cidade;
        existing.Estado = evento.Estado;
        existing.Cep = evento.Cep;
        existing.DataInicio = evento.DataInicio;
        existing.DataFim = evento.DataFim;
        existing.EspecieAlvo = evento.EspecieAlvo;
        existing.Organizador = evento.Organizador;
        existing.Gratuito = evento.Gratuito;
        existing.LinkInscricao = evento.LinkInscricao;
        existing.Ativo = evento.Ativo;

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var evento = await _context.EventosPet.FindAsync(id);
        if (evento is null) return false;

        _context.EventosPet.Remove(evento);
        await _context.SaveChangesAsync();
        return true;
    }
}
