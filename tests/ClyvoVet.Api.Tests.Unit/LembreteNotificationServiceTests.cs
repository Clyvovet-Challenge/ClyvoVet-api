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
            .ReturnsAsync(2424L);

        await Servico().VerificarLembretesAsync(CancellationToken.None);

        _telegram.Verify(t => t.EnviarMensagemAsync(2424L, It.IsAny<string>()), Times.Once);
        _lembretes.Verify(r => r.UpdateAsync("bom", It.IsAny<Lembrete>()), Times.Once);
    }

    [Fact]
    public async Task LembreteNotificado_ViraEnviado()
    {
        var lembrete = Lembrete("um", "tutor-1", "11999990000");
        _lembretes.Setup(r => r.GetPendentesVencendoAsync(It.IsAny<DateTime>()))
            .ReturnsAsync([lembrete]);
        _telegramRepo.Setup(r => r.GetChatIdByTutorIdAsync(It.IsAny<string>()))
            .ReturnsAsync(4242L);

        await Servico().VerificarLembretesAsync(CancellationToken.None);

        Assert.Equal(StatusLembreteEnum.Enviado, lembrete.Status);
    }

    /// <summary>
    /// Telegram falhando, o lembrete fica Pendente e volta no próximo ciclo —
    /// não há mais segundo canal desde que o WhatsApp saiu do escopo.
    /// </summary>
    [Fact]
    public async Task TelegramFalhando_LembreteContinuaPendente()
    {
        var lembrete = Lembrete("um", "tutor-1", "11999990000");
        _lembretes.Setup(r => r.GetPendentesVencendoAsync(It.IsAny<DateTime>()))
            .ReturnsAsync([lembrete]);
        _telegramRepo.Setup(r => r.GetChatIdByTutorIdAsync("tutor-1")).ReturnsAsync(4242L);
        _telegram.Setup(t => t.EnviarMensagemAsync(It.IsAny<long>(), It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException("telegram fora do ar"));

        await Servico().VerificarLembretesAsync(CancellationToken.None);

        Assert.Equal(StatusLembreteEnum.Pendente, lembrete.Status);
        _lembretes.Verify(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<Lembrete>()), Times.Never);
    }

    /// <summary>
    /// Sem Telegram vinculado o lembrete continua Pendente — telefone cadastrado
    /// não é mais canal. Marcá-lo como Enviado seria mentir: ninguém foi avisado.
    /// </summary>
    [Fact]
    public async Task SemTelegramVinculado_ContinuaPendente()
    {
        var lembrete = Lembrete("um", "tutor-1", telefone: "11999990000");
        _lembretes.Setup(r => r.GetPendentesVencendoAsync(It.IsAny<DateTime>()))
            .ReturnsAsync([lembrete]);
        _telegramRepo.Setup(r => r.GetChatIdByTutorIdAsync("tutor-1")).ReturnsAsync((long?)null);

        await Servico().VerificarLembretesAsync(CancellationToken.None);

        Assert.Equal(StatusLembreteEnum.Pendente, lembrete.Status);
        _lembretes.Verify(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<Lembrete>()), Times.Never);
    }

    // ================================================================
    // A serie (V18): "a cada X dias", com fim opcional
    // ================================================================

    /// <summary>
    /// O defeito que a V18 consertou: <c>Recorrente</c> existia em oito lugares
    /// da API e nenhum decidia nada. Um lembrete "recorrente" disparava uma vez
    /// e virava Enviado — a interface prometia repeticao que nunca acontecia.
    /// </summary>
    [Fact]
    public async Task LembreteComIntervalo_NaoViraEnviado_EAndaParaAProximaData()
    {
        var lembrete = Lembrete("serie", "tutor-1", "11999990000");
        lembrete.AgendadoEm = DateTime.UtcNow.AddMinutes(30);
        lembrete.IntervaloDias = 30;

        _lembretes.Setup(r => r.GetPendentesVencendoAsync(It.IsAny<DateTime>()))
            .ReturnsAsync([lembrete]);
        _telegramRepo.Setup(r => r.GetChatIdByTutorIdAsync(It.IsAny<string>()))
            .ReturnsAsync(4242L);

        var antes = lembrete.AgendadoEm;
        await Servico().VerificarLembretesAsync(CancellationToken.None);

        Assert.Equal(StatusLembreteEnum.Pendente, lembrete.Status);
        Assert.Equal(antes.AddDays(30), lembrete.AgendadoEm);
        _lembretes.Verify(r => r.UpdateAsync("serie", It.IsAny<Lembrete>()), Times.Once);
    }

    /// <summary>Sem intervalo, nada muda: o comportamento de antes continua.</summary>
    [Fact]
    public async Task LembreteSemIntervalo_ContinuaVirandoEnviado()
    {
        var lembrete = Lembrete("unico", "tutor-1", "11999990000");
        lembrete.IntervaloDias = null;

        _lembretes.Setup(r => r.GetPendentesVencendoAsync(It.IsAny<DateTime>()))
            .ReturnsAsync([lembrete]);
        _telegramRepo.Setup(r => r.GetChatIdByTutorIdAsync(It.IsAny<string>()))
            .ReturnsAsync(4242L);

        await Servico().VerificarLembretesAsync(CancellationToken.None);

        Assert.Equal(StatusLembreteEnum.Enviado, lembrete.Status);
    }

    [Fact]
    public void AvancarSerie_SemFim_SoSomaOIntervalo()
    {
        var agora = new DateTime(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc);

        var (proxima, terminou) = LembreteNotificationService.AvancarSerie(
            agendadoEm: agora.AddMinutes(30), intervaloDias: 7, repetirAte: null, agora: agora);

        Assert.False(terminou);
        Assert.Equal(agora.AddMinutes(30).AddDays(7), proxima);
    }

    /// <summary>
    /// O "de x dia ate y dia": a serie para quando a proxima data passa do fim.
    /// </summary>
    [Fact]
    public void AvancarSerie_QuandoPassaDoFim_TerminaASerie()
    {
        var agora = new DateTime(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc);
        var inicio = new DateTime(2026, 9, 10, 8, 0, 0, DateTimeKind.Utc);

        // Antibiotico de 10 dias, uma dose por dia: no dia 20 a serie acaba.
        var (_, terminou) = LembreteNotificationService.AvancarSerie(
            agendadoEm: inicio.AddDays(10), intervaloDias: 1,
            repetirAte: inicio.AddDays(10), agora: agora);

        Assert.True(terminou);
    }

    /// <summary>
    /// <b>O caso do tempo parado.</b> Com a API um mes fora do ar, um lembrete
    /// diario esta trinta dias atrasado. Avancar um intervalo por ciclo faria o
    /// tutor receber trinta mensagens iguais para "se atualizar"; o laco pula de
    /// uma vez para a proxima data futura.
    /// </summary>
    [Fact]
    public void AvancarSerie_ComAtrasoDeUmMes_PulaDeUmaVezParaOFuturo()
    {
        var agora = new DateTime(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc);

        var (proxima, terminou) = LembreteNotificationService.AvancarSerie(
            agendadoEm: agora.AddDays(-30), intervaloDias: 1, repetirAte: null, agora: agora);

        Assert.False(terminou);
        Assert.True(proxima > agora, "a proxima data precisa estar no futuro");
        // Uma unica passada: nao sobrou atraso para o proximo ciclo varrer.
        Assert.True(proxima <= agora.AddDays(1));
    }

    /// <summary>
    /// Uma serie que ja acabou nao volta so porque houve atraso: o fim manda,
    /// mesmo com a data ainda no passado.
    /// </summary>
    [Fact]
    public void AvancarSerie_ComAtrasoEFimJaPassado_Termina()
    {
        var agora = new DateTime(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc);

        var (_, terminou) = LembreteNotificationService.AvancarSerie(
            agendadoEm: agora.AddDays(-30), intervaloDias: 1,
            repetirAte: agora.AddDays(-20), agora: agora);

        Assert.True(terminou);
    }

    /// <summary>
    /// Cinto de seguranca: o CHECK do banco e o [Range(1,365)] do request ja
    /// barram intervalo zero, mas se um deles falhar o resultado precisa ser um
    /// fim de serie, e nao um BackgroundService girando para sempre.
    /// </summary>
    [Fact]
    public void AvancarSerie_ComIntervaloInvalido_TerminaEmVezDeGirar()
    {
        var agora = DateTime.UtcNow;

        var (_, terminou) = LembreteNotificationService.AvancarSerie(
            agendadoEm: agora, intervaloDias: 0, repetirAte: null, agora: agora);

        Assert.True(terminou);
    }
}
