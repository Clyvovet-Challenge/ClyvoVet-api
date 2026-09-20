# F2 — Exceções globais e JWT: plano de implementação

> **Para quem for executar:** use `superpowers:executing-plans` (cada commit precisa do "sim"
> do dono do repositório, algo que um subagente não tem como pedir sozinho). As caixas `- [ ]`
> marcam o andamento. Antes de começar, leia [`../02-design.md`](../02-design.md) §6.1–§6.2 e
> [`../decisoes/ADR-003-jwt-bearer-e-authorize.md`](../decisoes/ADR-003-jwt-bearer-e-authorize.md).
> **Em qualquer divergência entre este plano e o design, vale o que está aqui** — a seção
> "Achados" lista o que o design não previa, descoberto lendo o código (commit `1285c34`) e
> rodando um protótipo descartável em 20/09/2026.

**Meta:** trocar o middleware "inerte" por autenticação/autorização padrão do ASP.NET
(`AddJwtBearer` + `[Authorize]`) e o lambda do `UseExceptionHandler` por um
`IExceptionHandler` com `ProblemDetails` — **sem quebrar o contrato com o app**.

**Arquitetura:** o `JwtBearer` valida o token com os mesmos parâmetros de hoje; duas políticas
(`Autenticado`, `Equipe`) são aplicadas **por controller/ação**, sem `FallbackPolicy`.
`IUsuarioAtual` passa a ler `HttpContext.User`. `MapaDeErro` continua decidindo status e
mensagem; o `TratadorGlobalDeExcecoes` só escreve a resposta.

**Stack:** .NET 8, `Microsoft.AspNetCore.Authentication.JwtBearer 8.0.11`,
`System.IdentityModel.Tokens.Jwt 8.3.1` (já presente), xUnit + Moq.

**Spec:** [`../02-design.md`](../02-design.md) §6.1, §6.2 · **Plano-mestre:** [`../03-plano.md`](../03-plano.md) F2.

## Restrições globais

Copiadas de `03-plano.md` e do `CLAUDE.md`; valem para toda tarefa.

- Alvo `net8.0`; EF Core `8.0.11`; Pomelo `8.0.2`; `Swashbuckle.AspNetCore 10.1.7`; `Microsoft.OpenApi 2.12.2`.
- **Contrato com o app:** erro mantém `error` (e `referencia` em falha de servidor); listagem devolve array JSON.
- **Autorização:** sem `FallbackPolicy`; chave JWT = `Convert.FromBase64String(segredo)` (nunca `UTF8.GetBytes`);
  só `tipo = access`; `tutorId` nulo **nega**; recurso alheio = 404; nunca `AllowAnyOrigin`.
- Não escrever em `animal`/`tutor`; sem migrations (F7).
- **Sem mudança de comportamento além do declarado** em "Achados" (A6, A7, A9 e o 401 sem Bearer da ADR-003).
- **Git:** commit só depois do "sim" do dono; um commit por tarefa; **sem** `--amend`/`rebase`/`reset --hard`;
  **sem** linha de atribuição nem menção a IA (nem ao arquivo de instruções do projeto) na mensagem; nada de `push`.
- Comentários explicam o **porquê**; ao migrar código, os comentários **vão junto**.
- Português para domínio/comentários; termos técnicos em inglês. Testes: AAA, `Metodo_Cenario_Resultado`.

## Decisão que depende do dono

**D1 — o app escreve produto?** O design (§6.2) põe `Produto` POST/PUT/DELETE em `Equipe`
(ADMIN ou VETERINARIO). O app é outro repositório: não dá para conferir daqui. Pista no
código: o comentário do `ProdutoController.GetAll` cita "a gestão da clínica" reativando
produtos — então **existe** tela que escreve produto, e ela só funciona se o usuário for
ADMIN/VETERINARIO. **Perguntar ao dono antes da T4.** Se o app escreve produto com perfil
`TUTOR`, `Produto` inteiro fica em `Autenticado` (a T4 já diz onde mudar) e a ADR-003 registra.

## Achados que ajustam o design

Verificados em 20/09/2026 lendo o código e com um protótipo descartável (Program.cs de ~150 linhas,
SDK 8.0.425, runtime 8.0.31) que exercitou o `JwtBearer` de verdade.

| # | Achado | Consequência neste plano |
|---|---|---|
| A1 | **O `JwtBearer 8.0.11` convive com o `IdentityModel 8.3.1` em execução.** Grafo resolvido: `Protocols` e `Protocols.OpenIdConnect` em **7.1.2**, todo o resto em **8.3.1**; sem `NU1605`. O protótipo validou token, recusou os inválidos e devolveu 401/403 como o design pede. | A T1 vira uma confirmação, não uma aposta. Registrar o grafo na ADR-003 (T7). |
| A2 | Os testes de token que existem hoje exercitam o `JwtSecurityTokenHandler`, **não** o `JwtBearer`. A T1 prova restauração e compilação; a prova de **execução** é o primeiro verde do `AutenticacaoJwtTests` (T2). | A T2 é **puramente aditiva** (nada é removido) e só depois dela a T3 apaga o middleware e o validador. Se a T2 acusar `TypeLoadException`/`MissingMethodException`, o remédio é fixar `Microsoft.IdentityModel.Protocols.OpenIdConnect 8.3.1` e refazer a T1. |
| A3 | **A configuração precisa ser lida na resolução, não no registro.** O `Program.cs` roda antes de o `WebApplicationFactory` aplicar `ConfigureAppConfiguration`; ler `builder.Configuration["Jwt:Secret"]` ali ignoraria o segredo dos testes. O `EscopoLigadoFixture` já depende disso e funciona porque o `ValidadorDeTokenJwt` lê `IConfiguration` injetado. | `AddOptions<JwtBearerOptions>(...).Configure<IConfiguration, ILoggerFactory>(...)` e um `AuthorizationHandler` que lê `Auth:ExigirToken` a cada requisição. **Nada** de `builder.Configuration[...]` em `AddAutenticacaoJwt`. |
| A4 | **`tipo = access` (e `sub`) vai no `OnTokenValidated`, não na política.** O `TelegramController` (`link/{tutorId}`, `vinculo/{tutorId}`) não tem `[Authorize]`, mas o `EscopoDoTutor` lê a identidade dele. Com a regra só na política, um *refresh token* viraria identidade ali. Protótipo: com `Auth:ExigirToken=false`, refresh e lixo seguem **sem identidade** (`sub=` vazio). | Diverge do design §6.2 (a política `Autenticado` lá inclui `tipo=access`); vale este plano. A política `Autenticado` fica = "autenticado". |
| A5 | **Sem segredo utilizável a app sobe e responde 401.** Protótipo: chave aleatória de 32 bytes no lugar da ausente → nenhum token confere, `/livre` segue 200. Não depende de detalhe de validação do `JwtBearerOptions` que eu não pude confirmar. | `ChaveDoJwt.Ler` devolve `null` (com log) e a extensão usa `RandomNumberGenerator.GetBytes(32)`. Ausente = `Warning` (design §6.2); base64 inválido/curto = `Error` (como hoje). |
| A6 | **Oito testes passariam pelo motivo errado.** `Produto/Lembrete/EventoPet/SugestaoProduto` × ("sem ApiKey", "ApiKey errada") usam `_fixture.Server.CreateClient()` (cliente cru, sem headers). Com `[Authorize]`, o 401 passa a vir do **JWT**, que roda **antes** do filtro da `X-Api-Key` — o filtro deixaria de ser testado sem ninguém notar (o mesmo ponto cego que o `MapaDeErroTests` descreve). | T4: esses oito passam a usar `_fixture.CreateClientComBearer()`, para o 401 só poder vir da `X-Api-Key`. |
| A7 | **Dois testes do escopo mudam de resultado:** `GetAll_ComEscopoLigadoESemToken` e `GetAllSugestoes_SemToken` esperam **403** hoje; sem Bearer o 401 do JWT chega antes. É a consequência declarada da ADR-003 ("quem não mandar Bearer passa a levar 401"). O caminho "escopo ligado + sem token → 403" **continua alcançável** com `Auth:ExigirToken=false`. | T4 renomeia os dois para `..._RetornaUnauthorized` e acrescenta um teste no fixture da alavanca que prova o 403 sem token. Nenhum caso é perdido. |
| A8 | **`/health` (completo) não é 200 no ambiente de teste:** o `TelegramHealthCheck` chama a Bot API de verdade com token falso → `Unhealthy` → 503. | O teste de rotas anônimas exige **200** em `/health/live`, `/health/ready`, `/metrics`, `/swagger/v1/swagger.json`, `/swagger/index.html`, e para `/health` exige só "≠ 401 e ≠ 403". |
| A9 | **O `Content-Type` do erro muda** de `application/json` para `application/problem+json` (protótipo). O corpo mantém `error` e `referencia`; somam-se `type`, `title`, `status`, `traceId`. Com `Accept: text/html` o `IProblemDetailsService` recusa escrever; sem *fallback* a resposta sairia **sem corpo**, quebrando o contrato. | O tratador tem *fallback* que escreve o mesmo `ProblemDetails` via `WriteAsJsonAsync` (`application/json`); há teste para isso (T6). Conferir na verificação manual que o app lê o corpo. |
| A10 | 401/403 do `JwtBearer` saem **sem corpo** (protótipo). O 401 da `X-Api-Key` (`UnauthorizedResult`) já é assim hoje, então o app já sabe lidar. | Fora de escopo: não escrever `error` em 401/403. |
| A11 | Ordem no pipeline: a autorização (JWT) roda **antes** dos filtros de ação (`X-Api-Key`). Com Bearer ausente e chave errada, quem responde é o JWT. | Já refletido em A6. |

## Mapa de arquivos

**Criar — `src/ClyvoVet.Api`:**

| Arquivo | Responsabilidade |
|---|---|
| `Security/ClaimsDoToken.cs` | nomes das claims (`sub`, `tipo`, `tutorId`, `perfil`), `TipoAccess`, perfis |
| `Security/PoliticasDeAcesso.cs` | nomes das políticas: `Autenticado`, `Equipe` |
| `Security/ChaveDoJwt.cs` | segredo base64 → `SymmetricSecurityKey?`; nunca lança |
| `Security/AcessoRequirement.cs` | requisito + handler (lê `Auth:ExigirToken` por requisição) |
| `Extensions/AutenticacaoJwtExtensions.cs` | `AddAutenticacaoJwt()` |
| `Extensions/TratamentoDeExcecoesExtensions.cs` | `AddTratamentoDeExcecoes()` |
| `Errors/TratadorGlobalDeExcecoes.cs` | `IExceptionHandler` |
| `Swagger/BearerSecurityDocumentFilter.cs` | cadeado Bearer nas ações com `[Authorize]` |

