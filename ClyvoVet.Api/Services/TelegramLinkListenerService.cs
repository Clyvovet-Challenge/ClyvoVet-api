using ClyvoVet.Api.Repositories.Interfaces;
using Telegram.Bot;

namespace ClyvoVet.Api.Services;

// Fica de olho nas mensagens que chegam pro bot (via polling em getUpdates, já
// que rodar sem HTTPS público não permite usar webhook do Telegram) e, ao ver
// um "/start <tutorId>" (o deep link gerado em /api/v1/telegram/link/{tutorId}),
// salva o vínculo TutorId -> ChatId.
public class TelegramLinkListenerService : BackgroundService
{
    private static readonly TimeSpan IntervaloPolling = TimeSpan.FromSeconds(5);

    private readonly ITelegramBotClient _botClient;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TelegramLinkListenerService> _logger;
    private int _offset;

    public TelegramLinkListenerService(
        ITelegramBotClient botClient,
        IServiceScopeFactory scopeFactory,
        ILogger<TelegramLinkListenerService> logger)
    {
        _botClient = botClient;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var updates = await _botClient.GetUpdates(offset: _offset, timeout: 0, cancellationToken: stoppingToken);

                foreach (var update in updates)
                {
                    _offset = update.Id + 1;

                    try
                    {
                        await ProcessarAsync(update, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Falha ao processar update {UpdateId} do Telegram — esse update não será reprocessado.", update.Id);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao consultar updates do Telegram.");
            }

            try
            {
                await Task.Delay(IntervaloPolling, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task ProcessarAsync(Telegram.Bot.Types.Update update, CancellationToken cancellationToken)
    {
        var texto = update.Message?.Text;
        if (string.IsNullOrWhiteSpace(texto))
            return;

        var chatId = update.Message!.Chat.Id;

        if (texto.StartsWith("/start "))
        {
            var tutorId = texto["/start ".Length..].Trim();
            if (string.IsNullOrWhiteSpace(tutorId))
                return;

            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<ITutorTelegramRepository>();
            await repository.VincularAsync(tutorId, chatId);

            _logger.LogInformation("Tutor {TutorId} vinculado ao chatId {ChatId} no Telegram.", tutorId, chatId);

            await _botClient.SendMessage(
                chatId,
                "✅ Vínculo confirmado! A partir de agora você recebe por aqui os lembretes de cuidados do seu pet.",
                cancellationToken: cancellationToken);
            return;
        }

        // Bot só envia notificações automáticas — não tem fluxo de conversa, então
        // qualquer mensagem que não seja o /start do deep link cai aqui.
        await _botClient.SendMessage(
            chatId,
            "Esse bot só envia notificações automáticas da ClyvoVet (lembretes de cuidados do seu pet) — não é possível conversar por aqui.",
            cancellationToken: cancellationToken);
    }
}
