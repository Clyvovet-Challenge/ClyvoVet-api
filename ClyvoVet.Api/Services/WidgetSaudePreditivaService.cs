using ClyvoVet.Api.DTOs.Response;
using ClyvoVet.Api.Enums;
using ClyvoVet.Api.Exceptions;
using ClyvoVet.Api.Repositories.Interfaces;
using ClyvoVet.Api.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace ClyvoVet.Api.Services;

/// <summary>
/// Cruza a espécie/raça/idade de um animal com o catálogo de predisposições de saúde
/// e monta o card do Widget de Saúde Preditiva, sugerindo agendar consulta quando
/// alguma condição relevante para a idade atual do animal for encontrada.
/// </summary>
public class WidgetSaudePreditivaService : IWidgetSaudePreditivaService
{
    private readonly IAnimalRepository _animalRepository;
    private readonly IPredisposicaoSaudeRepository _predisposicaoRepository;
    private readonly ILogger<WidgetSaudePreditivaService> _logger;

    public WidgetSaudePreditivaService(
        IAnimalRepository animalRepository,
        IPredisposicaoSaudeRepository predisposicaoRepository,
        ILogger<WidgetSaudePreditivaService> logger)
    {
        _animalRepository = animalRepository;
        _predisposicaoRepository = predisposicaoRepository;
        _logger = logger;
    }

    public async Task<WidgetSaudePreditivaResponse> GetPredisposicoesAsync(string animalId)
    {
        var animal = await _animalRepository.GetByIdAsync(animalId);
        if (animal is null)
            throw new NotFoundException($"Animal com id {animalId} não encontrado.");

        var idadeAnos = CalcularIdadeAnos(animal.DataNascimento);

        // Espécies fora do catálogo (ex.: "OUTRO", ou string inesperada vinda da API Java)
        // simplesmente não têm predisposição cadastrada — não é um erro.
        if (!Enum.TryParse<EspecieEnum>(animal.Especie, ignoreCase: true, out var especie))
        {
            _logger.LogWarning(
                "Widget de saude preditiva: especie '{Especie}' do animal {AnimalId} nao consta no catalogo de predisposicoes.",
                animal.Especie, animal.Id);
            return MontarResposta(animal.Id, animal.Nome, animal.Especie, animal.Raca, idadeAnos, []);
        }

        var candidatas = await _predisposicaoRepository.GetByEspecieAsync(especie);

        var predisposicoes = candidatas
            .Where(p => RacaCompativel(p.Raca, animal.Raca))
            .Where(p => IdadeCompativel(p.IdadeMinimaAnos, idadeAnos))
            .Select(p => new PredisposicaoItemResponse
            {
                Doenca = p.Doenca,
                Recomendacao = p.Recomendacao,
                IdadeMinimaAnos = p.IdadeMinimaAnos,
                FonteReferencia = p.FonteReferencia
            })
            .ToList();

        return MontarResposta(animal.Id, animal.Nome, animal.Especie, animal.Raca, idadeAnos, predisposicoes);
    }

    private static WidgetSaudePreditivaResponse MontarResposta(
        string animalId, string nome, string especie, string? raca, decimal? idadeAnos, List<PredisposicaoItemResponse> predisposicoes) => new()
    {
        AnimalId = animalId,
        NomeAnimal = nome,
        Especie = especie,
        Raca = raca,
        IdadeAnos = idadeAnos,
        SugerirAgendamentoConsulta = predisposicoes.Count > 0,
        Predisposicoes = predisposicoes
    };

    private static decimal? CalcularIdadeAnos(DateTime? dataNascimento)
    {
        if (dataNascimento is null)
            return null;

        var dias = (DateTime.UtcNow.Date - dataNascimento.Value.Date).TotalDays;
        return Math.Round((decimal)(dias / 365.25), 1);
    }

    // Compara de forma tolerante (ex.: "Labrador" casa com "Labrador Retriever" cadastrado
    // no Animal) — a raça do Animal vem de texto livre da API Java, sem padronização.
    private static bool RacaCompativel(string? racaPredisposicao, string? racaAnimal)
    {
        if (racaPredisposicao is null)
            return true;

        if (string.IsNullOrWhiteSpace(racaAnimal))
            return false;

        var a = racaPredisposicao.Trim().ToLowerInvariant();
        var b = racaAnimal.Trim().ToLowerInvariant();
        return a.Contains(b) || b.Contains(a);
    }

    private static bool IdadeCompativel(decimal? idadeMinima, decimal? idadeAnimal)
    {
        if (idadeMinima is null or 0)
            return true;

        if (idadeAnimal is null)
            return false;

        return idadeAnimal >= idadeMinima;
    }
}
