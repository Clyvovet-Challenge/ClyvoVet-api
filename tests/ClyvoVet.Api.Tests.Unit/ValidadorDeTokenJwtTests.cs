using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ClyvoVet.Api.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;

namespace ClyvoVet.Api.Tests.Unit;

/// <summary>
/// O contrato entre esta API e a API Java, travado por teste.
///
/// <para>
/// Os dois lados assinam com HMAC-SHA256 sobre o MESMO valor de configuração, mas
/// derivam a chave de formas que precisam coincidir — e o modo errado não dá erro
/// de compilação nem aviso: dá <b>401 em cem por cento das chamadas</b>, em
/// produção, sem pista no log. Metade destes testes existe só para isso.
/// </para>
/// </summary>
public class ValidadorDeTokenJwtTests
{
    /// <summary>O mesmo segredo de <c>src/test/resources/application.properties</c> da API Java.</summary>
    private const string Segredo = "dGVzdGUtY2x5dm92ZXQtY2hhdmUtaG1hYy1zaGEyNTYtcGFyYS10ZXN0ZXM=";

    private const string Emissor = "clyvovet-api-java";
    private const string Publico = "clyvovet";

    private static ValidadorDeTokenJwt Validador(string? segredo = Segredo)
    {
        var configuracao = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Secret"] = segredo })
            .Build();

        return new ValidadorDeTokenJwt(configuracao, NullLogger<ValidadorDeTokenJwt>.Instance);
    }

    /// <summary>Monta um token como a API Java monta: chave = base64 DECODIFICADO.</summary>
    private static string TokenComoAJava(
        string tipo = "access",
        string? tutorId = "44444444-4444-4444-4444-000000000001",
        string perfil = "TUTOR",
        string emissor = Emissor,
        string publico = Publico,
        int minutosDeVida = 15,
        byte[]? chaveCrua = null)
    {
        var chave = new SymmetricSecurityKey(chaveCrua ?? Convert.FromBase64String(Segredo));

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, "11111111-1111-1111-1111-000000000001"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("perfil", perfil),
            new("tipo", tipo),
        };
        if (tutorId is not null)
            claims.Add(new Claim("tutorId", tutorId));

