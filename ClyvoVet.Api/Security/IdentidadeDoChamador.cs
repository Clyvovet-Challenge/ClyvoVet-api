namespace ClyvoVet.Api.Security;

/// <summary>
/// Quem está chamando, quando o pedido traz um access token válido emitido pela
/// API Java. É só identidade — não decide nada sozinha.
/// </summary>
/// <param name="UsuarioId">O <c>sub</c> do token: id do usuário, não do tutor.</param>
/// <param name="TutorId">
/// A claim <c>tutorId</c>, ou <c>null</c>. É <c>null</c> para ADMIN e VETERINARIO,
/// que não têm tutor.
/// <para>
/// <b>Nulo significa "este token não identifica um tutor", nunca "sem
/// restrição".</b> Quem recorta dados por dono precisa NEGAR quando vier nulo. O
/// idioma preguiçoso — <c>if (tutorId != null) query = query.Where(...)</c> —
/// devolveria a base inteira justamente para os perfis mais poderosos.
/// </para>
/// </param>
/// <param name="Perfil">ADMIN, VETERINARIO ou TUTOR.</param>
public record IdentidadeDoChamador(string UsuarioId, string? TutorId, string? Perfil);