**Criar — testes:** `Unit/ChaveDoJwtTests.cs`, `Unit/UsuarioAtualHttpTests.cs`,
`Unit/TratadorGlobalDeExcecoesTests.cs`, `Integration/TokensDeTeste.cs`,
`Integration/ControllerDeAutenticacaoDeTeste.cs`, `Integration/AutenticacaoJwtFixtures.cs`,
`Integration/AutenticacaoJwtTests.cs`, `Integration/AutorizacaoEndpointsTests.cs`,
`Integration/TratamentoDeErrosEndpointsTests.cs`.

**Modificar:** `ClyvoVet.Api.csproj`, `Program.cs`, `Security/UsuarioAtualHttp.cs`,
`Extensions/DocumentacaoApiExtensions.cs`, 6 controllers (`Lembrete`, `EventoPet`,
`SugestaoProduto`, `SaudePreditiva`, `WidgetSaudePreditiva`, `Produto`),
`Integration/IntegrationTestFixture.cs`, `Integration/EscopoPorTutorEndpointsTests.cs`, os 4
`*EndpointsTests` com "ApiKey", `Integration/SwaggerEndpointsTests.cs`, e a documentação (T7).

**Apagar (T3):** `Middleware/IdentidadeMiddleware.cs`, `Security/ValidadorDeTokenJwt.cs`,
`Unit/ValidadorDeTokenJwtTests.cs`.

## Contagem esperada de testes

Linha de base (F1): **272** (176 unidade + 96 integração). O total é aproximado; **mais** que o
esperado é aceitável, **menos** que o da linha anterior da tabela é motivo para parar.

| Depois de | Acréscimo | Total ≈ |
|---|---|---|
| T1 | 0 | 272 |
| T2 | +8 `ChaveDoJwtTests`, +16 `AutenticacaoJwtTests`, +3 sem segredo, +2 alavanca | 301 |
| T3 | −20 `ValidadorDeTokenJwtTests` (migrados, ver tabela), +5 `UsuarioAtualHttpTests` | 286 |
| T4 | +6 rotas 401, +4 `Produto`, +6 infra anônima, +2 alavanca em rotas reais | 304 |
| T5 | +2 Swagger | 306 |
| T6 | +12 tratador (unidade), +2 pipeline | 320 |
| T7 | 0 | 320 |

## Convenções de execução

Todos os comandos rodam **da raiz do repositório** (a pasta que contém `ClyvoVet-api.slnx`).

```bash
# BUILD — o SDK 10 do sistema lê o .slnx e compila os projetos net8.0
dotnet build ClyvoVet-api.slnx

# TESTES — só o SDK 8 em ~/.dotnet tem o runtime 8 (ver CLAUDE.local.md); ele não lê .slnx
export DOTNET_ROOT="$HOME/.dotnet"
~/.dotnet/dotnet test tests/ClyvoVet.Api.Tests.Unit/ClyvoVet.Api.Tests.Unit.csproj
~/.dotnet/dotnet test tests/ClyvoVet.Api.Tests.Integration/ClyvoVet.Api.Tests.Integration.csproj
# um teste só: acrescente --filter "FullyQualifiedName~NomeDaClasse"
```

- **Nunca** use `DOTNET_ROLL_FORWARD` para "fazer passar" (CLAUDE.md).
- Se, ao alternar de SDK, o restore reclamar de `project.assets.json`, apague os `obj/` e rode de novo.
- `sed` do macOS é o BSD (`sed -i ''`). As anotações dos controllers usam `perl -0pi` (já testado numa cópia).
- Antes de qualquer commit: `git log -1 --format=%B | grep -icE "claude|anthropic|co-authored|generated"` deve dar **0**.

---

## T1 — Prova de compatibilidade do `JwtBearer`

**Files:** Modify `src/ClyvoVet.Api/ClyvoVet.Api.csproj`.

**Produces:** o pacote referenciado; o grafo do IdentityModel registrado para a ADR-003.

- [ ] **Step 1: adicionar o pacote**

```bash
export DOTNET_ROOT="$HOME/.dotnet"
~/.dotnet/dotnet add src/ClyvoVet.Api/ClyvoVet.Api.csproj package Microsoft.AspNetCore.Authentication.JwtBearer --version 8.0.11
```

- [ ] **Step 2: conferir o grafo** — esperado (A1): `Protocols` e `Protocols.OpenIdConnect` em 7.1.2, o resto em 8.3.1.

```bash
~/.dotnet/dotnet list src/ClyvoVet.Api/ClyvoVet.Api.csproj package --include-transitive | grep -i "IdentityModel"
```

Anote a saída (vai para a ADR-003 na T7).

- [ ] **Step 3: build e testes** — `NU1605`/`NU1608`/`NU1903` novos são bloqueio; qualquer outro warning novo precisa de justificativa.

```bash
dotnet build ClyvoVet-api.slnx
~/.dotnet/dotnet test tests/ClyvoVet.Api.Tests.Unit/ClyvoVet.Api.Tests.Unit.csproj
~/.dotnet/dotnet test tests/ClyvoVet.Api.Tests.Integration/ClyvoVet.Api.Tests.Integration.csproj
```

Esperado: **272 verdes** (176 + 96). Nada mudou em comportamento; a prova de execução vem na T2 (A2).

- [ ] **Step 4: commitar** (após o "sim")

```bash
git add src/ClyvoVet.Api/ClyvoVet.Api.csproj
git commit -m "chore: adiciona o JwtBearer 8.0.11 (prova de compatibilidade com o IdentityModel 8.3.1)"
```

---

## T2 — `AddAutenticacaoJwt` (aditiva)

**Files:** ver o mapa. Nada é removido nesta tarefa: `IdentidadeMiddleware` e `ValidadorDeTokenJwt` continuam de pé.

**Interfaces — Produces** (a T3/T4/T5 dependem destes nomes exatos):

```csharp
// Security/ClaimsDoToken.cs
public static class ClaimsDoToken { const string UsuarioId="sub", Tipo="tipo", TutorId="tutorId", Perfil="perfil",
                                     TipoAccess="access", PerfilAdmin="ADMIN", PerfilVeterinario="VETERINARIO"; }
// Security/PoliticasDeAcesso.cs
public static class PoliticasDeAcesso { const string Autenticado="Autenticado", Equipe="Equipe"; }
// Security/ChaveDoJwt.cs
public static SymmetricSecurityKey? ChaveDoJwt.Ler(string? segredo, ILogger log)
// Extensions/AutenticacaoJwtExtensions.cs
public static IServiceCollection AddAutenticacaoJwt(this IServiceCollection services)
// tests/Integration/TokensDeTeste.cs
public static string TokensDeTeste.Access(string? tutorId = "44444444-4444-4444-4444-000000000001", string perfil = "TUTOR",
    string tipo = "access", string emissor = Emissor, string publico = Publico, int minutosDeVida = 15,
    byte[]? chaveCrua = null, bool comSub = true)
```

### 2a — a chave (unidade)

- [ ] **Step 1: teste primeiro** — `tests/ClyvoVet.Api.Tests.Unit/ChaveDoJwtTests.cs`. Migra 8 dos 20 casos de `ValidadorDeTokenJwtTests` (ver a tabela na T3).

```csharp
using System.Text;
using ClyvoVet.Api.Security;
using Microsoft.Extensions.Logging.Abstractions;

namespace ClyvoVet.Api.Tests.Unit;

/// <summary>
/// A derivação da chave — o contrato que quebra tudo em silêncio. Os dois lados assinam com
/// HMAC-SHA256 sobre o MESMO valor de configuração, mas precisam derivar a chave do mesmo jeito:
/// o modo errado não dá erro de compilação nem aviso, dá 401 em cem por cento das chamadas.
/// </summary>
public class ChaveDoJwtTests
{
    /// <summary>O mesmo segredo de <c>src/test/resources/application.properties</c> da API Java.</summary>
    private const string Segredo = "dGVzdGUtY2x5dm92ZXQtY2hhdmUtaG1hYy1zaGEyNTYtcGFyYS10ZXN0ZXM=";

    [Fact]
    public void ChaveDerivada_DoBase64_ProduzOsMesmosBytesQueAApiJava()
    {
        // Arrange & Act
        var bytes = Convert.FromBase64String(Segredo);

        // Assert
        // A API Java trava a mesma asserção em JwtServiceTest. Se um dos lados mudar a forma de
        // derivar, um destes dois testes cai.
        Assert.Equal("teste-clyvovet-chave-hmac-sha256-para-testes", Encoding.UTF8.GetString(bytes));
        Assert.True(bytes.Length >= 32);
    }

    [Fact]
    public void Ler_SegredoValido_DevolveAChaveComOsBytesDecodificados()
    {
        // Arrange & Act
        var chave = ChaveDoJwt.Ler(Segredo, NullLogger.Instance);

        // Assert
        Assert.NotNull(chave);
        Assert.Equal(Convert.FromBase64String(Segredo), chave.Key);
    }

    [Theory]
    [InlineData(null)]                      // app setting ausente
    [InlineData("")]                        // operador "desligou" setando vazio
    [InlineData("   ")]
    [InlineData("SUA_JWT_SECRET")]          // placeholder, no estilo do appsettings.json
    [InlineData("nao!eh!base64!")]          // fora do alfabeto base64
    [InlineData("Y3VydG8=")]                // base64 válido, mas 6 bytes: curto para HS256
    public void Ler_SegredoAusenteOuInvalido_DevolveNuloENaoLanca(string? segredo)
    {
        // Arrange & Act
        var chave = ChaveDoJwt.Ler(segredo, NullLogger.Instance);

        // Assert
        // Nenhuma configuração desta API é capaz de impedir app.Run(), e esta camada não pode ser
        // a primeira: um erro de digitação numa app setting não vira aplicação que não sobe.
        Assert.Null(chave);
    }
}
```

- [ ] **Step 2: ver falhar**

```bash
export DOTNET_ROOT="$HOME/.dotnet"
~/.dotnet/dotnet test tests/ClyvoVet.Api.Tests.Unit/ClyvoVet.Api.Tests.Unit.csproj --filter "FullyQualifiedName~ChaveDoJwtTests"
```

