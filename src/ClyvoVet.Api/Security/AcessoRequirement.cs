using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace ClyvoVet.Api.Security;

/// <summary>Autenticado e, se <see cref="PerfisPermitidos"/> não for vazio, com um desses perfis.</summary>
public class AcessoRequirement(params string[] perfisPermitidos) : IAuthorizationRequirement
{
    public IReadOnlyCollection<string> PerfisPermitidos { get; } = perfisPermitidos;
}

/// <summary>
/// Avalia o <see cref="AcessoRequirement"/>. Lê <c>Auth:ExigirToken</c> a cada requisição — não no
/// registro — para que a alavanca funcione com uma app setting, sem redeploy, e para que os testes
/// consigam trocá-la.
/// </summary>
public class AcessoHandler(IConfiguration configuracao) : AuthorizationHandler<AcessoRequirement>
{
    private const string ChaveDaAlavanca = "Auth:ExigirToken";

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext contexto, AcessoRequirement requisito)
    {
        if (!ExigirToken || Autorizado(contexto.User, requisito))
            contexto.Succeed(requisito);

        return Task.CompletedTask;
    }

    private static bool Autorizado(ClaimsPrincipal usuario, AcessoRequirement requisito) =>
        usuario.Identity?.IsAuthenticated == true
        && (requisito.PerfisPermitidos.Count == 0 || requisito.PerfisPermitidos.Any(usuario.IsInRole));

    /// <summary>
    /// Padrão <c>true</c>. A leitura é tolerante de propósito, como a do <c>EscopoDoTutor</c>:
    /// <c>GetValue&lt;bool&gt;</c> lança com "1", e o interruptor de emergência não pode derrubar a
    /// API que ele existe para salvar. Valor ilegível <b>exige</b> token — falha para o lado seguro.
    /// </summary>
    private bool ExigirToken
    {
        get
        {
            var valor = configuracao[ChaveDaAlavanca];
            if (string.IsNullOrWhiteSpace(valor)) return true;

            var limpo = valor.Trim();
            return bool.TryParse(limpo, out var booleano)
                ? booleano
                : limpo is not ("0" or "nao" or "no");
        }
    }
}
