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

    public async Task<IEnumerable<Produto>> GetAllAsync(
        int page,
        int pageSize,
        CategoriaEnum? categoria,
        EspecieEnum? especieIndicada,
        bool? ativo = null,
        PorteEnum? porteIndicado = null)
    {
        var query = _context.Produtos.AsQueryable();

        if (categoria.HasValue)
            query = query.Where(p => p.Categoria == categoria.Value);

        // `Todos` entra junto com a especie pedida, e nao e detalhe: e a unica
        // forma de um produto universal aparecer numa lista recortada por
        // animal. Com igualdade exata, perguntar "o que serve para um cachorro"
        // escondia justamente o que serve para todos -- consulta de rotina,
        // servico de banho, o que nao e especifico de especie nenhuma.
        if (especieIndicada.HasValue && especieIndicada.Value != EspecieEnum.Todos)
            query = query.Where(p => p.EspecieIndicada == especieIndicada.Value
                                  || p.EspecieIndicada == EspecieEnum.Todos);
        else if (especieIndicada.HasValue)
            query = query.Where(p => p.EspecieIndicada == EspecieEnum.Todos);

        // O `Ativo` existia no modelo, no banco e no update, e nenhuma consulta
        // o lia -- desativar um produto nao o tirava de lugar nenhum. Opcional
        // para nao mudar quem ja chamava sem ele; a vitrine do tutor pede true.
        if (ativo.HasValue)
            query = query.Where(p => p.Ativo == ativo.Value);

        // Mesma regra da especie, e pelo mesmo motivo: um porte especifico traz
        // junto o que serve a qualquer porte. Sem isso, filtrar por PEQUENO
        // esconderia a consulta de rotina, o shampoo neutro e o vermifugo em
        // gotas -- que sao a maior parte do catalogo e servem a todo mundo.
        //
        // Racao de cachorro pequeno e de cachorro grande sao produtos
        // diferentes; consulta veterinaria nao tem porte. O `Todos` e o que
        // permite as duas coisas conviverem na mesma lista.
        if (porteIndicado.HasValue && porteIndicado.Value != PorteEnum.Todos)
            query = query.Where(p => p.PorteIndicado == porteIndicado.Value
                                  || p.PorteIndicado == PorteEnum.Todos);
        else if (porteIndicado.HasValue)
            query = query.Where(p => p.PorteIndicado == PorteEnum.Todos);

        var consulta = query
            .OrderBy(p => p.Nome);

        return await Paginacao.Aplicar(consulta, page, pageSize).ToListAsync();
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
