using ClyvoVet.Api.Data;
using ClyvoVet.Api.Enums;
using ClyvoVet.Api.Models;
using ClyvoVet.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClyvoVet.Api.Repositories;

public class LembreteRepository : ILembreteRepository
{
    private readonly AppDbContext _context;

    public LembreteRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Lembrete>> GetAllAsync(int page, int pageSize, string? animalId, TipoLembreteEnum? tipo, StatusLembreteEnum? status)
    {
        var query = _context.Lembretes
            .Include(l => l.Animal)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(animalId))
            query = query.Where(l => l.AnimalId == animalId);

        if (tipo.HasValue)
            query = query.Where(l => l.Tipo == tipo.Value);

        if (status.HasValue)
            query = query.Where(l => l.Status == status.Value);

        return await query
            .OrderBy(l => l.AgendadoEm)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<Lembrete?> GetByIdAsync(string id)
    {
        return await _context.Lembretes
            .Include(l => l.Animal)
            .FirstOrDefaultAsync(l => l.Id == id);
    }

    public async Task<Lembrete> CreateAsync(Lembrete lembrete)
    {
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
