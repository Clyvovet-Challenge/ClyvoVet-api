using ClyvoVet.Api.DTOs.Request;
using ClyvoVet.Api.DTOs.Response;
using ClyvoVet.Api.Exceptions;
using ClyvoVet.Api.Models;
using ClyvoVet.Api.Repositories.Interfaces;
using ClyvoVet.Api.Services.Interfaces;

namespace ClyvoVet.Api.Services;

public class SugestaoProdutoService : ISugestaoProdutoService
{
    private readonly ISugestaoProdutoRepository _repository;
    private readonly IAnimalRepository _animalRepository;
    private readonly IProdutoRepository _produtoRepository;

    public SugestaoProdutoService(ISugestaoProdutoRepository repository, IAnimalRepository animalRepository, IProdutoRepository produtoRepository)
    {
        _repository = repository;
        _animalRepository = animalRepository;
        _produtoRepository = produtoRepository;
    }

    public async Task<IEnumerable<SugestaoProdutoResponse>> GetAllAsync(int page, int pageSize, string? animalId)
    {
        var sugestoes = await _repository.GetAllAsync(page, pageSize, animalId);
        return sugestoes.Select(MapToResponse);
    }

    public async Task<SugestaoProdutoResponse> GetByIdAsync(string id)
    {
        var sugestao = await _repository.GetByIdAsync(id);
        if (sugestao is null)
            throw new NotFoundException($"Sugestão com id {id} não encontrada.");
        return MapToResponse(sugestao);
    }

    public async Task<SugestaoProdutoResponse> CreateAsync(SugestaoProdutoRequest request)
    {
        var animal = await _animalRepository.GetByIdAsync(request.AnimalId);
        if (animal is null)
            throw new NotFoundException($"Animal com id {request.AnimalId} não encontrado.");

        var produto = await _produtoRepository.GetByIdAsync(request.ProdutoId);
        if (produto is null)
            throw new NotFoundException($"Produto com id {request.ProdutoId} não encontrado.");

        var sugestao = new SugestaoProduto
        {
            AnimalId = request.AnimalId,
            ProdutoId = request.ProdutoId,
            Justificativa = request.Justificativa,
            DataSugestao = request.DataSugestao ?? DateOnly.FromDateTime(DateTime.Today),
            Ativo = request.Ativo
        };

        var created = await _repository.CreateAsync(sugestao);
        var full = await _repository.GetByIdAsync(created.Id);
        return MapToResponse(full!);
    }

    public async Task<SugestaoProdutoResponse> UpdateAsync(string id, SugestaoProdutoRequest request)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing is null)
            throw new NotFoundException($"Sugestão com id {id} não encontrada.");

        var animal = await _animalRepository.GetByIdAsync(request.AnimalId);
        if (animal is null)
            throw new NotFoundException($"Animal com id {request.AnimalId} não encontrado.");

        var produto = await _produtoRepository.GetByIdAsync(request.ProdutoId);
        if (produto is null)
            throw new NotFoundException($"Produto com id {request.ProdutoId} não encontrado.");

        var sugestao = new SugestaoProduto
        {
            AnimalId = request.AnimalId,
            ProdutoId = request.ProdutoId,
            Justificativa = request.Justificativa,
            DataSugestao = request.DataSugestao ?? DateOnly.FromDateTime(DateTime.Today),
            Ativo = request.Ativo
        };

        await _repository.UpdateAsync(id, sugestao);
        var full = await _repository.GetByIdAsync(id);
        return MapToResponse(full!);
    }

    public async Task DeleteAsync(string id)
    {
        var deleted = await _repository.DeleteAsync(id);
        if (!deleted)
            throw new NotFoundException($"Sugestão com id {id} não encontrada.");
    }

    private static SugestaoProdutoResponse MapToResponse(SugestaoProduto sugestao) => new()
    {
        Id = sugestao.Id,
        AnimalId = sugestao.AnimalId,
        NomeAnimal = sugestao.Animal?.Nome ?? string.Empty,
        ProdutoId = sugestao.ProdutoId,
        NomeProduto = sugestao.Produto?.Nome ?? string.Empty,
        Justificativa = sugestao.Justificativa,
        DataSugestao = sugestao.DataSugestao,
        Ativo = sugestao.Ativo,
        CriadoEm = sugestao.CriadoEm
    };
}
