using ClyvoVet.Domain.Entities;
using ClyvoVet.Domain.Enums;
using ClyvoVet.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ClyvoVet.Api.Tests.Integration;

/// <summary>
/// A API em memória com dados <b>exatos</b> (um de cada recurso e três lembretes), para provar
/// <c>_links</c>, cabeçalhos e envelope sem depender do que outros testes criaram na fixture
/// compartilhada. Manda a <c>X-Api-Key</c> e um Bearer de ADMIN por padrão.
/// </summary>
public class HateoasFixture : WebApplicationFactory<Program>
{
    // Capturado num campo, e não gerado dentro do lambda abaixo: o registro de
    // DbContextOptions do AddDbContext é Scoped, então o lambda roda de novo a cada
    // escopo. Um Guid.NewGuid() inline daria um banco em memória diferente (e vazio)
    // para a semeadura e para cada requisição real.
    private readonly string _databaseName = $"HateoasDb-{Guid.NewGuid()}";

    public string TutorId { get; private set; } = null!;
    public string AnimalId { get; private set; } = null!;
    public string ProdutoId { get; private set; } = null!;
    public string EventoId { get; private set; } = null!;
    public string SugestaoId { get; private set; } = null!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configuracao) =>
            configuracao.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = TokensDeTeste.Segredo,
                // Desligado de propósito, como na IntegrationTestFixture: aqui o assunto são os links.
                ["Api:EscopoPorTutor"] = "false",
            }));

        builder.ConfigureServices(services =>
        {
            var descritor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descritor is not null) services.Remove(descritor);
            services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(_databaseName));

            using var escopo = services.BuildServiceProvider().CreateScope();
            var db = escopo.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
            Semear(db);
        });
    }

    protected override void ConfigureClient(HttpClient client)
    {
        base.ConfigureClient(client);
        client.DefaultRequestHeaders.Add("X-Api-Key", "SUA_API_KEY");
        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + TokensDeTeste.Access(tutorId: null, perfil: "ADMIN"));
    }

    private void Semear(AppDbContext db)
    {
        var tutor = new Tutor { Id = Guid.NewGuid().ToString(), Nome = "Tutor", Cpf = "00000000000" };
        var animal = new Animal { Id = Guid.NewGuid().ToString(), Nome = "Rex", Especie = "CACHORRO", TutorId = tutor.Id, Tutor = tutor };
        var produto = new Produto
        {
            Id = Guid.NewGuid().ToString(), Nome = "Ração", Categoria = CategoriaEnum.Racao,
            EspecieIndicada = EspecieEnum.Todos, Ativo = true, CriadoEm = DateTime.UtcNow,
        };
        var evento = new EventoPet
        {
            Id = Guid.NewGuid().ToString(), Titulo = "Feira de adoção", Tipo = TipoEventoPetEnum.Feira,
            DataInicio = DateOnly.FromDateTime(DateTime.Today.AddDays(5)), EspecieAlvo = EspecieEnum.Todos,
            Ativo = true, CriadoEm = DateTime.UtcNow,
        };
        var sugestao = new SugestaoProduto
        {
            Id = Guid.NewGuid().ToString(), AnimalId = animal.Id, Animal = animal, ProdutoId = produto.Id, Produto = produto,
            Justificativa = "Indicado", DataSugestao = DateOnly.FromDateTime(DateTime.Today), Ativo = true, CriadoEm = DateTime.UtcNow,
        };

        db.Tutores.Add(tutor);
        db.Animais.Add(animal);
        db.Produtos.Add(produto);
        db.EventosPet.Add(evento);
        db.SugestoesProduto.Add(sugestao);
        for (var i = 1; i <= 3; i++)
        {
            db.Lembretes.Add(new Lembrete
            {
                Id = Guid.NewGuid().ToString(), AnimalId = animal.Id, Animal = animal, Titulo = $"Lembrete {i}",
                Tipo = TipoLembreteEnum.Vacina, Status = StatusLembreteEnum.Pendente,
                AgendadoEm = DateTime.UtcNow.AddDays(i), CriadoEm = DateTime.UtcNow,
            });
        }
        db.SaveChanges();

        TutorId = tutor.Id;
        AnimalId = animal.Id;
        ProdutoId = produto.Id;
        EventoId = evento.Id;
        SugestaoId = sugestao.Id;
    }
}
