using System.Security.Cryptography;
using ClyvoVet.Api.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace ClyvoVet.Api.Extensions;

public static class AutenticacaoJwtExtensions
{
    /// <summary>Emissor e público que a API Java carimba nos tokens dela.</summary>
    private const string EmissorPadrao = "clyvovet-api-java";
    private const string PublicoPadrao = "clyvovet";

    public static IServiceCollection AddAutenticacaoJwt(this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        // As opções são montadas na RESOLUÇÃO, e não aqui, lendo IConfiguration injetada. O
        // Program.cs roda antes de o WebApplicationFactory aplicar a configuração dos testes: ler
        // builder.Configuration["Jwt:Secret"] neste método ignoraria o segredo que o teste define.
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IConfiguration, ILoggerFactory>(ConfigurarJwtBearer);

        services.AddSingleton<IAuthorizationHandler, AcessoHandler>();
        services.AddAuthorization(opcoes =>
        {
            opcoes.AddPolicy(PoliticasDeAcesso.Autenticado, politica =>
                politica.AddRequirements(new AcessoRequirement()));

            opcoes.AddPolicy(PoliticasDeAcesso.Equipe, politica =>
                politica.AddRequirements(new AcessoRequirement(
                    ClaimsDoToken.PerfilAdmin, ClaimsDoToken.PerfilVeterinario)));
        });

        return services;
    }

    private static void ConfigurarJwtBearer(JwtBearerOptions opcoes, IConfiguration configuracao, ILoggerFactory logs)
    {
        var log = logs.CreateLogger("ClyvoVet.Api.AutenticacaoJwt");

        // Sem chave utilizável a API continua no ar e responde 401: no lugar da chave ausente vai uma
        // aleatória de 32 bytes que ninguém conhece, então NENHUM token confere. Não depende de o
        // JwtBearerOptions tolerar uma chave nula.
        var chave = ChaveDoJwt.Ler(configuracao["Jwt:Secret"], log)
                    ?? new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(32));

        // Sem mapeamento: as claims mantêm os nomes que a API Java usa (sub, perfil, tipo, tutorId).
        opcoes.MapInboundClaims = false;

        opcoes.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = chave,

            // A API Java carimba iss e aud de propósito, justamente porque estas duas validações
            // vêm ligadas por padrão. Emitir os campos é melhor do que desligar a verificação.
            ValidateIssuer = true,
            ValidIssuer = ValorOu(configuracao["Jwt:Emissor"], EmissorPadrao),
            ValidateAudience = true,
            ValidAudience = ValorOu(configuracao["Jwt:Publico"], PublicoPadrao),

            ValidateLifetime = true,
            // O padrão do .NET é 5 minutos, o que estenderia em um terço a vida de um access token
            // de 15. Os dois lados rodam com relógio de nuvem.
            ClockSkew = TimeSpan.FromSeconds(30),

            // Só HS256. Sem isto, a lista de algoritmos aceitos é a padrão.
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],

            NameClaimType = ClaimsDoToken.UsuarioId,
            RoleClaimType = ClaimsDoToken.Perfil,
        };

        opcoes.Events = new JwtBearerEvents { OnTokenValidated = ExigirAccessToken };
    }

    /// <summary>
    /// SÓ ACCESS TOKEN. A API Java gera access e refresh pelo mesmo caminho: mesma chave, mesmo
    /// subject, mesmo formato — só mudam a claim "tipo" e a validade. Sem esta checagem, o refresh
    /// de SETE DIAS, que fica guardado em disco no aparelho, viraria credencial válida aqui.
    ///
    /// <para>
    /// Vive na autenticação e não na política de propósito: as rotas do Telegram não têm
    /// <c>[Authorize]</c>, mas o <c>EscopoDoTutor</c> lê a identidade delas. Falhando aqui, um
    /// refresh token nunca vira identidade em rota nenhuma.
    /// </para>
    /// </summary>
    private static Task ExigirAccessToken(TokenValidatedContext contexto)
    {
        var principal = contexto.Principal;

        var ehAccess = principal?.FindFirst(ClaimsDoToken.Tipo)?.Value == ClaimsDoToken.TipoAccess;
        var temUsuario = !string.IsNullOrWhiteSpace(principal?.FindFirst(ClaimsDoToken.UsuarioId)?.Value);

        if (!ehAccess || !temUsuario)
            contexto.Fail("O token não é um access token de usuário.");

        return Task.CompletedTask;
    }

    private static string ValorOu(string? valor, string padrao) =>
        string.IsNullOrWhiteSpace(valor) ? padrao : valor;
}
