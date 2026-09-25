using ClyvoVet.Application.Abstractions.Repositories;
using ClyvoVet.Infrastructure;
using ClyvoVet.Infrastructure.Mongo;
using ClyvoVet.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using MongoDB.Driver;

namespace ClyvoVet.Api.Tests.Unit;

/// <summary>
/// A escolha do repositório do parecer por configuração (design §7.2): sem <c>Mongo:ConnectionString</c>
/// nada muda em relação a hoje; com ela, o Mongo entra; e o ambiente Testing nunca usa Mongo.
/// </summary>
public class MongoRegistroTests
{
    private const string Mongo = "mongodb://127.0.0.1:27017";

    private static ServiceCollection Registrar(Dictionary<string, string?> extras, string ambiente = "Production")
    {
        var valores = new Dictionary<string, string?> { ["ConnectionStrings:DefaultConnection"] = "Server=x;Database=x;Uid=x;Pwd=x;" };
        foreach (var (chave, valor) in extras)
            valores[chave] = valor;

        var ambienteFalso = new Mock<IHostEnvironment>();
        ambienteFalso.SetupGet(a => a.EnvironmentName).Returns(ambiente);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(new ConfigurationBuilder().AddInMemoryCollection(valores).Build(), ambienteFalso.Object);
        return services;
    }

    private static Type? RepositorioDoParecer(IServiceCollection services) =>
        services.Single(d => d.ServiceType == typeof(IParecerIaRepository)).ImplementationType;

    [Fact]
    public void SemChaveDeConexao_UsaORepositorioDoEfCore()
    {
        var services = Registrar(new());

        Assert.Equal(typeof(ParecerIaRepository), RepositorioDoParecer(services));
        Assert.DoesNotContain(services, d => d.ServiceType == typeof(IMongoClient));
        Assert.DoesNotContain(services, d => d.ImplementationType == typeof(IndiceTtlDoParecerService));
    }

    [Fact]
    public void ChaveDeConexaoEmBranco_UsaORepositorioDoEfCore()
    {
        var services = Registrar(new() { ["Mongo:ConnectionString"] = "   " });

        Assert.Equal(typeof(ParecerIaRepository), RepositorioDoParecer(services));
    }

    [Fact]
    public void ChaveDeConexaoPresente_UsaOMongoEAgendaOIndice()
    {
        var services = Registrar(new() { ["Mongo:ConnectionString"] = Mongo });

        Assert.Equal(typeof(ParecerIaMongoRepository), RepositorioDoParecer(services));
        Assert.Contains(services, d => d.ImplementationType == typeof(IndiceTtlDoParecerService));
    }

    [Fact]
    public void AmbienteTesting_NuncaUsaMongo_MesmoComAChaveDefinida()
    {
        // Uma variável Mongo__ConnectionString esquecida no shell não pode fazer a suíte falar com um Mongo.
        var services = Registrar(new() { ["Mongo:ConnectionString"] = Mongo }, ambiente: "Testing");

        Assert.Equal(typeof(ParecerIaRepository), RepositorioDoParecer(services));
        Assert.DoesNotContain(services, d => d.ServiceType == typeof(IMongoClient));
    }

    [Fact]
    public void ConexaoSemTempoLimite_LimitaAEsperaPeloServidor()
    {
        // O padrão do driver é 30 s: com o Mongo fora do ar, a saúde preditiva ficaria pendurada.
        var provider = Registrar(new() { ["Mongo:ConnectionString"] = Mongo }).BuildServiceProvider();

        Assert.Equal(TimeSpan.FromSeconds(3), provider.GetRequiredService<IMongoClient>().Settings.ServerSelectionTimeout);
    }

    [Fact]
    public void ConexaoComTempoLimiteProprio_RespeitaOValorDela()
    {
        var provider = Registrar(new() { ["Mongo:ConnectionString"] = Mongo + "/?serverSelectionTimeoutMS=1000" }).BuildServiceProvider();

        Assert.Equal(TimeSpan.FromSeconds(1), provider.GetRequiredService<IMongoClient>().Settings.ServerSelectionTimeout);
    }

    [Theory]
    [InlineData(null, "clyvovet")]
    [InlineData("", "clyvovet")]
    [InlineData("outro_banco", "outro_banco")]
    public void NomeDoBanco_UsaOInformadoOuOPadrao(string? configurado, string esperado)
    {
        var provider = Registrar(new() { ["Mongo:ConnectionString"] = Mongo, ["Mongo:Database"] = configurado }).BuildServiceProvider();

        Assert.Equal(esperado, provider.GetRequiredService<IMongoDatabase>().DatabaseNamespace.DatabaseName);
    }

    private static IReadOnlyCollection<HealthCheckRegistration> ChecksRegistrados(IServiceCollection services) =>
        services.BuildServiceProvider().GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations
            .ToList();

    [Fact]
    public void ChaveDeConexaoPresente_RegistraOCheckMongoSoNaTagExternal()
    {
        var checks = ChecksRegistrados(Registrar(new() { ["Mongo:ConnectionString"] = Mongo }));

        var mongo = Assert.Single(checks, c => c.Name == "mongo");
        // Fora de "ready": cache instável não tira a API de rotação (mesmo raciocínio do Telegram).
        Assert.Equal(["external"], mongo.Tags.ToArray());
    }

    [Fact]
    public void SemChaveDeConexao_NaoRegistraOCheckMongo()
    {
        var checks = ChecksRegistrados(Registrar(new()));

        Assert.DoesNotContain(checks, c => c.Name == "mongo");
        Assert.Contains(checks, c => c.Name == "mysql-database");
    }
}
