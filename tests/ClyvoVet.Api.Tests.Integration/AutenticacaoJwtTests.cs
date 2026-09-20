using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace ClyvoVet.Api.Tests.Integration;

public class AutenticacaoJwtTests : IClassFixture<AutenticacaoJwtFixture>
{
    private const string RotaAutenticada = "/teste-autenticacao/autenticado";
    private const string RotaEquipe = "/teste-autenticacao/equipe";

    private readonly AutenticacaoJwtFixture _fixture;

    public AutenticacaoJwtTests(AutenticacaoJwtFixture fixture) => _fixture = fixture;

    /// <summary>Manda o valor do header <c>Authorization</c> como veio, ou nenhum.</summary>
    private async Task<HttpStatusCode> GetAsync(string rota, string? cabecalho)
    {
        var cliente = _fixture.CreateClient();
        if (cabecalho is not null)
            cliente.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", cabecalho);
        return (await cliente.GetAsync(rota)).StatusCode;
    }

    private Task<HttpStatusCode> GetComTokenAsync(string rota, string token) =>
        GetAsync(rota, "Bearer " + token);

    // ================================================================
    // O que passa
    // ================================================================

    [Fact]
    public async Task Autenticado_AccessTokenValido_RetornaOk()
    {
        Assert.Equal(HttpStatusCode.OK, await GetComTokenAsync(RotaAutenticada, TokensDeTeste.Access()));
    }

    // ================================================================
    // O que precisa ser recusado — 401
    // ================================================================

    [Fact]
    public async Task Autenticado_SemToken_RetornaUnauthorized()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, await GetAsync(RotaAutenticada, cabecalho: null));
    }

    [Fact]
    public async Task Autenticado_RefreshToken_RetornaUnauthorized()
    {
        // O refresh dura 7 dias, fica em disco no aparelho, e a revogação por jti que a Java faz no
        // logout não chega até aqui. Aceitá-lo seria trocar uma credencial de 15 minutos por uma
        // de uma semana.
        Assert.Equal(HttpStatusCode.Unauthorized,
            await GetComTokenAsync(RotaAutenticada, TokensDeTeste.Access(tipo: "refresh")));
    }

    [Fact]
    public async Task Autenticado_TokenExpirado_RetornaUnauthorized()
    {
        Assert.Equal(HttpStatusCode.Unauthorized,
            await GetComTokenAsync(RotaAutenticada, TokensDeTeste.Access(minutosDeVida: -10)));
    }

    [Fact]
    public async Task Autenticado_TokenAssinadoComOutraChave_RetornaUnauthorized()
    {
        Assert.Equal(HttpStatusCode.Unauthorized,
            await GetComTokenAsync(RotaAutenticada, TokensDeTeste.Access(chaveCrua: RandomNumberGenerator.GetBytes(32))));
    }

    [Fact]
    public async Task Autenticado_ChaveDerivadaDeUtf8_RetornaUnauthorized()
    {
        // É EXATAMENTE o bug que este arquivo existe para impedir. Todo tutorial de ASP.NET escreve
        // Encoding.UTF8.GetBytes(config["Jwt:Secret"]). Com o mesmo valor de app setting, a chave
        // resultante é OUTRA, e nenhuma assinatura confere.
        var token = TokensDeTeste.Access(chaveCrua: Encoding.UTF8.GetBytes(TokensDeTeste.Segredo));

        Assert.Equal(HttpStatusCode.Unauthorized, await GetComTokenAsync(RotaAutenticada, token));
    }

    [Theory]
    [InlineData("outro-emissor", TokensDeTeste.Publico)]
    [InlineData(TokensDeTeste.Emissor, "outro-publico")]
    public async Task Autenticado_EmissorOuPublicoErrado_RetornaUnauthorized(string emissor, string publico)
    {
        Assert.Equal(HttpStatusCode.Unauthorized,
            await GetComTokenAsync(RotaAutenticada, TokensDeTeste.Access(emissor: emissor, publico: publico)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("nao-e-um-jwt")]
    [InlineData("a.b.c")]
    public async Task Autenticado_TokenMalformado_RetornaUnauthorized(string token)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, await GetComTokenAsync(RotaAutenticada, token));
    }

    [Fact]
    public async Task Autenticado_TokenSemSub_RetornaUnauthorized()
    {
        Assert.Equal(HttpStatusCode.Unauthorized,
            await GetComTokenAsync(RotaAutenticada, TokensDeTeste.Access(comSub: false)));
    }

    // ================================================================
    // Perfil — 403
    // ================================================================

    [Fact]
    public async Task Equipe_PerfilTutor_RetornaForbidden()
    {
        // Autenticado, mas sem o perfil: 403 e não 401 — a credencial foi aceita.
        Assert.Equal(HttpStatusCode.Forbidden,
            await GetComTokenAsync(RotaEquipe, TokensDeTeste.Access(perfil: "TUTOR")));
    }

    [Theory]
    [InlineData("ADMIN")]
    [InlineData("VETERINARIO")]
    public async Task Equipe_PerfilDeEquipe_RetornaOk(string perfil)
    {
        // ADMIN e VETERINARIO não têm tutorId — e a rota de equipe não depende disso.
        Assert.Equal(HttpStatusCode.OK,
            await GetComTokenAsync(RotaEquipe, TokensDeTeste.Access(tutorId: null, perfil: perfil)));
    }
}

public class AutenticacaoJwtSemSegredoTests : IClassFixture<SemSegredoFixture>
{
    private readonly SemSegredoFixture _fixture;

    public AutenticacaoJwtSemSegredoTests(SemSegredoFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Autenticado_SemSegredoConfigurado_AAplicacaoSobeEResponde401()
    {
        // Arrange
        var cliente = _fixture.CreateClient();
        cliente.DefaultRequestHeaders.Add("Authorization", "Bearer " + TokensDeTeste.Access());

        // Act
        var resposta = await cliente.GetAsync("/teste-autenticacao/autenticado");

        // Assert
        // Sem Jwt__Secret nenhum token confere. A app SOBE (este teste só chega aqui se subiu) e a
        // falha é visível: 401, nunca queda de processo.
        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/swagger/v1/swagger.json")]
    public async Task RotasDeInfraestrutura_SemSegredo_ContinuamRespondendo(string rota)
    {
        var resposta = await _fixture.CreateClient().GetAsync(rota);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }
}

public class AutenticacaoJwtAlavancaTests : IClassFixture<ExigirTokenDesligadoFixture>
{
    private readonly ExigirTokenDesligadoFixture _fixture;

    public AutenticacaoJwtAlavancaTests(ExigirTokenDesligadoFixture fixture) => _fixture = fixture;

    [Theory]
    [InlineData("/teste-autenticacao/autenticado")]
    [InlineData("/teste-autenticacao/equipe")]
    public async Task ExigirTokenDesligado_SemToken_AutorizaAsPoliticas(string rota)
    {
        // A alavanca existe porque a produção está no ar e o Jwt__Secret precisa estar no Render
        // antes do deploy: desligada, a API volta a se comportar como antes da F2.
        var resposta = await _fixture.CreateClient().GetAsync(rota);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }
}
