using ClyvoVet.Application.Abstractions.Repositories;
using ClyvoVet.Application.Security;
using ClyvoVet.Application.Services;
using ClyvoVet.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Moq;

namespace ClyvoVet.Api.Tests.Unit;

public class VinculoTelegramServiceTests
{
    private const string Tutor = "tutor-1";

    private readonly Mock<ITutorTelegramRepository> _vinculos = new();
    private readonly VinculosPendentesDeTelegram _convites = new();

    private VinculoTelegramService Criar()
    {
        var configuracao = new Mock<IConfiguration>();
        configuracao.Setup(c => c["Telegram:BotUsername"]).Returns("clyvo_bot");
        return new VinculoTelegramService(configuracao.Object, _convites, _vinculos.Object);
    }

    [Fact]
    public void GerarLink_Tutor_MontaUrlDoBotComConviteQueApontaParaOTutor()
    {
        // Arrange
        var service = Criar();

        // Act
        var resposta = service.GerarLink(Tutor);

        // Assert — o que vai no start= é o convite, e consumi-lo devolve o tutor.
        const string prefixo = "https://t.me/clyvo_bot?start=";
        Assert.StartsWith(prefixo, resposta.Link);
        Assert.Equal(Tutor, _convites.Consumir(resposta.Link[prefixo.Length..]));
    }

    [Fact]
    public async Task ObterVinculoAsync_ComVinculo_DevolveVinculadoEDesde()
    {
        // Arrange
        var criadoEm = new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);
        _vinculos.Setup(r => r.ObterVinculoAsync(Tutor))
            .ReturnsAsync(new TutorTelegram { TutorId = Tutor, ChatId = 42, CriadoEm = criadoEm });

        // Act
        var resposta = await Criar().ObterVinculoAsync(Tutor);

        // Assert
        Assert.True(resposta.Vinculado);
        Assert.Equal(criadoEm, resposta.Desde);
    }

    [Fact]
    public async Task ObterVinculoAsync_SemVinculo_DevolveNaoVinculadoSemData()
    {
        // Arrange
        _vinculos.Setup(r => r.ObterVinculoAsync(Tutor)).ReturnsAsync((TutorTelegram?)null);

        // Act
        var resposta = await Criar().ObterVinculoAsync(Tutor);

        // Assert
        Assert.False(resposta.Vinculado);
        Assert.Null(resposta.Desde);
    }

    [Fact]
    public async Task DesvincularAsync_Tutor_RemoveOVinculoNoRepositorio()
    {
        // Act
        await Criar().DesvincularAsync(Tutor);

        // Assert
        _vinculos.Verify(r => r.DesvincularAsync(Tutor), Times.Once);
    }
}