Esperado: **erro de compilação** (`ChaveDoJwt` não existe).

- [ ] **Step 3: implementar** — `Security/ClaimsDoToken.cs`, `Security/PoliticasDeAcesso.cs`, `Security/ChaveDoJwt.cs`.
  Os comentários "BASE64, E NUNCA Encoding.UTF8.GetBytes" e "incapaz de derrubar o boot" **vêm do
  `ValidadorDeTokenJwt` para cá**, adaptados.

```csharp
// Security/ClaimsDoToken.cs
namespace ClyvoVet.Api.Security;

/// <summary>As claims que a API Java carimba no access token, e os valores que esta API confere.</summary>
public static class ClaimsDoToken
{
    public const string UsuarioId = "sub";
    public const string Tipo = "tipo";
    public const string TutorId = "tutorId";
    public const string Perfil = "perfil";

    public const string TipoAccess = "access";

    public const string PerfilAdmin = "ADMIN";
    public const string PerfilVeterinario = "VETERINARIO";
}
```

```csharp
// Security/PoliticasDeAcesso.cs
namespace ClyvoVet.Api.Security;

/// <summary>Nomes das políticas de <c>[Authorize(Policy = ...)]</c>. Definidas em AddAutenticacaoJwt.</summary>
public static class PoliticasDeAcesso
{
    /// <summary>Qualquer usuário com access token válido.</summary>
    public const string Autenticado = "Autenticado";

    /// <summary>Access token válido de ADMIN ou VETERINARIO.</summary>
    public const string Equipe = "Equipe";
}
```

```csharp
// Security/ChaveDoJwt.cs
using Microsoft.IdentityModel.Tokens;

namespace ClyvoVet.Api.Security;

/// <summary>
/// Monta a chave HMAC a partir de <c>Jwt:Secret</c>.
///
/// <para>
/// <b>É incapaz de derrubar o boot, e isso é requisito, não estilo.</b> Qualquer problema —
/// configuração ausente, valor que não é base64, chave curta demais — devolve <c>null</c> e
/// registra o motivo, em vez de lançar. Hoje nenhuma configuração desta API impede
/// <c>app.Run()</c>, e introduzir a primeira seria transformar um erro de digitação numa app
/// setting em aplicação que não sobe. Sem chave, a API continua no ar e responde 401: falha
/// visível, nunca queda de processo.
/// </para>
/// </summary>
public static class ChaveDoJwt
{
    /// <summary>HMAC-SHA256 exige 256 bits.</summary>
    private const int BytesMinimos = 32;

    public static SymmetricSecurityKey? Ler(string? segredo, ILogger log)
    {
        if (string.IsNullOrWhiteSpace(segredo))
        {
            log.LogWarning(
                "Jwt:Secret não configurado — nenhum token será aceito e as rotas protegidas " +
                "responderão 401. Defina Jwt__Secret com o MESMO valor de JWT_SECRET da API Java.");
            return null;
        }

        byte[] chave;
        try
        {
            // BASE64, E NUNCA Encoding.UTF8.GetBytes
            // A API Java faz Keys.hmacShaKeyFor(Decoders.BASE64.decode(segredo)): a chave são os
            // bytes DECODIFICADOS. O idioma comum em ASP.NET é UTF8.GetBytes(segredo), que produz
            // uma chave DIFERENTE a partir do MESMO valor de configuração — e então nenhuma
            // assinatura confere e o sintoma é 401 em cem por cento das chamadas, sem pista no log.
            // O teste ChaveDerivada_DoBase64_ProduzOsMesmosBytesQueAApiJava trava esse contrato.
            chave = Convert.FromBase64String(segredo);
        }
        catch (FormatException)
        {
            log.LogError(
                "Jwt:Secret não é base64 válido — nenhum token será aceito. O valor precisa ser o " +
                "MESMO passado à API Java em JWT_SECRET, que o decodifica de base64 antes de usar " +
                "como chave HMAC.");
            return null;
        }

        if (chave.Length < BytesMinimos)
        {
            log.LogError(
                "Jwt:Secret decodifica para {Bytes} bytes, e HMAC-SHA256 exige ao menos {Minimo} — " +
                "nenhum token será aceito.", chave.Length, BytesMinimos);
            return null;
        }

        return new SymmetricSecurityKey(chave);
    }
}
```

- [ ] **Step 4: ver passar** — mesmo comando do Step 2; esperado **8 verdes**.

### 2b — autenticação e políticas (integração, HTTP)

- [ ] **Step 5: apoio de teste** — `tests/ClyvoVet.Api.Tests.Integration/TokensDeTeste.cs` (o gerador de token que hoje vive
  no `ValidadorDeTokenJwtTests` e no `EscopoLigadoFixture`, com o `comSub` a mais):

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace ClyvoVet.Api.Tests.Integration;

/// <summary>Monta access tokens como a API Java monta: chave = base64 DECODIFICADO.</summary>
public static class TokensDeTeste
{
    /// <summary>O mesmo segredo de <c>src/test/resources/application.properties</c> da API Java.</summary>
    public const string Segredo = "dGVzdGUtY2x5dm92ZXQtY2hhdmUtaG1hYy1zaGEyNTYtcGFyYS10ZXN0ZXM=";
    public const string Emissor = "clyvovet-api-java";
    public const string Publico = "clyvovet";

    public static string Access(
        string? tutorId = "44444444-4444-4444-4444-000000000001",
        string perfil = "TUTOR",
        string tipo = "access",
        string emissor = Emissor,
        string publico = Publico,
        int minutosDeVida = 15,
        byte[]? chaveCrua = null,
        bool comSub = true)
    {
        var chave = new SymmetricSecurityKey(chaveCrua ?? Convert.FromBase64String(Segredo));

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("perfil", perfil),
            new("tipo", tipo),
        };
        if (comSub) claims.Add(new Claim(JwtRegisteredClaimNames.Sub, "11111111-1111-1111-1111-000000000001"));
        if (tutorId is not null) claims.Add(new Claim("tutorId", tutorId));

