using Microsoft.Extensions.Logging;
using Moq;
using MongoDB.Driver;

namespace ClyvoVet.Api.Tests.Unit;

internal static class MongoDeTeste
{
    /// <summary>
    /// Um banco que NÃO existe: porta 1, sem servidor, e o driver desiste em ~300 ms (o padrão são 30 s).
    /// É o que permite testar "Mongo fora do ar" sem nenhum servidor e sem esperar.
    /// </summary>
    public static IMongoDatabase BancoInalcancavel() =>
        new MongoClient("mongodb://127.0.0.1:1/?serverSelectionTimeoutMS=300&connectTimeoutMS=300")
            .GetDatabase("clyvovet_teste");

    public static void VerificarUmWarning<T>(Mock<ILogger<T>> log) => log.Verify(l => l.Log(
        LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
        It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
}
