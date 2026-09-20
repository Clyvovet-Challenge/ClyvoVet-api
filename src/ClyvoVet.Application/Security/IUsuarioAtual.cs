namespace ClyvoVet.Application.Security;

/// <summary>
/// Quem está chamando a requisição corrente.
///
/// <para>
/// Existe para a Application não conhecer <c>HttpContext</c>: o <c>EscopoDoTutor</c>
/// precisa saber o tutor do chamador, e quem sabe ler isso do pedido é a Api. Também é
/// o que torna o <c>EscopoDoTutor</c> testável sem montar um contexto HTTP.
/// </para>
/// </summary>
public interface IUsuarioAtual
{
    /// <summary>A identidade do token, ou <c>null</c> se o pedido não trouxe token válido.</summary>
    IdentidadeDoChamador? Identidade { get; }
}
