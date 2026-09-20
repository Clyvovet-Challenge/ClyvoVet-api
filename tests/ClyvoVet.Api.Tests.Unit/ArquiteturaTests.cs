using System.Reflection;
using ClyvoVet.Domain.Entities;

namespace ClyvoVet.Api.Tests.Unit;

/// <summary>
/// As regras de dependência entre as camadas, verificadas no que o compilador gravou.
///
/// <para>
/// Referência de projeto não basta: os pacotes fluem da Infrastructure para a Api por
/// transitividade, então a Api compilaria usando tipos do EF sem reclamar. Estes testes
/// leem os assemblies já compilados — e o compilador só grava uma referência quando o
/// código realmente usa um tipo do outro assembly. Enxergam uso real, não intenção.
/// </para>
///
/// <para>
/// Se um deles falhar, a arquitetura está errada — não o teste.
/// </para>
/// </summary>
public class ArquiteturaTests
{
    private static readonly Assembly AssemblyDoDomain = typeof(Animal).Assembly;

    private static IReadOnlyList<string> Referencias(Assembly assembly) =>
        assembly.GetReferencedAssemblies().Select(nome => nome.Name!).ToList();

    private static void NaoPodeReferenciar(Assembly assembly, params string[] prefixos)
    {
        var proibidas = Referencias(assembly)
            .Where(nome => prefixos.Any(prefixo => nome.StartsWith(prefixo, StringComparison.Ordinal)))
            .ToList();

        Assert.True(
            proibidas.Count == 0,
            $"{assembly.GetName().Name} não pode referenciar: {string.Join(", ", proibidas)}");
    }

    [Fact]
    public void Domain_NaoReferenciaNenhumProjetoNemPacote()
    {
        NaoPodeReferenciar(AssemblyDoDomain, "ClyvoVet.", "Microsoft.");
    }
}
