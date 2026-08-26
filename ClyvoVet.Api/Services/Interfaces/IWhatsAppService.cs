namespace ClyvoVet.Api.Services.Interfaces;

public interface IWhatsAppService
{
    Task EnviarMensagemAsync(string telefone, string mensagem);
}