        var agora = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            issuer: emissor,
            audience: publico,
            claims: claims,
            // nbf sempre antes do exp: com minutosDeVida negativo (token que nasce expirado) um nbf
            // fixo em -1 ficaria DEPOIS do exp, e o próprio construtor lançaria — a validação nem
            // seria exercida.
            notBefore: agora.AddMinutes(Math.Min(-1, minutosDeVida - 1)),
            expires: agora.AddMinutes(minutosDeVida),
            signingCredentials: new SigningCredentials(chave, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

`tests/ClyvoVet.Api.Tests.Integration/ControllerDeAutenticacaoDeTeste.cs` — duas rotas que só existem nos testes,
para provar as políticas sem depender de nenhuma rota de negócio (a T4 é quem anota as reais):

```csharp
using ClyvoVet.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClyvoVet.Api.Tests.Integration;

[ApiController]
[Route("teste-autenticacao")]
public class ControllerDeAutenticacaoDeTeste : ControllerBase
{
    [HttpGet("autenticado")]
    [Authorize(Policy = PoliticasDeAcesso.Autenticado)]
    public IActionResult Autenticado() => Ok();

    [HttpGet("equipe")]
    [Authorize(Policy = PoliticasDeAcesso.Equipe)]
    public IActionResult Equipe() => Ok();
}
```

`tests/ClyvoVet.Api.Tests.Integration/AutenticacaoJwtFixtures.cs`:

```csharp
using ClyvoVet.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ClyvoVet.Api.Tests.Integration;

/// <summary>
/// A API inteira em memória com o segredo de teste e o <see cref="ControllerDeAutenticacaoDeTeste"/>.
/// As variantes mudam só a configuração — sem duplicar a suíte.
/// </summary>
public class AutenticacaoJwtFixture : WebApplicationFactory<Program>
{
    protected virtual string? Segredo => TokensDeTeste.Segredo;
    protected virtual bool? ExigirToken => null;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configuracao) =>
        {
            var valores = new Dictionary<string, string?> { ["Jwt:Secret"] = Segredo };
            if (ExigirToken is not null)
                valores["Auth:ExigirToken"] = ExigirToken.Value ? "true" : "false";
            configuracao.AddInMemoryCollection(valores);
        });

        builder.ConfigureServices(services =>
        {
            var descritor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descritor is not null) services.Remove(descritor);
            services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase($"AutenticacaoJwtDb-{Guid.NewGuid()}"));

            // Os controllers deste assembly de teste entram no pipeline da API.
            services.AddControllers().AddApplicationPart(typeof(ControllerDeAutenticacaoDeTeste).Assembly);
        });
    }
}

/// <summary>Sem <c>Jwt:Secret</c>: a API tem de subir e responder 401, não cair.</summary>
public class SemSegredoFixture : AutenticacaoJwtFixture
{
    protected override string? Segredo => null;
}

/// <summary>A alavanca <c>Auth:ExigirToken=false</c>: as políticas passam a autorizar sem token.</summary>
public class ExigirTokenDesligadoFixture : AutenticacaoJwtFixture
{
    protected override bool? ExigirToken => false;
}
```

`tests/ClyvoVet.Api.Tests.Integration/AutenticacaoJwtTests.cs` — os 16 + 3 + 2 casos. Aqui mora a
maior parte dos casos que eram do `ValidadorDeTokenJwtTests` (tabela na T3):

```csharp
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
```

- [ ] **Step 6: ver falhar**

```bash
~/.dotnet/dotnet test tests/ClyvoVet.Api.Tests.Integration/ClyvoVet.Api.Tests.Integration.csproj --filter "FullyQualifiedName~AutenticacaoJwt"
```

Esperado: os testes **compilam** (`PoliticasDeAcesso` já existe do Step 3) e **falham**: as políticas
`Autenticado`/`Equipe` ainda não estão registradas, então o pipeline de autorização lança
`InvalidOperationException` (*policy not found*) nas duas rotas de teste. Qualquer falha desses testes é
o vermelho esperado.

- [ ] **Step 7: implementar** — `Security/AcessoRequirement.cs`:

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace ClyvoVet.Api.Security;

/// <summary>Autenticado e, se <see cref="PerfisPermitidos"/> não for vazio, com um desses perfis.</summary>
public class AcessoRequirement(params string[] perfisPermitidos) : IAuthorizationRequirement
{
    public IReadOnlyCollection<string> PerfisPermitidos { get; } = perfisPermitidos;
}

/// <summary>
/// Avalia o <see cref="AcessoRequirement"/>. Lê <c>Auth:ExigirToken</c> a cada requisição — não no
/// registro — para que a alavanca funcione com uma app setting, sem redeploy, e para que os testes
/// consigam trocá-la.
/// </summary>
public class AcessoHandler(IConfiguration configuracao) : AuthorizationHandler<AcessoRequirement>
{
    private const string ChaveDaAlavanca = "Auth:ExigirToken";

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext contexto, AcessoRequirement requisito)
    {
        if (!ExigirToken || Autorizado(contexto.User, requisito))
            contexto.Succeed(requisito);

        return Task.CompletedTask;
    }

    private static bool Autorizado(ClaimsPrincipal usuario, AcessoRequirement requisito) =>
        usuario.Identity?.IsAuthenticated == true
        && (requisito.PerfisPermitidos.Count == 0 || requisito.PerfisPermitidos.Any(usuario.IsInRole));

    /// <summary>
    /// Padrão <c>true</c>. A leitura é tolerante de propósito, como a do <c>EscopoDoTutor</c>:
    /// <c>GetValue&lt;bool&gt;</c> lança com "1", e o interruptor de emergência não pode derrubar a
    /// API que ele existe para salvar. Valor ilegível <b>exige</b> token — falha para o lado seguro.
    /// </summary>
    private bool ExigirToken
    {
        get
        {
            var valor = configuracao[ChaveDaAlavanca];
            if (string.IsNullOrWhiteSpace(valor)) return true;

            var limpo = valor.Trim();
            return bool.TryParse(limpo, out var booleano)
                ? booleano
                : limpo is not ("0" or "nao" or "no");
        }
    }
}
```

`Extensions/AutenticacaoJwtExtensions.cs`:

```csharp
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
```

`Program.cs` — duas mudanças **aditivas**:

```csharp
// (1) junto do registro do ValidadorDeTokenJwt (que só sai na T3):
// JWT do ASP.NET e políticas de acesso; o porquê está em Extensions/AutenticacaoJwtExtensions.cs.
builder.Services.AddAutenticacaoJwt();

// (2) trocar
//     app.UseCors(PoliticaCors);
//     app.UseAuthorization();
// por
app.UseCors(PoliticaCors);
app.UseAuthentication();
app.UseAuthorization();
```

Mantenha o comentário que já existe acima do `UseCors` ("Antes de UseAuthorization: o preflight OPTIONS
chega sem credencial nenhuma…") e diga que agora vale para `UseAuthentication` também.

- [ ] **Step 8: ver passar — esta é a prova de execução do A2.**

```bash
~/.dotnet/dotnet test tests/ClyvoVet.Api.Tests.Integration/ClyvoVet.Api.Tests.Integration.csproj --filter "FullyQualifiedName~AutenticacaoJwt"
```

Esperado: **21 verdes** (16 + 3 + 2). Se aparecer `TypeLoadException`/`MissingMethodException`/`FileLoadException`,
**pare**: fixe `Microsoft.IdentityModel.Protocols.OpenIdConnect` em `8.3.1` no `csproj` da Api, refaça o
Step 3 da T1 e registre na ADR-003.

- [ ] **Step 9: suíte completa** — nada existente pode ter mudado (ninguém tem `[Authorize]` ainda).

```bash
dotnet build ClyvoVet-api.slnx
~/.dotnet/dotnet test tests/ClyvoVet.Api.Tests.Unit/ClyvoVet.Api.Tests.Unit.csproj
~/.dotnet/dotnet test tests/ClyvoVet.Api.Tests.Integration/ClyvoVet.Api.Tests.Integration.csproj
```

Esperado: ≈ **301** verdes.

- [ ] **Step 10: commitar** (após o "sim")

```bash
git add src/ClyvoVet.Api tests
git commit -m "feat: autenticação JWT padrão do ASP.NET (AddJwtBearer, políticas e alavanca Auth:ExigirToken)"
```

---

## T3 — `IUsuarioAtual` sobre `HttpContext.User`; remover o middleware e o validador

**Files:** Modify `Security/UsuarioAtualHttp.cs`, `Program.cs`; Delete `Middleware/IdentidadeMiddleware.cs`,
`Security/ValidadorDeTokenJwt.cs`, `tests/…Unit/ValidadorDeTokenJwtTests.cs`; Create `tests/…Unit/UsuarioAtualHttpTests.cs`.

**Consumes:** `ClaimsDoToken` (T2). **Produces:** `UsuarioAtualHttp` lendo `HttpContext.User` (mesma assinatura pública).

**Onde foi parar cada um dos 20 casos do `ValidadorDeTokenJwtTests`** — conferir antes de apagar o arquivo:

| Caso antigo | Novo lugar |
|---|---|
| `ChaveDerivada_DoBase64_…` | `ChaveDoJwtTests` (idêntico) |
| `Construtor_SegredoAusenteOuInvalido_…` (6) | `ChaveDoJwtTests.Ler_SegredoAusenteOuInvalido_…` (6) |
| `Ativo_ComSegredoValido_EhVerdadeiro` | `ChaveDoJwtTests.Ler_SegredoValido_…` |
| `TentarLer_TokenAssinadoComChaveDerivadaDeUtf8_Rejeita` | `AutenticacaoJwtTests.Autenticado_ChaveDerivadaDeUtf8_…` |
| `TentarLer_RefreshToken_Rejeita` | `AutenticacaoJwtTests.Autenticado_RefreshToken_…` |
| `TentarLer_TokenExpirado_Rejeita` | `AutenticacaoJwtTests.Autenticado_TokenExpirado_…` |
| `TentarLer_EmissorOuPublicoErrado_Rejeita` (2) | `AutenticacaoJwtTests.Autenticado_EmissorOuPublicoErrado_…` (2) |
| `TentarLer_EntradaInvalida_…` (5: nulo, "", "   ", "nao-e-um-jwt", "a.b.c") | `Autenticado_SemToken_…` (nulo) + `Autenticado_TokenMalformado_…` (4) |
| `TentarLer_AccessTokenValido_DevolveUsuarioTutorEPerfil` | `UsuarioAtualHttpTests` + `Autenticado_AccessTokenValido_RetornaOk` |
| `TentarLer_TokenDeAdminSemTutorId_…` | `UsuarioAtualHttpTests.Identidade_TokenDeAdminSemTutorId_…` |

- [ ] **Step 1: teste primeiro** — `tests/ClyvoVet.Api.Tests.Unit/UsuarioAtualHttpTests.cs`:

```csharp
using System.Security.Claims;
using ClyvoVet.Api.Security;
using Microsoft.AspNetCore.Http;

namespace ClyvoVet.Api.Tests.Unit;

/// <summary>
/// A tradução de <c>HttpContext.User</c> para a identidade que a Application entende. É o único
/// ponto em que as claims da API Java viram <c>IdentidadeDoChamador</c>; o
/// <c>EscopoPorTutorEndpointsTests</c> continua sendo a rede de segurança ponta a ponta.
/// </summary>
public class UsuarioAtualHttpTests
{
    private static UsuarioAtualHttp Criar(ClaimsPrincipal? usuario, bool comHttpContext = true)
    {
        var acessor = new HttpContextAccessor();
        if (comHttpContext)
        {
            var contexto = new DefaultHttpContext();
            if (usuario is not null) contexto.User = usuario;
            acessor.HttpContext = contexto;
        }
        return new UsuarioAtualHttp(acessor);
    }

    /// <summary>Um principal autenticado, como o JwtBearer o entrega (claims sem mapeamento).</summary>
    private static ClaimsPrincipal Autenticado(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, authenticationType: "Bearer"));

    [Fact]
    public void Identidade_AccessTokenValido_DevolveUsuarioTutorEPerfil()
    {
        // Arrange
        var usuario = Autenticado(
            new Claim("sub", "11111111-1111-1111-1111-000000000001"),
            new Claim("tutorId", "44444444-4444-4444-4444-000000000001"),
            new Claim("perfil", "TUTOR"));

        // Act
        var identidade = Criar(usuario).Identidade;

        // Assert
        Assert.NotNull(identidade);
        Assert.Equal("11111111-1111-1111-1111-000000000001", identidade.UsuarioId);
        Assert.Equal("44444444-4444-4444-4444-000000000001", identidade.TutorId);
        Assert.Equal("TUTOR", identidade.Perfil);
    }

    [Fact]
    public void Identidade_TokenDeAdminSemTutorId_DevolveIdentidadeComTutorIdNulo()
    {
        // Arrange
        var usuario = Autenticado(new Claim("sub", "u1"), new Claim("perfil", "ADMIN"));

        // Act
        var identidade = Criar(usuario).Identidade;

        // Assert
        // O token é válido: o chamador É quem diz ser. O que ele não é, é um tutor — e quem recorta
        // por dono precisa negar, não liberar.
        Assert.NotNull(identidade);
        Assert.Null(identidade.TutorId);
        Assert.Equal("ADMIN", identidade.Perfil);
    }

    [Fact]
    public void Identidade_UsuarioNaoAutenticado_DevolveNulo()
    {
        // Arrange & Act & Assert
        Assert.Null(Criar(usuario: null).Identidade);
    }

    [Fact]
    public void Identidade_AutenticadoSemSub_DevolveNulo()
    {
        // Arrange
        var usuario = Autenticado(new Claim("perfil", "TUTOR"));

        // Act & Assert
        Assert.Null(Criar(usuario).Identidade);
    }

    [Fact]
    public void Identidade_SemHttpContext_DevolveNulo()
    {
        // Arrange & Act & Assert
        // Fora de uma requisição (um BackgroundService, por exemplo) não há quem identificar.
        Assert.Null(Criar(usuario: null, comHttpContext: false).Identidade);
    }
}
```

- [ ] **Step 2: ver falhar** (o `UsuarioAtualHttp` ainda lê `HttpContext.Items`)

```bash
~/.dotnet/dotnet test tests/ClyvoVet.Api.Tests.Unit/ClyvoVet.Api.Tests.Unit.csproj --filter "FullyQualifiedName~UsuarioAtualHttpTests"
```

Esperado: `AccessTokenValido` e `AdminSemTutorId` **falham** (devolvem `null`); os outros três passam.

- [ ] **Step 3: reescrever** `Security/UsuarioAtualHttp.cs`:

```csharp
using ClyvoVet.Application.Security;

