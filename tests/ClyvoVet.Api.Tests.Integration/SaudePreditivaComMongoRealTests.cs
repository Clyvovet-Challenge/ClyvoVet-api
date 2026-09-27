using ClyvoVet.Application.Abstractions.External;
using ClyvoVet.Application.Abstractions.Repositories;
using ClyvoVet.Application.Services;
using ClyvoVet.Application.Services.Interfaces;
using ClyvoVet.Application.Services.SaudePreditiva;
using ClyvoVet.Domain.Entities;
using ClyvoVet.Infrastructure.Mongo;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ClyvoVet.Api.Tests.Integration;

/// <summary>
/// O <see cref="SaudePreditivaService"/> de verdade com o <see cref="ParecerIaMongoRepository"/> de verdade
/// contra um Mongo real: o segundo pedido não gera nada, lê o documento (pulado sem <c>MONGO_TEST_URI</c>).
/// </summary>
public class SaudePreditivaComMongoRealTests : IClassFixture<MongoRealFixture>
{
    private readonly MongoRealFixture _mongo;

    public SaudePreditivaComMongoRealTests(MongoRealFixture mongo) => _mongo = mongo;

    private static Animal Bolinha() => new()
    {
        Id = "animal-1", Nome = "Bolinha", Especie = "Cachorro", Raca = "Labrador Retriever",
        DataNascimento = DateTime.UtcNow.AddYears(-6), TutorId = "tutor-1",
        Tutor = new Tutor { Id = "tutor-1", Nome = "Lucas" }, RacaId = "raca-1",
        RacaCatalogo = new Raca { Id = "raca-1", Especie = "CAO", Nome = "Labrador Retriever", Chave = "labrador-retriever", Ativo = true },
    };

    private static BaseDoenca Linha() => new()
    {
        Id = Guid.NewGuid().ToString(), Especie = "CAO", RacaTexto = "labrador_retriever", RacaChave = "labrador-retriever",
        DoencaCodigo = "MCT", DoencaNome = "Mastocitoma", Categoria = "ONCOLOGICA", Casos = 306, Controles = 10, Doi = "10.5061/dryad.266k4",
    };

    [FactRequiresMongo]
    public async Task GetParecerAsync_ChamadoDuasVezes_ASegundaVemDoCacheNoMongo()
    {
        // Arrange
        var banco = _mongo.NovoBanco();
        var animais = new Mock<IAnimalRepository>();
        animais.Setup(r => r.GetByIdAsync("animal-1")).ReturnsAsync(Bolinha());
        var baseDoencas = new Mock<IBaseDoencaRepository>();
        baseDoencas.Setup(r => r.GetByEspecieAsync("CAO")).ReturnsAsync([Linha()]);
        var ia = new Mock<IOciGenerativeAiClient>();
        ia.SetupGet(i => i.Configurado).Returns(false);
        var tutorTelegram = new Mock<ITutorTelegramRepository>();
        tutorTelegram.Setup(r => r.GetChatIdByTutorIdAsync(It.IsAny<string>())).ReturnsAsync((long?)null);

        var servico = new SaudePreditivaService(
            animais.Object, baseDoencas.Object,
            new ParecerIaMongoRepository(banco, NullLogger<ParecerIaMongoRepository>.Instance),
            ia.Object, tutorTelegram.Object, new Mock<ITelegramService>().Object, new TravasPorAnimal(),
            NullLogger<SaudePreditivaService>.Instance);

        // Act
        var primeira = await servico.GetParecerAsync("animal-1");
        var segunda = await servico.GetParecerAsync("animal-1");

        // Assert: gerou UMA vez (a base de doenças só foi consultada na primeira).
        baseDoencas.Verify(r => r.GetByEspecieAsync("CAO"), Times.Once);
        Assert.Equal(primeira.GeradoEm, segunda.GeradoEm, TimeSpan.FromMilliseconds(1));
        Assert.NotEmpty(segunda.Riscos);

        // E o documento está lá, com o conteúdo como subdocumento.
        var bruto = await banco.GetCollection<BsonDocument>(ParecerIaMongoRepository.NomeDaColecao)
            .Find(new BsonDocument("_id", "animal-1")).SingleAsync();
        Assert.Equal("REGRAS", bruto["origem"].AsString);
        Assert.Equal(BsonType.Document, bruto["conteudo"].BsonType);
    }
}
