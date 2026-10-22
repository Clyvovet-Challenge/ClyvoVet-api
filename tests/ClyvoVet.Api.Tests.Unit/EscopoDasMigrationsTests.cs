using ClyvoVet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace ClyvoVet.Api.Tests.Unit;

// A migration descreve só o que esta API grava. O que ela apenas lê pertence à API
// Java (Flyway), e um CREATE TABLE dessas tabelas aqui seria uma segunda fonte de
// verdade para o mesmo schema (ADR-005).
public class EscopoDasMigrationsTests
{
    private static readonly string[] SoDeLeitura =
    [
        "t_clyvo_animal",
        "t_clyvo_tutor",
        "t_clyvo_raca",
        "t_clyvo_base_doencas",
    ];

    private static readonly string[] DaDotnet =
    [
        "t_clyvo_produto",
        "t_clyvo_sugestao_produto",
        "t_clyvo_lembrete",
        "t_clyvo_evento_pet",
        "t_clyvo_predisposicao_saude",
        "t_clyvo_tutor_telegram",
        "t_clyvo_parecer_ia",
    ];

    public static TheoryData<string> TabelasSoDeLeitura => new(SoDeLeitura);
    public static TheoryData<string> TabelasDaDotnet => new(DaDotnet);

    [Theory]
    [MemberData(nameof(TabelasSoDeLeitura))]
    public void Modelo_TabelasSoDeLeitura_FicamForaDasMigrations(string tabela)
    {
        // Arrange
        var entidade = EntidadeDaTabela(tabela);

        // Act
        var excluida = entidade.IsTableExcludedFromMigrations();

        // Assert
        Assert.True(excluida, $"{tabela} é da API Java e não pode entrar nas migrations.");
    }

    [Theory]
    [MemberData(nameof(TabelasDaDotnet))]
    public void Modelo_TabelasDaDotnet_EntramNasMigrations(string tabela)
    {
        // Arrange
        var entidade = EntidadeDaTabela(tabela);

        // Act
        var excluida = entidade.IsTableExcludedFromMigrations();

        // Assert
        Assert.False(excluida, $"{tabela} é gravada por esta API e precisa estar nas migrations.");
    }

    [Fact]
    public void Modelo_TodaTabela_EstaClassificada()
    {
        // Arrange
        var classificadas = SoDeLeitura.Concat(DaDotnet).Order();

        // Act
        var doModelo = ModeloDeDesignTime().GetEntityTypes()
            .Select(e => e.GetTableName()!)
            .Order();

        // Assert — tabela nova no modelo obriga a decidir de que lado ela fica
        Assert.Equal(classificadas, doModelo);
    }

    // O modelo de design-time é o que o `dotnet ef` enxerga; o de execução descarta
    // anotações que só servem às migrations, como a de exclusão.
    private static IModel ModeloDeDesignTime()
    {
        using var contexto = new AppDbContextFactory().CreateDbContext([]);
        return contexto.GetService<IDesignTimeModel>().Model;
    }

    private static IEntityType EntidadeDaTabela(string tabela) =>
        ModeloDeDesignTime().GetEntityTypes().Single(e => e.GetTableName() == tabela);
}
