using ClyvoVet.Api.Enums;
using ClyvoVet.Api.Exceptions;
using ClyvoVet.Api.Models;
using ClyvoVet.Api.Repositories.Interfaces;
using ClyvoVet.Api.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace ClyvoVet.Api.Tests.Unit;

public class WidgetSaudePreditivaServiceTests
{
    private readonly Mock<IAnimalRepository> _animalRepositoryMock = new();
    private readonly Mock<IPredisposicaoSaudeRepository> _predisposicaoRepositoryMock = new();
    private readonly Mock<ILogger<WidgetSaudePreditivaService>> _loggerMock = new();
    private readonly WidgetSaudePreditivaService _service;

    public WidgetSaudePreditivaServiceTests()
    {
        _service = new WidgetSaudePreditivaService(_animalRepositoryMock.Object, _predisposicaoRepositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetPredisposicoesAsync_Labrador7Anos_SugereConsultaComDisplasiaEObesidade()
    {
        // Arrange
        var animal = new Animal
        {
            Id = "animal-1",
            Nome = "Rex",
            Especie = "CACHORRO",
            Raca = "Labrador Retriever", // nome "cheio", diferente do cadastrado ("Labrador")
            DataNascimento = DateTime.UtcNow.AddYears(-7)
        };
        _animalRepositoryMock.Setup(r => r.GetByIdAsync("animal-1")).ReturnsAsync(animal);

        var predisposicoes = new List<PredisposicaoSaude>
        {
            new() { Especie = EspecieEnum.Cachorro, Raca = "Labrador", IdadeMinimaAnos = 6, Doenca = "Displasia de quadril", Recomendacao = "X" },
            new() { Especie = EspecieEnum.Cachorro, Raca = "Labrador", IdadeMinimaAnos = 7, Doenca = "Obesidade", Recomendacao = "Y" },
            new() { Especie = EspecieEnum.Cachorro, Raca = "Dachshund", IdadeMinimaAnos = 3, Doenca = "Hernia de disco", Recomendacao = "Z" },
        };
        _predisposicaoRepositoryMock.Setup(r => r.GetByEspecieAsync(EspecieEnum.Cachorro)).ReturnsAsync(predisposicoes);

        // Act
        var result = await _service.GetPredisposicoesAsync("animal-1");

        // Assert
        Assert.True(result.SugerirAgendamentoConsulta);
        Assert.Equal(2, result.Predisposicoes.Count);
        Assert.Equal(7.0m, result.IdadeAnos);
        Assert.Contains(result.Predisposicoes, p => p.Doenca == "Displasia de quadril");
        Assert.Contains(result.Predisposicoes, p => p.Doenca == "Obesidade");
        Assert.DoesNotContain(result.Predisposicoes, p => p.Doenca == "Hernia de disco");
    }

    [Fact]
    public async Task GetPredisposicoesAsync_Labrador2Anos_NaoSugereConsulta_IdadeAbaixoDoMinimo()
    {
        // Arrange
        var animal = new Animal { Id = "animal-2", Nome = "Bolt", Especie = "CACHORRO", Raca = "Labrador", DataNascimento = DateTime.UtcNow.AddYears(-2) };
        _animalRepositoryMock.Setup(r => r.GetByIdAsync("animal-2")).ReturnsAsync(animal);

        var predisposicoes = new List<PredisposicaoSaude>
        {
            new() { Especie = EspecieEnum.Cachorro, Raca = "Labrador", IdadeMinimaAnos = 6, Doenca = "Displasia de quadril", Recomendacao = "X" },
        };
        _predisposicaoRepositoryMock.Setup(r => r.GetByEspecieAsync(EspecieEnum.Cachorro)).ReturnsAsync(predisposicoes);

        // Act
        var result = await _service.GetPredisposicoesAsync("animal-2");

        // Assert
        Assert.False(result.SugerirAgendamentoConsulta);
        Assert.Empty(result.Predisposicoes);
    }

    [Fact]
    public async Task GetPredisposicoesAsync_CoelhoSemDataNascimento_AindaAssimPegaCondicaoDeIdadeMinimaZero()
    {
        // Arrange
        var animal = new Animal { Id = "animal-3", Nome = "Thumper", Especie = "ROEDOR", Raca = "Coelho", DataNascimento = null };
        _animalRepositoryMock.Setup(r => r.GetByIdAsync("animal-3")).ReturnsAsync(animal);

        var predisposicoes = new List<PredisposicaoSaude>
        {
            new() { Especie = EspecieEnum.Roedor, Raca = "Coelho", IdadeMinimaAnos = 0, Doenca = "Estase gastrointestinal", Recomendacao = "X" },
            new() { Especie = EspecieEnum.Roedor, Raca = "Hamster", IdadeMinimaAnos = 1.5m, Doenca = "Tumor adrenal", Recomendacao = "Y" },
        };
        _predisposicaoRepositoryMock.Setup(r => r.GetByEspecieAsync(EspecieEnum.Roedor)).ReturnsAsync(predisposicoes);

        // Act
        var result = await _service.GetPredisposicoesAsync("animal-3");

        // Assert
        Assert.True(result.SugerirAgendamentoConsulta);
        Assert.Single(result.Predisposicoes);
        Assert.Null(result.IdadeAnos);
    }

    [Fact]
    public async Task GetPredisposicoesAsync_EspecieForaDoCatalogo_RetornaSemPredisposicoes()
    {
        // Arrange
        var animal = new Animal { Id = "animal-4", Nome = "Nemo", Especie = "PEIXE", DataNascimento = DateTime.UtcNow.AddYears(-1) };
        _animalRepositoryMock.Setup(r => r.GetByIdAsync("animal-4")).ReturnsAsync(animal);

        // Act
        var result = await _service.GetPredisposicoesAsync("animal-4");

        // Assert
        Assert.False(result.SugerirAgendamentoConsulta);
        Assert.Empty(result.Predisposicoes);
        _predisposicaoRepositoryMock.Verify(r => r.GetByEspecieAsync(It.IsAny<EspecieEnum>()), Times.Never);
    }

    [Fact]
    public async Task GetPredisposicoesAsync_AnimalIdInexistente_LancaNotFoundException()
    {
        // Arrange
        _animalRepositoryMock.Setup(r => r.GetByIdAsync("id-invalido")).ReturnsAsync((Animal?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetPredisposicoesAsync("id-invalido"));
    }

    [Fact]
    public async Task GetPredisposicoesAsync_AnimalSemRacaCadastrada_NaoCasaComPredisposicaoDeRacaEspecifica()
    {
        // Arrange
        var animal = new Animal { Id = "animal-5", Nome = "SemRaca", Especie = "CACHORRO", Raca = null, DataNascimento = DateTime.UtcNow.AddYears(-8) };
        _animalRepositoryMock.Setup(r => r.GetByIdAsync("animal-5")).ReturnsAsync(animal);

        var predisposicoes = new List<PredisposicaoSaude>
        {
            new() { Especie = EspecieEnum.Cachorro, Raca = "Labrador", IdadeMinimaAnos = 6, Doenca = "Displasia de quadril", Recomendacao = "X" },
        };
        _predisposicaoRepositoryMock.Setup(r => r.GetByEspecieAsync(EspecieEnum.Cachorro)).ReturnsAsync(predisposicoes);

        // Act
        var result = await _service.GetPredisposicoesAsync("animal-5");

        // Assert
        Assert.False(result.SugerirAgendamentoConsulta);
        Assert.Empty(result.Predisposicoes);
    }

    [Fact]
    public async Task GetPredisposicoesAsync_IdadeExatamenteIgualAoMinimo_ConsideraCompativel()
    {
        // Arrange
        var animal = new Animal { Id = "animal-6", Nome = "Fronteira", Especie = "CACHORRO", Raca = "Labrador", DataNascimento = DateTime.UtcNow.AddYears(-6) };
        _animalRepositoryMock.Setup(r => r.GetByIdAsync("animal-6")).ReturnsAsync(animal);

        var predisposicoes = new List<PredisposicaoSaude>
        {
            new() { Especie = EspecieEnum.Cachorro, Raca = "Labrador", IdadeMinimaAnos = 6, Doenca = "Displasia de quadril", Recomendacao = "X" },
        };
        _predisposicaoRepositoryMock.Setup(r => r.GetByEspecieAsync(EspecieEnum.Cachorro)).ReturnsAsync(predisposicoes);

        // Act
        var result = await _service.GetPredisposicoesAsync("animal-6");

        // Assert
        Assert.True(result.SugerirAgendamentoConsulta);
        Assert.Single(result.Predisposicoes);
    }
}
