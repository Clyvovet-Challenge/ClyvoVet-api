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
    private readonly Mock<ILembreteRepository> _lembretes = new();
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
        servicos.AddScoped(_ => _lembretes.Object);

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

    // ─────────────────────────────────────────────────────────────────────────
    // Caracterizacao: o texto exato que cada comando responde hoje. Escritos
    // antes de mover os comandos para a camada de aplicacao, para que a mudanca
    // de lugar nao mude uma virgula do que o tutor le.
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task MeusAnimais_ComPets_ListaNomeEspecieERaca()
    {
        _vinculos.Setup(v => v.GetTutorIdByChatIdAsync(ChatId)).ReturnsAsync("tutor-1");
        _animais.Setup(a => a.GetByTutorIdAsync("tutor-1")).ReturnsAsync(
        [
            new Domain.Entities.Animal { Id = "a1", Nome = "Bolinha", Especie = "CAO", Raca = "Poodle", TutorId = "tutor-1" },
            new Domain.Entities.Animal { Id = "a2", Nome = "Mimi", Especie = "GATO", Raca = null, TutorId = "tutor-1" },
        ]);

        await Ouvinte().ProcessarAsync(Mensagem("/meusanimais"), CancellationToken.None);

        Assert.Equal("🐾 Seus pets:\n\n• Bolinha — CAO (Poodle)\n• Mimi — GATO", Assert.Single(_respostas));
    }

    [Fact]
    public async Task MeusAnimais_SemPets_AvisaQueNaoHaNenhum()
    {
        _vinculos.Setup(v => v.GetTutorIdByChatIdAsync(ChatId)).ReturnsAsync("tutor-1");
        _animais.Setup(a => a.GetByTutorIdAsync("tutor-1")).ReturnsAsync([]);

        await Ouvinte().ProcessarAsync(Mensagem("/meusanimais"), CancellationToken.None);

        Assert.Equal("Você ainda não tem nenhum pet cadastrado.", Assert.Single(_respostas));
    }

    [Fact]
    public async Task MeusLembretes_SemVinculo_NaoConsultaLembretes()
    {
        _vinculos.Setup(v => v.GetTutorIdByChatIdAsync(ChatId)).ReturnsAsync((string?)null);

        await Ouvinte().ProcessarAsync(Mensagem("/meuslembretes"), CancellationToken.None);

        _lembretes.Verify(l => l.GetPendentesByTutorIdAsync(It.IsAny<string>()), Times.Never);
        Assert.Equal("Você ainda não está vinculado a nenhum tutor. Peça o link de vínculo no app da ClyvoVet.",
            Assert.Single(_respostas));
    }

    [Fact]
    public async Task MeusLembretes_ComPendentes_ListaTituloPetEHorario()
    {
        _vinculos.Setup(v => v.GetTutorIdByChatIdAsync(ChatId)).ReturnsAsync("tutor-1");
        var bolinha = new Domain.Entities.Animal { Id = "a1", Nome = "Bolinha", TutorId = "tutor-1" };
        _lembretes.Setup(l => l.GetPendentesByTutorIdAsync("tutor-1")).ReturnsAsync(
        [
            new Domain.Entities.Lembrete
            {
                Id = "l1", AnimalId = "a1", Animal = bolinha, Titulo = "Vermifugo",
                AgendadoEm = new DateTime(2026, 11, 10, 9, 30, 0),
            },
        ]);

        await Ouvinte().ProcessarAsync(Mensagem("/meuslembretes"), CancellationToken.None);

        Assert.Equal("📋 Seus lembretes pendentes:\n\n• Vermifugo (Bolinha) — 10/11/2026 09:30", Assert.Single(_respostas));
    }

    [Fact]
    public async Task MeusLembretes_SemPendentes_Comemora()
    {
        _vinculos.Setup(v => v.GetTutorIdByChatIdAsync(ChatId)).ReturnsAsync("tutor-1");
        _lembretes.Setup(l => l.GetPendentesByTutorIdAsync("tutor-1")).ReturnsAsync([]);

        await Ouvinte().ProcessarAsync(Mensagem("/meuslembretes"), CancellationToken.None);

        Assert.Equal("Você não tem lembretes pendentes no momento. 🎉", Assert.Single(_respostas));
    }

    [Fact]
    public async Task Desvincular_Vinculado_RemoveOVinculoDoTutor()
    {
        _vinculos.Setup(v => v.GetTutorIdByChatIdAsync(ChatId)).ReturnsAsync("tutor-1");

        await Ouvinte().ProcessarAsync(Mensagem("/desvincular"), CancellationToken.None);

        _vinculos.Verify(v => v.DesvincularAsync("tutor-1"), Times.Once);
        Assert.Equal("✅ Vínculo removido. Você não vai mais receber notificações da ClyvoVet por aqui.",
            Assert.Single(_respostas));
    }

    [Fact]
    public async Task Desvincular_SemVinculo_RespondeOTextoDeHoje()
    {
        _vinculos.Setup(v => v.GetTutorIdByChatIdAsync(ChatId)).ReturnsAsync((string?)null);

        await Ouvinte().ProcessarAsync(Mensagem("/desvincular"), CancellationToken.None);

        Assert.Equal("Você não está vinculado a nenhum tutor no momento.", Assert.Single(_respostas));
    }
}
