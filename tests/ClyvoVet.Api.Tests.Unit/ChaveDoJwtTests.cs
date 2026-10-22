using System.Text;
using ClyvoVet.Api.Security;
using Microsoft.Extensions.Logging.Abstractions;

namespace ClyvoVet.Api.Tests.Unit;

/// <summary>
/// A derivação da chave — o contrato que quebra tudo em silêncio. Os dois lados assinam com
/// HMAC-SHA256 sobre o MESMO valor de configuração, mas precisam derivar a chave do mesmo jeito:
/// o modo errado não dá erro de compilação nem aviso, dá 401 em cem por cento das chamadas.
/// </summary>
public class ChaveDoJwtTests
{
    /// <summary>O mesmo segredo de <c>src/test/resources/application.properties</c> da API Java.</summary>
    private const string Segredo = "dGVzdGUtY2x5dm92ZXQtY2hhdmUtaG1hYy1zaGEyNTYtcGFyYS10ZXN0ZXM=";

    [Fact]
    public void ChaveDerivada_DoBase64_ProduzOsMesmosBytesQueAApiJava()
    {
        // Arrange & Act
        var bytes = Convert.FromBase64String(Segredo);

        // Assert
        // A API Java trava a mesma asserção em JwtServiceTest. Se um dos lados mudar a forma de
        // derivar, um destes dois testes cai.
        Assert.Equal("teste-clyvovet-chave-hmac-sha256-para-testes", Encoding.UTF8.GetString(bytes));
        Assert.True(bytes.Length >= 32);
    }

    [Fact]
    public void Ler_SegredoValido_DevolveAChaveComOsBytesDecodificados()
    {
        // Arrange & Act
        var chave = ChaveDoJwt.Ler(Segredo, NullLogger.Instance);

        // Assert
        Assert.NotNull(chave);
        Assert.Equal(Convert.FromBase64String(Segredo), chave.Key);
    }

    [Theory]
    [InlineData(null)]                      // app setting ausente
    [InlineData("")]                        // operador "desligou" setando vazio
    [InlineData("   ")]
    [InlineData("SUA_JWT_SECRET")]          // placeholder, no estilo do appsettings.json
    [InlineData("nao!eh!base64!")]          // fora do alfabeto base64
    [InlineData("Y3VydG8=")]                // base64 válido, mas 6 bytes: curto para HS256
    public void Ler_SegredoAusenteOuInvalido_DevolveNuloENaoLanca(string? segredo)
    {
        // Arrange & Act
        var chave = ChaveDoJwt.Ler(segredo, NullLogger.Instance);

        // Assert
        // Nenhuma configuração desta API é capaz de impedir app.Run(), e esta camada não pode ser
        // a primeira: um erro de digitação numa app setting não vira aplicação que não sobe.
        Assert.Null(chave);
    }
}
