using ClyvoVet.Api.Listagem;
using Microsoft.AspNetCore.Http;

namespace ClyvoVet.Api.Tests.Unit;

public class PreferenciaDeEnvelopeTests
{
    private const string Vnd = "application/vnd.clyvovet.hateoas+json";

    private static bool Pede(string? accept)
    {
        var contexto = new DefaultHttpContext();
        if (accept is not null) contexto.Request.Headers.Accept = accept;
        return PreferenciaDeEnvelope.Pede(contexto.Request);
    }

    [Theory]
    [InlineData(null, false)]                                            // o app móvel de hoje não manda nada especial
    [InlineData("application/json", false)]
    [InlineData("*/*", false)]
    [InlineData(Vnd, true)]
    [InlineData("APPLICATION/VND.CLYVOVET.HATEOAS+JSON", true)]          // tipo de mídia não distingue caixa
    [InlineData("application/json, " + Vnd + ";q=0.9", false)]           // prefere JSON puro
    [InlineData(Vnd + ", application/json;q=0.5", true)]                 // prefere o envelope
    [InlineData("application/json, " + Vnd, true)]                       // empate: pediu explicitamente
    [InlineData(Vnd + ";q=0", false)]                                    // q=0 significa "não aceito"
    public void Pede_DadoOAcceptDaRequisicao_EscolheOEnvelopeSoQuandoOClienteOPrefere(string? accept, bool esperado)
    {
        Assert.Equal(esperado, Pede(accept));
    }
}
