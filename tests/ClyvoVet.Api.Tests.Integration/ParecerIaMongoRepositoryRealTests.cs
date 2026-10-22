using System.Text.Json;
using ClyvoVet.Domain.Entities;
using ClyvoVet.Infrastructure.Mongo;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ClyvoVet.Api.Tests.Integration;

/// <summary>O caminho feliz contra um Mongo de verdade (pulado sem <c>MONGO_TEST_URI</c>).</summary>
public class ParecerIaMongoRepositoryRealTests : IClassFixture<MongoRealFixture>
{
    private readonly MongoRealFixture _mongo;

    public ParecerIaMongoRepositoryRealTests(MongoRealFixture mongo) => _mongo = mongo;

    private const string Conteudo = """{"riscos":[{"doenca":"Mastocitoma","nivel":"ALTO"}],"recomendacoes":["Checkup"],"resumo":"ação já","baseLimitada":false}""";

    private static ParecerIa Parecer(string origem = "IA") => new()
    {
        Id = "ignorado", AnimalId = "animal-1", Origem = origem, Modelo = origem == "IA" ? "meta.llama" : null,
        Conteudo = Conteudo, GeradoEm = DateTime.UtcNow, ValidoAte = DateTime.UtcNow.AddDays(7),
    };

    private static string Normalizar(string json) => JsonSerializer.Serialize(JsonDocument.Parse(json).RootElement);

    [FactRequiresMongo]
    public async Task SalvarEDepoisLer_ParecerNovo_DevolveOMesmoParecer()
    {
        // Arrange
        var repositorio = new ParecerIaMongoRepository(_mongo.NovoBanco(), NullLogger<ParecerIaMongoRepository>.Instance);
        var original = Parecer();

        // Act
        await repositorio.SalvarAsync(original);
        var lido = await repositorio.GetByAnimalIdAsync("animal-1");

        // Assert
        Assert.NotNull(lido);
        Assert.Equal("IA", lido.Origem);
        Assert.Equal("meta.llama", lido.Modelo);
        Assert.Equal(Normalizar(Conteudo), Normalizar(lido.Conteudo));
        // O Mongo guarda milissegundos: tolerância de 1 ms.
        Assert.Equal(original.ValidoAte, lido.ValidoAte, TimeSpan.FromMilliseconds(1));
    }

    [FactRequiresMongo]
    public async Task SalvarAsync_MesmoAnimalDuasVezes_SubstituiSemDuplicar()
    {
        // Arrange
        var banco = _mongo.NovoBanco();
        var repositorio = new ParecerIaMongoRepository(banco, NullLogger<ParecerIaMongoRepository>.Instance);

        // Act
        await repositorio.SalvarAsync(Parecer("IA"));
        await repositorio.SalvarAsync(Parecer("REGRAS"));

        // Assert: é cache, não histórico — o upsert substitui.
        var colecao = banco.GetCollection<BsonDocument>(ParecerIaMongoRepository.NomeDaColecao);
        Assert.Equal(1, await colecao.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        Assert.Equal("REGRAS", (await repositorio.GetByAnimalIdAsync("animal-1"))!.Origem);
    }

    [FactRequiresMongo]
    public async Task GetByAnimalIdAsync_AnimalSemParecer_DevolveNull()
    {
        var repositorio = new ParecerIaMongoRepository(_mongo.NovoBanco(), NullLogger<ParecerIaMongoRepository>.Instance);

        Assert.Null(await repositorio.GetByAnimalIdAsync("nao-existe"));
    }

    [FactRequiresMongo]
    public async Task SalvarAsync_ParecerNovo_GravaOConteudoComoSubdocumento()
    {
        // Arrange
        var banco = _mongo.NovoBanco();
        var repositorio = new ParecerIaMongoRepository(banco, NullLogger<ParecerIaMongoRepository>.Instance);

        // Act
        await repositorio.SalvarAsync(Parecer());

        // Assert: olhando o documento cru, como um "mongosh" veria.
        var bruto = await banco.GetCollection<BsonDocument>(ParecerIaMongoRepository.NomeDaColecao)
            .Find(new BsonDocument("_id", "animal-1")).SingleAsync();
        Assert.Equal(BsonType.Document, bruto["conteudo"].BsonType);
        Assert.Equal("Mastocitoma", bruto["conteudo"]["riscos"][0]["doenca"].AsString);
    }
}
