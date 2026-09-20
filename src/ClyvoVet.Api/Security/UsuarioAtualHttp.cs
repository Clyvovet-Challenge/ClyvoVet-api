using ClyvoVet.Api.Middleware;
using ClyvoVet.Application.Security;

namespace ClyvoVet.Api.Security;

/// <summary>
/// <see cref="IUsuarioAtual"/> sobre o <c>HttpContext</c>. Lê o que o
/// <c>IdentidadeMiddleware</c> guardou; na F2 passa a ler <c>HttpContext.User</c>.
/// </summary>
public class UsuarioAtualHttp(IHttpContextAccessor acessor) : IUsuarioAtual
{
    public IdentidadeDoChamador? Identidade => acessor.HttpContext?.ObterIdentidade();
}
