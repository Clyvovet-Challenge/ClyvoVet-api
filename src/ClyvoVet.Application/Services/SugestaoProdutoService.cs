using ClyvoVet.Application.Common;
using ClyvoVet.Application.DTOs.Request;
using ClyvoVet.Application.DTOs.Response;
using ClyvoVet.Domain.Exceptions;
using ClyvoVet.Domain.Entities;
using ClyvoVet.Application.Abstractions.Repositories;
using ClyvoVet.Application.Services.Interfaces;

namespace ClyvoVet.Application.Services;

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

    public async Task<PaginaDeResultados<SugestaoProdutoResponse>> GetAllAsync(ConsultaPaginada consulta, string? animalId, string? tutorId = null, bool? ativo = null)
    {
        var pagina = await _repository.GetAllAsync(consulta, animalId, tutorId, ativo);
        return pagina.Mapear(MapToResponse);
    }

    public async Task<SugestaoProdutoResponse> GetByIdAsync(string id)
    {
        var sugestao = await _repository.GetByIdAsync(id);
        if (sugestao is null)
            throw new NotFoundException($"Sugestão com id {id} não encontrada.");
        return MapToResponse(sugestao);
    }

    /// <summary>Criar e atualizar aceitam o mesmo corpo: animal e produto precisam existir.</summary>
    private async Task ValidarReferenciasAsync(SugestaoProdutoRequest request)
    {
        var animal = await _animalRepository.GetByIdAsync(request.AnimalId);
        if (animal is null)
            throw new NotFoundException($"Animal com id {request.AnimalId} não encontrado.");

        var produto = await _produtoRepository.GetByIdAsync(request.ProdutoId);
        if (produto is null)
            throw new NotFoundException($"Produto com id {request.ProdutoId} não encontrado.");
    }

    /// <param name="dataPadrao">Usada quando o corpo não traz a data: hoje ao criar, a data já gravada ao atualizar.</param>
    private static SugestaoProduto Montar(SugestaoProdutoRequest request, DateOnly dataPadrao) => new()
    {
        AnimalId = request.AnimalId,
        ProdutoId = request.ProdutoId,
        Justificativa = request.Justificativa,
        DataSugestao = request.DataSugestao ?? dataPadrao,
        Ativo = request.Ativo
    };

    public async Task<SugestaoProdutoResponse> CreateAsync(SugestaoProdutoRequest request)
    {
        await ValidarReferenciasAsync(request);

        // O mesmo relogio de quem VALIDA datas (DataValidationHelper): o de
        // Brasilia. Com relogios diferentes, os dois discordavam sobre que dia e
        // hoje, e uma sugestao criada perto da meia-noite nascia com outra data.
        var hoje = HorarioDeBrasilia.Hoje();
        var created = await _repository.CreateAsync(Montar(request, hoje));
        var full = await _repository.GetByIdAsync(created.Id);
        return MapToResponse(full!);
    }

    public async Task<SugestaoProdutoResponse> UpdateAsync(string id, SugestaoProdutoRequest request)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing is null)
            throw new NotFoundException($"Sugestão com id {id} não encontrada.");

        await ValidarReferenciasAsync(request);

        // Um PUT sem a data não é um pedido para "trocar pela de hoje".
        await _repository.UpdateAsync(id, Montar(request, existing.DataSugestao));
        // Outra requisição pode apagá-la entre a leitura e a releitura.
        var full = await _repository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Sugestão com id {id} não encontrada.");
        return MapToResponse(full);
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
