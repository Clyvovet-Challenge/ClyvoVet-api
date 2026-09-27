using ClyvoVet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace ClyvoVet.Api.Tests.Unit;

public class MigrationsTests
{
    // Mudar uma entidade ou configuração sem gerar migration faz o script entregue
    // descrever um banco diferente do que o código espera. Este teste é o que acusa:
    // é a mesma comparação que o `dotnet ef migrations add` faz para decidir se há
    // algo novo (o HasPendingModelChanges() equivalente só chegou no EF 9).
    [Fact]
    public void Migrations_ComparadasAoModelo_NaoTemDiferencas()
    {
        // Arrange
        using var contexto = new AppDbContextFactory().CreateDbContext([]);
        var snapshot = contexto.GetService<IMigrationsAssembly>().ModelSnapshot;
        Assert.NotNull(snapshot);

        var modeloDoSnapshot = snapshot.Model is IMutableModel mutavel
            ? mutavel.FinalizeModel()
            : snapshot.Model;
        modeloDoSnapshot = contexto.GetService<IModelRuntimeInitializer>()
            .Initialize(modeloDoSnapshot, designTime: true, validationLogger: null);
        var modeloAtual = contexto.GetService<IDesignTimeModel>().Model;

        // Act
        var diferencas = contexto.GetService<IMigrationsModelDiffer>().GetDifferences(
            modeloDoSnapshot.GetRelationalModel(), modeloAtual.GetRelationalModel());

        // Assert
        Assert.Empty(diferencas);
    }

    [Fact]
    public void MigrationInicial_CriaExatamenteAsSeteTabelasDaDotnet()
    {
        // Arrange
        using var contexto = new AppDbContextFactory().CreateDbContext([]);
        var migrations = contexto.GetService<IMigrationsAssembly>();
        var inicial = migrations.Migrations.Single(m => m.Key.EndsWith("_Inicial")).Value;

        // Act
        var tabelasCriadas = migrations.CreateMigration(inicial, contexto.Database.ProviderName!)
            .UpOperations.OfType<CreateTableOperation>()
            .Select(operacao => operacao.Name)
            .Order();

        // Assert
        string[] esperadas =
        [
            "t_clyvo_evento_pet",
            "t_clyvo_lembrete",
            "t_clyvo_parecer_ia",
            "t_clyvo_predisposicao_saude",
            "t_clyvo_produto",
            "t_clyvo_sugestao_produto",
            "t_clyvo_tutor_telegram",
        ];
        Assert.Equal(esperadas, tabelasCriadas);
    }
}
