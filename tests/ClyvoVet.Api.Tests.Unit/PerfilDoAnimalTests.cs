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
}
