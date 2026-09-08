namespace ClyvoVet.Api.Repositories;

/// <summary>
/// O recorte de página, num lugar só.
/// </summary>
/// <remarks>
/// <para>
/// <b>Por que existe.</b> As quatro listagens desta API faziam
/// <c>.Skip((page - 1) * pageSize)</c>, e essa conta é feita em <c>int</c>. Com
/// <c>?page=2147483647&amp;pageSize=100</c> ela estoura e <b>dá negativo</b>: o
/// produto real é 214.748.364.600, que ao voltar para 32 bits vira <c>-200</c>. O
/// <c>Skip</c> negativo derruba a consulta, e as quatro rotas respondiam
/// <b>500</b> — verificado contra a pilha no ar, em <c>/lembretes</c>,
/// <c>/produtos</c>, <c>/eventos-pet</c> e <c>/sugestoes-produto</c>, com a chave de
/// API correta. Qualquer cliente derrubava a listagem inteira com uma query string,
/// que é o mesmo defeito que a API Java já tinha na ordenação.
/// </para>
///
/// <para>
/// A validação nos controllers não pegava: ela checa <c>page &gt;= 1</c> e
/// <c>pageSize</c> entre 1 e 100, e 2147483647 passa nas duas. O estouro acontece
/// depois, na multiplicação.
/// </para>
///
/// <para>
/// <b>Por que uma página vazia, e não um 400.</b> Passar do fim da coleção não é
/// erro do cliente — <c>?page=999999999</c> já respondia 200 com lista vazia, porque
/// aquela multiplicação por acaso não estourou. Devolver 400 só para os valores que
/// estouram faria a API tratar duas páginas igualmente inexistentes de formas
/// diferentes, pelo motivo mais arbitrário possível: onde cai o módulo de 2³².
/// </para>
/// </remarks>
public static class Paginacao
{
    public static IQueryable<T> Aplicar<T>(IQueryable<T> consulta, int page, int pageSize)
    {
        // O cast vem ANTES do `- 1`, e não depois. Escrito como
        // `(long)(page - 1) * pageSize`, a subtração ainda acontece em int: com
        // page = int.MinValue ela estoura para int.MaxValue, e a página mais
        // negativa possível vira a mais distante possível. Foi o teste de
        // int.MinValue que pegou isso — na primeira versão deste método.
        var pular = ((long)page - 1) * pageSize;

        // Os controllers já recusam page < 1 e pageSize fora de 1..100, mas os
        // repositórios também são chamados pelos dois BackgroundService e pelos
        // testes, e não podem depender de quem chama ter validado. Skip negativo
        // quebra tão bem quanto o estouro.
        if (pular < 0) pular = 0;
        if (pageSize < 0) pageSize = 0;

        // Além de int.MaxValue não há nada para pular: a página está muito além do
        // fim de qualquer coleção. Take(0) resolve com um LIMIT 0.
        if (pular > int.MaxValue) return consulta.Take(0);

        return consulta.Skip((int)pular).Take(pageSize);
    }
}
