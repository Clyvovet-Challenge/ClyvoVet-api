using ClyvoVet.Api.Security;

namespace ClyvoVet.Api.Middleware;

/// <summary>
/// Lê o header <c>Authorization: Bearer</c>, quando houver, e guarda quem está
/// chamando em <see cref="HttpContext.Items"/>.
///
/// <para>
/// <b>Não rejeita nada.</b> Token ausente, expirado ou de outra chave passam
/// direto — só não deixam identidade para trás. Quem decide negar é a camada de
/// escopo, e ela só age nos controllers que a usam.
/// </para>
///
/// <para>
/// Isso é deliberado, e é o que permite ligar a validação sem risco: o middleware
/// atravessa <c>/health</c>, <c>/metrics</c>, <c>/swagger</c> e os webhooks do
/// Telegram sem tocar em nenhum deles. Uma <c>FallbackPolicy</c> com
/// <c>[Authorize]</c> global faria o oposto — derrubaria os quatro de uma vez, e o
/// health check quebrado tira a aplicação de rotação no App Service.
/// </para>
/// </summary>
public class IdentidadeMiddleware(RequestDelegate proximo)
{
    private const string Header = "Authorization";
    private const string Prefixo = "Bearer ";

    /// <summary>Chave em <c>HttpContext.Items</c>. Use <see cref="IdentidadeHttpContextExtensions"/>.</summary>
    internal const string ChaveDoItem = "clyvovet:identidade";

    public async Task InvokeAsync(HttpContext contexto, ValidadorDeTokenJwt validador)
    {
        // Nem lê o header quando a camada está desligada — o caminho fica
        // idêntico ao de antes de ela existir.
        if (validador.Ativo)
        {
            var cabecalho = contexto.Request.Headers[Header].ToString();
            if (cabecalho.StartsWith(Prefixo, StringComparison.OrdinalIgnoreCase))
            {
                var identidade = validador.TentarLer(cabecalho[Prefixo.Length..].Trim());
                if (identidade is not null)
                    contexto.Items[ChaveDoItem] = identidade;
            }
        }

        await proximo(contexto);
    }
}

public static class IdentidadeHttpContextExtensions
{
    /// <summary>
    /// Quem está chamando, ou <c>null</c> se o pedido não trouxe token válido.
    /// </summary>
    public static IdentidadeDoChamador? ObterIdentidade(this HttpContext contexto) =>
        contexto.Items.TryGetValue(IdentidadeMiddleware.ChaveDoItem, out var valor)
            ? valor as IdentidadeDoChamador
            : null;
}
