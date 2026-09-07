using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ClyvoVet.Api.Filters;

public class ApiKeyFilterAttribute : IActionFilter
{
    private const string HeaderName = "X-Api-Key";
    private readonly IConfiguration _configuration;
    private readonly string _configKey;

    public ApiKeyFilterAttribute(IConfiguration configuration, string configKey)
    {
        _configuration = configuration;
        _configKey = configKey;
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        var apiKeyEsperada = _configuration[_configKey];

        // FALHA FECHADA QUANDO A CHAVE NAO ESTA CONFIGURADA
        // Se Api__ApiKey nao chegar no App Service, apiKeyEsperada vem nula. Antes a
        // comparacao seguia assim mesmo, e o resultado passava a depender de detalhe
        // de conversao do StringValues -- um jeito ruim de descobrir que a app
        // setting faltou. Sem chave configurada, ninguem entra.
        if (string.IsNullOrEmpty(apiKeyEsperada))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var apiKeyRecebida) ||
            !ChavesConferem(apiKeyRecebida.ToString(), apiKeyEsperada))
        {
            context.Result = new UnauthorizedResult();
        }
    }

    /// <summary>
    /// Compara em tempo fixo. O <c>!=</c> entre strings para na primeira diferenca,
    /// entao o tempo de resposta cresce junto com o tamanho do prefixo correto — em
    /// tese da para descobrir a chave caractere a caractere, medindo. Fechar isso
    /// custa uma linha; deixar aberto e que precisaria de justificativa.
    ///
    /// O tamanho continua vazando: <c>FixedTimeEquals</c> devolve <c>false</c> na
    /// hora quando os buffers tem tamanhos diferentes. E o comportamento padrao da
    /// primitiva, e conhecer o tamanho de uma chave aleatoria nao ajuda a adivinha-la.
    /// </summary>
    private static bool ChavesConferem(string recebida, string esperada)
    {
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(recebida),
            Encoding.UTF8.GetBytes(esperada));
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