namespace ClyvoVet.Api.Security;

/// <summary>
/// <see cref="IUsuarioAtual"/> sobre o <c>HttpContext.User</c>, que o JwtBearer preenche. Só
/// existe identidade para access token válido: o <c>tipo = access</c> e o <c>sub</c> são exigidos
/// na própria autenticação (ver <c>AutenticacaoJwtExtensions</c>), então um refresh token nunca
/// chega aqui como usuário.
/// </summary>
public class UsuarioAtualHttp(IHttpContextAccessor acessor) : IUsuarioAtual
{
    public IdentidadeDoChamador? Identidade
    {
        get
        {
            var usuario = acessor.HttpContext?.User;
            if (usuario?.Identity?.IsAuthenticated != true)
                return null;

            var usuarioId = usuario.FindFirst(ClaimsDoToken.UsuarioId)?.Value;
            if (string.IsNullOrWhiteSpace(usuarioId))
                return null;

            return new IdentidadeDoChamador(
                UsuarioId: usuarioId,
                TutorId: usuario.FindFirst(ClaimsDoToken.TutorId)?.Value,
                Perfil: usuario.FindFirst(ClaimsDoToken.Perfil)?.Value);
        }
    }
}
```

- [ ] **Step 4: ver passar** — mesmo comando do Step 2; esperado **5 verdes**.

- [ ] **Step 5: remover o middleware e o validador**

```bash
git rm src/ClyvoVet.Api/Middleware/IdentidadeMiddleware.cs \
       src/ClyvoVet.Api/Security/ValidadorDeTokenJwt.cs \
       tests/ClyvoVet.Api.Tests.Unit/ValidadorDeTokenJwtTests.cs
```

Em `Program.cs`, **apague**: o comentário + `builder.Services.AddSingleton<ValidadorDeTokenJwt>();`
e o comentário + `app.UseMiddleware<IdentidadeMiddleware>();`. O trecho final do pipeline fica:

```csharp
app.UseHttpsRedirection();

// Antes de UseAuthentication/UseAuthorization: o preflight OPTIONS chega sem credencial nenhuma e
// precisa ser respondido pelo CORS, não recusado pela autorização.
app.UseCors(PoliticaCors);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
```

Mantenha `using ClyvoVet.Api.Middleware;` (o `CorrelationIdMiddleware` mora lá) e `using ClyvoVet.Api.Security;`
(o `UsuarioAtualHttp`).

- [ ] **Step 6: nenhuma referência restante ao código removido** (docs históricos ficam como estão)

```bash
grep -rn "IdentidadeMiddleware\|ValidadorDeTokenJwt\|ObterIdentidade" src tests
```

Esperado: **vazio**. (`CLAUDE.md` e `README.md` são tratados na T7.)

- [ ] **Step 7: suíte completa.** O `EscopoPorTutorEndpointsTests` (que ainda manda `Bearer` de verdade) é a prova de
  que `JwtBearer` + `UsuarioAtualHttp` reproduzem o comportamento antigo.

```bash
dotnet build ClyvoVet-api.slnx
~/.dotnet/dotnet test tests/ClyvoVet.Api.Tests.Unit/ClyvoVet.Api.Tests.Unit.csproj
~/.dotnet/dotnet test tests/ClyvoVet.Api.Tests.Integration/ClyvoVet.Api.Tests.Integration.csproj
```

Esperado: ≈ **286** verdes, **nenhuma** falha em `EscopoPorTutorEndpointsTests`.

- [ ] **Step 8: commitar** (após o "sim")

```bash
git add -A src tests
git commit -m "refactor: IUsuarioAtual lê HttpContext.User e sai o middleware de identidade"
```

---

## T4 — `[Authorize]` nas rotas

**Files:** 6 controllers; `IntegrationTestFixture.cs`; `EscopoPorTutorEndpointsTests.cs`; 4 `*EndpointsTests` (A6);
Create `AutorizacaoEndpointsTests.cs`.

- [ ] **Step 0: responder a D1** com o dono (o app escreve produto?). Sem resposta, **não** siga.
  Se **sim, com perfil TUTOR**: no Step 3 pule o `perl` do `Equipe` do `Produto` e ajuste o teste `Produto_Escrita…`
  (passa a esperar 2xx/4xx de negócio com `TUTOR`); registre na ADR-003 (T7).

- [ ] **Step 1: testes primeiro** — `tests/ClyvoVet.Api.Tests.Integration/AutorizacaoEndpointsTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using ClyvoVet.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ClyvoVet.Api.Tests.Integration;

/// <summary>
/// A autorização nas rotas REAIS. O cliente é "cru" (<c>Server.CreateClient()</c>, sem os headers
/// padrão da fixture) e cada teste manda só o que quer provar — a <c>X-Api-Key</c> certa, quando o
/// 401 esperado tem de vir do JWT, e não do filtro da chave.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class AutorizacaoEndpointsTests
{
    private readonly IntegrationTestFixture _fixture;

    public AutorizacaoEndpointsTests(IntegrationTestFixture fixture) => _fixture = fixture;

    [Theory]
    [InlineData("/api/v1/lembretes")]
    [InlineData("/api/v1/eventos-pet")]
    [InlineData("/api/v1/sugestoes-produto")]
    [InlineData("/api/v1/produtos")]
    [InlineData("/api/v1/saude-preditiva/qualquer-id")]
    [InlineData("/api/v1/widget-saude-preditiva/qualquer-id")]
    public async Task RotaProtegida_SemToken_RetornaUnauthorized(string rota)
    {
        // Arrange
        var cliente = _fixture.Server.CreateClient();
        cliente.DefaultRequestHeaders.Add("X-Api-Key", "SUA_API_KEY");

        // Act
        var resposta = await cliente.GetAsync(rota);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Theory]
    [InlineData("POST", "/api/v1/produtos")]
    [InlineData("PUT", "/api/v1/produtos/qualquer-id")]
    [InlineData("DELETE", "/api/v1/produtos/qualquer-id")]
    public async Task Produto_EscritaComPerfilTutor_RetornaForbidden(string metodo, string rota)
    {
        // Arrange
        var cliente = _fixture.CreateClientComBearer("TUTOR");
        cliente.DefaultRequestHeaders.Add("X-Api-Key", "SUA_API_KEY");
        var requisicao = new HttpRequestMessage(new HttpMethod(metodo), rota) { Content = JsonContent.Create(new { }) };

        // Act
        var resposta = await cliente.SendAsync(requisicao);

        // Assert
        // A autorização roda antes do model binding: 403 mesmo com corpo vazio.
        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task Produto_LeituraComPerfilTutor_RetornaOk()
    {
        // Arrange
        var cliente = _fixture.CreateClientComBearer("TUTOR");
        cliente.DefaultRequestHeaders.Add("X-Api-Key", "SUA_API_KEY");

        // Act
        var resposta = await cliente.GetAsync("/api/v1/produtos");

        // Assert
        // A vitrine do tutor é leitura: só escrever exige a equipe.
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    [InlineData("/metrics")]
    [InlineData("/swagger/v1/swagger.json")]
    [InlineData("/swagger/index.html")]
    public async Task RotasDeInfraestrutura_SemToken_ContinuamRespondendo(string rota)
    {
        // Arrange
        var cliente = _fixture.Server.CreateClient();

        // Act
        var resposta = await cliente.GetAsync(rota);

        // Assert
        // Sem FallbackPolicy: um health check quebrado tira a aplicação de rotação no Render.
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    public async Task Health_SemToken_NaoExigeAutenticacao()
    {
        // Arrange
        var cliente = _fixture.Server.CreateClient();

        // Act
        var resposta = await cliente.GetAsync("/health");

        // Assert
        // Em teste o check do Telegram chama a Bot API com token falso e o /health completo vira 503
        // (A8). O que importa aqui é que a rota é anônima.
        Assert.NotEqual(HttpStatusCode.Unauthorized, resposta.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, resposta.StatusCode);
    }
}

/// <summary>Alavanca desligada, escopo ligado: as duas travas independentes e como elas se compõem.</summary>
public class AlavancaDesligadaFixture : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configuracao) =>
            configuracao.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = TokensDeTeste.Segredo,
                ["Auth:ExigirToken"] = "false",
                ["Api:EscopoPorTutor"] = "true",
            }));

        builder.ConfigureServices(services =>
        {
            var descritor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descritor is not null) services.Remove(descritor);
            services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase($"AlavancaDb-{Guid.NewGuid()}"));
        });
    }

    protected override void ConfigureClient(HttpClient client)
    {
        base.ConfigureClient(client);
        client.DefaultRequestHeaders.Add("X-Api-Key", "SUA_API_KEY");
    }
}

public class AlavancaDesligadaTests : IClassFixture<AlavancaDesligadaFixture>
{
    private readonly AlavancaDesligadaFixture _fixture;

