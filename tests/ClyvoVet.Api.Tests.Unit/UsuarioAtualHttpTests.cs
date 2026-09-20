using System.Security.Claims;
using ClyvoVet.Api.Security;
using Microsoft.AspNetCore.Http;

namespace ClyvoVet.Api.Tests.Unit;

/// <summary>
/// A tradução de <c>HttpContext.User</c> para a identidade que a Application entende. É o único
/// ponto em que as claims da API Java viram <c>IdentidadeDoChamador</c>; o
/// <c>EscopoPorTutorEndpointsTests</c> continua sendo a rede de segurança ponta a ponta.
/// </summary>
public class UsuarioAtualHttpTests
{
    private static UsuarioAtualHttp Criar(ClaimsPrincipal? usuario, bool comHttpContext = true)
    {
        var acessor = new HttpContextAccessor();
        if (comHttpContext)
        {
            var contexto = new DefaultHttpContext();
            if (usuario is not null) contexto.User = usuario;
            acessor.HttpContext = contexto;
        }
        return new UsuarioAtualHttp(acessor);
    }

    /// <summary>Um principal autenticado, como o JwtBearer o entrega (claims sem mapeamento).</summary>
    private static ClaimsPrincipal Autenticado(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, authenticationType: "Bearer"));

    [Fact]
    public void Identidade_AccessTokenValido_DevolveUsuarioTutorEPerfil()
    {
        // Arrange
        var usuario = Autenticado(
            new Claim("sub", "11111111-1111-1111-1111-000000000001"),
            new Claim("tutorId", "44444444-4444-4444-4444-000000000001"),
            new Claim("perfil", "TUTOR"));

        // Act
        var identidade = Criar(usuario).Identidade;

        // Assert
        Assert.NotNull(identidade);
        Assert.Equal("11111111-1111-1111-1111-000000000001", identidade.UsuarioId);
        Assert.Equal("44444444-4444-4444-4444-000000000001", identidade.TutorId);
        Assert.Equal("TUTOR", identidade.Perfil);
    }

    [Fact]
    public void Identidade_TokenDeAdminSemTutorId_DevolveIdentidadeComTutorIdNulo()
    {
        // Arrange
        var usuario = Autenticado(new Claim("sub", "u1"), new Claim("perfil", "ADMIN"));

        // Act
        var identidade = Criar(usuario).Identidade;

        // Assert
        // O token é válido: o chamador É quem diz ser. O que ele não é, é um tutor — e quem recorta
        // por dono precisa negar, não liberar.
        Assert.NotNull(identidade);
        Assert.Null(identidade.TutorId);
        Assert.Equal("ADMIN", identidade.Perfil);
    }

    [Fact]
    public void Identidade_UsuarioNaoAutenticado_DevolveNulo()
    {
        // Arrange & Act & Assert
        Assert.Null(Criar(usuario: null).Identidade);
    }

    [Fact]
    public void Identidade_AutenticadoSemSub_DevolveNulo()
    {
        // Arrange
        var usuario = Autenticado(new Claim("perfil", "TUTOR"));

        // Act & Assert
        Assert.Null(Criar(usuario).Identidade);
    }

    [Fact]
    public void Identidade_SemHttpContext_DevolveNulo()
    {
        // Arrange & Act & Assert
        // Fora de uma requisição (um BackgroundService, por exemplo) não há quem identificar.
        Assert.Null(Criar(usuario: null, comHttpContext: false).Identidade);
    }
}
