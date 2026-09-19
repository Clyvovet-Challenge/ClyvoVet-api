using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace ClyvoVet.Api.Security;

/// <summary>
/// Valida o access token que a API Java emite, para que esta API saiba QUEM está
/// chamando — e não apenas que a chamada traz a chave do aplicativo.
///
/// <para>
/// <b>Esta classe é incapaz de derrubar o boot, e isso é requisito, não estilo.</b>
/// A chave é montada no construtor dentro de <c>try</c>, e qualquer problema —
/// configuração ausente, valor que não é base64, chave curta demais — deixa o
/// validador <b>inerte</b> em vez de lançar. O motivo é concreto: hoje nenhuma
/// configuração desta API é capaz de impedir <c>app.Run()</c>, e introduzir a
/// primeira seria transformar um erro de digitação numa app setting em aplicação
/// que não sobe. Com o recorte desligado (o padrão), um segredo errado não tem
/// consequência nenhuma; um app morto tem.
/// </para>
/// </summary>
public class ValidadorDeTokenJwt
{
    /// <summary>Emissor e público que a API Java carimba nos tokens dela.</summary>
    private const string Emissor = "clyvovet-api-java";
    private const string Publico = "clyvovet";

    private const string ClaimTipo = "tipo";
    private const string ClaimTutorId = "tutorId";
    private const string TipoAccess = "access";

    /// <summary>HMAC-SHA256 exige 256 bits.</summary>
    private const int BytesMinimos = 32;

    private readonly TokenValidationParameters? _parametros;
    private readonly JwtSecurityTokenHandler _leitor = new();

    public ValidadorDeTokenJwt(IConfiguration configuracao, ILogger<ValidadorDeTokenJwt> log)
    {
        var segredo = configuracao["Jwt:Secret"];

        if (string.IsNullOrWhiteSpace(segredo))
        {
            // Estado normal enquanto a camada não está ligada. Information, não
            // Warning: não há nada errado aqui.
            log.LogInformation(
                "Jwt:Secret não configurado — validação de token desativada. " +
                "A autenticação continua sendo apenas a X-Api-Key.");
            _parametros = null;
            return;
        }

        byte[] chave;
        try
        {
            // BASE64, E NUNCA Encoding.UTF8.GetBytes
            // A API Java faz Keys.hmacShaKeyFor(Decoders.BASE64.decode(segredo)):
            // a chave são os bytes DECODIFICADOS. O idioma comum em ASP.NET é
            // UTF8.GetBytes(segredo), que produz uma chave DIFERENTE a partir do
            // MESMO valor de configuração — e então nenhuma assinatura confere e
            // o sintoma é 401 em cem por cento das chamadas, sem pista no log.
            // O teste ChaveDerivaDoBase64 trava esse contrato.
            chave = Convert.FromBase64String(segredo);
        }
        catch (FormatException)
        {
            log.LogError(
                "Jwt:Secret não é base64 válido — validação de token desativada. " +
                "O valor precisa ser o MESMO passado à API Java em JWT_SECRET, que o " +
                "decodifica de base64 antes de usar como chave HMAC.");
            _parametros = null;
            return;
        }

        if (chave.Length < BytesMinimos)
        {
            log.LogError(
                "Jwt:Secret decodifica para {Bytes} bytes, e HMAC-SHA256 exige ao menos {Minimo} — " +
                "validação de token desativada.", chave.Length, BytesMinimos);
            _parametros = null;
            return;
        }

        _parametros = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(chave),

            // A API Java carimba iss e aud de propósito, justamente porque estas
            // duas validações vêm ligadas por padrão aqui. Emitir os campos é
            // melhor do que desligar a verificação.
            ValidateIssuer = true,
            ValidIssuer = Emissor,
            ValidateAudience = true,
            ValidAudience = Publico,

            ValidateLifetime = true,
            // O padrão do .NET é 5 minutos, o que estenderia em um terço a vida de
            // um access token de 15. Os dois lados rodam com relógio de nuvem.
            ClockSkew = TimeSpan.FromSeconds(30),

            // Só HS256. Sem isto, a lista de algoritmos aceitos é a padrão.
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
        };

        log.LogInformation("Validação de token JWT ativa, emissor {Emissor}.", Emissor);
    }

    /// <summary>Se a camada está ligada. Falso quando não há segredo utilizável.</summary>
    public bool Ativo => _parametros is not null;

    /// <summary>
    /// Lê o token e devolve quem está chamando, ou <c>null</c> se ele não presta.
    /// Nunca lança: token inválido é resposta, não exceção.
    /// </summary>
    public IdentidadeDoChamador? TentarLer(string? token)
    {
        if (_parametros is null || string.IsNullOrWhiteSpace(token))
            return null;

        try
        {
            var principal = _leitor.ValidateToken(token, _parametros, out var validado);

            if (validado is not JwtSecurityToken jwt)
                return null;

            // SÓ ACCESS TOKEN
            // A API Java gera access e refresh pelo mesmo caminho: mesma chave,
            // mesmo subject, mesmo formato — só mudam a claim "tipo" e a
            // validade. Sem esta checagem, o refresh de SETE DIAS, que fica
            // guardado em disco no aparelho, viraria credencial válida aqui. E a
            // revogação por jti que a Java faz no logout não alcança esta API.
            var tipo = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTipo)?.Value;
            if (tipo != TipoAccess)
                return null;

            var usuarioId = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                            ?? jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;
            if (string.IsNullOrWhiteSpace(usuarioId))
                return null;

            return new IdentidadeDoChamador(
                UsuarioId: usuarioId,
                TutorId: jwt.Claims.FirstOrDefault(c => c.Type == ClaimTutorId)?.Value,
                Perfil: jwt.Claims.FirstOrDefault(c => c.Type == "perfil")?.Value);
        }
        catch (SecurityTokenException)
        {
            // Assinatura, emissor, público, validade, algoritmo: tudo cai aqui.
            return null;
        }
        catch (ArgumentException)
        {
            // Token malformado — nem chega a ser um JWT.
            return null;
        }
        catch (CryptographicException)
        {
            return null;
        }
    }
}
