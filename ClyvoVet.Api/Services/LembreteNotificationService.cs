using ClyvoVet.Api.Enums;
using ClyvoVet.Api.Repositories.Interfaces;
using ClyvoVet.Api.Services.Interfaces;

namespace ClyvoVet.Api.Services;

// Fica de olho nos lembretes pendentes que estão vencendo (próxima 1h) e manda
// uma notificação pro tutor — por Telegram, se ele tiver vinculado a conta
// (T_CLYVO_TUTOR_TELEGRAM), ou por WhatsApp, usando o telefone já cadastrado
// (Tutor.Telefone, dado da API Java). Depois de notificar, marca o lembrete
// como Enviado para não notificar de novo.
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

    private async Task VerificarLembretesAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var lembreteRepository = scope.ServiceProvider.GetRequiredService<ILembreteRepository>();
        var tutorTelegramRepository = scope.ServiceProvider.GetRequiredService<ITutorTelegramRepository>();
        var whatsAppService = scope.ServiceProvider.GetRequiredService<IWhatsAppService>();
        var telegramService = scope.ServiceProvider.GetRequiredService<ITelegramService>();

        var lembretes = await lembreteRepository.GetPendentesVencendoAsync(DateTime.UtcNow.Add(JanelaDeAntecedencia));

        foreach (var lembrete in lembretes)
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

            if (!notificado && !string.IsNullOrWhiteSpace(tutor.Telefone))
            {
                try
                {
                    await whatsAppService.EnviarMensagemAsync(tutor.Telefone, mensagem);
                    notificado = true;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Falha ao notificar lembrete {LembreteId} via WhatsApp.", lembrete.Id);
                }
            }

            if (notificado)
            {
                try
                {
                    lembrete.Status = StatusLembreteEnum.Enviado;
                    await lembreteRepository.UpdateAsync(lembrete.Id, lembrete);
                    _logger.LogInformation("Lembrete {LembreteId} notificado e marcado como Enviado.", lembrete.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lembrete {LembreteId} foi notificado, mas falhou ao marcar como Enviado — será notificado de novo no próximo ciclo.", lembrete.Id);
                }
            }
        }
    }
}
