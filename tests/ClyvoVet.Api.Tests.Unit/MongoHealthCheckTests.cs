using ClyvoVet.Infrastructure.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ClyvoVet.Api.Tests.Unit;

public class MongoHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_MongoForaDoAr_RetornaUnhealthyComAExcecao()
    {
        // Arrange
        var check = new MongoHealthCheck(MongoDeTeste.BancoInalcancavel());

        // Act
        var resultado = await check.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, resultado.Status);
        Assert.NotNull(resultado.Exception);
    }
}
