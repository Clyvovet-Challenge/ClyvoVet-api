using System.Net;
using System.Text.Json;

namespace ClyvoVet.Api.Tests.Integration;

/// <summary>
/// O Swagger é entregável avaliado — o professor testa a API por ele, sem cliente
/// HTTP externo (ver o comentário no <c>Program.cs</c>, "Swagger sempre ativo").
/// Mesmo assim ele não tinha teste nenhum, e a geração do documento é justamente o
/// que quebra em silêncio: ela só acontece quando alguém pede
/// <c>/swagger/v1/swagger.json</c>, e uma falha ali é 500 em tempo de execução, não
/// erro de compilação.
///
/// O gatilho concreto para escrever isto foi subir o <c>Microsoft.OpenApi</c> de
/// 2.4.1 para 2.12.2 (GHSA-v5pm-xwqc-g5wc). O <see cref="Api.Swagger.ApiKeySecurityDocumentFilter"/>
/// programa contra a API desse pacote — <c>document.Paths</c>,
/// <c>pathItem.Operations</c>, <c>OpenApiSecuritySchemeReference</c> — então é o
/// ponto exato onde um salto de versão apareceria. Compilar não provava nada.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class SwaggerEndpointsTests
{
    private readonly HttpClient _client;

    public SwaggerEndpointsTests(IntegrationTestFixture fixture)
    {
        _client = fixture.CreateClient();
    }

    [Fact]
    public async Task GetSwaggerJson_ApiEmExecucao_RetornaOkComDocumentoOpenApiValido()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var documento = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(documento.RootElement.TryGetProperty("openapi", out _));
        Assert.True(documento.RootElement.TryGetProperty("paths", out var paths));
        Assert.NotEmpty(paths.EnumerateObject());
    }

    [Fact]
    public async Task GetSwaggerJson_RotaProtegidaPorApiKey_DeclaraORequisitoDeSeguranca()
    {
        // Arrange
        // /api/v1/lembretes carrega o ApiKeyFilterAttribute com "Api:ApiKey", que é a
        // condição que o ApiKeySecurityDocumentFilter procura para anexar o security
        // requirement. Se o filtro deixar de rodar, o Swagger continua respondendo 200
        // e o botão "Authorize" simplesmente não aparece — falha silenciosa.
        var response = await _client.GetAsync("/swagger/v1/swagger.json");
        var documento = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        // Act
        var operacaoGet = documento.RootElement
            .GetProperty("paths")
            .GetProperty("/api/v1/lembretes")
            .GetProperty("get");

        // Assert
        Assert.True(operacaoGet.TryGetProperty("security", out var seguranca));
        Assert.Contains(
            seguranca.EnumerateArray(),
            requisito => requisito.TryGetProperty("ApiKey", out _));
    }

    [Fact]
    public async Task GetSwaggerJson_Documento_DeclaraOEsquemaBearer()
    {
        // Arrange
        var resposta = await _client.GetAsync("/swagger/v1/swagger.json");
        var documento = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());

        // Act
        var bearer = documento.RootElement
            .GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");

        // Assert
        Assert.Equal("http", bearer.GetProperty("type").GetString());
        Assert.Equal("bearer", bearer.GetProperty("scheme").GetString());
        Assert.Equal("JWT", bearer.GetProperty("bearerFormat").GetString());
    }

    [Fact]
    public async Task GetSwaggerJson_RotaComAuthorize_ExigeApiKeyEBearerJuntos()
    {
        // Arrange
        var resposta = await _client.GetAsync("/swagger/v1/swagger.json");
        var documento = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        var paths = documento.RootElement.GetProperty("paths");

        // Act
        var requisito = paths.GetProperty("/api/v1/lembretes").GetProperty("get")
            .GetProperty("security")[0];
        var telegram = paths.GetProperty("/api/v1/telegram/enviar").GetProperty("post");

        // Assert
        // Um ÚNICO requisito com os dois esquemas é E (as duas credenciais); dois requisitos seriam OU.
        Assert.True(requisito.TryGetProperty("ApiKey", out _));
        Assert.True(requisito.TryGetProperty("Bearer", out _));
        // O Telegram não tem [Authorize]: não ganha o cadeado do Bearer.
        Assert.False(telegram.TryGetProperty("security", out var seguranca) && seguranca.ToString().Contains("Bearer"));
    }

    [Fact]
    public async Task GetSwaggerUi_ApiEmExecucao_RetornaOkComAPaginaHtml()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/swagger/index.html");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
    }

    /// <summary>
    /// Os comentários <c>///</c> dos DTOs aparecem no Swagger.
    ///
    /// <para>
    /// O Swashbuckle só enxerga o XML que mandarem incluir. Enquanto os DTOs moram no
    /// mesmo projeto dos controllers, um arquivo basta; ao movê-los para outro projeto, o
    /// XML dele precisa entrar também — senão o Swagger continua respondendo 200 e a
    /// descrição dos campos some em silêncio.
    /// </para>
    /// </summary>
    [Fact]
    public async Task GetSwaggerJson_DtoComComentarioXml_ExpoeADescricaoDoCampo()
    {
        // Arrange
        // "repete sem fim previsto" é o <summary> de LembreteRequest.RepetirAte. Não serve
        // um campo enum, como ProdutoRequest.PorteIndicado: ele sai como $ref para o
        // enum, e o OpenAPI 3.0 descarta a descrição escrita ao lado de um $ref.
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        // Act
        var json = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Contains("repete sem fim previsto", json);
    }
}
