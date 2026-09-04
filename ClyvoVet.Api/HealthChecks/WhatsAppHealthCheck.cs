using Microsoft.Extensions.Diagnostics.HealthChecks;
using Twilio.Rest.Api.V2010;

namespace ClyvoVet.Api.HealthChecks;

/// <summary>
/// Verifica a disponibilidade da API do Twilio (WhatsApp) buscando os dados da conta
/// configurada — chamada de leitura, sem custo/side-effect.
/// </summary>
public class WhatsAppHealthCheck : IHealthCheck
{
    private readonly string? _accountSid;

    public WhatsAppHealthCheck(IConfiguration configuration)
    {
        _accountSid = configuration["Twilio:AccountSid"];
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var account = await AccountResource.FetchAsync(pathSid: _accountSid);
            return HealthCheckResult.Healthy($"Conta Twilio {account.Status} respondendo.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("API do Twilio (WhatsApp) inacessível.", ex);
        }
    }
}
