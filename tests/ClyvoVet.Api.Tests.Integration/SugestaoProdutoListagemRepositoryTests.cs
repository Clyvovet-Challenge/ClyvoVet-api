using ClyvoVet.Application.Common;
using ClyvoVet.Domain.Entities;
using ClyvoVet.Domain.Enums;
using ClyvoVet.Domain.Exceptions;
using ClyvoVet.Infrastructure.Data;
using ClyvoVet.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ClyvoVet.Api.Tests.Integration;

public class SugestaoProdutoListagemRepositoryTests
{
    private static AppDbContext CriarContexto() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase($"SugestaoListagemDb-{Guid.NewGuid()}").Options);

    private static (Animal Animal, Tutor Tutor) NovoAnimal(AppDbContext contexto, string nome)
    {
        var tutor = new Tutor { Id = Guid.NewGuid().ToString(), Nome = $"Tutor de {nome}", Cpf = "00000000000" };
        var animal = new Animal { Id = Guid.NewGuid().ToString(), Nome = nome, Especie = "CACHORRO", TutorId = tutor.Id, Tutor = tutor };
        contexto.Tutores.Add(tutor);
        contexto.Animais.Add(animal);
        return (animal, tutor);
    }

    private static Produto NovoProduto(AppDbContext contexto, string nome)
    {
        var produto = new Produto
        {
            Id = Guid.NewGuid().ToString(), Nome = nome, Categoria = CategoriaEnum.Racao,
            EspecieIndicada = EspecieEnum.Todos, Ativo = true, CriadoEm = DateTime.UtcNow,
        };
        contexto.Produtos.Add(produto);
        return produto;
    }

    private static SugestaoProduto Nova(Animal animal, Produto produto, string justificativa, int diasAtras) => new()
    {
        Id = Guid.NewGuid().ToString(),
        AnimalId = animal.Id,
        Animal = animal,
        ProdutoId = produto.Id,
        Produto = produto,
        Justificativa = justificativa,
        DataSugestao = DateOnly.FromDateTime(DateTime.Today.AddDays(-diasAtras)),
        Ativo = true,
        CriadoEm = DateTime.UtcNow,
    };

    private static string[] Justificativas(PaginaDeResultados<SugestaoProduto> pagina) =>
        pagina.Itens.Select(s => s.Justificativa!).ToArray();

    private static Task<PaginaDeResultados<SugestaoProduto>> Listar(AppDbContext contexto, ConsultaPaginada consulta) =>
        new SugestaoProdutoRepository(contexto).GetAllAsync(consulta, null, null, null);

    [Fact]
    public async Task GetAllAsync_SemOrdenarPor_MantemAOrdemPorDataSugestaoDecrescente()
    {
        // Arrange
        using var contexto = CriarContexto();
        var (rex, _) = NovoAnimal(contexto, "Rex");
        var racao = NovoProduto(contexto, "Ração");
        contexto.SugestoesProduto.AddRange(Nova(rex, racao, "antiga", 10), Nova(rex, racao, "nova", 1), Nova(rex, racao, "media", 5));
        await contexto.SaveChangesAsync();

        // Act
        var pagina = await Listar(contexto, new ConsultaPaginada(1, 10));

        // Assert
        Assert.Equal(["nova", "media", "antiga"], Justificativas(pagina));
    }

    [Fact]
    public async Task GetAllAsync_OrdenarPorNomeProduto_OrdenaPelaNavegacao()
    {
        // Arrange
        using var contexto = CriarContexto();
        var (rex, _) = NovoAnimal(contexto, "Rex");
        var zinco = NovoProduto(contexto, "Zinco");
        var acido = NovoProduto(contexto, "Ácido");
        contexto.SugestoesProduto.AddRange(Nova(rex, zinco, "do zinco", 1), Nova(rex, acido, "do ácido", 2));
        await contexto.SaveChangesAsync();

        // Act
        var pagina = await Listar(contexto, new ConsultaPaginada(1, 10, "nomeProduto"));

        // Assert
        Assert.Equal(["do ácido", "do zinco"], Justificativas(pagina));
    }

    [Fact]
    public async Task GetAllAsync_CampoForaDaListaBranca_LancaBadRequest()
    {
        using var contexto = CriarContexto();

        var erro = await Assert.ThrowsAsync<BadRequestException>(() =>
            Listar(contexto, new ConsultaPaginada(1, 10, "senha")));

        Assert.StartsWith("Campo de ordenação 'senha' inválido. Permitidos: ", erro.Message);
    }

    [Fact]
    public async Task GetAllAsync_RecorteDoTutor_TotalContaSoAsSugestoesDele()
    {
        // Arrange
        using var contexto = CriarContexto();
        var (rex, tutorDoRex) = NovoAnimal(contexto, "Rex");
        var (mel, _) = NovoAnimal(contexto, "Mel");
        var racao = NovoProduto(contexto, "Ração");
        contexto.SugestoesProduto.AddRange(Nova(rex, racao, "r1", 1), Nova(rex, racao, "r2", 2), Nova(mel, racao, "m1", 3));
        await contexto.SaveChangesAsync();

        // Act
        var pagina = await new SugestaoProdutoRepository(contexto)
            .GetAllAsync(new ConsultaPaginada(1, 1), null, tutorDoRex.Id, null);

        // Assert: um item na página, mas o total é o do tutor (2), e não o da base (3).
        Assert.Single(pagina.Itens);
        Assert.Equal(2, pagina.Total);
    }
}
