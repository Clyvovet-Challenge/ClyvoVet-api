using ClyvoVet.Infrastructure.Mongo;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ClyvoVet.Api.Tests.Integration;

public class IndiceTtlDoParecerRealTests : IClassFixture<MongoRealFixture>
{
    private readonly MongoRealFixture _mongo;

    public IndiceTtlDoParecerRealTests(MongoRealFixture mongo) => _mongo = mongo;

    private static async Task RodarAsync(IMongoDatabase banco)
    {
        using var servico = new IndiceTtlDoParecerService(banco, NullLogger<IndiceTtlDoParecerService>.Instance);
        await servico.StartAsync(CancellationToken.None);
        await servico.ExecuteTask!;
    }

    private static async Task<List<BsonDocument>> IndicesAsync(IMongoDatabase banco) =>
        await (await banco.GetCollection<BsonDocument>(ParecerIaMongoRepository.NomeDaColecao).Indexes.ListAsync()).ToListAsync();

    [FactRequiresMongo]
    public async Task Servico_BancoNovo_CriaOIndiceTtlEmValidoAte()
    {
        // Arrange
        var banco = _mongo.NovoBanco();

        // Act
        await RodarAsync(banco);

        // Assert
        var indice = Assert.Single(await IndicesAsync(banco), i => i["name"] == IndiceTtlDoParecerService.NomeDoIndice);
        Assert.Equal(1, indice["key"]["validoAte"].ToInt32());
        Assert.Equal(0, indice["expireAfterSeconds"].ToInt32());
    }

    [FactRequiresMongo]
    public async Task Servico_RodadoDuasVezes_ContinuaComUmSoIndiceSemFalhar()
    {
        // Arrange
        var banco = _mongo.NovoBanco();

        // Act: é o que acontece a cada boot da API.
        await RodarAsync(banco);
        await RodarAsync(banco);

        // Assert: o índice do _id + o TTL.
        Assert.Equal(2, (await IndicesAsync(banco)).Count);
    }
}
