using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClyvoVet.Application.DTOs.Request;
using ClyvoVet.Domain.Enums;

namespace ClyvoVet.Api.Tests.Integration;

public class HateoasEndpointsTests : IClassFixture<HateoasFixture>
{
    private readonly HateoasFixture _fixture;
    private readonly HttpClient _client;

    public HateoasEndpointsTests(HateoasFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateClient();
    }

    private async Task<JsonElement> LerAsync(string rota)
    {
        var resposta = await _client.GetAsync(rota);
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        return JsonDocument.Parse(await resposta.Content.ReadAsStringAsync()).RootElement.Clone();
    }

    private static JsonElement Primeiro(JsonElement lista) => lista.EnumerateArray().First();

    private static void AssertRelacoes(JsonElement item, params string[] esperadas)
    {
        var atuais = item.GetProperty("_links").EnumerateObject().Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal);
        Assert.Equal(esperadas.OrderBy(n => n, StringComparer.Ordinal), atuais);
    }

    private static void AssertLink(JsonElement item, string relacao, string metodo, string href)
    {
        var link = item.GetProperty("_links").GetProperty(relacao);
        Assert.Equal(href, link.GetProperty("href").GetString());
        Assert.Equal(metodo, link.GetProperty("method").GetString());
    }

    [Fact]
    public async Task Lembretes_Lista_CadaItemTemLinks()
    {
        var lista = await LerAsync($"/api/v1/lembretes?animalId={_fixture.AnimalId}");

        // >= e não == 3: a fixture é compartilhada entre os testes da classe (mesmo banco em
        // memória), e Post_Lembrete_NaoRecebeLinks cria um quarto lembrete no mesmo animal. O que
        // este teste prova é a forma do _links, não a contagem — essa já é coberta pelos testes de
        // paginação das tarefas anteriores.
        Assert.True(lista.GetArrayLength() >= 3);
        foreach (var item in lista.EnumerateArray())
            AssertRelacoes(item, "self", "atualizar", "excluir", "colecao", "saudePreditiva");

        var primeiro = Primeiro(lista);
        var id = primeiro.GetProperty("id").GetString();
        AssertLink(primeiro, "self", "GET", $"/api/v1/lembretes/{id}");
        AssertLink(primeiro, "atualizar", "PUT", $"/api/v1/lembretes/{id}");
        AssertLink(primeiro, "excluir", "DELETE", $"/api/v1/lembretes/{id}");
        AssertLink(primeiro, "colecao", "GET", "/api/v1/lembretes");
        AssertLink(primeiro, "saudePreditiva", "GET", $"/api/v1/saude-preditiva/{_fixture.AnimalId}");
    }

    [Fact]
    public async Task Lembrete_PorId_TemLinks()
    {
        var lista = await LerAsync($"/api/v1/lembretes?animalId={_fixture.AnimalId}");
        var id = Primeiro(lista).GetProperty("id").GetString();

        var item = await LerAsync($"/api/v1/lembretes/{id}");

        AssertRelacoes(item, "self", "atualizar", "excluir", "colecao", "saudePreditiva");
        AssertLink(item, "self", "GET", $"/api/v1/lembretes/{id}");
    }

    [Fact]
    public async Task EventoPet_ListaEPorId_TemLinks()
    {
        var lista = await LerAsync("/api/v1/eventos-pet");
        AssertRelacoes(Primeiro(lista), "self", "atualizar", "excluir", "colecao");

        var item = await LerAsync($"/api/v1/eventos-pet/{_fixture.EventoId}");
        AssertLink(item, "self", "GET", $"/api/v1/eventos-pet/{_fixture.EventoId}");
        AssertLink(item, "colecao", "GET", "/api/v1/eventos-pet");
    }

    [Fact]
    public async Task Produto_ListaEPorId_TemLinks()
    {
        var lista = await LerAsync("/api/v1/produtos");
        AssertRelacoes(Primeiro(lista), "self", "atualizar", "excluir", "colecao");

        var item = await LerAsync($"/api/v1/produtos/{_fixture.ProdutoId}");
        AssertLink(item, "atualizar", "PUT", $"/api/v1/produtos/{_fixture.ProdutoId}");
        AssertLink(item, "excluir", "DELETE", $"/api/v1/produtos/{_fixture.ProdutoId}");
    }

    [Fact]
    public async Task SugestaoProduto_ListaEPorId_TemLinksInclusiveParaASaudePreditiva()
    {
        var lista = await LerAsync("/api/v1/sugestoes-produto");
        AssertRelacoes(Primeiro(lista), "self", "atualizar", "excluir", "colecao", "saudePreditiva");

        var item = await LerAsync($"/api/v1/sugestoes-produto/{_fixture.SugestaoId}");
        AssertLink(item, "saudePreditiva", "GET", $"/api/v1/saude-preditiva/{_fixture.AnimalId}");
    }

    [Fact]
    public async Task SaudePreditiva_PorAnimal_TemLinksParaWidgetLembretesESugestoes()
    {
        // Esta rota usa só o banco em memória e o fallback determinístico (sem OCI configurada).
        var item = await LerAsync($"/api/v1/saude-preditiva/{_fixture.AnimalId}");

        AssertRelacoes(item, "self", "widget", "lembretes", "sugestoes");
        AssertLink(item, "self", "GET", $"/api/v1/saude-preditiva/{_fixture.AnimalId}");
        AssertLink(item, "widget", "GET", $"/api/v1/widget-saude-preditiva/{_fixture.AnimalId}");
        AssertLink(item, "lembretes", "GET", $"/api/v1/lembretes?animalId={_fixture.AnimalId}");
        AssertLink(item, "sugestoes", "GET", $"/api/v1/sugestoes-produto?animalId={_fixture.AnimalId}");
    }

    [Fact]
    public async Task Widget_PorAnimal_TemLinksParaSiEParaASaudePreditiva()
    {
        var item = await LerAsync($"/api/v1/widget-saude-preditiva/{_fixture.AnimalId}");

        AssertRelacoes(item, "self", "saudePreditiva");
        AssertLink(item, "saudePreditiva", "GET", $"/api/v1/saude-preditiva/{_fixture.AnimalId}");
    }

    [Fact]
    public async Task Telegram_GerarLink_TemLinksParaSiEParaOVinculo()
    {
        var item = await LerAsync($"/api/v1/telegram/link/{_fixture.TutorId}");

        AssertRelacoes(item, "self", "vinculo");
        AssertLink(item, "self", "GET", $"/api/v1/telegram/link/{_fixture.TutorId}");
        AssertLink(item, "vinculo", "GET", $"/api/v1/telegram/vinculo/{_fixture.TutorId}");
    }

    [Fact]
    public async Task Telegram_Vinculo_TemLinksParaLinkEDesvincular()
    {
        var item = await LerAsync($"/api/v1/telegram/vinculo/{_fixture.TutorId}");

        AssertRelacoes(item, "self", "link", "desvincular");
        AssertLink(item, "link", "GET", $"/api/v1/telegram/link/{_fixture.TutorId}");
        AssertLink(item, "desvincular", "DELETE", $"/api/v1/telegram/vinculo/{_fixture.TutorId}");
    }

    [Fact]
    public async Task Lista_SemAcceptEspecial_ContinuaArrayComOsCamposAntigosIntactos()
    {
        // Contrato com o app: o corpo é array e cada item mantém os campos que o app já lê.
        var resposta = await _client.GetAsync($"/api/v1/lembretes?animalId={_fixture.AnimalId}");

        Assert.Equal("application/json", resposta.Content.Headers.ContentType!.MediaType);
        var lista = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(JsonValueKind.Array, lista.ValueKind);
        var item = lista.EnumerateArray().First();
        foreach (var campo in new[] { "id", "animalId", "nomeAnimal", "titulo", "tipo", "agendadoEm", "recorrente", "status", "criadoEm" })
            Assert.True(item.TryGetProperty(campo, out _), $"O campo '{campo}' sumiu do item.");
    }

    [Fact]
    public async Task Post_Lembrete_NaoRecebeLinks()
    {
        // Só os GET de consulta ganham _links (design §6.4): o POST responde exatamente como antes.
        var resposta = await _client.PostAsJsonAsync("/api/v1/lembretes", new LembreteRequest
        {
            AnimalId = _fixture.AnimalId,
            Titulo = "Novo lembrete",
            Tipo = TipoLembreteEnum.Consulta,
            AgendadoEm = DateTime.UtcNow.AddDays(20),
        });

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync()).RootElement;
        Assert.False(corpo.TryGetProperty("_links", out _));
    }
}
