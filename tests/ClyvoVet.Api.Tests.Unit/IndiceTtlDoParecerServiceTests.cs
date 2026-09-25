using ClyvoVet.Infrastructure.Mongo;
using Microsoft.Extensions.Logging;
using Moq;

namespace ClyvoVet.Api.Tests.Unit;

public class IndiceTtlDoParecerServiceTests
{
    [Fact]
    public async Task Servico_MongoForaDoAr_TerminaSemLancarERegistraWarning()
    {
        // Arrange
        var log = new Mock<ILogger<IndiceTtlDoParecerService>>();
        using var servico = new IndiceTtlDoParecerService(MongoDeTeste.BancoInalcancavel(), log.Object);

        // Act
        await servico.StartAsync(CancellationToken.None);
        await servico.ExecuteTask!;

        // Assert: o boot da API não pode depender do Mongo — no máximo um aviso no log.
        MongoDeTeste.VerificarUmWarning(log);
    }
}