    public AlavancaDesligadaTests(AlavancaDesligadaFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task ExigirTokenDesligado_RotaSemEscopo_ResponderOkSemToken()
    {
        var resposta = await _fixture.CreateClient().GetAsync("/api/v1/produtos");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    public async Task ExigirTokenDesligado_ComEscopoLigadoESemToken_RetornaForbidden()
    {
        // A alavanca abre a AUTENTICAÇÃO, não o escopo: sem tutor no token, quem recorta por dono
        // continua negando (o 403 que o EscopoPorTutorEndpointsTests provava antes da F2, A7).
        var resposta = await _fixture.CreateClient().GetAsync("/api/v1/lembretes");

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }
}
```

- [ ] **Step 2: ver falhar** — o método `CreateClientComBearer` ainda não existe. Acrescente-o à `IntegrationTestFixture`
  **agora** (o código está no Step 4) e rode:

```bash
~/.dotnet/dotnet test tests/ClyvoVet.Api.Tests.Integration/ClyvoVet.Api.Tests.Integration.csproj --filter "FullyQualifiedName~AutorizacaoEndpointsTests|FullyQualifiedName~AlavancaDesligadaTests"
```

Esperado: **9 falhas** — as 6 rotas sem token (respondem 200/404, não 401) e as 3 escritas de produto
(não dão 403). O resto passa: são testes-guarda de infraestrutura e da alavanca.

- [ ] **Step 3: anotar os controllers.** Comandos testados numa cópia; o `Telegram` **não** entra.

```bash
for f in Lembrete EventoPet SugestaoProduto SaudePreditiva WidgetSaudePreditiva Produto; do
  perl -0pi -e 's/(\[TypeFilter\(typeof\(ApiKeyFilterAttribute\), Arguments = new object\[\] \{ "Api:ApiKey" \}\)\]\n)(public class)/[Authorize(Policy = PoliticasDeAcesso.Autenticado)]\n$1$2/; s/^using Microsoft\.AspNetCore\.Mvc;/using ClyvoVet.Api.Security;\nusing Microsoft.AspNetCore.Authorization;\nusing Microsoft.AspNetCore.Mvc;/m' \
    src/ClyvoVet.Api/Controllers/${f}Controller.cs
done

# Produto: escrita só para a equipe (pule este comando se a D1 mandar deixar tudo em Autenticado)
perl -0pi -e 's/^(    )(\[Http(?:Post|Put|Delete)[^\]]*\]\n)/$1\[Authorize(Policy = PoliticasDeAcesso.Equipe)]\n$1$2/mg' \
  src/ClyvoVet.Api/Controllers/ProdutoController.cs

grep -c "Authorize(Policy" src/ClyvoVet.Api/Controllers/*Controller.cs
```

Esperado: `Produto` = **4**, os outros cinco = **1**, `Telegram` = **0**. O `[Authorize]` de classe fica
**acima** do `[TypeFilter]` da `X-Api-Key` (a leitura no arquivo: `[Produces]`, `[Authorize]`, `[TypeFilter]`).
Autorização e filtros de ação são camadas diferentes; a ordem dos atributos é só estética.

- [ ] **Step 4: os testes existentes passam a precisar de Bearer** — em `IntegrationTestFixture.cs`:

```csharp
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // O JwtBearer valida com este segredo; sem ele nenhum token dos testes confere.
        builder.ConfigureAppConfiguration((_, configuracao) =>
            configuracao.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = TokensDeTeste.Segredo,
            }));

        // ... (o ConfigureServices existente segue igual)
    }

    // Os controllers principais exigem X-Api-Key E Bearer — injeta os dois aqui pra não editar cada
    // teste. ADMIN: sem tutorId, sem restrição de perfil, e o escopo por tutor fica desligado.
    protected override void ConfigureClient(HttpClient client)
    {
        base.ConfigureClient(client);
        client.DefaultRequestHeaders.Add("X-Api-Key", "SUA_API_KEY");
        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + TokensDeTeste.Access(tutorId: null, perfil: "ADMIN"));
    }

    /// <summary>
    /// Cliente "cru" (sem a <c>X-Api-Key</c>) que traz só o Bearer — para provar que o 401 de uma
    /// chave errada vem do filtro da chave, e não do JWT, que roda antes dele.
    /// </summary>
    public HttpClient CreateClientComBearer(string perfil = "ADMIN")
    {
        var cliente = Server.CreateClient();
        cliente.DefaultRequestHeaders.Add(
            "Authorization",
            "Bearer " + TokensDeTeste.Access(tutorId: perfil == "TUTOR" ? TutorId : null, perfil: perfil));
        return cliente;
    }
```

Acrescente `using Microsoft.Extensions.Configuration;` no topo.

- [ ] **Step 5: A6 — os oito testes que passariam pelo motivo errado.**

```bash
grep -n "Server.CreateClient" tests/ClyvoVet.Api.Tests.Integration/*EndpointsTests.cs
```

Nas linhas de `clientSemApiKey` e `clientComApiKeyErrada` de `Produto`, `EventoPet`, `SugestaoProduto` e
`Lembrete`, troque `_fixture.Server.CreateClient()` por `_fixture.CreateClientComBearer()`. Confira que o
`Assert` continua `Unauthorized` — agora só o filtro da `X-Api-Key` pode produzi-lo.
(**Não** mexa nos testes do `Telegram`: ele não tem `[Authorize]`.)

- [ ] **Step 6: A7 — o escopo.** Em `EscopoPorTutorEndpointsTests.cs`:
  1. `EscopoLigadoFixture`: apague a constante `Segredo` e o método estático `Token`; a configuração usa
     `TokensDeTeste.Segredo`; `ClienteDe` usa `TokensDeTeste.Access(tutorId, perfil)`. Remova os `using` que sobrarem.
  2. Renomeie e mude a expectativa dos dois testes que passavam por não ter Bearer:
     `GetAll_ComEscopoLigadoESemToken_RetornaForbidden` → `GetAll_ComEscopoLigadoESemToken_RetornaUnauthorized`
     e `GetAllSugestoes_SemToken_RetornaForbidden` → `GetAllSugestoes_SemToken_RetornaUnauthorized`, ambos com
     `HttpStatusCode.Unauthorized` e este comentário no `Assert`:
     `// Sem Bearer o 401 do JWT chega antes do escopo (ADR-003). O 403 sem token continua provado, com a alavanca desligada, em AlavancaDesligadaTests.`
  3. `GetAll_ComTokenDeAdminSemTutorId_RetornaForbidden` **não muda**: há token, e é o escopo que nega.

- [ ] **Step 7: suíte completa**

```bash
dotnet build ClyvoVet-api.slnx
~/.dotnet/dotnet test tests/ClyvoVet.Api.Tests.Unit/ClyvoVet.Api.Tests.Unit.csproj
~/.dotnet/dotnet test tests/ClyvoVet.Api.Tests.Integration/ClyvoVet.Api.Tests.Integration.csproj
```

Esperado: ≈ **304** verdes. Se um teste de negócio antigo falhar com 401, o `Authorization` da fixture não
chegou a ele — confira o `ConfigureClient` e se o teste usa `_fixture.CreateClient()` (com headers) e não `Server.CreateClient()`.

- [ ] **Step 8: commitar** (após o "sim")

```bash
git add -A src tests
git commit -m "feat: protege as rotas com [Authorize] (Autenticado e Equipe)"
```

---

## T5 — Swagger com o esquema `Bearer`

**Files:** Create `Swagger/BearerSecurityDocumentFilter.cs`; Modify `Extensions/DocumentacaoApiExtensions.cs`, `SwaggerEndpointsTests.cs`.

- [ ] **Step 1: testes primeiro** — acrescentar a `SwaggerEndpointsTests` (usa o `_client` da fixture; o Swagger é anônimo):

```csharp
    [Fact]
    public async Task GetSwaggerJson_Documento_DeclaraOEsquemaBearer()
    {
        // Arrange
        var resposta = await _client.GetAsync("/swagger/v1/swagger.json");
        var documento = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());

        // Act
        var bearer = documento.RootElement
            .GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");

        // Assert
        Assert.Equal("http", bearer.GetProperty("type").GetString());
        Assert.Equal("bearer", bearer.GetProperty("scheme").GetString());
        Assert.Equal("JWT", bearer.GetProperty("bearerFormat").GetString());
    }

    [Fact]
    public async Task GetSwaggerJson_RotaComAuthorize_ExigeApiKeyEBearerJuntos()
    {
        // Arrange
        var resposta = await _client.GetAsync("/swagger/v1/swagger.json");
        var documento = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        var paths = documento.RootElement.GetProperty("paths");

        // Act
        var requisito = paths.GetProperty("/api/v1/lembretes").GetProperty("get")
            .GetProperty("security")[0];
        var telegram = paths.GetProperty("/api/v1/telegram/enviar").GetProperty("post");

        // Assert
        // Um ÚNICO requisito com os dois esquemas é E (as duas credenciais); dois requisitos seriam OU.
        Assert.True(requisito.TryGetProperty("ApiKey", out _));
        Assert.True(requisito.TryGetProperty("Bearer", out _));
        // O Telegram não tem [Authorize]: não ganha o cadeado do Bearer.
        Assert.False(telegram.TryGetProperty("security", out var seguranca) && seguranca.ToString().Contains("Bearer"));
    }
```

- [ ] **Step 2: ver falhar** — `--filter "FullyQualifiedName~SwaggerEndpointsTests"`; esperado: **2 falhas** (não há esquema `Bearer`).

- [ ] **Step 3: implementar** — `Swagger/BearerSecurityDocumentFilter.cs` (mesmo motivo do `ApiKeySecurityDocumentFilter`:
  a referência ao esquema só serializa direito com acesso ao `OpenApiDocument`, que só o `DocumentFilter` recebe):

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ClyvoVet.Api.Swagger;

/// <summary>
/// Põe o cadeado do <c>Bearer</c> nas ações com <c>[Authorize]</c> (na ação ou no controller).
/// Precisa ser um DocumentFilter pelo mesmo motivo do <see cref="ApiKeySecurityDocumentFilter"/>:
/// só ele recebe o documento já montado, e sem isso o Swagger UI não resolve a referência.
///
/// <para>
/// O esquema entra no <b>mesmo</b> requisito que a <c>X-Api-Key</c> (registre este filtro
/// <b>depois</b> dele): num requisito só os dois esquemas valem juntos (E), que é o que a API
/// exige. Dois requisitos separados significariam OU, e o botão Authorize mandaria só um dos dois.
/// </para>
/// </summary>
public sealed class BearerSecurityDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument document, DocumentFilterContext context)
    {
        foreach (var apiDescription in context.ApiDescriptions)
        {
            var exigeToken = (apiDescription.ActionDescriptor.EndpointMetadata ?? [])
                .OfType<IAuthorizeData>().Any();
            if (!exigeToken)
                continue;

            var path = "/" + apiDescription.RelativePath?.TrimStart('/');
            if (!document.Paths.TryGetValue(path, out var pathItem))
                continue;

            var method = new HttpMethod(apiDescription.HttpMethod!);
            if (!pathItem.Operations.TryGetValue(method, out var operation))
                continue;

            var requisito = operation.Security?.FirstOrDefault() ?? new OpenApiSecurityRequirement();
            requisito[new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>();
            operation.Security = [requisito];
        }
    }
}
```

Em `DocumentacaoApiExtensions.AddDocumentacaoApi`, logo **depois** de `options.DocumentFilter<ApiKeySecurityDocumentFilter>();`:

```csharp
            // O access token vem do login da API Java; o Swagger só o repassa no header.
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type         = SecuritySchemeType.Http,
                Scheme       = "bearer",
                BearerFormat = "JWT",
                Description  = "Access token emitido pela API Java no login. Cole só o token: o Swagger acrescenta o prefixo Bearer."
            });
            options.DocumentFilter<BearerSecurityDocumentFilter>();
```

- [ ] **Step 4: ver passar** + suíte completa; esperado ≈ **306** verdes. **Confira no navegador** (`dotnet run`, `/swagger`):
  o botão **Authorize** lista `ApiKey` e `Bearer`, e o cadeado aparece nos endpoints de negócio (não no Telegram `enviar`).

- [ ] **Step 5: commitar** (após o "sim")

```bash
git add -A src tests
git commit -m "feat: esquema Bearer no Swagger, junto da X-Api-Key"
```

---

## T6 — `TratadorGlobalDeExcecoes` + `ProblemDetails`

**Files:** Create `Errors/TratadorGlobalDeExcecoes.cs`, `Extensions/TratamentoDeExcecoesExtensions.cs`,
`tests/…Unit/TratadorGlobalDeExcecoesTests.cs`, `tests/…Integration/TratamentoDeErrosEndpointsTests.cs`; Modify `Program.cs`.

**Consumes:** `MapaDeErro.Status/Mensagem/Referencia` (já testados; **não mudam**).

- [ ] **Step 1: testes primeiro (unidade)** — `tests/ClyvoVet.Api.Tests.Unit/TratadorGlobalDeExcecoesTests.cs`:

```csharp
using System.Text.Json;
using ClyvoVet.Api.Errors;
using ClyvoVet.Api.Extensions;
using ClyvoVet.Application.Security;
using ClyvoVet.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace ClyvoVet.Api.Tests.Unit;

/// <summary>
/// O tratador só ESCREVE a resposta; quem decide status e mensagem é o <see cref="MapaDeErro"/>. O
/// que se prova aqui é o contrato com o app: <c>error</c> sempre, <c>referencia</c> só em falha de
/// servidor, e o log no mesmo nível de antes.
/// </summary>
public class TratadorGlobalDeExcecoesTests
{
    private static (TratadorGlobalDeExcecoes Tratador, Mock<ILogger<TratadorGlobalDeExcecoes>> Log, ServiceProvider Servicos) Criar()
    {
        var servicos = new ServiceCollection().AddLogging().AddTratamentoDeExcecoes().BuildServiceProvider();
        var log = new Mock<ILogger<TratadorGlobalDeExcecoes>>();
        var tratador = new TratadorGlobalDeExcecoes(servicos.GetRequiredService<IProblemDetailsService>(), log.Object);
        return (tratador, log, servicos);
    }

    private static DefaultHttpContext Contexto(IServiceProvider servicos, string? accept = null)
    {
        var contexto = new DefaultHttpContext { RequestServices = servicos, TraceIdentifier = "trace-de-teste" };
        contexto.Response.Body = new MemoryStream();
        contexto.Response.Headers["X-Correlation-Id"] = "corr-123";
        if (accept is not null) contexto.Request.Headers.Accept = accept;
        return contexto;
    }

    private static async Task<JsonElement> CorpoAsync(DefaultHttpContext contexto)
    {
        contexto.Response.Body.Position = 0;
        return (await JsonDocument.ParseAsync(contexto.Response.Body)).RootElement;
    }

    public static IEnumerable<object[]> Casos() =>
    [
        [new NotFoundException("Produto com id abc não encontrado."), 404, "Produto com id abc não encontrado."],
        [new BadRequestException("data no passado"), 400, "data no passado"],
        [new SemTutorNoTokenException(), 403, new SemTutorNoTokenException().Message],
        [new RegistroEmUsoException(), 409, "Registro em uso por outro cadastro."],
        [new InvalidOperationException("detalhe interno que nao deve vazar"), 500, "Erro interno no servidor."],
    ];

    [Theory]
    [MemberData(nameof(Casos))]
    public async Task TryHandleAsync_Excecao_EscreveStatusEErrorDoMapa(Exception excecao, int status, string mensagem)
    {
        // Arrange
        var (tratador, _, servicos) = Criar();
        var contexto = Contexto(servicos);

        // Act
        var tratada = await tratador.TryHandleAsync(contexto, excecao, CancellationToken.None);

        // Assert
        Assert.True(tratada);
        Assert.Equal(status, contexto.Response.StatusCode);
        var corpo = await CorpoAsync(contexto);
        Assert.Equal(mensagem, corpo.GetProperty("error").GetString());
        Assert.Equal(status, corpo.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task TryHandleAsync_FalhaDeServidor_TrazReferenciaETraceId()
    {
        // Arrange
        var (tratador, _, servicos) = Criar();
        var contexto = Contexto(servicos);

        // Act
        await tratador.TryHandleAsync(contexto, new InvalidOperationException("banco fora do ar"), CancellationToken.None);

        // Assert
        var corpo = await CorpoAsync(contexto);
        // O mesmo id que já viaja no header e que o Serilog imprime em toda linha da requisição.
        Assert.Equal("corr-123", corpo.GetProperty("referencia").GetString());
        Assert.Equal("trace-de-teste", corpo.GetProperty("traceId").GetString());
    }

    [Fact]
    public async Task TryHandleAsync_ErroDoUsuario_NaoTrazReferencia()
    {
        // Arrange
        var (tratador, _, servicos) = Criar();
        var contexto = Contexto(servicos);

        // Act
        await tratador.TryHandleAsync(contexto, new NotFoundException("x"), CancellationToken.None);

        // Assert
        // O usuário tem o que corrigir: um código ao lado da frase só acrescentaria ruído.
        Assert.False((await CorpoAsync(contexto)).TryGetProperty("referencia", out _));
    }

    [Fact]
    public async Task TryHandleAsync_ClienteQueNaoAceitaJson_AindaRecebeOCorpoComError()
    {
        // Arrange
        var (tratador, _, servicos) = Criar();
        var contexto = Contexto(servicos, accept: "text/html");

        // Act
        var tratada = await tratador.TryHandleAsync(contexto, new NotFoundException("nao achei"), CancellationToken.None);

        // Assert
        // O IProblemDetailsService recusa escrever para quem não aceita JSON; sem o fallback a
        // resposta sairia SEM corpo, e o app perderia o `error` (A9).
        Assert.True(tratada);
        Assert.Equal("nao achei", (await CorpoAsync(contexto)).GetProperty("error").GetString());
    }

    [Theory]
    [InlineData(typeof(NotFoundException), LogLevel.Warning)]
    [InlineData(typeof(BadRequestException), LogLevel.Warning)]
    [InlineData(typeof(SemTutorNoTokenException), LogLevel.Warning)]
    [InlineData(typeof(InvalidOperationException), LogLevel.Error)]
    public async Task TryHandleAsync_Excecao_RegistraNoMesmoNivelDeAntes(Type tipo, LogLevel esperado)
    {
        // Arrange
        var (tratador, log, servicos) = Criar();
        var excecao = tipo == typeof(SemTutorNoTokenException)
            ? new SemTutorNoTokenException()
            : (Exception)Activator.CreateInstance(tipo, "mensagem")!;

        // Act
        await tratador.TryHandleAsync(Contexto(servicos), excecao, CancellationToken.None);

        // Assert
        log.Verify(l => l.Log(
            esperado, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }
}
```

Integração — `tests/…Integration/TratamentoDeErrosEndpointsTests.cs` (o pipeline inteiro, não só o tratador):

```csharp
using System.Net;
using System.Text.Json;

namespace ClyvoVet.Api.Tests.Integration;

[Collection(IntegrationTestCollection.Name)]
public class TratamentoDeErrosEndpointsTests
{
    private readonly HttpClient _client;

    public TratamentoDeErrosEndpointsTests(IntegrationTestFixture fixture) => _client = fixture.CreateClient();

    [Fact]
    public async Task GetById_ProdutoInexistente_RetornaNotFoundComErrorEProblemDetails()
    {
        // Arrange & Act
        var resposta = await _client.GetAsync($"/api/v1/produtos/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
        Assert.Contains("json", resposta.Content.Headers.ContentType!.MediaType);
        var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync()).RootElement;
        Assert.False(string.IsNullOrWhiteSpace(corpo.GetProperty("error").GetString()));
        Assert.Equal(404, corpo.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(corpo.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task GetAll_PaginaInvalida_RetornaBadRequestComError()
    {
        // Arrange & Act
        var resposta = await _client.GetAsync("/api/v1/produtos?page=0");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync()).RootElement;
        Assert.False(string.IsNullOrWhiteSpace(corpo.GetProperty("error").GetString()));
    }
}
```

- [ ] **Step 2: ver falhar** — erro de compilação (`TratadorGlobalDeExcecoes`, `AddTratamentoDeExcecoes` não existem).

- [ ] **Step 3: implementar** — `Errors/TratadorGlobalDeExcecoes.cs` (o log e a decisão vêm do lambda antigo do `Program.cs`;
  **os comentários de porquê vão junto**):

```csharp
using ClyvoVet.Application.Security;
using ClyvoVet.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace ClyvoVet.Api.Errors;

/// <summary>
/// Transforma a exceção não tratada em resposta. A DECISÃO (status, mensagem, referência) vive no
/// <see cref="MapaDeErro"/>, fora daqui, para poder ser testada: o caso do 409 depende de chave
/// estrangeira real, e os testes de integração rodam em InMemory, que não aplica FK.
/// </summary>
public class TratadorGlobalDeExcecoes(
    IProblemDetailsService problemas,
    ILogger<TratadorGlobalDeExcecoes> log) : IExceptionHandler
{
    private const string HeaderCorrelationId = "X-Correlation-Id";

    public async ValueTask<bool> TryHandleAsync(HttpContext contexto, Exception excecao, CancellationToken ct)
    {
        var status = MapaDeErro.Status(excecao);
        var mensagem = MapaDeErro.Mensagem(excecao);

        // O mesmo id que o CorrelationIdMiddleware já colocou no header e que o Serilog imprime em
        // cada linha desta requisição.
        var correlationId = contexto.Response.Headers[HeaderCorrelationId].ToString();
        var referencia = MapaDeErro.Referencia(excecao, correlationId);

        Registrar(contexto, excecao, mensagem);

        contexto.Response.StatusCode = status;
        var problema = new ProblemDetails { Status = status, Title = ReasonPhrases.GetReasonPhrase(status) };

        // `error` continua onde estava: é o que o aplicativo lê hoje, e mexer nele quebraria a tela
        // sem ganho nenhum. `referencia` entra ao lado, e só existe quando é falha de servidor — é o
        // campo que permite ao usuário dizer QUAL erro aconteceu, com o mesmo nome que a API Java usa.
        problema.Extensions["error"] = mensagem;
        if (referencia is not null)
            problema.Extensions["referencia"] = referencia;

        var escrito = await problemas.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = contexto,
            ProblemDetails = problema,
            Exception = excecao,
        });

        // O serviço recusa escrever para quem não aceita JSON (Accept: text/html). Sem este fallback
        // a resposta sairia sem corpo, e o app perderia o `error`.
        if (!escrito)
            await contexto.Response.WriteAsJsonAsync(problema, ct);

        return true;
    }

    private void Registrar(HttpContext contexto, Exception excecao, string mensagem)
    {
        if (excecao is NotFoundException or BadRequestException or SemTutorNoTokenException)
            log.LogWarning("Requisição inválida em {Path}: {Message}", contexto.Request.Path, mensagem);
        else
            log.LogError(excecao, "Erro não tratado em {Path}", contexto.Request.Path);
    }
}
```

`Extensions/TratamentoDeExcecoesExtensions.cs`:

```csharp
using System.Diagnostics;
using ClyvoVet.Api.Errors;

namespace ClyvoVet.Api.Extensions;

public static class TratamentoDeExcecoesExtensions
{
    public static IServiceCollection AddTratamentoDeExcecoes(this IServiceCollection services)
    {
        // traceId: o mesmo que o OpenTelemetry usa para a requisição (design §6.1).
        services.AddProblemDetails(opcoes => opcoes.CustomizeProblemDetails = contexto =>
            contexto.ProblemDetails.Extensions.TryAdd(
                "traceId", Activity.Current?.Id ?? contexto.HttpContext.TraceIdentifier));

        services.AddExceptionHandler<TratadorGlobalDeExcecoes>();
        return services;
    }
}
```

`Program.cs`: registre `builder.Services.AddTratamentoDeExcecoes();` (junto de `AddControllers`) e **troque o
bloco inteiro** `app.UseExceptionHandler(errorApp => { ... });` (≈ 45 linhas) por:

```csharp
// A decisão de status e mensagem vive no MapaDeErro; o tratador só escreve a resposta.
app.UseExceptionHandler();
```

Remova os `using` que ficaram sem uso (`Microsoft.AspNetCore.Diagnostics`, `ClyvoVet.Domain.Exceptions`).
Mantenha `using ClyvoVet.Api.Errors;` só se o compilador reclamar de nome; senão remova também.

- [ ] **Step 4: ver passar** — os dois arquivos novos; esperado **14 verdes** (12 + 2).

- [ ] **Step 5: suíte completa** — `MapaDeErroTests` **sem nenhuma alteração** e verdes. Esperado ≈ **320**.

- [ ] **Step 6: commitar** (após o "sim")

```bash
git add -A src tests
git commit -m "refactor: tratador global de exceções com ProblemDetails, mantendo error e referencia"
```

---

## T7 — Documentação, ADR e checklist de deploy

**Files:** `docs/sprint4/decisoes/ADR-003-…md`, `README.md`, `CLAUDE.md`, `docs/sprint4/README.md`,
`docs/sprint4/03-plano.md`, `docs/sprint4/01-gap-analysis.md`, `src/ClyvoVet.Api/ClyvoVet.Api.csproj` (comentário).

- [ ] **Step 1: comentário do `csproj`** — o bloco que justifica **não** usar o `JwtBearer` ficou falso. Troque o
  comentário sobre `System.IdentityModel.Tokens.Jwt` por (a referência **fica**):

```xml
    <!-- Fixa a cadeia do Microsoft.IdentityModel em 8.3.1. O JwtBearer 8.0.11 (usado em
         AutenticacaoJwtExtensions) traz Protocols.OpenIdConnect 7.1.2; com esta referência direta o
         restante do grafo (Tokens, JsonWebTokens, Jwt) resolve em 8.3.1, e a convivência foi provada
         em execução (ADR-003, F2). Os testes também montam tokens com o JwtSecurityTokenHandler daqui. -->
    <PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="8.3.1" />
```

- [ ] **Step 2: ADR-003** — `Status: aceita` → `implementada · 20/09/2026`; acrescente ao fim a seção
  **"Resultado (F2)"** com: o grafo do IdentityModel da T1; que a prova de execução foi o `AutenticacaoJwtTests`; a
  **decisão D1** (o que o dono respondeu sobre `Produto`); que `tipo=access` mora na autenticação e não na política (A4);
  que 401/403 saem sem corpo (A10); e a mudança do `Content-Type` do erro para `application/problem+json` (A9).
  Troque o item ❓ das consequências pela resposta.

- [ ] **Step 3: `README.md`** — na seção **"🔐 Autenticação"** (hoje só fala de `X-Api-Key`), reescreva para o que vale agora e
  acrescente o checklist. O texto (ajuste só o que o restante do README já usar):

````markdown
### 🔐 Autenticação

Os endpoints de negócio exigem **duas credenciais**: `Authorization: Bearer <access token>` (emitido pela
API Java no login) **e** o header `X-Api-Key`. Sem o Bearer a resposta é `401`; com um perfil sem permissão, `403`.

| Endpoints | Quem acessa |
|---|---|
| Lembretes, Eventos Pet, Sugestões de Produto, Saúde Preditiva, Widget, `GET` de Produtos | qualquer usuário autenticado |
| `POST`/`PUT`/`DELETE` de Produtos | `ADMIN` ou `VETERINARIO` |
| Telegram | chaves próprias (`Api:ApiKey` / `Telegram:ApiKey`), como antes |
| `/health*`, `/metrics`, `/swagger` | anônimos |

Só o **access token** vale: o *refresh token* (7 dias) é recusado.

| Variável de ambiente | Para quê |
|---|---|
| `Jwt__Secret` | o **mesmo** valor de `JWT_SECRET` da API Java (base64; a chave é o valor *decodificado*) |
| `Jwt__Emissor`, `Jwt__Publico` | opcionais; padrões `clyvovet-api-java` e `clyvovet` |
| `Auth__ExigirToken` | `false` desliga a exigência do Bearer (alavanca de emergência); padrão `true` |

```bash
curl http://localhost:5191/api/v1/lembretes \
  -H "Authorization: Bearer SEU_ACCESS_TOKEN" -H "X-Api-Key: SUA_CHAVE_AQUI"
```

#### Checklist de deploy (Render)

1. Defina `Jwt__Secret` no serviço **antes** do deploy, com o mesmo valor do `JWT_SECRET` da API Java.
   Sem ele a aplicação sobe, registra um `Warning` e **todas as rotas protegidas respondem 401**.
2. Confirme nos logs da subida que **não** há a mensagem "Jwt:Secret não configurado".
3. Teste `GET /health/live` (200) e uma rota protegida sem token (401) e com token (200).
4. Rollback rápido, sem redeploy: `Auth__ExigirToken=false`.
````

- [ ] **Step 4: instruções do projeto (`CLAUDE.md`)** — (a) a linha que cita `IdentidadeMiddleware.cs` como exemplo de bom
  comentário passa a citar `ChaveDoJwt.cs`; (b) a linha de base passa a dizer "F2 fechada, 20/09/2026: **N unidade + M integração = T**"
  com os números reais impressos pelo `dotnet test`.

- [ ] **Step 5: estado** — `docs/sprint4/README.md`: F2 ✅. `docs/sprint4/03-plano.md`: marque as tarefas da F2.
  `docs/sprint4/01-gap-analysis.md`: as linhas de **"Exceções globais"** e **"JWT / Identity"** passam a ✅, com a evidência
  (`TratadorGlobalDeExcecoes.cs`, `AutenticacaoJwtExtensions.cs`, `AutenticacaoJwtTests`).

- [ ] **Step 6: verificação final da fase**

```bash
dotnet build ClyvoVet-api.slnx
~/.dotnet/dotnet test tests/ClyvoVet.Api.Tests.Unit/ClyvoVet.Api.Tests.Unit.csproj
~/.dotnet/dotnet test tests/ClyvoVet.Api.Tests.Integration/ClyvoVet.Api.Tests.Integration.csproj
grep -rn "IdentidadeMiddleware\|ValidadorDeTokenJwt" src tests CLAUDE.md README.md   # vazio
ASPNETCORE_ENVIRONMENT=Testing dotnet run --no-launch-profile --project src/ClyvoVet.Api/ClyvoVet.Api.csproj
# noutro terminal:
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5191/health/live                 # 200
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5191/api/v1/lembretes -H "X-Api-Key: SUA_API_KEY"   # 401
```

Confira também que o **app** (ou o `test_api.sh` adaptado, F6) lê o corpo de um erro com o `Content-Type`
`application/problem+json` (A9). O `docker build -t clyvovet-api .` continua verde (nada mudou no `Dockerfile`).

- [ ] **Step 7: commitar** (após o "sim")

```bash
git add -A docs README.md CLAUDE.md src/ClyvoVet.Api/ClyvoVet.Api.csproj
git commit -m "docs: fecha a F2 (ADR-003, autenticação no README e checklist de deploy)"
```

---

## Pronto quando

- [ ] DoD do plano-mestre: build sem erros; testes no runtime 8 verdes e **≥ 272**; `README` da Sprint 4 e `CLAUDE.md` atualizados no commit de fechamento.
- [ ] `AutenticacaoJwtTests` cobre: token válido 200; sem token 401; `tipo=refresh` 401; expirado 401; chave errada 401; `TUTOR` em rota `Equipe` 403.
- [ ] `/health/live`, `/health/ready`, `/metrics`, `/swagger` respondem **sem token**; rotas de negócio dão 401 sem token; `Auth:ExigirToken=false` → 200 sem token.
- [ ] `MapaDeErroTests` verdes **sem alteração**; erro mantém `error` (e `referencia` só no 500).
- [ ] Nenhuma referência a `IdentidadeMiddleware`/`ValidadorDeTokenJwt` em `src`, `tests`, `CLAUDE.md`, `README.md`.
- [ ] ADR-003 *implementada*, com o resultado da prova de compatibilidade e a resposta da D1.
- [ ] Checklist de deploy (`Jwt__Secret` no Render **antes** do deploy) no README.

## Fora de escopo (não adiantar)

- Paginação, ordenação e HATEOAS (F3); MongoDB (F4); cobertura e Serilog JSON (F5).
- Corpo `error` em 401/403 do JWT (A10) — o 401 da `X-Api-Key` já é sem corpo.
- Atualizar `test_api.sh` e os exemplos `curl` restantes do README para mandar o Bearer — é F6 T5.
- Deploy e `push`: só sob pedido.
