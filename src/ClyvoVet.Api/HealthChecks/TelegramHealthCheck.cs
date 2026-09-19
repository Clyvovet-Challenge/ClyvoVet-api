using Microsoft.Extensions.Diagnostics.HealthChecks;
using Telegram.Bot;

namespace ClyvoVet.Api.HealthChecks;

/// <summary>
/// Verifica a disponibilidade da Telegram Bot API chamando GetMe (leitura, sem custo/side-effect).
/// </summary>
public class TelegramHealthCheck : IHealthCheck
{
    private readonly ITelegramBotClient _botClient;

    public TelegramHealthCheck(ITelegramBotClient botClient)
    {
        _botClient = botClient;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var me = await _botClient.GetMe(cancellationToken);
            return HealthCheckResult.Healthy($"Bot @{me.Username} respondendo.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Telegram Bot API inacessível.", ex);
        }
    }
}
