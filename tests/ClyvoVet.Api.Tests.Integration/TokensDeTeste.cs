using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace ClyvoVet.Api.Tests.Integration;

/// <summary>Monta access tokens como a API Java monta: chave = base64 DECODIFICADO.</summary>
public static class TokensDeTeste
{
    /// <summary>O mesmo segredo de <c>src/test/resources/application.properties</c> da API Java.</summary>
    public const string Segredo = "dGVzdGUtY2x5dm92ZXQtY2hhdmUtaG1hYy1zaGEyNTYtcGFyYS10ZXN0ZXM=";
    public const string Emissor = "clyvovet-api-java";
    public const string Publico = "clyvovet";

    public static string Access(
        string? tutorId = "44444444-4444-4444-4444-000000000001",
        string perfil = "TUTOR",
        string tipo = "access",
        string emissor = Emissor,
        string publico = Publico,
        int minutosDeVida = 15,
        byte[]? chaveCrua = null,
        bool comSub = true)
    {
        var chave = new SymmetricSecurityKey(chaveCrua ?? Convert.FromBase64String(Segredo));

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("perfil", perfil),
            new("tipo", tipo),
        };
        if (comSub) claims.Add(new Claim(JwtRegisteredClaimNames.Sub, "11111111-1111-1111-1111-000000000001"));
        if (tutorId is not null) claims.Add(new Claim("tutorId", tutorId));

        var agora = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            issuer: emissor,
            audience: publico,
            claims: claims,
            // nbf sempre antes do exp: com minutosDeVida negativo (token que nasce expirado) um nbf
            // fixo em -1 ficaria DEPOIS do exp, e o próprio construtor lançaria — a validação nem
            // seria exercida.
            notBefore: agora.AddMinutes(Math.Min(-1, minutosDeVida - 1)),
            expires: agora.AddMinutes(minutosDeVida),
            signingCredentials: new SigningCredentials(chave, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
