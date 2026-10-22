using ClyvoVet.Application.Security;

namespace ClyvoVet.Api.Security;

/// <summary>
/// <see cref="IUsuarioAtual"/> sobre o <c>HttpContext.User</c>, que o JwtBearer preenche. Só
/// existe identidade para access token válido: o <c>tipo = access</c> e o <c>sub</c> são exigidos
/// na própria autenticação (ver <c>AutenticacaoJwtExtensions</c>), então um refresh token nunca
/// chega aqui como usuário.
/// </summary>
public class UsuarioAtualHttp(IHttpContextAccessor acessor) : IUsuarioAtual
{
    public IdentidadeDoChamador? Identidade
    {
        get
        {
            var usuario = acessor.HttpContext?.User;
            if (usuario?.Identity?.IsAuthenticated != true)
                return null;

            var usuarioId = usuario.FindFirst(ClaimsDoToken.UsuarioId)?.Value;
            if (string.IsNullOrWhiteSpace(usuarioId))
                return null;

            return new IdentidadeDoChamador(
                UsuarioId: usuarioId,
                TutorId: usuario.FindFirst(ClaimsDoToken.TutorId)?.Value,
                Perfil: usuario.FindFirst(ClaimsDoToken.Perfil)?.Value);
        }
    }
}
