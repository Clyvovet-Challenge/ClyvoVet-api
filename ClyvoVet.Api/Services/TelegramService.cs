using ClyvoVet.Api.Exceptions;
using ClyvoVet.Api.Services.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Exceptions;

namespace ClyvoVet.Api.Services;

public class TelegramService : ITelegramService
{
    private readonly ITelegramBotClient _botClient;

    public TelegramService(ITelegramBotClient botClient)
    {
        _botClient = botClient;
    }

    public async Task EnviarMensagemAsync(long chatId, string mensagem)
    {
        try
        {
            await _botClient.SendMessage(chatId, mensagem);
        }
        catch (ApiRequestException ex)
        {
            throw new BadRequestException($"Falha ao enviar mensagem no Telegram: {ex.Message}");
        }
    }
}
