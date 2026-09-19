using System.Net;
using System.Net.Http.Json;
using ClyvoVet.Api.DTOs.Request;
using ClyvoVet.Api.DTOs.Response;
using ClyvoVet.Api.Enums;

namespace ClyvoVet.Api.Tests.Integration;

[Collection(IntegrationTestCollection.Name)]
public class ProdutoEndpointsTests
{
    private readonly HttpClient _client;
    private readonly IntegrationTestFixture _fixture;

    public ProdutoEndpointsTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateClient();
    }

    [Fact]
    public async Task GetAll_SemApiKey_RetornaUnauthorized()
    {
        // Arrange — client "cru" do TestServer, sem passar pelo ConfigureClient
        // da fixture (que injeta o X-Api-Key automaticamente pros outros testes).
        var clientSemApiKey = _fixture.Server.CreateClient();

        // Act
        var response = await clientSemApiKey.GetAsync("/api/v1/produtos");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_ApiKeyErrada_RetornaUnauthorized()
    {
        // Arrange
        var clientComApiKeyErrada = _fixture.Server.CreateClient();
        clientComApiKeyErrada.DefaultRequestHeaders.Add("X-Api-Key", "chave-errada");

        // Act
        var response = await clientComApiKeyErrada.GetAsync("/api/v1/produtos");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_ComProdutoSeedCadastrado_RetornaOkComLista()
    {
        // Arrange
        // (produto de seed já criado pela fixture)

        // Act
        var response = await _client.GetAsync("/api/v1/produtos");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var produtos = await response.Content.ReadFromJsonAsync<List<ProdutoResponse>>();
        Assert.NotNull(produtos);
        Assert.NotEmpty(produtos);
    }

    [Fact]
    public async Task GetAll_PageInvalido_RetornaBadRequest()
    {
        // Arrange
        const string url = "/api/v1/produtos?page=0";

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_PageSizeLimitado_RespeitaOTamanhoDaPagina()
    {
        // Arrange
        for (var i = 0; i < 3; i++)
        {
            var request = new ProdutoRequest
            {
                Nome = $"Produto Paginacao {Guid.NewGuid()}",
                Categoria = CategoriaEnum.Acessorio,
                Preco = 10m,
                EspecieIndicada = EspecieEnum.Todos,
                Ativo = true
            };
            await _client.PostAsJsonAsync("/api/v1/produtos", request);
        }

        // Act
        var response = await _client.GetAsync("/api/v1/produtos?page=1&pageSize=2");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<ProdutoResponse>>();
        Assert.NotNull(result);
        Assert.True(result!.Count <= 2);
    }

    [Fact]
    public async Task Create_DadosValidos_RetornaCreatedComProduto()
    {
        // Arrange
        var request = new ProdutoRequest
        {
            Nome = "Shampoo Pet Neutro 500ml",
            Descricao = "Shampoo hipoalergênico.",
            Categoria = CategoriaEnum.Acessorio,
            Preco = 28.90m,
            EspecieIndicada = EspecieEnum.Todos,
            Ativo = true
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/produtos", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ProdutoResponse>();
        Assert.NotNull(created);
        Assert.False(string.IsNullOrWhiteSpace(created!.Id));
        Assert.Equal(request.Nome, created.Nome);
    }

    [Fact]
    public async Task Create_PrecoNegativo_RetornaBadRequest()
    {
        // Arrange
        var request = new ProdutoRequest
        {
            Nome = "Produto Inválido",
            Categoria = CategoriaEnum.Racao,
            Preco = -50m,
            EspecieIndicada = EspecieEnum.Cachorro,
            Ativo = true
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/produtos", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetById_IdInexistente_RetornaNotFound()
    {
        // Arrange
        const string idInexistente = "id-que-nao-existe";

        // Act
        var response = await _client.GetAsync($"/api/v1/produtos/{idInexistente}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateELerEDeletar_FluxoCompleto_RetornaStatusEsperadosEmCadaEtapa()
    {
        // Arrange
        var request = new ProdutoRequest
        {
            Nome = "Produto Para Deletar",
            Categoria = CategoriaEnum.Outro,
            Preco = 10m,
            EspecieIndicada = EspecieEnum.Outro,
            Ativo = true
        };

        // Act — cria
        var createResponse = await _client.PostAsJsonAsync("/api/v1/produtos", request);
        var created = await createResponse.Content.ReadFromJsonAsync<ProdutoResponse>();

        // Act — busca
        var getResponse = await _client.GetAsync($"/api/v1/produtos/{created!.Id}");

        // Act — atualiza
        request.Nome = "Produto Atualizado";
        var updateResponse = await _client.PutAsJsonAsync($"/api/v1/produtos/{created.Id}", request);

        // Act — deleta
        var deleteResponse = await _client.DeleteAsync($"/api/v1/produtos/{created.Id}");

        // Act — confirma remoção
        var getAfterDeleteResponse = await _client.GetAsync($"/api/v1/produtos/{created.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getAfterDeleteResponse.StatusCode);
    }
    // ─────────────────────────────────────────────────────────────────────────
    // Os dois filtros que a vitrine por animal exigiu
    //
    // Ambos falhariam antes da correcao, e e por isso que existem: o primeiro
    // escondia produto universal de quem perguntava por especie, e o segundo
    // deixava produto desativado na prateleira do tutor.
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Perguntar "o que serve para um cachorro" precisa trazer tambem o que
    /// serve para qualquer animal -- consulta de rotina, banho, coleira neutra.
    /// Com igualdade exata esses sumiam justamente da tela feita para mostra-los.
    /// </summary>
    [Fact]
    public async Task GetAll_PorEspecie_TrazTambemOsMarcadosComoTodos()
    {
        var universal = new ProdutoRequest
        {
            Nome = $"Banho e Tosa Universal {Guid.NewGuid():N}",
            Categoria = CategoriaEnum.Servico,
            Preco = 70m,
            EspecieIndicada = EspecieEnum.Todos,
            Ativo = true
        };
        var soDeCachorro = new ProdutoRequest
        {
            Nome = $"Racao Canina {Guid.NewGuid():N}",
            Categoria = CategoriaEnum.Racao,
            Preco = 150m,
            EspecieIndicada = EspecieEnum.Cachorro,
            Ativo = true
        };
        await _client.PostAsJsonAsync("/api/v1/produtos", universal);
        await _client.PostAsJsonAsync("/api/v1/produtos", soDeCachorro);

        var response = await _client.GetAsync(
            "/api/v1/produtos?especieIndicada=Cachorro&pageSize=100");
        var result = await response.Content.ReadFromJsonAsync<List<ProdutoResponse>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.Contains(result!, p => p.Nome == universal.Nome);
        Assert.Contains(result!, p => p.Nome == soDeCachorro.Nome);
        // E continua sendo um recorte: gato nao entra.
        Assert.DoesNotContain(result!, p => p.EspecieIndicada == EspecieEnum.Gato);
    }

    /// <summary>
    /// O `Ativo` existia no modelo, no banco e no update, e nenhuma consulta o
    /// lia: desativar um produto nao o tirava de lugar nenhum.
    /// </summary>
    [Fact]
    public async Task GetAll_ComAtivoTrue_NaoTrazOsDesativados()
    {
        var desativado = new ProdutoRequest
        {
            Nome = $"Produto Fora de Linha {Guid.NewGuid():N}",
            Categoria = CategoriaEnum.Outro,
            Preco = 10m,
            EspecieIndicada = EspecieEnum.Todos,
            Ativo = false
        };
        await _client.PostAsJsonAsync("/api/v1/produtos", desativado);

        var soAtivos = await (await _client.GetAsync("/api/v1/produtos?ativo=true&pageSize=100"))
            .Content.ReadFromJsonAsync<List<ProdutoResponse>>();
        var todos = await (await _client.GetAsync("/api/v1/produtos?pageSize=100"))
            .Content.ReadFromJsonAsync<List<ProdutoResponse>>();

        Assert.DoesNotContain(soAtivos!, p => p.Nome == desativado.Nome);
        // Sem o filtro o comportamento e o de sempre -- a gestao da clinica
        // precisa enxergar o que desativou para poder reativar.
        Assert.Contains(todos!, p => p.Nome == desativado.Nome);
    }
    /// <summary>
    /// Porte segue a mesma regra da especie: pedir um porte traz junto o que
    /// serve a qualquer porte.
    ///
    /// Sem isso, filtrar por PEQUENO esconderia consulta, shampoo neutro e
    /// vermifugo em gotas -- a maior parte do catalogo, que nao tem porte. O
    /// tutor de um cachorro pequeno veria uma loja quase vazia.
    /// </summary>
    [Fact]
    public async Task GetAll_PorPorte_TrazOEspecificoEOsSemPorte()
    {
        var soPequeno = new ProdutoRequest
        {
            Nome = $"Racao Racas Pequenas {Guid.NewGuid():N}",
            Categoria = CategoriaEnum.Racao,
            Preco = 120m,
            EspecieIndicada = EspecieEnum.Cachorro,
            PorteIndicado = PorteEnum.Pequeno,
            Ativo = true
        };
        var soGrande = new ProdutoRequest
        {
            Nome = $"Racao Racas Grandes {Guid.NewGuid():N}",
            Categoria = CategoriaEnum.Racao,
            Preco = 190m,
            EspecieIndicada = EspecieEnum.Cachorro,
            PorteIndicado = PorteEnum.Grande,
            Ativo = true
        };
        var semPorte = new ProdutoRequest
        {
            Nome = $"Consulta Sem Porte {Guid.NewGuid():N}",
            Categoria = CategoriaEnum.Servico,
            Preco = 150m,
            EspecieIndicada = EspecieEnum.Todos,
            PorteIndicado = PorteEnum.Todos,
            Ativo = true
        };
        foreach (var r in new[] { soPequeno, soGrande, semPorte })
            await _client.PostAsJsonAsync("/api/v1/produtos", r);

        var response = await _client.GetAsync(
            "/api/v1/produtos?especieIndicada=Cachorro&porteIndicado=Pequeno&pageSize=100");
        var result = await response.Content.ReadFromJsonAsync<List<ProdutoResponse>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(result!, p => p.Nome == soPequeno.Nome);
        Assert.Contains(result!, p => p.Nome == semPorte.Nome);
        // e continua sendo recorte: a racao de raca grande fica de fora
        Assert.DoesNotContain(result!, p => p.Nome == soGrande.Nome);
    }

    /// <summary>
    /// Produto criado sem declarar porte nasce <c>Todos</c>.
    ///
    /// E o que preserva o comportamento anterior a coluna: quem ja chamava o
    /// POST sem o campo continua criando produto que aparece para qualquer
    /// animal, em vez de um que nao aparece para nenhum.
    /// </summary>
    [Fact]
    public async Task Create_SemPorte_NasceComoTodos()
    {
        var request = new ProdutoRequest
        {
            Nome = $"Produto Sem Porte Declarado {Guid.NewGuid():N}",
            Categoria = CategoriaEnum.Outro,
            Preco = 30m,
            EspecieIndicada = EspecieEnum.Todos,
            Ativo = true
        };

        var response = await _client.PostAsJsonAsync("/api/v1/produtos", request);
        var criado = await response.Content.ReadFromJsonAsync<ProdutoResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(PorteEnum.Todos, criado!.PorteIndicado);
    }
    // ─────────────────────────────────────────────────────────────────────────
    // Paridade de regra com a API Java
    //
    // As duas APIs sao do mesmo produto e respondiam diferente a mesma
    // pergunta. Estes testes fixam as respostas equivalentes.
    // ─────────────────────────────────────────────────────────────────────────

    // O 409 de "registro em uso" NAO e testado aqui, de proposito: estes testes
    // rodam em EF Core InMemory, que nao aplica chave estrangeira, e o mesmo
    // delete responde 204 la dentro. Um teste escrito aqui passaria pelo motivo
    // errado. A regra e coberta em MapaDeErroTests (unidade) e foi verificada
    // contra o MySQL do compose, onde o DELETE responde 409.

    /// <summary>
    /// Nome com menos de 3 caracteres e recusado, como do lado Java.
    ///
    /// O <c>[Required]</c> sozinho barra vazio e nulo, mas aceitava "X" -- e a
    /// API Java devolve 400 com "size must be between 3 and 100" para o mesmo
    /// nome. Mesma pergunta, mesma resposta.
    /// </summary>
    [Fact]
    public async Task Create_NomeComUmaLetra_Responde400()
    {
        var request = new ProdutoRequest
        {
            Nome = "X",
            Categoria = CategoriaEnum.Outro,
            Preco = 10m,
            EspecieIndicada = EspecieEnum.Todos,
            Ativo = true
        };

        var response = await _client.PostAsJsonAsync("/api/v1/produtos", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
