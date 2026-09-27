using System.Linq.Expressions;
using ClyvoVet.Infrastructure.Data;
using ClyvoVet.Domain.Enums;
using ClyvoVet.Domain.Entities;
using ClyvoVet.Application.Abstractions.Repositories;
using ClyvoVet.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace ClyvoVet.Infrastructure.Repositories;

public class LembreteRepository : ILembreteRepository
{
    private readonly AppDbContext _context;

    public LembreteRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Os campos aceitos em <c>ordenarPor</c> (camelCase, sem distinguir caixa). Acompanha os campos
    /// escalares do <c>LembreteResponse</c> — o <c>CamposOrdenaveisTests</c> trava isso.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, Expression<Func<Lembrete, object?>>> CamposOrdenaveis =
        new Dictionary<string, Expression<Func<Lembrete, object?>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["animalId"] = l => l.AnimalId,
            ["nomeAnimal"] = l => l.Animal.Nome,
            ["titulo"] = l => l.Titulo,
            ["descricao"] = l => l.Descricao,
            ["tipo"] = l => l.Tipo,
            ["agendadoEm"] = l => l.AgendadoEm,
            ["recorrente"] = l => l.Recorrente,
            ["intervaloDias"] = l => l.IntervaloDias,
            ["repetirAte"] = l => l.RepetirAte,
            ["status"] = l => l.Status,
            ["criadoEm"] = l => l.CriadoEm,
        };

    public async Task<PaginaDeResultados<Lembrete>> GetAllAsync(ConsultaPaginada consulta, string? animalId, TipoLembreteEnum? tipo, StatusLembreteEnum? status, string? tutorId = null)
    {
        var query = _context.Lembretes
            .Include(l => l.Animal)
            .AsQueryable();

        // O recorte por dono entra ANTES do filtro por animal, e os dois se
        // somam. Se o animalId substituisse o recorte, `?animalId=<de outro
        // tutor>` seria o proprio bypass -- o parametro de conveniencia viraria
        // o furo.
        if (!string.IsNullOrWhiteSpace(tutorId))
            query = query.Where(l => l.Animal.TutorId == tutorId);

        if (!string.IsNullOrWhiteSpace(animalId))
            query = query.Where(l => l.AnimalId == animalId);

        if (tipo.HasValue)
            query = query.Where(l => l.Tipo == tipo.Value);

        if (status.HasValue)
            query = query.Where(l => l.Status == status.Value);

        // O total conta a consulta JÁ filtrada (inclusive pelo recorte de dono) e ANTES do recorte
        // de página: é ele que permite montar o link `last` e o X-Total-Count.
        var total = await query.CountAsync();

        var ordenada = Ordenacao.Aplicar(query, consulta, CamposOrdenaveis,
            q => q.OrderBy(l => l.AgendadoEm), l => l.Id);

        var itens = await Paginacao.Aplicar(ordenada, consulta.Page, consulta.PageSize).ToListAsync();
        return new PaginaDeResultados<Lembrete>(itens, total, consulta.Page, consulta.PageSize);
    }

    public async Task<Lembrete?> GetByIdAsync(string id)
    {
        return await _context.Lembretes
            .Include(l => l.Animal)
            .FirstOrDefaultAsync(l => l.Id == id);
    }

    public async Task<IEnumerable<Lembrete>> GetPendentesVencendoAsync(DateTime limite)
    {
        return await _context.Lembretes
            .Include(l => l.Animal)
            .ThenInclude(a => a.Tutor)
            .Where(l => l.Status == StatusLembreteEnum.Pendente && l.AgendadoEm <= limite)
            .Where(l => _context.TutoresTelegram.Any(v => v.TutorId == l.Animal.TutorId))
            .ToListAsync();
    }

    public Task<int> ContarPendentesSemTelegramAsync(DateTime limite) =>
        _context.Lembretes
            .Where(l => l.Status == StatusLembreteEnum.Pendente && l.AgendadoEm <= limite)
            .Where(l => !_context.TutoresTelegram.Any(v => v.TutorId == l.Animal.TutorId))
            .CountAsync();

    public async Task<IEnumerable<Lembrete>> GetPendentesByTutorIdAsync(string tutorId)
    {
        return await _context.Lembretes
            .Include(l => l.Animal)
            .Where(l => l.Status == StatusLembreteEnum.Pendente && l.Animal.TutorId == tutorId)
            .OrderBy(l => l.AgendadoEm)
            .ToListAsync();
    }

    public async Task<Lembrete> CreateAsync(Lembrete lembrete)
    {
        // MySQL nao suporta RETURNING, entao o Id/CriadoEm sao gerados aqui em vez de
        // depender de DEFAULT do banco + leitura de volta pelo EF Core.
        lembrete.Id = Guid.NewGuid().ToString();
        lembrete.CriadoEm = DateTime.UtcNow;
        _context.Lembretes.Add(lembrete);
        await _context.SaveChangesAsync();
        return lembrete;
    }

    public async Task<Lembrete?> UpdateAsync(string id, Lembrete lembrete)
    {
        var existing = await _context.Lembretes.FindAsync(id);
        if (existing is null) return null;

        existing.AnimalId = lembrete.AnimalId;
        existing.Titulo = lembrete.Titulo;
        existing.Descricao = lembrete.Descricao;
        existing.Tipo = lembrete.Tipo;
        existing.AgendadoEm = lembrete.AgendadoEm;
        existing.Recorrente = lembrete.Recorrente;
        existing.IntervaloDias = lembrete.IntervaloDias;
        existing.RepetirAte = lembrete.RepetirAte;
        existing.Status = lembrete.Status;

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var lembrete = await _context.Lembretes.FindAsync(id);
        if (lembrete is null) return false;

        _context.Lembretes.Remove(lembrete);
        await _context.SaveChangesAsync();
        return true;
    }
}
