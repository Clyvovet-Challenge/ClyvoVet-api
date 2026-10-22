namespace ClyvoVet.Api.Tests.Integration;

/// <summary>
/// Um <c>[Fact]</c> que só roda com um MongoDB real. Sem <c>MONGO_TEST_URI</c> ele aparece como
/// <b>Skipped</b> no relatório, com a razão — e não como verde falso nem como falha.
/// </summary>
public sealed class FactRequiresMongoAttribute : FactAttribute
{
    public const string Variavel = "MONGO_TEST_URI";

    public FactRequiresMongoAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(Variavel)))
            Skip = $"Requer um MongoDB real: defina {Variavel} (ex.: mongodb://localhost:27017). Ver README, seção NoSQL.";
    }
}
