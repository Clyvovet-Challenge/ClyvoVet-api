using ClyvoVet.Application.Abstractions.Repositories;
using ClyvoVet.Application.Security;
using ClyvoVet.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Moq;

namespace ClyvoVet.Api.Tests.Unit;

public class EscopoDoTutorTests
{
    private static EscopoDoTutor Criar(
        string? flag,
        IdentidadeDoChamador? identidade,
        Mock<IAnimalRepository>? animais = null)
    {
        var configuracao = new Mock<IConfiguration>();
        configuracao.Setup(c => c["Api:EscopoPorTutor"]).Returns(flag);

        var usuario = new Mock<IUsuarioAtual>();
        usuario.Setup(u => u.Identidade).Returns(identidade);

        return new EscopoDoTutor(
            configuracao.Object,
            usuario.Object,
            (animais ?? new Mock<IAnimalRepository>()).Object);
    }

    [Fact]
    public void FiltroDeListagem_RecorteDesligado_DevolveNullSemExigirToken()
    {
        // Arrange
        var escopo = Criar(flag: null, identidade: null);

        // Act & Assert
        Assert.Null(escopo.FiltroDeListagem());
    }

    [Fact]
    public void FiltroDeListagem_RecorteLigadoSemTutorNoToken_Lanca()
    {
        // Arrange — ADMIN não tem tutor: nulo NEGA, nunca "passa sem filtro".
        var escopo = Criar("true", new IdentidadeDoChamador("u1", null, "ADMIN"));

        // Act & Assert
        Assert.Throws<SemTutorNoTokenException>(() => escopo.FiltroDeListagem());
    }

    [Fact]
    public void FiltroDeListagem_RecorteLigadoComTutor_DevolveOTutorDoToken()
    {
        // Arrange
        var escopo = Criar("true", new IdentidadeDoChamador("u1", "tutor-1", "TUTOR"));

        // Act & Assert
        Assert.Equal("tutor-1", escopo.FiltroDeListagem());
    }

    [Fact]
    public async Task AnimalEDoTutorAsync_AnimalDeOutroTutor_DevolveFalse()
    {
        // Arrange — animal de outro tutor responde "não" (vira 404 no controller, nunca 403).
        var animais = new Mock<IAnimalRepository>();
        animais.Setup(a => a.GetByIdAsync("animal-1"))
            .ReturnsAsync(new Animal { Id = "animal-1", TutorId = "tutor-2" });
        var escopo = Criar("true", new IdentidadeDoChamador("u1", "tutor-1", "TUTOR"), animais);

        // Act
        var resultado = await escopo.AnimalEDoTutorAsync("animal-1");

        // Assert
        Assert.False(resultado);
    }
}
