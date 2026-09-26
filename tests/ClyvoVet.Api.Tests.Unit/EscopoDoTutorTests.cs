using ClyvoVet.Application.Abstractions.Repositories;
using ClyvoVet.Application.Security;
using ClyvoVet.Domain.Entities;
using ClyvoVet.Domain.Exceptions;
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

    [Fact]
    public async Task ExigirAnimalDoTutorAsync_AnimalDeOutroTutor_Lanca404ComAMensagemDaRota()
    {
        // Arrange
        var animais = new Mock<IAnimalRepository>();
        animais.Setup(a => a.GetByIdAsync("animal-1"))
            .ReturnsAsync(new Animal { Id = "animal-1", TutorId = "tutor-2" });
        var escopo = Criar("true", new IdentidadeDoChamador("u1", "tutor-1", "TUTOR"), animais);

        // Act
        var excecao = await Assert.ThrowsAsync<NotFoundException>(
            () => escopo.ExigirAnimalDoTutorAsync("animal-1", "Lembrete l-1 nao encontrado."));

        // Assert
        Assert.Equal("Lembrete l-1 nao encontrado.", excecao.Message);
    }

    [Fact]
    public async Task ExigirAnimalDoTutorAsync_RecorteDesligado_NaoConsultaNemLanca()
    {
        // Arrange
        var animais = new Mock<IAnimalRepository>();
        var escopo = Criar(flag: null, identidade: null, animais);

        // Act
        await escopo.ExigirAnimalDoTutorAsync("animal-1", "nao deveria lancar");

        // Assert
        animais.Verify(a => a.GetByIdAsync(It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData(null, null, "tutor-1", true)]            // recorte desligado: a X-Api-Key é a barreira
    [InlineData("true", "tutor-1", "tutor-1", true)]     // o próprio tutor
    [InlineData("true", "tutor-1", "tutor-2", false)]    // outro tutor
    [InlineData("true", null, "tutor-1", false)]         // ADMIN/VETERINARIO: nulo nega
    public void PermiteTutor_ComparaOTutorDoTokenComODaRota(
        string? flag, string? tutorDoToken, string tutorDaRota, bool esperado)
    {
        // Arrange
        var escopo = Criar(flag, new IdentidadeDoChamador("u1", tutorDoToken, "TUTOR"));

        // Act
        var permitido = escopo.PermiteTutor(tutorDaRota);

        // Assert
        Assert.Equal(esperado, permitido);
    }
}
