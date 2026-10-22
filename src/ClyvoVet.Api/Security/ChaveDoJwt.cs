using Microsoft.IdentityModel.Tokens;

namespace ClyvoVet.Api.Security;

/// <summary>
/// Monta a chave HMAC a partir de <c>Jwt:Secret</c>.
///
/// <para>
/// <b>É incapaz de derrubar o boot, e isso é requisito, não estilo.</b> Qualquer problema —
/// configuração ausente, valor que não é base64, chave curta demais — devolve <c>null</c> e
/// registra o motivo, em vez de lançar. Hoje nenhuma configuração desta API impede
/// <c>app.Run()</c>, e introduzir a primeira seria transformar um erro de digitação numa app
/// setting em aplicação que não sobe. Sem chave, a API continua no ar e responde 401: falha
/// visível, nunca queda de processo.
/// </para>
/// </summary>
public static class ChaveDoJwt
{
    /// <summary>HMAC-SHA256 exige 256 bits.</summary>
    private const int BytesMinimos = 32;

    public static SymmetricSecurityKey? Ler(string? segredo, ILogger log)
    {
        if (string.IsNullOrWhiteSpace(segredo))
        {
            log.LogWarning(
                "Jwt:Secret não configurado — nenhum token será aceito e as rotas protegidas " +
                "responderão 401. Defina Jwt__Secret com o MESMO valor de JWT_SECRET da API Java.");
            return null;
        }

        byte[] chave;
        try
        {
            // BASE64, E NUNCA Encoding.UTF8.GetBytes
            // A API Java faz Keys.hmacShaKeyFor(Decoders.BASE64.decode(segredo)): a chave são os
            // bytes DECODIFICADOS. O idioma comum em ASP.NET é UTF8.GetBytes(segredo), que produz
            // uma chave DIFERENTE a partir do MESMO valor de configuração — e então nenhuma
            // assinatura confere e o sintoma é 401 em cem por cento das chamadas, sem pista no log.
            // O teste ChaveDerivada_DoBase64_ProduzOsMesmosBytesQueAApiJava trava esse contrato.
            chave = Convert.FromBase64String(segredo);
        }
        catch (FormatException)
        {
            log.LogError(
                "Jwt:Secret não é base64 válido — nenhum token será aceito. O valor precisa ser o " +
                "MESMO passado à API Java em JWT_SECRET, que o decodifica de base64 antes de usar " +
                "como chave HMAC.");
            return null;
        }

        if (chave.Length < BytesMinimos)
        {
            log.LogError(
                "Jwt:Secret decodifica para {Bytes} bytes, e HMAC-SHA256 exige ao menos {Minimo} — " +
                "nenhum token será aceito.", chave.Length, BytesMinimos);
            return null;
        }

        return new SymmetricSecurityKey(chave);
    }
}
