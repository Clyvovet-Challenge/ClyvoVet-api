using System.Linq.Expressions;
using ClyvoVet.Application.Common;
using ClyvoVet.Domain.Exceptions;

namespace ClyvoVet.Infrastructure.Repositories;

/// <summary>
/// A ordenação pedida pelo cliente, aplicada só sobre uma <b>lista branca</b> declarada no
/// repositório. Nunca se monta <c>ORDER BY</c> a partir de texto livre: o texto só escolhe uma
/// entrada do dicionário, e o que vai para a consulta é a expressão já escrita em código.
/// </summary>
/// <remarks>
/// <para>
/// <b>Sempre desempata por um campo único.</b> Ordenar por um campo com valores repetidos deixa a
/// ordem entre os empatados indefinida, e com <c>Skip/Take</c> isso duplica ou pula linhas entre
/// páginas. O desempate vale também para a ordem padrão: só muda o que era indefinido.
/// </para>
/// <para>
/// A expressão <c>Func&lt;T, object?&gt;</c> "embrulha" tipos de valor (data, enum, bool). O EF Core
/// desfaz esse embrulho e gera o <c>ORDER BY</c> normal — verificado contra o provedor do MySQL.
/// </para>
/// </remarks>
public static class Ordenacao
{
    public static IOrderedQueryable<T> Aplicar<T>(
        IQueryable<T> consulta,
        ConsultaPaginada pedido,
        IReadOnlyDictionary<string, Expression<Func<T, object?>>> permitidos,
        Func<IQueryable<T>, IOrderedQueryable<T>> ordemPadrao,
        Expression<Func<T, object?>> desempate)
    {
        if (string.IsNullOrWhiteSpace(pedido.OrdenarPor))
            return ordemPadrao(consulta).ThenBy(desempate);

        if (!permitidos.TryGetValue(pedido.OrdenarPor.Trim(), out var chave))
        {
            var lista = string.Join(", ", permitidos.Keys.Order(StringComparer.OrdinalIgnoreCase));
            throw new BadRequestException(
                $"Campo de ordenação '{pedido.OrdenarPor}' inválido. Permitidos: {lista}.");
        }

        var ordenada = pedido.Direcao == DirecaoOrdenacao.Desc
            ? consulta.OrderByDescending(chave)
            : consulta.OrderBy(chave);

        return ordenada.ThenBy(desempate);
    }
}
