using ClyvoVet.Api.Data;
using ClyvoVet.Api.Enums;
using ClyvoVet.Api.Models;
using ClyvoVet.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClyvoVet.Api.Repositories;

public class PredisposicaoSaudeRepository : IPredisposicaoSaudeRepository
{
    private readonly AppDbContext _context;

    public PredisposicaoSaudeRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<PredisposicaoSaude>> GetByEspecieAsync(EspecieEnum especie)
    {
        return await _context.PredisposicoesSaude
            .Where(p => p.Especie == especie)
            .ToListAsync();
    }
}
