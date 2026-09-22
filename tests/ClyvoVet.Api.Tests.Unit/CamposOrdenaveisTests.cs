using System.Linq.Expressions;
using System.Reflection;
using ClyvoVet.Application.DTOs.Response;
using ClyvoVet.Infrastructure.Data;
using ClyvoVet.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ClyvoVet.Api.Tests.Unit;

/// <summary>
/// Duas travas sobre a lista branca de ordenação de cada recurso: (1) ela acompanha os campos
/// escalares do DTO de resposta — campo novo no DTO sem entrar na lista (ou ao contrário) derruba o
/// teste; (2) cada campo vira um <c>ORDER BY</c> no SQL do MySQL. A segunda existe porque os testes
/// de integração rodam em InMemory, que aceita qualquer expressão: uma propriedade calculada
/// passaria lá e só quebraria em produção.
/// </summary>
public class CamposOrdenaveisTests
{
    private static AppDbContext ContextoMysql() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseMySql("Server=x;Database=x;Uid=x;Pwd=x;", new MySqlServerVersion(new Version(8, 0))).Options);

    /// <summary>O <c>id</c> fica de fora: é um UUID opaco, ordenar por ele não significa nada.</summary>
    private static void BatemComOsCamposDoDto<TDto>(IEnumerable<string> campos)
    {
        var esperados = typeof(TDto).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.Name is not ("Id" or "Links"))
            .Select(p => char.ToLowerInvariant(p.Name[0]) + p.Name[1..])
            .OrderBy(nome => nome, StringComparer.Ordinal);

        Assert.Equal(esperados, campos.OrderBy(nome => nome, StringComparer.Ordinal));
    }

    private static void TodosGeramOrderBy<T>(
        IQueryable<T> origem, IReadOnlyDictionary<string, Expression<Func<T, object?>>> campos)
    {
        foreach (var (nome, chave) in campos)
        {
            var sql = origem.OrderBy(chave).ToQueryString();
            Assert.True(sql.Contains("ORDER BY"), $"O campo '{nome}' não gerou ORDER BY no MySQL:\n{sql}");
        }
    }

    [Fact]
    public void Lembrete_CamposOrdenaveis_SaoOsCamposEscalaresDoDto() =>
        BatemComOsCamposDoDto<LembreteResponse>(LembreteRepository.CamposOrdenaveis.Keys);

    [Fact]
    public void Lembrete_CamposOrdenaveis_TraduzemParaOrderByNoMysql()
    {
        using var db = ContextoMysql();
        TodosGeramOrderBy(db.Lembretes.Include(l => l.Animal), LembreteRepository.CamposOrdenaveis);
    }
}
