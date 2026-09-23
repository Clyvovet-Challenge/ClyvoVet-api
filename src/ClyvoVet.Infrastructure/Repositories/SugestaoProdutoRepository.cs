using System.Linq.Expressions;
using ClyvoVet.Application.Common;
using ClyvoVet.Infrastructure.Data;
using ClyvoVet.Domain.Entities;
using ClyvoVet.Application.Abstractions.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ClyvoVet.Infrastructure.Repositories;

public class SugestaoProdutoRepository : ISugestaoProdutoRepository
{
    private readonly AppDbContext _context;

    public static readonly IReadOnlyDictionary<string, Expression<Func<SugestaoProduto, object?>>> CamposOrdenaveis =
        new Dictionary<string, Expression<Func<SugestaoProduto, object?>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["animalId"] = s => s.AnimalId,
            ["nomeAnimal"] = s => s.Animal.Nome,
            ["produtoId"] = s => s.ProdutoId,
            ["nomeProduto"] = s => s.Produto.Nome,
            ["justificativa"] = s => s.Justificativa,
            ["dataSugestao"] = s => s.DataSugestao,
            ["ativo"] = s => s.Ativo,
            ["criadoEm"] = s => s.CriadoEm,
        };

    public SugestaoProdutoRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PaginaDeResultados<SugestaoProduto>> GetAllAsync(
        ConsultaPaginada consulta,
        string? animalId,
        string? tutorId = null,
        bool? ativo = null)
    {
        var query = _context.SugestoesProduto
            .Include(s => s.Animal)
            .Include(s => s.Produto)
            .AsQueryable();

        // O recorte por dono SOMA com o filtro por animal, nunca e substituido
        // por ele -- senao `?animalId=<de outro tutor>` seria o proprio bypass.
        if (!string.IsNullOrWhiteSpace(tutorId))
            query = query.Where(s => s.Animal.TutorId == tutorId);

        if (!string.IsNullOrWhiteSpace(animalId))
            query = query.Where(s => s.AnimalId == animalId);

        // Mesma historia do produto: a sugestao tinha `Ativo` e ninguem o lia.
        // Uma indicacao retirada pela clinica continuava aparecendo para o tutor.
        if (ativo.HasValue)
            query = query.Where(s => s.Ativo == ativo.Value);

        var total = await query.CountAsync();

        var ordenada = Ordenacao.Aplicar(query, consulta, CamposOrdenaveis,
            q => q.OrderByDescending(s => s.DataSugestao), s => s.Id);

        var itens = await Paginacao.Aplicar(ordenada, consulta.Page, consulta.PageSize).ToListAsync();
        return new PaginaDeResultados<SugestaoProduto>(itens, total, consulta.Page, consulta.PageSize);
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
        // MySQL nao suporta RETURNING, entao o Id/CriadoEm sao gerados aqui em vez de
        // depender de DEFAULT do banco + leitura de volta pelo EF Core.
        sugestao.Id = Guid.NewGuid().ToString();
        sugestao.CriadoEm = DateTime.UtcNow;
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
