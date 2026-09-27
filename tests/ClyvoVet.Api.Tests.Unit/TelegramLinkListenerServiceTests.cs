using ClyvoVet.Application.Abstractions.Repositories;
using ClyvoVet.Application.Security;
using ClyvoVet.Infrastructure.Background;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Requests.Abstractions;
using Telegram.Bot.Types;

namespace ClyvoVet.Api.Tests.Unit;

/// <summary>
/// O ouvinte do bot: é ele que troca o convite pelo vínculo, e o bot é público. Qualquer pessoa
/// no Telegram manda o que quiser para ele, então o que importa aqui é o que NÃO vira vínculo.
/// </summary>
public class TelegramLinkListenerServiceTests
{
    private const long ChatId = 9090L;

    private readonly Mock<ITelegramBotClient> _bot = new();
    private readonly Mock<ITutorTelegramRepository> _vinculos = new();
    private readonly Mock<IAnimalRepository> _animais = new();
    private readonly RelogioDeTeste _relogio = new(new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero));
    private readonly VinculosPendentesDeTelegram _convites;
    private readonly List<string> _respostas = [];

    public TelegramLinkListenerServiceTests()
    {
        _convites = new VinculosPendentesDeTelegram(_relogio);

        // SendMessage é método de extensão; por baixo ele chama SendRequest(SendMessageRequest).
        _bot.Setup(b => b.SendRequest(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<Message>, CancellationToken>((pedido, _) => _respostas.Add(((SendMessageRequest)pedido).Text))
            .ReturnsAsync(new Message());
    }

    private TelegramLinkListenerService Ouvinte()
    {
        var servicos = new ServiceCollection();
        servicos.AddScoped(_ => _vinculos.Object);
        servicos.AddScoped(_ => _animais.Object);

        return new TelegramLinkListenerService(
            _bot.Object,
            servicos.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(),
            _convites,
            NullLogger<TelegramLinkListenerService>.Instance);
    }

    private static Update Mensagem(string texto) => new()
    {
        Id = 1,
        Message = new Message { Text = texto, Chat = new Chat { Id = ChatId } }
    };

    [Fact]
    public async Task Start_ConviteValido_VinculaOTutorDoConvite()
    {
        // Arrange
        var convite = _convites.Gerar("tutor-1");

        // Act
        await Ouvinte().ProcessarAsync(Mensagem($"/start {convite}"), CancellationToken.None);

        // Assert
        _vinculos.Verify(v => v.VincularAsync("tutor-1", ChatId), Times.Once);
        Assert.Contains("Vínculo confirmado", Assert.Single(_respostas));
    }

    /// <summary>O ataque de antes: o <c>tutorId</c> não é segredo, e já não serve de convite.</summary>
    [Fact]
    public async Task Start_ComOTutorIdEmVezDoConvite_NaoVincula()
    {
        // Act
        await Ouvinte().ProcessarAsync(Mensagem("/start tutor-1"), CancellationToken.None);

        // Assert
        _vinculos.Verify(v => v.VincularAsync(It.IsAny<string>(), It.IsAny<long>()), Times.Never);
        Assert.Contains("não vale mais", Assert.Single(_respostas));
    }

    [Fact]
    public async Task Start_ConviteJaUsado_NaoVinculaDeNovo()
    {
        // Arrange
        var convite = _convites.Gerar("tutor-1");
        var ouvinte = Ouvinte();
        await ouvinte.ProcessarAsync(Mensagem($"/start {convite}"), CancellationToken.None);

        // Act: o mesmo link de novo, por exemplo de quem viu o print da tela.
        await ouvinte.ProcessarAsync(Mensagem($"/start {convite}"), CancellationToken.None);

        // Assert
        _vinculos.Verify(v => v.VincularAsync(It.IsAny<string>(), It.IsAny<long>()), Times.Once);
        Assert.Contains("não vale mais", _respostas[^1]);
    }

    [Fact]
    public async Task Start_ConviteVencido_NaoVincula()
    {
        // Arrange
        var convite = _convites.Gerar("tutor-1");
        _relogio.Avancar(VinculosPendentesDeTelegram.Validade + TimeSpan.FromSeconds(1));

        // Act
        await Ouvinte().ProcessarAsync(Mensagem($"/start {convite}"), CancellationToken.None);

        // Assert
        _vinculos.Verify(v => v.VincularAsync(It.IsAny<string>(), It.IsAny<long>()), Times.Never);
    }

    [Fact]
    public async Task MeusAnimais_SemVinculo_NaoConsultaAnimais()
    {
        // Arrange
        _vinculos.Setup(v => v.GetTutorIdByChatIdAsync(ChatId)).ReturnsAsync((string?)null);

        // Act
        await Ouvinte().ProcessarAsync(Mensagem("/meusanimais"), CancellationToken.None);

        // Assert
        _animais.Verify(a => a.GetByTutorIdAsync(It.IsAny<string>()), Times.Never);
        Assert.Contains("não está vinculado", Assert.Single(_respostas));
    }

    [Fact]
    public async Task Desvincular_SemVinculo_NaoDesvinculaNinguem()
    {
        // Arrange
        _vinculos.Setup(v => v.GetTutorIdByChatIdAsync(ChatId)).ReturnsAsync((string?)null);

        // Act
        await Ouvinte().ProcessarAsync(Mensagem("/desvincular"), CancellationToken.None);

        // Assert
        _vinculos.Verify(v => v.DesvincularAsync(It.IsAny<string>()), Times.Never);
        Assert.Contains("não está vinculado", Assert.Single(_respostas));
    }
}
