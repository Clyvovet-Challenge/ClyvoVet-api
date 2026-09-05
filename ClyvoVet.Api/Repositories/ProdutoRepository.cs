using ClyvoVet.Api.Data;
using ClyvoVet.Api.Enums;
using ClyvoVet.Api.Models;
using ClyvoVet.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClyvoVet.Api.Repositories;

public class ProdutoRepository : IProdutoRepository
{
    private readonly AppDbContext _context;

    public ProdutoRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Produto>> GetAllAsync(int page, int pageSize, CategoriaEnum? categoria, EspecieEnum? especieIndicada)
    {
        var query = _context.Produtos.AsQueryable();

        if (categoria.HasValue)
            query = query.Where(p => p.Categoria == categoria.Value);

        if (especieIndicada.HasValue)
            query = query.Where(p => p.EspecieIndicada == especieIndicada.Value);

        return await query
            .OrderBy(p => p.Nome)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<Produto?> GetByIdAsync(string id)
    {
        return await _context.Produtos.FindAsync(id);
    }

    public async Task<Produto> CreateAsync(Produto produto)
    {
        // MySQL nao suporta RETURNING, entao o Id/CriadoEm sao gerados aqui em vez de
        // depender de DEFAULT do banco + leitura de volta pelo EF Core.
        produto.Id = Guid.NewGuid().ToString();
        produto.CriadoEm = DateTime.UtcNow;
        _context.Produtos.Add(produto);
        await _context.SaveChangesAsync();
        return produto;
    }

    public async Task<Produto?> UpdateAsync(string id, Produto produto)
    {
        var existing = await _context.Produtos.FindAsync(id);
        if (existing is null) return null;

        existing.Nome = produto.Nome;
        existing.Descricao = produto.Descricao;
        existing.Categoria = produto.Categoria;
        existing.Preco = produto.Preco;
        existing.EspecieIndicada = produto.EspecieIndicada;
        existing.Ativo = produto.Ativo;

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var produto = await _context.Produtos.FindAsync(id);
        if (produto is null) return false;

        _context.Produtos.Remove(produto);
        await _context.SaveChangesAsync();
        return true;
    }
}
