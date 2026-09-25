using MongoDB.Driver;

namespace ClyvoVet.Api.Tests.Integration;

/// <summary>
/// Entrega bancos <b>descartáveis</b> (<c>clyvovet_teste_&lt;guid&gt;</c>) no Mongo apontado por
/// <c>MONGO_TEST_URI</c> e os apaga no fim: nunca encosta em dados de desenvolvimento.
/// </summary>
public sealed class MongoRealFixture : IDisposable
{
    private readonly IMongoClient? _cliente;
    private readonly List<string> _bancos = [];

    public MongoRealFixture()
    {
        var uri = Environment.GetEnvironmentVariable(FactRequiresMongoAttribute.Variavel);
        if (string.IsNullOrWhiteSpace(uri))
            return;

        var configuracao = MongoClientSettings.FromConnectionString(uri);
        configuracao.ServerSelectionTimeout = TimeSpan.FromSeconds(5);
        _cliente = new MongoClient(configuracao);
    }

    public IMongoDatabase NovoBanco()
    {
        var nome = $"clyvovet_teste_{Guid.NewGuid():N}";
        _bancos.Add(nome);
        return (_cliente ?? throw new InvalidOperationException("MONGO_TEST_URI não definida.")).GetDatabase(nome);
    }

    public void Dispose()
    {
        foreach (var nome in _bancos)
            _cliente?.DropDatabase(nome);
    }
}
