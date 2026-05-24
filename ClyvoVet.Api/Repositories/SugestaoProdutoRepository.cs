using ClyvoVet.Api.Data;
using ClyvoVet.Api.Models;
using ClyvoVet.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClyvoVet.Api.Repositories;

public class SugestaoProdutoRepository : ISugestaoProdutoRepository
{
    private readonly AppDbContext _context;

    public SugestaoProdutoRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<SugestaoProduto>> GetAllAsync(int page, int pageSize, string? animalId)
    {
        var query = _context.SugestoesProduto
            .Include(s => s.Animal)
            .Include(s => s.Produto)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(animalId))
            query = query.Where(s => s.AnimalId == animalId);

        return await query
            .OrderByDescending(s => s.DataSugestao)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<SugestaoProduto?> GetByIdAsync(string id)
    {
        return await _context.SugestoesProduto
            .Include(s => s.Animal)
            .Include(s => s.Produto)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<SugestaoProduto> CreateAsync(SugestaoProduto sugestao)
    {
        // Id e CriadoEm gerados pelo banco via trigger + DEFAULT SYSTIMESTAMP.
        _context.SugestoesProduto.Add(sugestao);
        await _context.SaveChangesAsync();
        return sugestao;
    }

    public async Task<SugestaoProduto?> UpdateAsync(string id, SugestaoProduto sugestao)
    {
        var existing = await _context.SugestoesProduto.FindAsync(id);
        if (existing is null) return null;

        existing.AnimalId = sugestao.AnimalId;
        existing.ProdutoId = sugestao.ProdutoId;
        existing.Justificativa = sugestao.Justificativa;
        existing.DataSugestao = sugestao.DataSugestao;
        existing.Ativo = sugestao.Ativo;

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var sugestao = await _context.SugestoesProduto.FindAsync(id);
        if (sugestao is null) return false;

        _context.SugestoesProduto.Remove(sugestao);
        await _context.SaveChangesAsync();
        return true;
    }
}
