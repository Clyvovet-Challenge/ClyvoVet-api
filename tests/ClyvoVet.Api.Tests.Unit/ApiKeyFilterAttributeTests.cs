using ClyvoVet.Api.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;

namespace ClyvoVet.Api.Tests.Unit;

/// <summary>
/// O <see cref="ApiKeyFilterAttribute"/> é a única barreira entre a internet e os
/// endpoints de Produto, Lembrete, EventoPet e Sugestão de Produto. Os testes de
/// integração já cobrem chave ausente e chave errada; o que faltava aqui é o caso em
/// que a <b>configuração</b> falha — a app setting <c>Api__ApiKey</c> não chegar no
/// App Service. Esse cenário não aparece em teste de integração, porque lá o
/// <c>appsettings.json</c> sempre traz a chave.
/// </summary>
public class ApiKeyFilterAttributeTests
{
    private const string ChaveConfigurada = "chave-de-teste-do-filtro";
    private const string CaminhoDaChave = "Api:ApiKey";

    private static ActionExecutingContext ContextoCom(string? valorDoHeader)
    {
        var httpContext = new DefaultHttpContext();
        if (valorDoHeader is not null)
            httpContext.Request.Headers["X-Api-Key"] = valorDoHeader;

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            controller: new object());
    }

    private static ApiKeyFilterAttribute FiltroCom(string? chaveConfigurada)
    {
        var configuracao = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [CaminhoDaChave] = chaveConfigurada })
            .Build();

        return new ApiKeyFilterAttribute(configuracao, CaminhoDaChave);
    }

    [Fact]
    public void OnActionExecuting_ChaveCorreta_DeixaPassar()
    {
        // Arrange
        var filtro = FiltroCom(ChaveConfigurada);
        var contexto = ContextoCom(ChaveConfigurada);

        // Act
        filtro.OnActionExecuting(contexto);

        // Assert
        Assert.Null(contexto.Result);
    }

    [Theory]
    [InlineData(null)]                              // header ausente
    [InlineData("")]                                // header vazio
    [InlineData("chave-errada")]                    // valor diferente
    [InlineData("chave-de-teste-do-filtro-a-mais")] // prefixo correto, mais longo
    [InlineData("chave-de-teste-do-filtr")]         // prefixo correto, mais curto
    public void OnActionExecuting_ChaveInvalida_RetornaUnauthorized(string? valorDoHeader)
    {
        // Arrange
        var filtro = FiltroCom(ChaveConfigurada);
        var contexto = ContextoCom(valorDoHeader);

        // Act
        filtro.OnActionExecuting(contexto);

        // Assert
        Assert.IsType<UnauthorizedResult>(contexto.Result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void OnActionExecuting_ChaveNaoConfigurada_RetornaUnauthorizedMesmoComHeaderIgual(string? chaveConfigurada)
    {
        // Arrange
        // Simula a app setting Api__ApiKey faltando no App Service. O filtro tem de
        // falhar fechado: sem chave configurada, ninguém entra — nem quem mandar um
        // header que "casa" com o vazio.
        var filtro = FiltroCom(chaveConfigurada);
        var contexto = ContextoCom(chaveConfigurada);

        // Act
        filtro.OnActionExecuting(contexto);

        // Assert
        Assert.IsType<UnauthorizedResult>(contexto.Result);
    }
}
