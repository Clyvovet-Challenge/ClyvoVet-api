using ClyvoVet.Application.Abstractions.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;

namespace ClyvoVet.Infrastructure.Mongo;

internal static class MongoServiceExtensions
{
    private const string ChaveDaConexao = "Mongo:ConnectionString";
    private const string ChaveDoBanco = "Mongo:Database";
    private const string BancoPadrao = "clyvovet";

    // O padrão do driver é 30 s para desistir de achar o servidor. Para um CACHE isso é o pior dos
    // mundos: com o Mongo fora do ar, cada pedido de saúde preditiva ficaria 30 s pendurado só para
    // descobrir que não há cache. 3 s bastam para um Mongo saudável e limitam o estrago.
    private static readonly TimeSpan EsperaPeloServidor = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Registra o Mongo <b>só se</b> houver <c>Mongo:ConnectionString</c> e o ambiente não for
    /// <c>Testing</c>. Devolve <c>true</c> quando registrou; <c>false</c> significa "fica o EF/MySQL de hoje".
    /// </summary>
    internal static bool AddMongoSeConfigurado(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var conexao = configuration[ChaveDaConexao];
        if (string.IsNullOrWhiteSpace(conexao) || environment.IsEnvironment("Testing"))
            return false;

        var nomeDoBanco = configuration[ChaveDoBanco];
        if (string.IsNullOrWhiteSpace(nomeDoBanco))
            nomeDoBanco = BancoPadrao;

        // O cliente é thread-safe e caro de criar: um só, para a vida toda da aplicação. O driver só
        // conecta na primeira operação, então registrar não exige o servidor no ar.
        services.AddSingleton<IMongoClient>(_ => new MongoClient(Configurar(conexao)));
        services.AddSingleton(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(nomeDoBanco));
        services.AddSingleton<IParecerIaRepository, ParecerIaMongoRepository>();
        services.AddHostedService<IndiceTtlDoParecerService>();
        return true;
    }

    private static MongoClientSettings Configurar(string conexao)
    {
        var url = MongoUrl.Create(conexao);
        var configuracao = MongoClientSettings.FromUrl(url);

        // Se a connection string já define o tempo (serverSelectionTimeoutMS), vale o dela.
        if (url.ServerSelectionTimeout == MongoDefaults.ServerSelectionTimeout)
            configuracao.ServerSelectionTimeout = EsperaPeloServidor;

        return configuracao;
    }
}
