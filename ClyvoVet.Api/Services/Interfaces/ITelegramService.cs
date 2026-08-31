namespace ClyvoVet.Api.Services.Interfaces;

public interface ITelegramService
{
    Task EnviarMensagemAsync(long chatId, string mensagem);
}
