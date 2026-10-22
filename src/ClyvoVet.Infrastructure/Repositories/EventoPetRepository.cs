using System.Linq.Expressions;
using ClyvoVet.Infrastructure.Data;
using ClyvoVet.Domain.Enums;
using ClyvoVet.Domain.Entities;
using ClyvoVet.Application.Abstractions.Repositories;
using ClyvoVet.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace ClyvoVet.Infrastructure.Repositories;

public class EventoPetRepository : IEventoPetRepository
{
    private readonly AppDbContext _context;

    public EventoPetRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Os campos aceitos em <c>ordenarPor</c> (camelCase, sem distinguir caixa). Acompanha os campos
    /// escalares do <c>EventoPetResponse</c> — o <c>CamposOrdenaveisTests</c> trava isso.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, Expression<Func<EventoPet, object?>>> CamposOrdenaveis =
        new Dictionary<string, Expression<Func<EventoPet, object?>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["titulo"] = e => e.Titulo,
            ["descricao"] = e => e.Descricao,
            ["tipo"] = e => e.Tipo,
            ["rua"] = e => e.Rua,
            ["numero"] = e => e.Numero,
            ["bairro"] = e => e.Bairro,
            ["cidade"] = e => e.Cidade,
            ["estado"] = e => e.Estado,
            ["cep"] = e => e.Cep,
            ["dataInicio"] = e => e.DataInicio,
            ["dataFim"] = e => e.DataFim,
            ["especieAlvo"] = e => e.EspecieAlvo,
            ["organizador"] = e => e.Organizador,
            ["gratuito"] = e => e.Gratuito,
            ["linkInscricao"] = e => e.LinkInscricao,
            ["ativo"] = e => e.Ativo,
            ["criadoEm"] = e => e.CriadoEm,
        };

    public async Task<PaginaDeResultados<EventoPet>> GetAllAsync(ConsultaPaginada consulta, string? cidade, TipoEventoPetEnum? tipo, EspecieEnum? especieAlvo)
    {
        var query = _context.EventosPet.AsQueryable();

        if (!string.IsNullOrWhiteSpace(cidade))
            query = query.Where(e => e.Cidade != null && e.Cidade.ToUpper() == cidade.ToUpper());

        if (tipo.HasValue)
            query = query.Where(e => e.Tipo == tipo.Value);

        if (especieAlvo.HasValue)
            query = query.Where(e => e.EspecieAlvo == especieAlvo.Value);

        var total = await query.CountAsync();

        var ordenada = Ordenacao.Aplicar(query, consulta, CamposOrdenaveis,
            q => q.OrderBy(e => e.DataInicio), e => e.Id);

        var itens = await Paginacao.Aplicar(ordenada, consulta.Page, consulta.PageSize).ToListAsync();
        return new PaginaDeResultados<EventoPet>(itens, total, consulta.Page, consulta.PageSize);
    }

    public async Task<EventoPet?> GetByIdAsync(string id)
    {
        return await _context.EventosPet.FindAsync(id);
    }

    public async Task<EventoPet> CreateAsync(EventoPet evento)
    {
        // MySQL nao suporta RETURNING, entao o Id/CriadoEm sao gerados aqui em vez de
        // depender de DEFAULT do banco + leitura de volta pelo EF Core.
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
