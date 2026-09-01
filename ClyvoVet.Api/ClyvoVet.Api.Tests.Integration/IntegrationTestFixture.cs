using ClyvoVet.Api.Data;
using ClyvoVet.Api.Enums;
using ClyvoVet.Api.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClyvoVet.Api.Tests.Integration;

/// <summary>
/// Sobe a API inteira em memória (WebApplicationFactory) trocando o Oracle real por um
/// banco EF Core InMemory, e semeia um Tutor + Animal + Produto para os testes que
/// dependem de FKs válidas (Lembrete e Sugestão de Produto).
/// Compartilhada entre todas as classes de teste via Collection Fixture — sobe a API
/// uma única vez para a suíte inteira, em vez de uma vez por classe.
/// </summary>
public class IntegrationTestFixture : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"ClyvoVetTestDb-{Guid.NewGuid()}";

    public string AnimalId { get; private set; } = null!;
    public string ProdutoId { get; private set; } = null!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(_databaseName));

            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
            Seed(db);
        });
    }

    // Os controllers principais (Produto, Lembrete, EventoPet, SugestaoProduto) agora
    // exigem X-Api-Key — injeta o header aqui pra não precisar editar cada teste.
    protected override void ConfigureClient(HttpClient client)
    {
        base.ConfigureClient(client);
        client.DefaultRequestHeaders.Add("X-Api-Key", "SUA_API_KEY");
    }

    private void Seed(AppDbContext db)
    {
        var tutor = new Tutor
        {
            Id = Guid.NewGuid().ToString(),
            Nome = "Tutor de Teste",
            Cpf = "00000000000",
            CriadoEm = DateTime.UtcNow
        };

        var animal = new Animal
        {
            Id = Guid.NewGuid().ToString(),
            Nome = "Rex",
            Especie = "CACHORRO",
            TutorId = tutor.Id,
            CriadoEm = DateTime.UtcNow
        };

        var produto = new Produto
        {
            Id = Guid.NewGuid().ToString(),
            Nome = "Ração de Teste",
            Categoria = CategoriaEnum.Racao,
            EspecieIndicada = EspecieEnum.Cachorro,
            Preco = 50m,
            Ativo = true,
            CriadoEm = DateTime.UtcNow
        };

        db.Tutores.Add(tutor);
        db.Animais.Add(animal);
        db.Produtos.Add(produto);
        db.SaveChanges();

        AnimalId = animal.Id;
        ProdutoId = produto.Id;
    }
}

[CollectionDefinition(Name)]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>
{
    public const string Name = "Integration Tests";
}
