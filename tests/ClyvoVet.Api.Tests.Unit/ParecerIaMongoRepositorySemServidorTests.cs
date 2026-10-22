using ClyvoVet.Domain.Entities;
using ClyvoVet.Infrastructure.Mongo;
using Microsoft.Extensions.Logging;
using Moq;

namespace ClyvoVet.Api.Tests.Unit;

/// <summary>
/// "O cache nunca derruba a feature": com o Mongo fora do ar, ler vira cache miss e gravar vira Warning.
/// Nenhum servidor é necessário — o banco aponta para uma porta sem ninguém.
/// </summary>
public class ParecerIaMongoRepositorySemServidorTests
{
    private readonly Mock<ILogger<ParecerIaMongoRepository>> _log = new();

    private ParecerIaMongoRepository Repositorio() => new(MongoDeTeste.BancoInalcancavel(), _log.Object);

    private static ParecerIa Parecer(string conteudo = """{"riscos":[],"recomendacoes":[],"resumo":"ok","baseLimitada":false}""") => new()
    {
        Id = "x", AnimalId = "animal-1", Origem = "REGRAS", Conteudo = conteudo,
        GeradoEm = DateTime.UtcNow, ValidoAte = DateTime.UtcNow.AddDays(7),
    };

    [Fact]
    public async Task GetByAnimalIdAsync_MongoForaDoAr_DevolveNullERegistraWarning()
    {
        // Act
        var parecer = await Repositorio().GetByAnimalIdAsync("animal-1");

        // Assert: null = cache miss; o serviço gera o parecer pelo caminho normal.
        Assert.Null(parecer);
        MongoDeTeste.VerificarUmWarning(_log);
    }

    [Fact]
    public async Task SalvarAsync_MongoForaDoAr_NaoLancaERegistraWarning()
    {
        // Act
        await Repositorio().SalvarAsync(Parecer());

        // Assert: chegou aqui sem exceção — o usuário já tem o parecer, só não foi cacheado.
        MongoDeTeste.VerificarUmWarning(_log);
    }

    [Fact]
    public async Task SalvarAsync_ConteudoQueNaoEJson_NaoLancaERegistraWarning()
    {
        // Act: o mapeamento falha ANTES de falar com o servidor.
        await Repositorio().SalvarAsync(Parecer(conteudo: "isto não é json"));

        // Assert
        MongoDeTeste.VerificarUmWarning(_log);
    }
}
