using ClyvoVet.Api.Repositories.Interfaces;
using Telegram.Bot;

namespace ClyvoVet.Api.Services;

// Fica de olho nas mensagens que chegam pro bot (via polling em getUpdates, já
// que rodar sem HTTPS público não permite usar webhook do Telegram). Trata dois
// comandos: "/start <tutorId>" (o deep link gerado em /api/v1/telegram/link/{tutorId}),
// que salva o vínculo TutorId -> ChatId, e "/meuslembretes", que lista os lembretes
// pendentes do tutor já vinculado. Qualquer outra mensagem recebe uma resposta
// padrão explicando que o bot não tem fluxo de conversa livre.
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

        if (texto.Trim().Equals("/meuslembretes", StringComparison.OrdinalIgnoreCase))
        {
            await ResponderMeusLembretesAsync(chatId, cancellationToken);
            return;
        }

        // Bot só envia notificações automáticas — não tem fluxo de conversa, então
        // qualquer mensagem que não seja um comando reconhecido cai aqui.
        await _botClient.SendMessage(
            chatId,
            "Esse bot só envia notificações automáticas da ClyvoVet (lembretes de cuidados do seu pet) — não é possível conversar por aqui.\n\nComandos disponíveis: /meuslembretes",
            cancellationToken: cancellationToken);
    }

    private async Task ResponderMeusLembretesAsync(long chatId, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var tutorTelegramRepository = scope.ServiceProvider.GetRequiredService<ITutorTelegramRepository>();
        var lembreteRepository = scope.ServiceProvider.GetRequiredService<ILembreteRepository>();

        var tutorId = await tutorTelegramRepository.GetTutorIdByChatIdAsync(chatId);
        if (tutorId is null)
        {
            await _botClient.SendMessage(
                chatId,
                "Você ainda não está vinculado a nenhum tutor. Peça o link de vínculo no app da ClyvoVet.",
                cancellationToken: cancellationToken);
            return;
        }

        var lembretes = (await lembreteRepository.GetPendentesByTutorIdAsync(tutorId)).ToList();
        if (lembretes.Count == 0)
        {
            await _botClient.SendMessage(chatId, "Você não tem lembretes pendentes no momento. 🎉", cancellationToken: cancellationToken);
            return;
        }

        var linhas = lembretes.Select(l => $"• {l.Titulo} ({l.Animal.Nome}) — {l.AgendadoEm:dd/MM/yyyy HH:mm}");
        var mensagem = "📋 Seus lembretes pendentes:\n\n" + string.Join("\n", linhas);
        await _botClient.SendMessage(chatId, mensagem, cancellationToken: cancellationToken);
    }
}
