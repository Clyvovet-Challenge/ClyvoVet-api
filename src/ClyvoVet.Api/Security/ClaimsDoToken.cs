namespace ClyvoVet.Api.Security;

/// <summary>As claims que a API Java carimba no access token, e os valores que esta API confere.</summary>
public static class ClaimsDoToken
{
    public const string UsuarioId = "sub";
    public const string Tipo = "tipo";
    public const string TutorId = "tutorId";
    public const string Perfil = "perfil";

    public const string TipoAccess = "access";

    public const string PerfilAdmin = "ADMIN";
    public const string PerfilVeterinario = "VETERINARIO";
}
