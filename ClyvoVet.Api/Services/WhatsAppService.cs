using ClyvoVet.Api.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace ClyvoVet.Api.Services;

public class WhatsAppService : IWhatsAppService
{
    private readonly string _numeroSandbox;

    public WhatsAppService(IConfiguration configuration)
    {
        var accountSid = configuration["Twilio:AccountSid"];
        var authToken = configuration["Twilio:AuthToken"];
        _numeroSandbox = configuration["Twilio:NumeroSandbox"]!;

        TwilioClient.Init(accountSid, authToken);
    }

    public async Task EnviarMensagemAsync(string telefone, string mensagem)
    {
        await MessageResource.CreateAsync(
            body: mensagem,
            from: new PhoneNumber(_numeroSandbox),
            to: new PhoneNumber($"whatsapp:{telefone}"));
    }
}
