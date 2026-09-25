using ClyvoVet.Infrastructure.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ClyvoVet.Api.Tests.Integration;

public class MongoHealthCheckRealTests : IClassFixture<MongoRealFixture>
{
    private readonly MongoRealFixture _mongo;

    public MongoHealthCheckRealTests(MongoRealFixture mongo) => _mongo = mongo;

    [FactRequiresMongo]
    public async Task CheckHealthAsync_MongoNoAr_RetornaHealthy()
    {
        var resultado = await new MongoHealthCheck(_mongo.NovoBanco()).CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, resultado.Status);
    }
}
