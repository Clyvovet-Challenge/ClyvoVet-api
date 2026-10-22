using ClyvoVet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClyvoVet.Api.Tests.Unit;

public class AppDbContextFactoryTests
{
    [Fact]
    public void CreateDbContext_SemBanco_UsaProviderMySql()
    {
        // Arrange
        var fabrica = new AppDbContextFactory();

        // Act
        using var contexto = fabrica.CreateDbContext([]);

        // Assert
        Assert.Equal("Pomelo.EntityFrameworkCore.MySql", contexto.Database.ProviderName);
    }
}
