namespace ClyvoVet.Api.Enums;

/// <summary>
/// O porte que um produto atende.
///
/// <para>
/// Espelha os tres valores de <c>t_clyvo_animal.porte</c> (a CHECK
/// <c>chk_animal_porte</c>, da V1) e acrescenta <see cref="Todos"/>, que e o
/// caso mais comum: consulta, shampoo neutro, vermifugo em gotas -- coisas em
/// que porte nao se aplica.
/// </para>
///
/// <para>
/// <see cref="Todos"/> nao e o mesmo que "nao sabemos". Ele afirma que o
/// produto serve a qualquer porte, e e por isso que ele APARECE na lista de um
/// animal especifico em vez de ser escondido por ela. Um valor nulo diria a
/// outra coisa, e nao daria para distinguir as duas.
/// </para>
/// </summary>
public enum PorteEnum
{
    Pequeno,
    Medio,
    Grande,
    Todos
}
