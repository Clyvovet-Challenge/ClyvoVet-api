using ClyvoVet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace ClyvoVet.Api.Tests.Unit;

// As tabelas que esta API grava nascem no Flyway da API Java (V8 em diante) e, desde
// as migrations do EF, também no script idempotente daqui. Os dois caminhos têm de
// produzir o mesmo banco: estes testes fixam no modelo do EF o que o Flyway declara.
// Os DEFAULTs ficam de fora de propósito (ver ADR-005): HasDefaultValue(true) num bool
// faz o EF omitir o false no INSERT, e o banco gravaria "ativo" onde a API mandou "inativo".
public class EspelhoDoFlywayTests
{
    private static readonly IModel Modelo = ModeloDeDesignTime();

    [Fact]
    public void Indices_TemOsNomesDoFlyway()
    {
        // Arrange
        string[] esperados =
        [
            "idx_lembrete_animal",
            "idx_lembrete_varredura",
            "idx_produto_especie_porte",
            "idx_sugestao_animal",
            "idx_sugestao_produto",
            "uk_parecer_ia_animal",
            // Nasce na migration ChatIdUnico, ainda sem par no Flyway: a Java precisa de uma
            // V21 com este mesmo nome. A migration só cria o índice se ele não existir.
            "uk_tutor_telegram_chat",
            "uk_tutor_telegram_tutor",
        ];

        // Act
        var doModelo = TabelasDaDotnet()
            .SelectMany(e => e.GetIndexes())
            .Select(i => i.GetDatabaseName()!)
            .Order();

        // Assert
        Assert.Equal(esperados, doModelo);
    }

    [Fact]
    public void CheckConstraints_SaoAsDoFlyway()
    {
        // Arrange
        string[] esperadas =
        [
            "chk_evento_pet_ativo",
            "chk_evento_pet_gratuito",
            "chk_lembrete_intervalo",
            "chk_lembrete_recorrente",
            "chk_produto_ativo",
            "chk_produto_porte",
            "chk_sugestao_ativo",
            "ck_parecer_ia_origem",
        ];

        // Act
        var doModelo = TabelasDaDotnet()
            .SelectMany(e => e.GetCheckConstraints())
            .Select(c => c.Name!)
            .Order();

        // Assert
        Assert.Equal(esperadas, doModelo);
    }

    [Theory]
    [InlineData("fk_lembrete_animal", DeleteBehavior.Cascade)]
    [InlineData("fk_sugestao_animal", DeleteBehavior.Cascade)]
    [InlineData("fk_parecer_ia_animal", DeleteBehavior.Cascade)]
    [InlineData("fk_sugestao_produto", DeleteBehavior.Restrict)]
    public void ChavesEstrangeiras_TemORegraDeExclusaoDoFlyway(string nome, DeleteBehavior esperada)
    {
        // Arrange
        var chaves = TabelasDaDotnet().SelectMany(e => e.GetForeignKeys());

        // Act
        var chave = chaves.Single(fk => fk.GetConstraintName() == nome);

        // Assert
        Assert.Equal(esperada, chave.DeleteBehavior);
    }

    [Fact]
    public void ColunasDeDataEHora_SaoDatetimeSemFracaoDeSegundo()
    {
        // Arrange
        var colunas = TabelasDaDotnet()
            .SelectMany(e => e.GetProperties())
            .Where(p => Nullable.GetUnderlyingType(p.ClrType) == typeof(DateTime) || p.ClrType == typeof(DateTime));

        // Act
        var foraDoPadrao = colunas
            .Where(p => p.GetColumnType() is not ("DATETIME" or "DATE"))
            .Select(p => $"{p.DeclaringType.GetTableName()}.{p.GetColumnName()}: {p.GetColumnType()}");

        // Assert
        Assert.Empty(foraDoPadrao);
    }

    [Fact]
    public void EventoPetEstado_TemOTamanhoDoFlyway()
    {
        // Arrange
        var estado = TabelasDaDotnet()
            .Single(e => e.GetTableName() == "t_clyvo_evento_pet")
            .GetProperties()
            .Single(p => p.GetColumnName() == "estado");

        // Act
        var tipo = estado.GetColumnType();

        // Assert
        Assert.Equal("VARCHAR(10)", tipo);
    }

    private static IEnumerable<IEntityType> TabelasDaDotnet() =>
        Modelo.GetEntityTypes().Where(e => !e.IsTableExcludedFromMigrations());

    private static IModel ModeloDeDesignTime()
    {
        using var contexto = new AppDbContextFactory().CreateDbContext([]);
        return contexto.GetService<IDesignTimeModel>().Model;
    }
}
