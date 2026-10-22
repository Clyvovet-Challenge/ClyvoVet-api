namespace ClyvoVet.Api.Security;

/// <summary>Nomes das políticas de <c>[Authorize(Policy = ...)]</c>. Definidas em AddAutenticacaoJwt.</summary>
public static class PoliticasDeAcesso
{
    /// <summary>Qualquer usuário com access token válido.</summary>
    public const string Autenticado = "Autenticado";

    /// <summary>Access token válido de ADMIN ou VETERINARIO.</summary>
    public const string Equipe = "Equipe";
}
