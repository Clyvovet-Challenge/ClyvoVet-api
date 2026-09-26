using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ClyvoVet.Application.Abstractions.External;
using ClyvoVet.Domain.Enums;
using ClyvoVet.Domain.Entities;
using ClyvoVet.Application.Abstractions.Repositories;
using ClyvoVet.Application.Services.Interfaces;

namespace ClyvoVet.Infrastructure.Background;

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
                lembrete.RegistrarNotificacao(DateTime.UtcNow);
                RegistrarNoLog(lembrete);

                await lembreteRepository.UpdateAsync(lembrete.Id, lembrete);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lembrete {LembreteId} foi notificado, mas falhou ao gravar o novo estado — será notificado de novo no próximo ciclo.", lembrete.Id);
            }
        }
    }

    // A decisao ja foi tomada pela entidade; aqui so se conta ao operador qual
    // dos tres caminhos ela seguiu, com as mesmas mensagens de antes.
    private void RegistrarNoLog(Lembrete lembrete)
    {
        if (lembrete.Status == StatusLembreteEnum.Pendente)
            _logger.LogInformation(
                "Lembrete {LembreteId} notificado; volta em {Proxima:g} (a cada {Dias} dias).",
                lembrete.Id, lembrete.AgendadoEm, lembrete.IntervaloDias);
        else if (lembrete.IntervaloDias is > 0)
            _logger.LogInformation(
                "Lembrete {LembreteId} notificado; a série terminou em {RepetirAte:d}.",
                lembrete.Id, lembrete.RepetirAte);
        else
            _logger.LogInformation("Lembrete {LembreteId} notificado e marcado como Enviado.", lembrete.Id);
    }
}
