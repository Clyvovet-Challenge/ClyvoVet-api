using ClyvoVet.Api.Enums;
using ClyvoVet.Api.Models;
using ClyvoVet.Api.Repositories.Interfaces;
using ClyvoVet.Api.Services.Interfaces;

namespace ClyvoVet.Api.Services;

// Fica de olho nos lembretes pendentes que estão vencendo (próxima 1h) e manda
// uma notificação pro tutor pelo Telegram, se ele tiver vinculado a conta
// (T_CLYVO_TUTOR_TELEGRAM). Depois de notificar, marca o lembrete como Enviado
// para não notificar de novo.
//
// O TELEGRAM E O UNICO CANAL, POR DECISAO — o WhatsApp saiu do escopo na
// Sprint 3 (e com ele o Twilio). Sem vínculo de Telegram o lembrete fica
// Pendente e o motivo vai para o log; marcá-lo Enviado seria mentir.
public class LembreteNotificationService : BackgroundService
{
    private static readonly TimeSpan IntervaloVerificacao = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan JanelaDeAntecedencia = TimeSpan.FromHours(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LembreteNotificationService> _logger;

    public LembreteNotificationService(IServiceScopeFactory scopeFactory, ILogger<LembreteNotificationService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await VerificarLembretesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao verificar lembretes pendentes.");
            }

            try
            {
                await Task.Delay(IntervaloVerificacao, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    internal async Task VerificarLembretesAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var lembreteRepository = scope.ServiceProvider.GetRequiredService<ILembreteRepository>();
        var tutorTelegramRepository = scope.ServiceProvider.GetRequiredService<ITutorTelegramRepository>();
        var telegramService = scope.ServiceProvider.GetRequiredService<ITelegramService>();

        var lembretes = await lembreteRepository.GetPendentesVencendoAsync(DateTime.UtcNow.Add(JanelaDeAntecedencia));

        foreach (var lembrete in lembretes)
        {
            // UM LEMBRETE RUIM NAO PODE LEVAR O LOTE INTEIRO.
            //
            // Os envios ja tinham try/catch, mas o resto do corpo nao: a leitura do
            // vinculo do Telegram, a montagem da mensagem e a gravacao do status
            // corriam soltos. Qualquer excecao ali -- uma queda momentanea do banco,
            // um registro inconsistente -- subia ate o catch do ExecuteAsync e
            // abortava o FOREACH. Todos os lembretes seguintes daquele ciclo eram
            // pulados, e como nenhum deles chega a virar Enviado, o mesmo lembrete
            // ruim reaparece no ciclo seguinte e derruba tudo de novo. Um unico
            // registro problematico parava a notificacao da plataforma inteira, e o
            // unico sinal era um LogWarning generico a cada minuto.
            try
            {
                await NotificarAsync(lembrete, lembreteRepository, tutorTelegramRepository,
                                     telegramService);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Falha ao processar o lembrete {LembreteId}. Os demais do ciclo seguem.",
                    lembrete.Id);
            }
        }
    }

    private async Task NotificarAsync(
        Lembrete lembrete,
        ILembreteRepository lembreteRepository,
        ITutorTelegramRepository tutorTelegramRepository,
        ITelegramService telegramService)
    {
        var tutor = lembrete.Animal.Tutor;
        var mensagem = $"Lembrete: {lembrete.Titulo} agendado para {lembrete.AgendadoEm:dd/MM/yyyy HH:mm} ({lembrete.Animal.Nome}).";
        var notificado = false;

        var chatId = await tutorTelegramRepository.GetChatIdByTutorIdAsync(tutor.Id);
        if (chatId.HasValue)
        {
            try
            {
                await telegramService.EnviarMensagemAsync(chatId.Value, mensagem);
                notificado = true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao notificar lembrete {LembreteId} via Telegram.", lembrete.Id);
            }
        }

        if (!notificado)
        {
            // Sem Telegram vinculado nao ha por onde avisar — o WhatsApp saiu
            // do escopo. O lembrete fica Pendente e volta a ser varrido a cada
            // minuto; o log diz o motivo para o operador nao ver so "a
            // notificacao nao chegou".
            _logger.LogWarning(
                "Lembrete {LembreteId} sem canal de notificacao: o tutor {TutorId} nao tem Telegram vinculado.",
                lembrete.Id, tutor.Id);
        }

        if (notificado)
        {
            try
            {
                if (lembrete.IntervaloDias is > 0)
                {
                    var (proxima, terminou) = AvancarSerie(
                        lembrete.AgendadoEm, lembrete.IntervaloDias.Value,
                        lembrete.RepetirAte, DateTime.UtcNow);

                    if (terminou)
                    {
                        lembrete.Status = StatusLembreteEnum.Enviado;
                        _logger.LogInformation(
                            "Lembrete {LembreteId} notificado; a série terminou em {RepetirAte:d}.",
                            lembrete.Id, lembrete.RepetirAte);
                    }
                    else
                    {
                        // Continua Pendente DE PROPOSITO: e a mesma linha que
                        // volta, com a data empurrada para a frente.
                        lembrete.AgendadoEm = proxima;
                        _logger.LogInformation(
                            "Lembrete {LembreteId} notificado; volta em {Proxima:g} (a cada {Dias} dias).",
                            lembrete.Id, proxima, lembrete.IntervaloDias);
                    }
                }
                else
                {
                    lembrete.Status = StatusLembreteEnum.Enviado;
                    _logger.LogInformation("Lembrete {LembreteId} notificado e marcado como Enviado.", lembrete.Id);
                }

                await lembreteRepository.UpdateAsync(lembrete.Id, lembrete);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lembrete {LembreteId} foi notificado, mas falhou ao gravar o novo estado — será notificado de novo no próximo ciclo.", lembrete.Id);
            }
        }
    }

    /// <summary>
    /// Onde a serie cai depois de um disparo, e se ela acabou.
    /// </summary>
    /// <remarks>
    /// <para>Extraido como metodo estatico de proposito: os testes de integracao
    /// desta API rodam em <c>UseInMemoryDatabase</c> e nao exercitam o
    /// BackgroundService, entao a aritmetica da serie so tem cobertura se ela
    /// puder ser chamada direto. E o mesmo motivo que levou o MapaDeErro a sair
    /// do lambda do UseExceptionHandler.</para>
    ///
    /// <para><b>O laco existe por causa do tempo parado.</b> Se a API ficou fora do
    /// ar por um mes, um lembrete diario esta trinta dias atrasado. Avancar UM
    /// intervalo o deixaria ainda no passado, ele seria varrido no minuto
    /// seguinte, notificado outra vez, e o tutor receberia trinta mensagens
    /// iguais para se atualizar. O laco pula de uma vez para a proxima data
    /// futura: uma notificacao, e a serie volta ao ritmo.</para>
    ///
    /// <para>O teto de iteracoes e cinto de seguranca. O CHECK do banco e o
    /// <c>[Range(1,365)]</c> do request ja impedem intervalo zero ou negativo;
    /// se algum dia um deles falhar, o que acontece e um limite atingido e nao
    /// um BackgroundService girando para sempre.</para>
    /// </remarks>
    internal static (DateTime proxima, bool serieTerminou) AvancarSerie(
        DateTime agendadoEm, int intervaloDias, DateTime? repetirAte, DateTime agora)
    {
        if (intervaloDias <= 0)
            return (agendadoEm, true);

        const int TetoDeIteracoes = 1000;
        var proxima = agendadoEm;

        for (var i = 0; i < TetoDeIteracoes; i++)
        {
            proxima = proxima.AddDays(intervaloDias);

            // O fim da serie manda, mesmo que a data ainda esteja no passado:
            // uma serie que acabou nao volta so porque houve atraso.
            if (repetirAte.HasValue && proxima > repetirAte.Value)
                return (proxima, true);

            if (proxima > agora)
                return (proxima, false);
        }

        return (proxima, false);
    }
}
