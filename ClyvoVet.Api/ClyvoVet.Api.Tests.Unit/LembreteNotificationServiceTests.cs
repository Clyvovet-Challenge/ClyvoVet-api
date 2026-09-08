using ClyvoVet.Api.Enums;
using ClyvoVet.Api.Models;
using ClyvoVet.Api.Repositories.Interfaces;
using ClyvoVet.Api.Services;
using ClyvoVet.Api.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ClyvoVet.Api.Tests.Unit;

/// <summary>
/// O laço que varre os lembretes vencendo.
///
/// <para>
/// Os envios sempre tiveram <c>try/catch</c>, mas o resto do corpo do laço não: a
/// leitura do vínculo do Telegram, a montagem da mensagem e a gravação do status
/// corriam soltos. Qualquer exceção ali abortava o <c>foreach</c> inteiro, e como
/// nenhum lembrete daquele ciclo chegava a virar Enviado, o registro problemático
/// reaparecia no ciclo seguinte e derrubava tudo de novo — um único registro ruim
/// parava a notificação da plataforma toda.
/// </para>
/// </summary>
public class LembreteNotificationServiceTests
{
    private readonly Mock<ILembreteRepository> _lembretes = new();
    private readonly Mock<ITutorTelegramRepository> _telegramRepo = new();
    private readonly Mock<IWhatsAppService> _whatsApp = new();
    private readonly Mock<ITelegramService> _telegram = new();

    private static Lembrete Lembrete(string id, string tutorId, string? telefone) => new()
    {
        Id = id,
        AnimalId = "animal-" + id,
        Titulo = "Vacina",
        AgendadoEm = DateTime.UtcNow.AddMinutes(30),
        Status = StatusLembreteEnum.Pendente,
        Animal = new Animal
        {
            Id = "animal-" + id,
            Nome = "Pet " + id,
            TutorId = tutorId,
            Tutor = new Tutor { Id = tutorId, Nome = "Tutor " + tutorId, Telefone = telefone }
        }
    };

    private LembreteNotificationService Servico()
    {
        var servicos = new ServiceCollection();
        servicos.AddScoped(_ => _lembretes.Object);
        servicos.AddScoped(_ => _telegramRepo.Object);
        servicos.AddScoped(_ => _whatsApp.Object);
        servicos.AddScoped(_ => _telegram.Object);

        return new LembreteNotificationService(
            servicos.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(),
            NullLogger<LembreteNotificationService>.Instance);
    }

    /// <summary>O defeito: o primeiro lembrete explodia e o segundo nunca era visto.</summary>
    [Fact]
    public async Task UmLembreteQueExplode_NaoImpedeOsSeguintes()
    {
        var ruim = Lembrete("ruim", "tutor-1", "11999990000");
        var bom = Lembrete("bom", "tutor-2", "11988880000");

        _lembretes.Setup(r => r.GetPendentesVencendoAsync(It.IsAny<DateTime>()))
            .ReturnsAsync([ruim, bom]);

        // Fora de qualquer try/catch que já existisse: é a leitura do vínculo.
        _telegramRepo.Setup(r => r.GetChatIdByTutorIdAsync("tutor-1"))
            .ThrowsAsync(new InvalidOperationException("banco indisponivel"));
        _telegramRepo.Setup(r => r.GetChatIdByTutorIdAsync("tutor-2"))
            .ReturnsAsync((long?)null);

        await Servico().VerificarLembretesAsync(CancellationToken.None);

        _whatsApp.Verify(w => w.EnviarMensagemAsync("11988880000", It.IsAny<string>()), Times.Once);
        _lembretes.Verify(r => r.UpdateAsync("bom", It.IsAny<Lembrete>()), Times.Once);
    }

    [Fact]
    public async Task LembreteNotificado_ViraEnviado()
    {
        var lembrete = Lembrete("um", "tutor-1", "11999990000");
        _lembretes.Setup(r => r.GetPendentesVencendoAsync(It.IsAny<DateTime>()))
            .ReturnsAsync([lembrete]);
        _telegramRepo.Setup(r => r.GetChatIdByTutorIdAsync(It.IsAny<string>()))
            .ReturnsAsync((long?)null);

        await Servico().VerificarLembretesAsync(CancellationToken.None);

        Assert.Equal(StatusLembreteEnum.Enviado, lembrete.Status);
    }

    /// <summary>
    /// Telegram vinculado ganha do WhatsApp, e o WhatsApp não é tentado depois —
    /// senão o tutor receberia a mesma mensagem por dois canais.
    /// </summary>
    [Fact]
    public async Task ComTelegramVinculado_NaoUsaOWhatsApp()
    {
        var lembrete = Lembrete("um", "tutor-1", "11999990000");
        _lembretes.Setup(r => r.GetPendentesVencendoAsync(It.IsAny<DateTime>()))
            .ReturnsAsync([lembrete]);
        _telegramRepo.Setup(r => r.GetChatIdByTutorIdAsync("tutor-1")).ReturnsAsync(4242L);

        await Servico().VerificarLembretesAsync(CancellationToken.None);

        _telegram.Verify(t => t.EnviarMensagemAsync(4242L, It.IsAny<string>()), Times.Once);
        _whatsApp.Verify(w => w.EnviarMensagemAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    /// <summary>Telegram falhando, o WhatsApp assume — é para isso que a alternativa existe.</summary>
    [Fact]
    public async Task TelegramFalhando_CaiNoWhatsApp()
    {
        var lembrete = Lembrete("um", "tutor-1", "11999990000");
        _lembretes.Setup(r => r.GetPendentesVencendoAsync(It.IsAny<DateTime>()))
            .ReturnsAsync([lembrete]);
        _telegramRepo.Setup(r => r.GetChatIdByTutorIdAsync("tutor-1")).ReturnsAsync(4242L);
        _telegram.Setup(t => t.EnviarMensagemAsync(It.IsAny<long>(), It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException("telegram fora do ar"));

        await Servico().VerificarLembretesAsync(CancellationToken.None);

        _whatsApp.Verify(w => w.EnviarMensagemAsync("11999990000", It.IsAny<string>()), Times.Once);
        Assert.Equal(StatusLembreteEnum.Enviado, lembrete.Status);
    }

    /// <summary>
    /// Sem canal nenhum o lembrete continua Pendente. Marcá-lo como Enviado seria
    /// mentir: ninguém foi avisado.
    /// </summary>
    [Fact]
    public async Task SemCanalNenhum_ContinuaPendente()
    {
        var lembrete = Lembrete("um", "tutor-1", telefone: null);
        _lembretes.Setup(r => r.GetPendentesVencendoAsync(It.IsAny<DateTime>()))
            .ReturnsAsync([lembrete]);
        _telegramRepo.Setup(r => r.GetChatIdByTutorIdAsync("tutor-1")).ReturnsAsync((long?)null);

        await Servico().VerificarLembretesAsync(CancellationToken.None);

        Assert.Equal(StatusLembreteEnum.Pendente, lembrete.Status);
        _lembretes.Verify(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<Lembrete>()), Times.Never);
    }
}
