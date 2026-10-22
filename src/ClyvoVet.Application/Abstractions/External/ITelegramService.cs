namespace ClyvoVet.Application.Abstractions.External;

public interface ITelegramService
{
    Task EnviarMensagemAsync(long chatId, string mensagem);
}
