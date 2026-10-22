using System.Reflection;
using ClyvoVet.Api.Errors;
using ClyvoVet.Application.Security;
using ClyvoVet.Domain.Entities;
using ClyvoVet.Infrastructure.Data;

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
    private static readonly Assembly AssemblyDaApplication = typeof(EscopoDoTutor).Assembly;
    private static readonly Assembly AssemblyDaInfrastructure = typeof(AppDbContext).Assembly;
    private static readonly Assembly AssemblyDaApi = typeof(MapaDeErro).Assembly;

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

    [Fact]
    public void Application_NaoReferenciaInfraestruturaNemFrameworkWeb()
    {
        NaoPodeReferenciar(
            AssemblyDaApplication,
            "Microsoft.EntityFrameworkCore", "MongoDB", "Telegram", "Microsoft.AspNetCore",
            "ClyvoVet.Infrastructure", "ClyvoVet.Api");
    }

    /// <summary>
    /// Sem este, a regra acima passaria mesmo que <c>GetReferencedAssemblies</c> devolvesse
    /// sempre uma lista vazia — uma verificação que nunca falha não verifica nada.
    /// </summary>
    [Fact]
    public void Application_UsaAbstracoesDoMicrosoftExtensions_ProvaQueAVerificacaoEnxergaReferencias()
    {
        Assert.Contains("Microsoft.Extensions.Logging.Abstractions", Referencias(AssemblyDaApplication));
    }

    [Fact]
    public void Infrastructure_NaoReferenciaMvcNemApi()
    {
        NaoPodeReferenciar(AssemblyDaInfrastructure, "Microsoft.AspNetCore.Mvc", "ClyvoVet.Api");
    }

    [Fact]
    public void Infrastructure_UsaEntityFramework_ProvaQueAVerificacaoEnxergaReferencias()
    {
        Assert.Contains(
            Referencias(AssemblyDaInfrastructure),
            nome => nome.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));
    }

    [Fact]
    public void Api_NaoReferenciaEntityFrameworkNemMongo()
    {
        NaoPodeReferenciar(AssemblyDaApi, "Microsoft.EntityFrameworkCore", "MongoDB");
    }

    [Fact]
    public void Api_UsaApplication_ProvaQueAVerificacaoEnxergaReferencias()
    {
        Assert.Contains("ClyvoVet.Application", Referencias(AssemblyDaApi));
    }
}
