using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using ClyvoVet.Api.Data;
using ClyvoVet.Api.Enums;
using ClyvoVet.Api.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace ClyvoVet.Api.Tests.Integration;

/// <summary>
/// O recorte por dono, <b>ligado</b>.
///
/// <para>
/// Os outros 69 testes de integração rodam com <c>Api:EscopoPorTutor</c>
/// desligado e sem mandar <c>Authorization</c> — e continuarem verdes é
/// justamente a prova de que, desligada, a camada não muda nada. Esta classe sobe
/// uma instância própria com a flag ligada para provar o outro lado, sem duplicar
/// a suíte inteira.
/// </para>
/// </summary>
public class EscopoLigadoFixture : WebApplicationFactory<Program>
{
    /// <summary>O mesmo segredo de teste da API Java.</summary>
    private const string Segredo = "dGVzdGUtY2x5dm92ZXQtY2hhdmUtaG1hYy1zaGEyNTYtcGFyYS10ZXN0ZXM=";

    private readonly string _banco = $"EscopoTestDb-{Guid.NewGuid()}";

    public string TutorA { get; private set; } = null!;
    public string AnimalA { get; private set; } = null!;
    public string LembreteA { get; private set; } = null!;
    public string TutorB { get; private set; } = null!;
    public string AnimalB { get; private set; } = null!;
    public string LembreteB { get; private set; } = null!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configuracao) =>
            configuracao.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Api:EscopoPorTutor"] = "true",
                ["Jwt:Secret"] = Segredo,
            }));

        builder.ConfigureServices(services =>
        {
            var descritor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descritor is not null) services.Remove(descritor);

            services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(_banco));

            using var escopo = services.BuildServiceProvider().CreateScope();
            var db = escopo.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
            Semear(db);
        });
    }

    private void Semear(AppDbContext db)
    {
        (TutorA, AnimalA, LembreteA) = Criar(db, "Tutor A", "11111111111", "Rex");
        (TutorB, AnimalB, LembreteB) = Criar(db, "Tutor B", "22222222222", "Mel");
        db.SaveChanges();
    }

    private static (string, string, string) Criar(AppDbContext db, string nome, string cpf, string pet)
    {
        var tutor = new Tutor { Id = Guid.NewGuid().ToString(), Nome = nome, Cpf = cpf };
        var animal = new Animal
        {
            Id = Guid.NewGuid().ToString(), Nome = pet, Especie = "CACHORRO", TutorId = tutor.Id,
        };
        var lembrete = new Lembrete
        {
            Id = Guid.NewGuid().ToString(),
            AnimalId = animal.Id,
            Titulo = $"Vacina do {pet}",
            Tipo = TipoLembreteEnum.Vacina,
            Status = StatusLembreteEnum.Pendente,
            AgendadoEm = DateTime.UtcNow.AddDays(30),
            CriadoEm = DateTime.UtcNow,
        };

        db.Tutores.Add(tutor);
        db.Animais.Add(animal);
        db.Lembretes.Add(lembrete);
        return (tutor.Id, animal.Id, lembrete.Id);
    }

    protected override void ConfigureClient(HttpClient client)
    {
        base.ConfigureClient(client);
        client.DefaultRequestHeaders.Add("X-Api-Key", "SUA_API_KEY");
    }

    /// <summary>Um access token como o que a API Java emite.</summary>
    public static string Token(string? tutorId, string perfil = "TUTOR")
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
            new("perfil", perfil),
            new("tipo", "access"),
        };
        if (tutorId is not null) claims.Add(new Claim("tutorId", tutorId));

        var token = new JwtSecurityToken(
            issuer: "clyvovet-api-java",
            audience: "clyvovet",
            claims: claims,
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Convert.FromBase64String(Segredo)),
                SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public class EscopoPorTutorEndpointsTests : IClassFixture<EscopoLigadoFixture>
{
    private readonly EscopoLigadoFixture _fixture;

    public EscopoPorTutorEndpointsTests(EscopoLigadoFixture fixture) => _fixture = fixture;

    private HttpClient ClienteDe(string? tutorId, string perfil = "TUTOR")
    {
        var cliente = _fixture.CreateClient();
        cliente.DefaultRequestHeaders.Add("Authorization", "Bearer " + EscopoLigadoFixture.Token(tutorId, perfil));
        return cliente;
    }

    private static async Task<List<JsonElement>> ItensAsync(HttpResponseMessage resposta) =>
        (await resposta.Content.ReadFromJsonAsync<List<JsonElement>>())!;

    // ================================================================
    // Sem identidade não passa — e nunca "passa sem filtro"
    // ================================================================

    [Fact]
    public async Task GetAll_ComEscopoLigadoESemToken_RetornaForbidden()
    {
        // Arrange
        // Só a X-Api-Key, que é o que o app manda hoje.
        var cliente = _fixture.CreateClient();

        // Act
        var resposta = await cliente.GetAsync("/api/v1/lembretes");

        // Assert
        // 403 e não 200-sem-filtro. O idioma preguiçoso de "filtro opcional"
        // devolveria a base inteira exatamente aqui.
        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task GetAll_ComTokenDeAdminSemTutorId_RetornaForbidden()
    {
        // Arrange
        var cliente = ClienteDe(tutorId: null, perfil: "ADMIN");

        // Act
        var resposta = await cliente.GetAsync("/api/v1/lembretes");

        // Assert
        // ADMIN e VETERINARIO não têm tutor. Sem tratamento explícito, seriam os
        // perfis que enxergariam tudo — o oposto do pretendido.
        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    // ================================================================
    // Com identidade, vê o seu e só o seu
    // ================================================================

    [Fact]
    public async Task GetAll_ComTokenDoTutor_RetornaApenasOsLembretesDele()
    {
        // Arrange
        var cliente = ClienteDe(_fixture.TutorA);

        // Act
        var resposta = await cliente.GetAsync("/api/v1/lembretes");

        // Assert
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var itens = await ItensAsync(resposta);
        Assert.Single(itens);
        Assert.Equal(_fixture.AnimalA, itens[0].GetProperty("animalId").GetString());
    }

    [Fact]
    public async Task GetAll_FiltrandoPeloAnimalDeOutroTutor_NaoVaza()
    {
        // Arrange
        // O animalId é conveniência de filtro, não credencial. Se ele
        // SUBSTITUÍSSE o recorte em vez de somar, seria o próprio bypass.
        var cliente = ClienteDe(_fixture.TutorA);

        // Act
        var resposta = await cliente.GetAsync($"/api/v1/lembretes?animalId={_fixture.AnimalB}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Empty(await ItensAsync(resposta));
    }

    [Fact]
    public async Task GetById_LembreteDeOutroTutor_RetornaNotFound()
    {
        // Arrange
        var cliente = ClienteDe(_fixture.TutorA);

        // Act
        var resposta = await cliente.GetAsync($"/api/v1/lembretes/{_fixture.LembreteB}");

        // Assert
        // 404 e não 403: 403 confirmaria que o lembrete existe, e a existência já
        // é informação que este chamador não tem direito de obter.
        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task GetById_LembretePropio_RetornaOk()
    {
        // Arrange
        var cliente = ClienteDe(_fixture.TutorA);

        // Act
        var resposta = await cliente.GetAsync($"/api/v1/lembretes/{_fixture.LembreteA}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    // ================================================================
    // Escrita — o POST é o que dispara notificação
    // ================================================================

    [Fact]
    public async Task Create_NoAnimalDeOutroTutor_RetornaNotFound()
    {
        // Arrange
        // Sem esta checagem, qualquer portador da X-Api-Key marcaria lembrete no
        // animal alheio, e o dono receberia o WhatsApp sem nunca ter pedido.
        var cliente = ClienteDe(_fixture.TutorA);
        var corpo = new
        {
            animalId = _fixture.AnimalB,
            titulo = "Lembrete intruso",
            tipo = 0,
            agendadoEm = DateTime.UtcNow.AddDays(10),
            recorrente = false,
        };

        // Act
        var resposta = await cliente.PostAsJsonAsync("/api/v1/lembretes", corpo);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task Update_TentandoTransferirLembreteParaAnimalDeOutroTutor_RetornaNotFound()
    {
        // Arrange
        // O PUT reescreve o AnimalId. Checar só o dono do lembrete existente
        // deixaria transferi-lo para fora; checar só o animal novo deixaria
        // sequestrar o alheio. As duas metades precisam ser verificadas.
        var cliente = ClienteDe(_fixture.TutorA);
        var corpo = new
        {
            animalId = _fixture.AnimalB,
            titulo = "Transferido",
            tipo = 0,
            agendadoEm = DateTime.UtcNow.AddDays(10),
            recorrente = false,
        };

        // Act
        var resposta = await cliente.PutAsJsonAsync($"/api/v1/lembretes/{_fixture.LembreteA}", corpo);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task Delete_LembreteDeOutroTutor_RetornaNotFound()
    {
        // Arrange
        var cliente = ClienteDe(_fixture.TutorA);

        // Act
        var resposta = await cliente.DeleteAsync($"/api/v1/lembretes/{_fixture.LembreteB}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    // ================================================================
    // A camada não pode derrubar a infraestrutura
    // ================================================================

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    [InlineData("/swagger/v1/swagger.json")]
    public async Task RotasDeInfraestrutura_ComEscopoLigadoESemToken_ContinuamRespondendo(string rota)
    {
        // Arrange
        var cliente = _fixture.CreateClient();

        // Act
        var resposta = await cliente.GetAsync(rota);

        // Assert
        // O recorte vive nos controllers, não numa policy global. Uma
        // FallbackPolicy com [Authorize] derrubaria estas três de uma vez — e
        // health check quebrado tira a aplicação de rotação no App Service.
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }
}
