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
    public async Task GetSwaggerUi_ApiEmExecucao_RetornaOkComAPaginaHtml()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/swagger/index.html");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
    }
}
