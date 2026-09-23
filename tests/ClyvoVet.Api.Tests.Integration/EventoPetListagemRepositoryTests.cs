using ClyvoVet.Application.Common;
using ClyvoVet.Domain.Entities;
using ClyvoVet.Domain.Enums;
using ClyvoVet.Domain.Exceptions;
using ClyvoVet.Infrastructure.Data;
using ClyvoVet.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ClyvoVet.Api.Tests.Integration;

public class EventoPetListagemRepositoryTests
{
    private static AppDbContext CriarContexto() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase($"EventoListagemDb-{Guid.NewGuid()}").Options);

    private static EventoPet Novo(string titulo, int diasAFrente) => new()
    {
        Id = Guid.NewGuid().ToString(),
        Titulo = titulo,
        Tipo = TipoEventoPetEnum.Feira,
        DataInicio = DateOnly.FromDateTime(DateTime.Today.AddDays(diasAFrente)),
        EspecieAlvo = EspecieEnum.Todos,
        Ativo = true,
        CriadoEm = DateTime.UtcNow,
    };

    private static string[] Titulos(PaginaDeResultados<EventoPet> pagina) => pagina.Itens.Select(e => e.Titulo).ToArray();

    [Fact]
    public async Task GetAllAsync_SemOrdenarPor_MantemAOrdemPorDataInicio()
    {
        // Arrange
        using var contexto = CriarContexto();
        contexto.EventosPet.AddRange(Novo("C", 3), Novo("A", 1), Novo("B", 2));
        await contexto.SaveChangesAsync();

        // Act
        var pagina = await new EventoPetRepository(contexto).GetAllAsync(new ConsultaPaginada(1, 10), null, null, null);

        // Assert
        Assert.Equal(["A", "B", "C"], Titulos(pagina));
    }

    [Fact]
    public async Task GetAllAsync_OrdenarPorTituloDesc_OrdenaDoMaiorParaOMenor()
    {
        // Arrange
        using var contexto = CriarContexto();
        contexto.EventosPet.AddRange(Novo("B", 1), Novo("C", 2), Novo("A", 3));
        await contexto.SaveChangesAsync();

        // Act
        var pedido = new ConsultaPaginada(1, 10, "titulo", DirecaoOrdenacao.Desc);
        var pagina = await new EventoPetRepository(contexto).GetAllAsync(pedido, null, null, null);

        // Assert
        Assert.Equal(["C", "B", "A"], Titulos(pagina));
    }

    [Fact]
    public async Task GetAllAsync_CampoForaDaListaBranca_LancaBadRequest()
    {
        using var contexto = CriarContexto();

        var erro = await Assert.ThrowsAsync<BadRequestException>(() =>
            new EventoPetRepository(contexto).GetAllAsync(new ConsultaPaginada(1, 10, "senha"), null, null, null));

        Assert.StartsWith("Campo de ordenação 'senha' inválido. Permitidos: ", erro.Message);
    }

    [Fact]
    public async Task GetAllAsync_TotalIgnoraORecorteDePaginaEContaSoOFiltrado()
    {
        // Arrange
        using var contexto = CriarContexto();
        for (var i = 1; i <= 5; i++) contexto.EventosPet.Add(Novo($"E{i}", i));
        var vacinacao = Novo("Vacinação", 9);
        vacinacao.Tipo = TipoEventoPetEnum.Vacinacao;
        contexto.EventosPet.Add(vacinacao);
        await contexto.SaveChangesAsync();

        // Act
        var pagina = await new EventoPetRepository(contexto)
            .GetAllAsync(new ConsultaPaginada(2, 2), null, TipoEventoPetEnum.Feira, null);

        // Assert
        Assert.Equal(2, pagina.Itens.Count);
        Assert.Equal(5, pagina.Total);
    }
}
