using ClyvoVet.Domain.Entities;
using ClyvoVet.Domain.Enums;
using ClyvoVet.Domain.Exceptions;
using ClyvoVet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ClyvoVet.Api.Tests.Integration;

/// <summary>
/// A fronteira com o EF: quem chama o repositório só conhece <c>RegistroEmUsoException</c>.
///
/// <para>
/// Os testes de integração rodam em InMemory, que não aplica chave estrangeira; por isso a
/// falha é provocada por um interceptador que lança a mesma <c>DbUpdateException</c> que o
/// provedor relacional lançaria.
/// </para>
/// </summary>
public class AppDbContextTests
{
    private sealed class FalhaDeIntegridadeInterceptor : SaveChangesInterceptor
    {
        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData, InterceptionResult<int> result)
            => throw new DbUpdateException("FK violation", (Exception?)null);

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
            => throw new DbUpdateException("FK violation", (Exception?)null);
    }

    private static AppDbContext CriarContextoQueFalhaAoSalvar()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"AppDbContextTestDb-{Guid.NewGuid()}")
            .AddInterceptors(new FalhaDeIntegridadeInterceptor())
            .Options;
        return new AppDbContext(options);
    }

    private static Produto NovoProduto() => new()
    {
        Id = Guid.NewGuid().ToString(),
        Nome = "Ração de Teste",
        Categoria = CategoriaEnum.Racao,
        EspecieIndicada = EspecieEnum.Cachorro,
        Preco = 50m,
        Ativo = true,
        CriadoEm = DateTime.UtcNow
    };

    [Fact]
    public async Task SaveChangesAsync_FalhaDeIntegridade_LancaRegistroEmUsoExceptionComACausaOriginal()
    {
        // Arrange
        using var contexto = CriarContextoQueFalhaAoSalvar();
        contexto.Produtos.Add(NovoProduto());

        // Act
        var excecao = await Assert.ThrowsAsync<RegistroEmUsoException>(
            () => contexto.SaveChangesAsync());

        // Assert
        Assert.IsType<DbUpdateException>(excecao.InnerException);
    }

    [Fact]
    public void SaveChanges_FalhaDeIntegridade_LancaRegistroEmUsoExceptionComACausaOriginal()
    {
        // Arrange
        using var contexto = CriarContextoQueFalhaAoSalvar();
        contexto.Produtos.Add(NovoProduto());

        // Act
        var excecao = Assert.Throws<RegistroEmUsoException>(() => contexto.SaveChanges());

        // Assert
        Assert.IsType<DbUpdateException>(excecao.InnerException);
    }
}
