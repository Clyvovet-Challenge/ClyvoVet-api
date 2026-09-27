using ClyvoVet.Application.Services.SaudePreditiva;
using ClyvoVet.Domain.Entities;

namespace ClyvoVet.Api.Tests.Unit;

public class PerfilDoAnimalTests
{
    [Theory]
    [InlineData("FEMEA", "ela")]
    [InlineData(" femea ", "ela")]
    [InlineData("MACHO", "ele")]
    [InlineData(null, "ele")]
    public void Pronome_PeloSexoCadastrado_DevolveElaOuEle(string? sexo, string esperado)
    {
        // Arrange
        var animal = new Animal { Id = "a1", Nome = "Luna", Sexo = sexo };

        // Act
        var pronome = PerfilDoAnimal.Pronome(animal);

        // Assert
        Assert.Equal(esperado, pronome);
    }

    [Fact]
    public void CalcularIdadeAnos_DepoisDasVinteUmEmBrasilia_ContaOsDiasAteOHojeDeBrasilia()
    {
        // Arrange: 00:30 UTC do dia 1º = 21:30 do dia 30 em Brasília. Nascido em 12/09, são
        // 18 dias em Brasília (0,049 ano → 0,0) e 19 em UTC (0,052 → 0,1).
        var relogio = new RelogioDeTeste(new DateTimeOffset(2026, 10, 1, 0, 30, 0, TimeSpan.Zero));

        // Act
        var idade = PerfilDoAnimal.CalcularIdadeAnos(new DateTime(2026, 9, 12), relogio);

        // Assert
        Assert.Equal(0.0m, idade);
    }
}
