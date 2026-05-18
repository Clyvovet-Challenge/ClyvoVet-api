using ClyvoVet.Api.DTOs.Request;
using ClyvoVet.Api.DTOs.Response;
using ClyvoVet.Api.Enums;
using ClyvoVet.Api.Exceptions;
using ClyvoVet.Api.Models;
using ClyvoVet.Api.Repositories.Interfaces;
using ClyvoVet.Api.Services.Interfaces;

namespace ClyvoVet.Api.Services;

public class ProdutoService : IProdutoService
{
    private readonly IProdutoRepository _repository;

    public ProdutoService(IProdutoRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<ProdutoResponse>> GetAllAsync(int page, int pageSize, CategoriaEnum? categoria, EspecieEnum? especieIndicada)
    {
        var produtos = await _repository.GetAllAsync(page, pageSize, categoria, especieIndicada);
        return produtos.Select(MapToResponse);
    }

    public async Task<ProdutoResponse> GetByIdAsync(string id)
    {
        var produto = await _repository.GetByIdAsync(id);
        if (produto is null)
            throw new NotFoundException($"Produto com id {id} não encontrado.");
        return MapToResponse(produto);
    }

    public async Task<ProdutoResponse> CreateAsync(ProdutoRequest request)
    {
        var produto = MapToEntity(request);
        var created = await _repository.CreateAsync(produto);
        return MapToResponse(created);
    }

    public async Task<ProdutoResponse> UpdateAsync(string id, ProdutoRequest request)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing is null)
            throw new NotFoundException($"Produto com id {id} não encontrado.");

        var produto = MapToEntity(request);
        var updated = await _repository.UpdateAsync(id, produto);
        return MapToResponse(updated!);
    }

    public async Task DeleteAsync(string id)
    {
        var deleted = await _repository.DeleteAsync(id);
        if (!deleted)
            throw new NotFoundException($"Produto com id {id} não encontrado.");
    }

    private static Produto MapToEntity(ProdutoRequest request) => new()
    {
        Nome = request.Nome,
        Descricao = request.Descricao,
        Categoria = request.Categoria,
        Preco = request.Preco,
        EspecieIndicada = request.EspecieIndicada,
        Ativo = request.Ativo
    };

    private static ProdutoResponse MapToResponse(Produto produto) => new()
    {
        Id = produto.Id,
        Nome = produto.Nome,
        Descricao = produto.Descricao,
        Categoria = produto.Categoria,
        Preco = produto.Preco,
        EspecieIndicada = produto.EspecieIndicada,
        Ativo = produto.Ativo,
        CriadoEm = produto.CriadoEm
    };
}
