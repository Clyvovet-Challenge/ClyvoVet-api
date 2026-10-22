using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ClyvoVet.Infrastructure.Mongo;

/// <summary>
/// Cria, no boot, o índice TTL de <c>validoAte</c> (<c>expireAfterSeconds: 0</c>: o documento some quando
/// a data chega). Criar de novo um índice idêntico não faz nada, então rodar a cada boot é seguro.
/// </summary>
/// <remarks>
/// <para>
/// <b>É higiene, não regra.</b> O monitor de TTL do Mongo roda a cada ~60 s, e o
/// <c>SaudePreditivaService</c> já confere <c>ValidoAte &gt; agora</c> no código. E como o <c>_id</c> é o
/// animal, a coleção nunca passa de um documento por animal — sem o índice ela só guardaria lixo velho.
/// Por isso <b>uma tentativa só</b>: com o Mongo fora do ar no boot, é um <c>Warning</c> e o índice nasce
/// no próximo boot.
/// </para>
/// <para>
/// É um <see cref="BackgroundService"/> (e não um <c>IHostedService</c> que faz I/O no
/// <c>StartAsync</c>) para que um Mongo lento ou ausente não atrase a subida da API.
/// </para>
/// </remarks>
public sealed class IndiceTtlDoParecerService : BackgroundService
{
    public const string NomeDoIndice = "ttl_valido_ate";

    private readonly IMongoDatabase _banco;
    private readonly ILogger<IndiceTtlDoParecerService> _logger;

    public IndiceTtlDoParecerService(IMongoDatabase banco, ILogger<IndiceTtlDoParecerService> logger)
    {
        _banco = banco;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var modelo = new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("validoAte"),
                new CreateIndexOptions { Name = NomeDoIndice, ExpireAfter = TimeSpan.Zero });

            await _banco.GetCollection<BsonDocument>(ParecerIaMongoRepository.NomeDaColecao)
                .Indexes.CreateOneAsync(modelo, cancellationToken: stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // A API foi encerrada no meio da tentativa; nada a avisar.
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Não foi possível criar o índice TTL de {Colecao}; o cache segue funcionando, só sem expiração automática até o próximo boot.",
                ParecerIaMongoRepository.NomeDaColecao);
        }
    }
}
