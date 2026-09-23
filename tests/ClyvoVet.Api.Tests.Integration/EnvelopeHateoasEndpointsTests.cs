using System.Net;
using System.Text.Json;
using ClyvoVet.Api.Listagem;

namespace ClyvoVet.Api.Tests.Integration;

public class EnvelopeHateoasEndpointsTests : IClassFixture<HateoasFixture>
{
    private readonly HateoasFixture _fixture;
    private readonly HttpClient _client;

    public EnvelopeHateoasEndpointsTests(HateoasFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateClient();
    }

    private Task<HttpResponseMessage> GetAsync(string rota, string? accept)
    {
        var requisicao = new HttpRequestMessage(HttpMethod.Get, rota);
        if (accept is not null) requisicao.Headers.TryAddWithoutValidation("Accept", accept);
        return _client.SendAsync(requisicao);
    }

    private static async Task<JsonElement> CorpoAsync(HttpResponseMessage resposta) =>
        JsonDocument.Parse(await resposta.Content.ReadAsStringAsync()).RootElement.Clone();

    private static string[] Relacoes(JsonElement envelope) =>
        envelope.GetProperty("_links").EnumerateObject().Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray();

    private string Lembretes(int page, int pageSize) =>
        $"/api/v1/lembretes?animalId={_fixture.AnimalId}&page={page}&pageSize={pageSize}";

    [Fact]
    public async Task Lembretes_ComAcceptDoEnvelope_DevolveEnvelopeComOTipoDeMidiaCerto()
    {
        var resposta = await GetAsync(Lembretes(1, 2), PreferenciaDeEnvelope.TipoDeMidia);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal(PreferenciaDeEnvelope.TipoDeMidia, resposta.Content.Headers.ContentType!.MediaType);
        var envelope = await CorpoAsync(resposta);
        Assert.Equal(2, envelope.GetProperty("itens").GetArrayLength());
        Assert.Equal(1, envelope.GetProperty("page").GetInt32());
        Assert.Equal(2, envelope.GetProperty("pageSize").GetInt32());
        Assert.Equal(3, envelope.GetProperty("total").GetInt32());
        // Cada item continua com os próprios _links (T7).
        Assert.True(envelope.GetProperty("itens")[0].TryGetProperty("_links", out _));
    }

    [Fact]
    public async Task Envelope_PrimeiraPagina_TemNextMasNaoPrev()
    {
        var envelope = await CorpoAsync(await GetAsync(Lembretes(1, 1), PreferenciaDeEnvelope.TipoDeMidia));

        Assert.Equal(["first", "last", "next", "self"], Relacoes(envelope));
    }

    [Fact]
    public async Task Envelope_PaginaDoMeio_TemPrevENext()
    {
        var envelope = await CorpoAsync(await GetAsync(Lembretes(2, 1), PreferenciaDeEnvelope.TipoDeMidia));

        Assert.Equal(["first", "last", "next", "prev", "self"], Relacoes(envelope));
    }

    [Fact]
    public async Task Envelope_UltimaPagina_TemPrevMasNaoNext()
    {
        var envelope = await CorpoAsync(await GetAsync(Lembretes(3, 1), PreferenciaDeEnvelope.TipoDeMidia));

        Assert.Equal(["first", "last", "prev", "self"], Relacoes(envelope));
    }

    [Fact]
    public async Task Envelope_LinksPreservamOsFiltrosEUsamGet()
    {
        var envelope = await CorpoAsync(await GetAsync(Lembretes(1, 1), PreferenciaDeEnvelope.TipoDeMidia));

        var next = envelope.GetProperty("_links").GetProperty("next");
        Assert.Equal($"/api/v1/lembretes?animalId={_fixture.AnimalId}&page=2&pageSize=1", next.GetProperty("href").GetString());
        Assert.Equal("GET", next.GetProperty("method").GetString());
    }

    [Fact]
    public async Task Envelope_ContinuaMandandoOsCabecalhosDeNavegacao()
    {
        var resposta = await GetAsync(Lembretes(1, 2), PreferenciaDeEnvelope.TipoDeMidia);

        Assert.Equal("3", resposta.Headers.GetValues("X-Total-Count").Single());
        Assert.Contains("rel=\"next\"", resposta.Headers.GetValues("Link").Single());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("application/json")]
    [InlineData("*/*")]
    [InlineData("application/json, " + PreferenciaDeEnvelope.TipoDeMidia + ";q=0.9")]
    public async Task Lembretes_SemPedirOEnvelope_ContinuaArrayComOsCabecalhos(string? accept)
    {
        var resposta = await GetAsync(Lembretes(1, 2), accept);

        Assert.Equal("application/json", resposta.Content.Headers.ContentType!.MediaType);
        var corpo = await CorpoAsync(resposta);
        Assert.Equal(JsonValueKind.Array, corpo.ValueKind);
        Assert.Equal("3", resposta.Headers.GetValues("X-Total-Count").Single());
    }

    [Theory]
    [InlineData("/api/v1/eventos-pet")]
    [InlineData("/api/v1/produtos")]
    [InlineData("/api/v1/sugestoes-produto")]
    public async Task OsOutrosTresRecursos_TambemDevolvemOEnvelope(string rota)
    {
        var resposta = await GetAsync(rota, PreferenciaDeEnvelope.TipoDeMidia);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal(PreferenciaDeEnvelope.TipoDeMidia, resposta.Content.Headers.ContentType!.MediaType);
        var envelope = await CorpoAsync(resposta);
        Assert.Equal(1, envelope.GetProperty("total").GetInt32());
        Assert.Equal(1, envelope.GetProperty("itens").GetArrayLength());
    }
}