        var agora = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            issuer: emissor,
            audience: publico,
            claims: claims,
            // nbf sempre antes do exp: com minutosDeVida negativo (token que
            // nasce expirado) um nbf fixo em -1 ficaria DEPOIS do exp, e o
            // proprio construtor lancaria — a validacao nem seria exercida.
            notBefore: agora.AddMinutes(Math.Min(-1, minutosDeVida - 1)),
            expires: agora.AddMinutes(minutosDeVida),
            signingCredentials: new SigningCredentials(chave, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // ================================================================
    // A derivação da chave — o contrato que quebra tudo em silêncio
    // ================================================================

    [Fact]
    public void ChaveDerivada_DoBase64_ProduzOsMesmosBytesQueAApiJava()
    {
        // Arrange & Act
        var bytes = Convert.FromBase64String(Segredo);

        // Assert
        // A API Java trava a mesma asserção em JwtServiceTest: o segredo
        // decodifica exatamente para esta frase. Se um dos dois lados mudar a
        // forma de derivar, um destes dois testes cai.
        Assert.Equal("teste-clyvovet-chave-hmac-sha256-para-testes", Encoding.UTF8.GetString(bytes));
        Assert.True(bytes.Length >= 32);
    }

    [Fact]
    public void TentarLer_TokenAssinadoComChaveDerivadaDeUtf8_Rejeita()
    {
        // Arrange
        // É EXATAMENTE o bug que este arquivo existe para impedir. Todo tutorial
        // de ASP.NET escreve Encoding.UTF8.GetBytes(config["Jwt:Secret"]). Com o
        // mesmo valor de app setting, a chave resultante é OUTRA, e nenhuma
        // assinatura confere.
        var chaveErrada = Encoding.UTF8.GetBytes(Segredo);
        var token = TokenComoAJava(chaveCrua: chaveErrada);

        // Act
        var identidade = Validador().TentarLer(token);

        // Assert
        Assert.Null(identidade);
    }

    // ================================================================
    // Leitura do token válido
    // ================================================================

    [Fact]
    public void TentarLer_AccessTokenValido_DevolveUsuarioTutorEPerfil()
    {
        // Arrange & Act
        var identidade = Validador().TentarLer(TokenComoAJava());

        // Assert
        Assert.NotNull(identidade);
        Assert.Equal("11111111-1111-1111-1111-000000000001", identidade.UsuarioId);
        Assert.Equal("44444444-4444-4444-4444-000000000001", identidade.TutorId);
        Assert.Equal("TUTOR", identidade.Perfil);
    }

    [Fact]
    public void TentarLer_TokenDeAdminSemTutorId_DevolveIdentidadeComTutorIdNulo()
    {
        // Arrange & Act
        var identidade = Validador().TentarLer(TokenComoAJava(tutorId: null, perfil: "ADMIN"));

        // Assert
        // O token é válido: o chamador É quem diz ser. O que ele não é, é um
        // tutor — e quem recorta por dono precisa negar, não liberar.
        Assert.NotNull(identidade);
        Assert.Null(identidade.TutorId);
        Assert.Equal("ADMIN", identidade.Perfil);
    }

    // ================================================================
    // O que precisa ser recusado
    // ================================================================

    [Fact]
    public void TentarLer_RefreshToken_Rejeita()
    {
        // Arrange & Act
        var identidade = Validador().TentarLer(TokenComoAJava(tipo: "refresh"));

        // Assert
        // O refresh dura 7 dias, fica em disco no aparelho, e a revogação por jti
        // que a Java faz no logout não chega até aqui. Aceitá-lo seria trocar uma
        // credencial de 15 minutos por uma de uma semana.
        Assert.Null(identidade);
    }

    [Fact]
    public void TentarLer_TokenExpirado_Rejeita()
    {
        // Arrange & Act
        var identidade = Validador().TentarLer(TokenComoAJava(minutosDeVida: -10));

        // Assert
        Assert.Null(identidade);
    }

    [Theory]
    [InlineData("outro-emissor", Publico)]
    [InlineData(Emissor, "outro-publico")]
    public void TentarLer_EmissorOuPublicoErrado_Rejeita(string emissor, string publico)
    {
        // Arrange & Act
        var identidade = Validador().TentarLer(TokenComoAJava(emissor: emissor, publico: publico));

        // Assert
        Assert.Null(identidade);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("nao-e-um-jwt")]
    [InlineData("a.b.c")]
    public void TentarLer_EntradaInvalida_DevolveNuloSemLancar(string? token)
    {
        // Arrange & Act & Assert
        Assert.Null(Validador().TentarLer(token));
    }

    // ================================================================
    // Incapaz de derrubar o boot — o requisito que veio da revisão de deploy
    // ================================================================

    [Theory]
    [InlineData(null)]                      // app setting ausente
    [InlineData("")]                        // operador "desligou" setando vazio
    [InlineData("   ")]
    [InlineData("SUA_JWT_SECRET")]          // placeholder, no estilo do appsettings.json
    [InlineData("nao!eh!base64!")]          // fora do alfabeto base64
    [InlineData("Y3VydG8=")]                // base64 válido, mas 6 bytes: curto para HS256
    public void Construtor_SegredoAusenteOuInvalido_FicaInerteENaoLanca(string? segredo)
    {
        // Arrange & Act
        var validador = Validador(segredo);

        // Assert
        // Nenhuma configuração desta API é capaz de impedir app.Run() hoje, e esta
        // camada não pode ser a primeira. Um erro de digitação numa app setting
        // não pode virar aplicação que não sobe: com o recorte desligado, um
        // segredo errado não tem consequência nenhuma; um app morto tem.
        Assert.False(validador.Ativo);
        Assert.Null(validador.TentarLer(TokenComoAJava()));
    }

    [Fact]
    public void Ativo_ComSegredoValido_EhVerdadeiro()
    {
        // Arrange & Act & Assert
        Assert.True(Validador().Ativo);
    }
}
