using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ClyvoVet.Infrastructure.HealthChecks;

/// <summary>
/// Verifica o MongoDB com um <c>ping</c> (leitura, sem efeito colateral). Fica na tag <c>external</c> e
/// <b>fora</b> de <c>ready</c>: o Mongo é só um cache, e a instabilidade dele não deve tirar a API de rotação.
/// </summary>
public class MongoHealthCheck : IHealthCheck
{
    private readonly IMongoDatabase _banco;

    public MongoHealthCheck(IMongoDatabase banco)
    {
        _banco = banco;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await _banco.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: cancellationToken);
            return HealthCheckResult.Healthy("MongoDB respondendo.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("MongoDB inacessível.", ex);
        }
    }
}
