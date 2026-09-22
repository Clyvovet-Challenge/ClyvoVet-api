using ClyvoVet.Application.Common;
using ClyvoVet.Domain.Entities;
using ClyvoVet.Domain.Enums;
using ClyvoVet.Domain.Exceptions;
using ClyvoVet.Infrastructure.Data;
using ClyvoVet.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ClyvoVet.Api.Tests.Integration;

public class LembreteListagemRepositoryTests
{
    private static AppDbContext CriarContexto() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase($"LembreteListagemDb-{Guid.NewGuid()}").Options);

    private static Animal NovoAnimal(AppDbContext contexto, string nomeAnimal)
    {
        var tutor = new Tutor { Id = Guid.NewGuid().ToString(), Nome = $"Tutor de {nomeAnimal}", Cpf = "00000000000" };
        var animal = new Animal
        {
            Id = Guid.NewGuid().ToString(), Nome = nomeAnimal, Especie = "CACHORRO", TutorId = tutor.Id, Tutor = tutor,
        };
        contexto.Tutores.Add(tutor);
        contexto.Animais.Add(animal);
        return animal;
    }

    private static Lembrete Novo(Animal animal, string titulo, int diasAFrente) => new()
    {
        Id = Guid.NewGuid().ToString(),
        AnimalId = animal.Id,
        Animal = animal,
        Titulo = titulo,
        Tipo = TipoLembreteEnum.Vacina,
        Status = StatusLembreteEnum.Pendente,
        AgendadoEm = DateTime.UtcNow.AddDays(diasAFrente),
        CriadoEm = DateTime.UtcNow,
    };

    private static string[] Titulos(PaginaDeResultados<Lembrete> pagina) => pagina.Itens.Select(l => l.Titulo).ToArray();

    [Fact]
    public async Task GetAllAsync_SemOrdenarPor_MantemAOrdemPorAgendadoEm()
    {
        // Arrange
        using var contexto = CriarContexto();
        var rex = NovoAnimal(contexto, "Rex");
        contexto.Lembretes.AddRange(Novo(rex, "C", 3), Novo(rex, "A", 1), Novo(rex, "B", 2));
        await contexto.SaveChangesAsync();

        // Act
        var pagina = await new LembreteRepository(contexto).GetAllAsync(new ConsultaPaginada(1, 10), null, null, null);

        // Assert
        Assert.Equal(["A", "B", "C"], Titulos(pagina));
    }

    [Fact]
    public async Task GetAllAsync_OrdenarPorTituloDesc_OrdenaDoMaiorParaOMenor()
    {
        // Arrange
        using var contexto = CriarContexto();
        var rex = NovoAnimal(contexto, "Rex");
        contexto.Lembretes.AddRange(Novo(rex, "B", 1), Novo(rex, "C", 2), Novo(rex, "A", 3));
        await contexto.SaveChangesAsync();

        // Act
        var pedido = new ConsultaPaginada(1, 10, "titulo", DirecaoOrdenacao.Desc);
        var pagina = await new LembreteRepository(contexto).GetAllAsync(pedido, null, null, null);

        // Assert
        Assert.Equal(["C", "B", "A"], Titulos(pagina));
    }

    [Fact]
    public async Task GetAllAsync_OrdenarPorNomeAnimal_OrdenaPelaNavegacao()
    {
        // Arrange
        using var contexto = CriarContexto();
        var zeca = NovoAnimal(contexto, "Zeca");
        var alfa = NovoAnimal(contexto, "Alfa");
        contexto.Lembretes.AddRange(Novo(zeca, "do Zeca", 1), Novo(alfa, "do Alfa", 2));
        await contexto.SaveChangesAsync();

        // Act
        var pagina = await new LembreteRepository(contexto)
            .GetAllAsync(new ConsultaPaginada(1, 10, "nomeAnimal"), null, null, null);

        // Assert
        Assert.Equal(["do Alfa", "do Zeca"], Titulos(pagina));
    }

    [Fact]
    public async Task GetAllAsync_CampoForaDaListaBranca_LancaBadRequest()
    {
        // Arrange
        using var contexto = CriarContexto();

        // Act
        var erro = await Assert.ThrowsAsync<BadRequestException>(() =>
            new LembreteRepository(contexto).GetAllAsync(new ConsultaPaginada(1, 10, "senha"), null, null, null));

        // Assert
        Assert.StartsWith("Campo de ordenação 'senha' inválido. Permitidos: ", erro.Message);
    }

    [Fact]
    public async Task GetAllAsync_TotalIgnoraORecorteDePagina()
    {
        // Arrange
        using var contexto = CriarContexto();
        var rex = NovoAnimal(contexto, "Rex");
        for (var i = 1; i <= 5; i++) contexto.Lembretes.Add(Novo(rex, $"L{i}", i));
        await contexto.SaveChangesAsync();

        // Act
        var pagina = await new LembreteRepository(contexto).GetAllAsync(new ConsultaPaginada(2, 2), null, null, null);

        // Assert: o total é o da consulta filtrada, e não o tamanho da página.
        Assert.Equal(2, pagina.Itens.Count);
        Assert.Equal(5, pagina.Total);
        Assert.Equal(2, pagina.Page);
        Assert.Equal(3, pagina.TotalDePaginas);
    }

    [Fact]
    public async Task GetAllAsync_RecorteDoTutor_ContaSoOsLembretesDele()
    {
        // Arrange
        using var contexto = CriarContexto();
        var rex = NovoAnimal(contexto, "Rex");
        var mel = NovoAnimal(contexto, "Mel");
        contexto.Lembretes.AddRange(Novo(rex, "R1", 1), Novo(rex, "R2", 2), Novo(mel, "M1", 3));
        await contexto.SaveChangesAsync();

        // Act
        var pagina = await new LembreteRepository(contexto)
            .GetAllAsync(new ConsultaPaginada(1, 10), null, null, null, tutorId: rex.TutorId);

        // Assert: o total respeita o recorte por dono, e não vaza a contagem da base inteira.
        Assert.Equal(2, pagina.Total);
        Assert.All(pagina.Itens, l => Assert.Equal(rex.Id, l.AnimalId));
    }
}
