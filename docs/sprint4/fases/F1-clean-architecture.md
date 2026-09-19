# F1 — Clean Architecture em 4 projetos: plano de implementação

> **Para quem for executar:** use `superpowers:executing-plans` (recomendado: cada commit
> precisa do "sim" do dono do repositório, e um subagente não consegue pedi-lo) ou
> `superpowers:subagent-driven-development`. Os passos usam caixas `- [ ]` para acompanhar.
> Leia antes [`../02-design.md`](../02-design.md) e [`../decisoes/ADR-001-clean-architecture-4-projetos.md`](../decisoes/ADR-001-clean-architecture-4-projetos.md).
> **Onde este plano e o design divergirem, vale este plano** — ver "Achados" abaixo, que
> saíram de ler o código e de protótipos, e que o design não previa.

**Meta:** separar a solução em Domain, Application, Infrastructure e Api, com as regras de
dependência **verificadas por teste**, **sem mudar nenhum comportamento observável**.

**Arquitetura:** `Api → Application → Domain` e `Api → Infrastructure → Application`.
Tudo é *movimento* (`git mv`) + troca de `namespace`/`using`; a lógica só muda em quatro
pontos pequenos e declarados: `IUsuarioAtual` (T4), tradução de `DbUpdateException` (T5),
inclusão do XML da Application no Swagger (T4) e composição em `Program.cs` (T5/T6).

**Stack:** .NET 8, EF Core 8.0.11, Pomelo 8.0.2, xUnit + Moq, Swashbuckle 10.1.7.

**Spec:** [`../02-design.md`](../02-design.md) §3, §4, §5, §7.1 · **Plano-mestre:** [`../03-plano.md`](../03-plano.md) F1.

## Restrições globais

Valem para toda tarefa (copiadas de `03-plano.md` e do `CLAUDE.md`):

- Alvo `net8.0`; EF Core `8.0.11`; Pomelo `8.0.2`; `Swashbuckle.AspNetCore 10.1.7`; `Microsoft.OpenApi 2.12.2`.
- **Contrato com o app:** erro mantém `error` (e `referencia` em falha de servidor); listagem devolve array JSON.
- **Não escrever em `animal`/`tutor`**; **sem migrations EF**; sem `FallbackPolicy`; nada de JWT novo (é a F2).
- **Sem mudança de comportamento.** Cada tarefa fecha com build e testes verdes *antes* do commit,
  e o número de casos **nunca cai** (ver "Contagem esperada").
- **Git:** commit só depois do "sim" do dono; um commit por tarefa; **sem** `--amend`/`rebase`/`reset --hard`;
  **sem** linha de atribuição nem menção a IA na mensagem; nada de `push`.
- Comentários do código explicam o **porquê**; ao mover código, os comentários **vão junto**, sem reescrever.
- Nomes de domínio e comentários em português; termos técnicos em inglês. Testes: AAA, `Metodo_Cenario_Resultado`.

## Achados que ajustam o design

Verificados em 19/09/2026 lendo o código do commit `6302e7e` e com protótipos descartáveis.

| # | Achado | Consequência neste plano |
|---|---|---|
| A1 | O Swagger monta a documentação a partir do **XML da Api**. Cinco DTOs têm `summary`, mas **só os campos de `LembreteRequest` (`recorrente`, `intervaloDias`, `repetirAte`) chegam ao `swagger.json` hoje**: os DTOs de resposta não viram schema, e um campo enum (como `ProdutoRequest.PorteIndicado`) sai como `$ref`, cuja descrição o OpenAPI 3.0 descarta. Movidos para a Application, **a descrição desses campos some do Swagger em silêncio** (a rota continua respondendo 200). Descoberto na execução da T1, medindo o `swagger.json` real. | T1 cria um teste de regressão *antes* de mover; T4 liga `GenerateDocumentationFile` na Application e manda o Swashbuckle incluir o XML dela. Protótipo: o `.xml` de um projeto referenciado chega ao `bin` da Api, ao `bin` dos testes e ao `publish` sem configuração extra. |
| A2 | Os testes usam `internal` de **duas** camadas: `SaudePreditivaService` (`MontarConvite`, `TentarLerRespostaDaIa`, `CodigoDoCatalogo`) e `LembreteNotificationService` (`VerificarLembretesAsync`, `AvancarSerie`). O design só citava a Infrastructure. | `InternalsVisibleTo("ClyvoVet.Api.Tests.Unit")` vai para a **Application (T4) e para a Infrastructure (T5)** e sai da Api. |
| A3 | O `Microsoft.NET.Sdk.Web` traz *implicit usings* de `Microsoft.Extensions.*`/`AspNetCore`; um `classlib` não. Vários arquivos usam `ILogger`, `IConfiguration`, `BackgroundService`, `IServiceScopeFactory` **sem `using`**. | T4/T5 listam os `using` a acrescentar; o compilador é o árbitro. Protótipo: os pacotes do design bastam (`GetValue<T>` compila sem `Configuration.Binder` explícito; sem `NU1605`). |
| A4 | Caminhos fora do código: `.gitignore` (`ClyvoVet.Api/Logs/`), `azure/03-deploy.sh` (`PROJECT_DIR`), `README.md`, e um **`ClyvoVet.Api/ClyvoVet.slnx` duplicado e velho**, versionado dentro da Api. | T2 corrige `.gitignore` e o script do Azure e **remove o `.slnx` duplicado** (dono confirma). T7 corrige o `README.md`. |
| A5 | Só `MapaDeErro` e seus dois testes citam `DbUpdateException`; **nenhum** repositório a captura. Só os testes chamam `SaveChanges()` síncrono. | T5 traduz no `AppDbContext` (async **e** sync, para nenhum caminho vazar a exceção crua). Comportamento idêntico: toda `DbUpdateException` (inclusive `DbUpdateConcurrencyException`) continua virando 409. |
| A6 | Depois da T5 nem o `MapaDeErro` nem o `Program.cs` tocam mais o EF. O plano-mestre punha a regra "Api sem EF" na T6, onde ela **já nasceria verde**. | A regra da Api entra na **T5** (vermelha até o fim dela). A T6 fica só com a reestruturação do `Program.cs`. |
| A7 | O único consumidor de `HttpContext.ObterIdentidade()` é o `EscopoDoTutor`; **nenhum teste** o constrói. | `IUsuarioAtual` é pequena (T4). Ganha 4 testes unitários novos; a rede de segurança ponta a ponta continua sendo `EscopoPorTutorEndpointsTests`. |
| A8 | `Dockerfile`: copiar `src/` inteiro (em vez de listar cada `csproj`) dispensa mexer nele a cada projeto novo. | T2 já escreve o `Dockerfile` final; T7 só revalida. |

## Contagem esperada de testes

`dotnet test` imprime o total. Linha de base (F0): **256** (163 unidade + 93 integração).

| Depois de | Acréscimo | Total |
|---|---|---|
| T1 | +1 (Swagger expõe XML dos DTOs) | 257 |
| T2 | 0 | 257 |
| T3 | +1 regra do Domain, +2 `RegistroEmUsoException` | 260 |
| T4 | +2 regras da Application (regra + sanidade), +4 `EscopoDoTutor` | 266 |
| T5 | +4 regras Infra/Api (2+2), +2 `AppDbContext` (async/sync) | 272 |
| T6 | 0 | 272 |
| T7 | 0 | 272 |

Se o total real diferir do esperado por **mais** (testes a mais), tudo bem; por **menos**, pare e investigue.

## Convenções de execução

Todos os comandos rodam **da raiz do repositório** (a pasta que contém `ClyvoVet-api.slnx`).

```bash
# BUILD — o SDK 10 do sistema lê o .slnx e compila os projetos net8.0
dotnet build ClyvoVet-api.slnx

# TESTES — só o SDK 8 em ~/.dotnet tem o runtime 8 (ver CLAUDE.local.md); ele não lê .slnx
export DOTNET_ROOT="$HOME/.dotnet"
~/.dotnet/dotnet test tests/ClyvoVet.Api.Tests.Unit/ClyvoVet.Api.Tests.Unit.csproj
~/.dotnet/dotnet test tests/ClyvoVet.Api.Tests.Integration/ClyvoVet.Api.Tests.Integration.csproj
```

- **Antes da T2** os projetos de teste moram em `ClyvoVet.Api/ClyvoVet.Api.Tests.*/`: use esses caminhos na T1.
- **Nunca** use `DOTNET_ROLL_FORWARD` para "fazer passar" (CLAUDE.md).
- Se, ao alternar de SDK, o restore reclamar de `project.assets.json`, apague os `obj/` dos projetos e rode de novo.
- `sed` do macOS é o BSD: use `sed -i ''`, **sem** `\b`. Os comandos deste plano já foram validados assim.

Auxiliar usado nas tarefas T3–T5. É idempotente (não duplica um `using` que já existe). **Funções de shell
não sobrevivem entre chamadas de ferramenta**: cole esta definição no começo de *cada* comando que usar `add_using`
(o mesmo vale para as variáveis `A`, `I`, `T`, `FILES` dos passos, que são redefinidas em cada bloco):

```bash
add_using() { local ns="$1"; shift; for f in "$@"; do grep -q "^using $ns;" "$f" || sed -i '' "1i\\
using $ns;
" "$f"; done; }
```

Por que `sed` e não a IDE: é reproduzível e auditável. Mas **o compilador manda**: depois de cada
rodada de `sed`, `dotnet build` aponta o que faltou (`CS0246`/`CS0234`), e as listas de "esperado"
abaixo são previsões, não garantias.

## Mapa de destino

| Hoje (`ClyvoVet.Api/…`) | Destino (`src/…`) | Namespace |
|---|---|---|
| `Models/*` (12, inclui `ParecerConteudo`) | `ClyvoVet.Domain/Entities` | `ClyvoVet.Domain.Entities` |
| `Enums/*` (6) | `ClyvoVet.Domain/Enums` | `ClyvoVet.Domain.Enums` |
| `Exceptions/*` (2) + **novo** `RegistroEmUsoException` | `ClyvoVet.Domain/Exceptions` | `ClyvoVet.Domain.Exceptions` |
| `DTOs/Request`, `DTOs/Response` | `ClyvoVet.Application/DTOs/…` | `ClyvoVet.Application.DTOs.Request` / `.Response` |
| `Services/{EventoPet,Lembrete,Produto,SugestaoProduto,SaudePreditiva,WidgetSaudePreditiva}Service` | `ClyvoVet.Application/Services` | `ClyvoVet.Application.Services` |
| `Services/Interfaces/I{…}Service` (os mesmos 6) | `ClyvoVet.Application/Services/Interfaces` | `ClyvoVet.Application.Services.Interfaces` |
| `Services/DataValidationHelper` | `ClyvoVet.Application/Common` | `ClyvoVet.Application.Common` |
| `Repositories/Interfaces/*` (9) | `ClyvoVet.Application/Abstractions/Repositories` | `ClyvoVet.Application.Abstractions.Repositories` |
| `Services/Interfaces/IOciGenerativeAiClient`, `ITelegramService` | `ClyvoVet.Application/Abstractions/External` | `ClyvoVet.Application.Abstractions.External` |
| `Security/{EscopoDoTutor, IdentidadeDoChamador, VinculosPendentesDeTelegram}` + **novo** `IUsuarioAtual` | `ClyvoVet.Application/Security` | `ClyvoVet.Application.Security` |
| `Data/AppDbContext`, `Data/Configurations/*` (11) | `ClyvoVet.Infrastructure/Data` | `ClyvoVet.Infrastructure.Data[.Configurations]` |
| `Repositories/*Repository`, `Paginacao` | `ClyvoVet.Infrastructure/Repositories` | `ClyvoVet.Infrastructure.Repositories` |
| `Services/{OciGenerativeAiClient, TelegramService}` | `ClyvoVet.Infrastructure/External` | `ClyvoVet.Infrastructure.External` |
| `Services/{TelegramLinkListenerService, LembreteNotificationService}` | `ClyvoVet.Infrastructure/Background` | `ClyvoVet.Infrastructure.Background` |
| `HealthChecks/TelegramHealthCheck` | `ClyvoVet.Infrastructure/HealthChecks` | `ClyvoVet.Infrastructure.HealthChecks` |
| `Controllers`, `Filters`, `Middleware`, `Swagger`, `Errors`, `HealthChecks/HealthCheckJsonWriter`, `Security/ValidadorDeTokenJwt`, `Program.cs`, `appsettings*`, `Properties` | ficam em `ClyvoVet.Api` | `ClyvoVet.Api.*` (inalterado) |
| *(novo)* `Security/UsuarioAtualHttp`, `Extensions/*` | `ClyvoVet.Api` | `ClyvoVet.Api.Security`, `ClyvoVet.Api.Extensions` |

`ValidadorDeTokenJwt` e `IdentidadeMiddleware` **ficam na Api** e somem na F2.

---

## Task 1 — Linha de base e rede de proteção

**Files:**
- Modify: `ClyvoVet.Api/ClyvoVet.Api.Tests.Integration/SwaggerEndpointsTests.cs`

**Por quê:** o achado A1 é um risco real de regressão silenciosa. O teste precisa existir e estar
**verde antes** de qualquer movimento; se ficar vermelho depois, é porque um passo o quebrou.

- [ ] **Step 1: Confirmar o ponto de partida**

```bash
git status --short          # esperado: vazio
git branch --show-current   # esperado: sprint-4
git ls-files ClyvoVet.Api/publish ClyvoVet.Api/Logs   # esperado: vazio (nada disso está versionado)
```

- [ ] **Step 2: Medir a linha de base**

```bash
export DOTNET_ROOT="$HOME/.dotnet"
~/.dotnet/dotnet test ClyvoVet.Api/ClyvoVet.Api.Tests.Unit/ClyvoVet.Api.Tests.Unit.csproj
~/.dotnet/dotnet test ClyvoVet.Api/ClyvoVet.Api.Tests.Integration/ClyvoVet.Api.Tests.Integration.csproj
```

Esperado: **163** e **93** aprovados, 0 falhas. Se não for isso, **pare** — a F1 não pode começar sobre
uma base que já está vermelha.

- [ ] **Step 3: Escrever o teste de regressão**

Acrescente ao **final da classe** `SwaggerEndpointsTests` (antes da `}` que fecha a classe):

```csharp
    /// <summary>
    /// Os comentários <c>///</c> dos DTOs aparecem no Swagger.
    ///
    /// <para>
    /// O Swashbuckle só enxerga o XML que mandarem incluir. Enquanto os DTOs moram no
    /// mesmo projeto dos controllers, um arquivo basta; ao movê-los para outro projeto, o
    /// XML dele precisa entrar também — senão o Swagger continua respondendo 200 e a
    /// descrição dos campos some em silêncio.
    /// </para>
    /// </summary>
    [Fact]
    public async Task GetSwaggerJson_DtoComComentarioXml_ExpoeADescricaoDoCampo()
    {
        // Arrange
        // "repete sem fim previsto" é o <summary> de LembreteRequest.RepetirAte. Não serve
        // um campo enum, como ProdutoRequest.PorteIndicado: ele sai como $ref para o
        // enum, e o OpenAPI 3.0 descarta a descrição escrita ao lado de um $ref.
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        // Act
        var json = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Contains("repete sem fim previsto", json);
    }
```

- [ ] **Step 4: Rodar e ver PASSAR (hoje ele tem que passar)**

```bash
~/.dotnet/dotnet test ClyvoVet.Api/ClyvoVet.Api.Tests.Integration/ClyvoVet.Api.Tests.Integration.csproj --filter "FullyQualifiedName~SwaggerEndpointsTests"
```

Esperado: 4 aprovados. **Se este teste falhar na linha de base**, o pressuposto do A1 está errado
(o XML não chega ao diretório dos testes, ou o texto mudou): investigue com
`ls ClyvoVet.Api/ClyvoVet.Api.Tests.Integration/bin/Debug/net8.0 | grep -i xml` antes de seguir.

- [ ] **Step 5: Suíte completa**

Rode os dois projetos como no Step 2. Esperado: **257** no total (163 + 94).

- [ ] **Step 6: Commit** — peça o "sim" ao dono do repositório.

```bash
git add ClyvoVet.Api/ClyvoVet.Api.Tests.Integration/SwaggerEndpointsTests.cs
git commit -m "test: garante que o Swagger expõe os comentários dos DTOs"
```

---

## Task 2 — Mover para `src/` e `tests/` (só movimento, zero lógica)

**Files:**
- Move: `ClyvoVet.Api` → `src/ClyvoVet.Api`; os dois projetos de teste → `tests/`
- Delete: `src/ClyvoVet.Api/ClyvoVet.slnx` (duplicado velho — ver A4)
- Modify: `ClyvoVet-api.slnx`, os dois `csproj` de teste, `src/ClyvoVet.Api/ClyvoVet.Api.csproj`, `Dockerfile`, `.dockerignore`, `.gitignore`, `azure/03-deploy.sh`

**Interfaces:** nenhuma; nenhum tipo muda.

- [ ] **Step 1: Limpar saídas de build (regeneráveis, ignoradas pelo Git)**

```bash
rm -rf ClyvoVet.Api/bin ClyvoVet.Api/obj \
       ClyvoVet.Api/ClyvoVet.Api.Tests.Unit/bin ClyvoVet.Api/ClyvoVet.Api.Tests.Unit/obj \
       ClyvoVet.Api/ClyvoVet.Api.Tests.Integration/bin ClyvoVet.Api/ClyvoVet.Api.Tests.Integration/obj
```

`ClyvoVet.Api/publish/` (25 MB), `publish.zip` (7,8 MB) e `Logs/` também são ignorados e **vão junto**
no `git mv`. `publish*` são gerados por `azure/03-deploy.sh` e podem ser apagados; **pergunte ao dono**
se quer apagá-los (`rm -rf ClyvoVet.Api/publish ClyvoVet.Api/publish.zip`) ou deixar. `Logs/` é log local dele: não apague.

- [ ] **Step 2: Mover com `git mv` (preserva o histórico)**

```bash
mkdir -p src tests
git mv ClyvoVet.Api src/ClyvoVet.Api
git mv src/ClyvoVet.Api/ClyvoVet.Api.Tests.Unit        tests/ClyvoVet.Api.Tests.Unit
git mv src/ClyvoVet.Api/ClyvoVet.Api.Tests.Integration tests/ClyvoVet.Api.Tests.Integration
```

- [ ] **Step 3: Remover o `.slnx` duplicado — peça o "sim" do dono**

`src/ClyvoVet.Api/ClyvoVet.slnx` lista os mesmos 3 projetos que o `ClyvoVet-api.slnx` da raiz, com
caminhos relativos que **quebram** com esta mudança. Nenhum arquivo do repositório o referencia.

```bash
git grep -n "ClyvoVet\.slnx" -- . ':!docs/sprint4'    # esperado: nada (além do próprio arquivo)
git rm src/ClyvoVet.Api/ClyvoVet.slnx
```

- [ ] **Step 4: Solução da raiz**

Conteúdo final de `ClyvoVet-api.slnx`:

```xml
<Solution>
<Project Path="src/ClyvoVet.Api/ClyvoVet.Api.csproj" />
<Project Path="tests/ClyvoVet.Api.Tests.Unit/ClyvoVet.Api.Tests.Unit.csproj" />
<Project Path="tests/ClyvoVet.Api.Tests.Integration/ClyvoVet.Api.Tests.Integration.csproj" />
</Solution>
```

- [ ] **Step 5: `ProjectReference` dos dois projetos de teste**

Nos dois `csproj` de `tests/`, troque a linha:

```xml
    <ProjectReference Include="..\ClyvoVet.Api.csproj" />
```

por:

```xml
    <ProjectReference Include="..\..\src\ClyvoVet.Api\ClyvoVet.Api.csproj" />
```

- [ ] **Step 6: Tirar os 8 itens `Remove` do `csproj` da Api**

Em `src/ClyvoVet.Api/ClyvoVet.Api.csproj`, apague o `ItemGroup` inteiro que contém
`<Compile Remove="ClyvoVet.Api.Tests.Unit\**" />` … `<None Remove="ClyvoVet.Api.Tests.Integration\**" />`
(as 8 linhas e o `<ItemGroup>`/`</ItemGroup>` em volta). Eles existiam só porque os testes moravam dentro da Api.

- [ ] **Step 7: `Dockerfile`**

Troque as linhas 4–9 (do primeiro `COPY` ao `RUN dotnet publish`) — o resto do arquivo, com seus
comentários sobre `PORT` e `exec`, **não muda**:

```dockerfile
# Copia src/ inteiro, e não só o csproj da Api: ela referencia Application e
# Infrastructure, e uma lista de csproj para manter à mão é o tipo de coisa que
# ninguém lembra de atualizar quando entra o próximo projeto.
COPY src/ src/
RUN dotnet restore src/ClyvoVet.Api/ClyvoVet.Api.csproj

WORKDIR /src/src/ClyvoVet.Api
RUN dotnet publish ClyvoVet.Api.csproj -c Release -o /app/publish /p:UseAppHost=false
```

- [ ] **Step 8: `.dockerignore`, `.gitignore`, script do Azure**

`.dockerignore`: troque as duas linhas `**/ClyvoVet.Api.Tests.Unit/` e `**/ClyvoVet.Api.Tests.Integration/` por uma só:

```
tests/
```

`.gitignore` (linha 374): `ClyvoVet.Api/Logs/` → `src/ClyvoVet.Api/Logs/`
(sem isso, rodar a Api em `Development` deixaria `Logs/` como arquivo novo para versionar).

`azure/03-deploy.sh` (linha 11): `PROJECT_DIR="../ClyvoVet.Api"` → `PROJECT_DIR="../src/ClyvoVet.Api"`.

- [ ] **Step 9: Build e testes**

```bash
dotnet build ClyvoVet-api.slnx
export DOTNET_ROOT="$HOME/.dotnet"
~/.dotnet/dotnet test tests/ClyvoVet.Api.Tests.Unit/ClyvoVet.Api.Tests.Unit.csproj
~/.dotnet/dotnet test tests/ClyvoVet.Api.Tests.Integration/ClyvoVet.Api.Tests.Integration.csproj
```

Esperado: build sem erro; **257** aprovados (163 + 94), mesmos números do fim da T1. Nenhum arquivo `.cs` mudou.

- [ ] **Step 10: `docker build`** (Docker Desktop costuma estar desligado: ligue antes)

```bash
docker build -t clyvovet-api .
```

Esperado: `naming to docker.io/library/clyvovet-api` sem erro. É a validação do risco de deploy (design §10).

- [ ] **Step 11: Conferir que o Git enxerga renomeações**

```bash
git add src tests ClyvoVet-api.slnx Dockerfile .dockerignore .gitignore azure/03-deploy.sh
git diff --cached --stat -M | tail -3
git diff --cached -M --summary | grep -cE '^ (create|delete) mode'    # esperado: 1 (o ClyvoVet.slnx removido)
```

- [ ] **Step 12: Commit** — peça o "sim".

```bash
git commit -m "refactor: move os projetos para src/ e tests/" \
  -m "Só movimento: nenhum arquivo .cs muda. Acompanham o Dockerfile, o .dockerignore, o .gitignore (Logs/), o script do Azure e a solução. Remove o ClyvoVet.slnx duplicado que ficava dentro da Api."
```

---

## Task 3 — Domain

**Files:**
- Create: `src/ClyvoVet.Domain/ClyvoVet.Domain.csproj`, `src/ClyvoVet.Domain/Exceptions/RegistroEmUsoException.cs`
- Create (teste): `tests/ClyvoVet.Api.Tests.Unit/ArquiteturaTests.cs`, `tests/ClyvoVet.Api.Tests.Unit/RegistroEmUsoExceptionTests.cs`
- Move: `Models/*`→`Entities`, `Enums/*`, `Exceptions/*`
- Modify: `ClyvoVet-api.slnx`; `ProjectReference` da Api e dos dois projetos de teste; `using`/`namespace` de todo o código

**Interfaces — Produz (as tarefas seguintes dependem disto):**
- `ClyvoVet.Domain.Exceptions.RegistroEmUsoException : Exception` com `const string MensagemPadrao` e `RegistroEmUsoException(Exception? causa = null)` (T5 e `MapaDeErro` usam).
- Assembly `ClyvoVet.Domain` sem **nenhuma** referência a `ClyvoVet.*` ou `Microsoft.*`.

- [ ] **Step 1: Escrever a regra do Domain (vermelho)**

Crie `tests/ClyvoVet.Api.Tests.Unit/ArquiteturaTests.cs`:

```csharp
using System.Reflection;
using ClyvoVet.Domain.Entities;

namespace ClyvoVet.Api.Tests.Unit;

/// <summary>
/// As regras de dependência entre as camadas, verificadas no que o compilador gravou.
///
/// <para>
/// Referência de projeto não basta: os pacotes fluem da Infrastructure para a Api por
/// transitividade, então a Api compilaria usando tipos do EF sem reclamar. Estes testes
/// leem os assemblies já compilados — e o compilador só grava uma referência quando o
/// código realmente usa um tipo do outro assembly. Enxergam uso real, não intenção.
/// </para>
///
/// <para>
/// Se um deles falhar, a arquitetura está errada — não o teste.
/// </para>
/// </summary>
public class ArquiteturaTests
{
    private static readonly Assembly AssemblyDoDomain = typeof(Animal).Assembly;

    private static IReadOnlyList<string> Referencias(Assembly assembly) =>
        assembly.GetReferencedAssemblies().Select(nome => nome.Name!).ToList();

    private static void NaoPodeReferenciar(Assembly assembly, params string[] prefixos)
    {
        var proibidas = Referencias(assembly)
            .Where(nome => prefixos.Any(prefixo => nome.StartsWith(prefixo, StringComparison.Ordinal)))
            .ToList();

        Assert.True(
            proibidas.Count == 0,
            $"{assembly.GetName().Name} não pode referenciar: {string.Join(", ", proibidas)}");
    }

    [Fact]
    public void Domain_NaoReferenciaNenhumProjetoNemPacote()
    {
        NaoPodeReferenciar(AssemblyDoDomain, "ClyvoVet.", "Microsoft.");
    }
}
```

- [ ] **Step 2: Escrever os testes da exceção (vermelho)**

Crie `tests/ClyvoVet.Api.Tests.Unit/RegistroEmUsoExceptionTests.cs`:

```csharp
using ClyvoVet.Domain.Exceptions;

namespace ClyvoVet.Api.Tests.Unit;

public class RegistroEmUsoExceptionTests
{
    [Fact]
    public void Construtor_SemCausa_UsaAMensagemPadrao()
    {
        // Arrange & Act
        var excecao = new RegistroEmUsoException();

        // Assert
        Assert.Equal("Registro em uso por outro cadastro.", excecao.Message);
        Assert.Null(excecao.InnerException);
    }

    [Fact]
    public void Construtor_ComCausa_PreservaAExcecaoOriginalSemVazarSuaMensagem()
    {
        // Arrange
        var causa = new InvalidOperationException(
            "FK fk_sugestao_produto na tabela t_clyvo_sugestao_produto");

        // Act
        var excecao = new RegistroEmUsoException(causa);

        // Assert
        Assert.Same(causa, excecao.InnerException);
        Assert.Equal("Registro em uso por outro cadastro.", excecao.Message);
    }
}
```

- [ ] **Step 3: Rodar e ver FALHAR**

```bash
~/.dotnet/dotnet test tests/ClyvoVet.Api.Tests.Unit/ClyvoVet.Api.Tests.Unit.csproj
```

Esperado: **erro de compilação** `CS0234`/`CS0246` — o namespace `ClyvoVet.Domain` não existe. É o vermelho.

- [ ] **Step 4: Criar o projeto Domain**

`src/ClyvoVet.Domain/ClyvoVet.Domain.csproj` (sem pacote nenhum, de propósito):

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

</Project>
```

Acrescente ao `ClyvoVet-api.slnx`, depois da linha da Api:

```xml
<Project Path="src/ClyvoVet.Domain/ClyvoVet.Domain.csproj" />
```

Referências (a Api usa as entidades; os dois projetos de teste também):

```bash
dotnet add src/ClyvoVet.Api/ClyvoVet.Api.csproj reference src/ClyvoVet.Domain/ClyvoVet.Domain.csproj
dotnet add tests/ClyvoVet.Api.Tests.Unit/ClyvoVet.Api.Tests.Unit.csproj reference src/ClyvoVet.Domain/ClyvoVet.Domain.csproj
dotnet add tests/ClyvoVet.Api.Tests.Integration/ClyvoVet.Api.Tests.Integration.csproj reference src/ClyvoVet.Domain/ClyvoVet.Domain.csproj
```

- [ ] **Step 5: Mover os arquivos**

```bash
mkdir -p src/ClyvoVet.Domain/{Entities,Enums,Exceptions}
git mv src/ClyvoVet.Api/Models/*.cs     src/ClyvoVet.Domain/Entities/
git mv src/ClyvoVet.Api/Enums/*.cs      src/ClyvoVet.Domain/Enums/
git mv src/ClyvoVet.Api/Exceptions/*.cs src/ClyvoVet.Domain/Exceptions/
```

- [ ] **Step 6: Trocar `namespace` e `using` em todo o código**

```bash
FILES=$(grep -rlE 'ClyvoVet\.Api\.(Models|Enums|Exceptions)' src tests --include='*.cs' --exclude-dir=bin --exclude-dir=obj)
sed -i '' -E \
  -e 's/ClyvoVet\.Api\.Models/ClyvoVet.Domain.Entities/g' \
  -e 's/ClyvoVet\.Api\.Enums/ClyvoVet.Domain.Enums/g' \
  -e 's/ClyvoVet\.Api\.Exceptions/ClyvoVet.Domain.Exceptions/g' \
  $FILES
```

Não toca em `ClyvoVet.Api.Tests.*` (o padrão é específico) nem em `ClyvoVet.Api.Errors`/`Filters`/…

- [ ] **Step 7: Criar `RegistroEmUsoException`**

`src/ClyvoVet.Domain/Exceptions/RegistroEmUsoException.cs`:

```csharp
namespace ClyvoVet.Domain.Exceptions;

/// <summary>
/// O registro está em uso por outro cadastro e não pode ser alterado ou removido.
///
/// <para>
/// Existe para que a Application e a Api não precisem conhecer o EF Core: quem
/// fala com o banco (<c>AppDbContext</c>) traduz a falha de integridade para cá.
/// </para>
///
/// <para>
/// A mensagem é fixa de propósito. O texto do provedor cita tabela e constraint —
/// detalhe de banco que não ajuda quem lê a tela e que descreve o schema para quem
/// não deveria vê-lo. A causa original fica em <see cref="Exception.InnerException"/>,
/// para o log.
/// </para>
/// </summary>
public class RegistroEmUsoException : Exception
{
    public const string MensagemPadrao = "Registro em uso por outro cadastro.";

    public RegistroEmUsoException(Exception? causa = null) : base(MensagemPadrao, causa) { }
}
```

- [ ] **Step 8: Build e testes**

```bash
dotnet build ClyvoVet-api.slnx
~/.dotnet/dotnet test tests/ClyvoVet.Api.Tests.Unit/ClyvoVet.Api.Tests.Unit.csproj
~/.dotnet/dotnet test tests/ClyvoVet.Api.Tests.Integration/ClyvoVet.Api.Tests.Integration.csproj
```

Esperado: build sem erro; **260** aprovados (Unit 166, Integration 94), incluindo `Domain_NaoReferenciaNenhumProjetoNemPacote`.
Se o build apontar `CS0246` em algum arquivo, é um `using` que o `sed` não alcançou (um tipo usado sem `using`
por estar antes no mesmo namespace): acrescente o `using` correspondente ao arquivo apontado.

- [ ] **Step 9: Commit** — peça o "sim".

```bash
git add src tests ClyvoVet-api.slnx
git commit -m "refactor: extrai o projeto Domain" \
  -m "Entidades, enums e exceções de negócio vão para o ClyvoVet.Domain, sem nenhum pacote. Nasce a RegistroEmUsoException, que a fronteira com o EF vai usar, e o ArquiteturaTests com a regra do Domain."
```

---

## Task 4 — Application

**Files:**
- Create: `src/ClyvoVet.Application/ClyvoVet.Application.csproj`, `src/ClyvoVet.Application/ApplicationServiceExtensions.cs`, `src/ClyvoVet.Application/Security/IUsuarioAtual.cs`, `src/ClyvoVet.Api/Security/UsuarioAtualHttp.cs`
- Create (teste): `tests/ClyvoVet.Api.Tests.Unit/EscopoDoTutorTests.cs`; acrescentar a `ArquiteturaTests.cs`
- Move: DTOs, 6 serviços + 6 interfaces, `DataValidationHelper`, 9 interfaces de repositório, `IOciGenerativeAiClient`, `ITelegramService`, 3 arquivos de `Security/`
- Modify: `EscopoDoTutor.cs`, `Program.cs`, `ClyvoVet-api.slnx`, `csproj` da Api e dos testes, e os `using` de todo o código

**Interfaces — Produz:**
- `IUsuarioAtual { IdentidadeDoChamador? Identidade { get; } }` (Application/Security). `null` = pedido sem token válido.
- `UsuarioAtualHttp(IHttpContextAccessor) : IUsuarioAtual` (Api). Na F2 passa a ler `HttpContext.User`.
- `services.AddApplication()` registra os 6 serviços, `EscopoDoTutor` (scoped) e `VinculosPendentesDeTelegram` (singleton).
- `EscopoDoTutor(IConfiguration, IUsuarioAtual, IAnimalRepository)` — o segundo parâmetro deixa de ser `IHttpContextAccessor`.

- [ ] **Step 1: Escrever as regras da Application (vermelho)**

Em `ArquiteturaTests.cs`, acrescente os `using` `ClyvoVet.Application.Security;` no topo, o campo abaixo junto do `AssemblyDoDomain`, e os dois testes no fim da classe:

```csharp
    private static readonly Assembly AssemblyDaApplication = typeof(EscopoDoTutor).Assembly;
```

```csharp
    [Fact]
    public void Application_NaoReferenciaInfraestruturaNemFrameworkWeb()
    {
        NaoPodeReferenciar(
            AssemblyDaApplication,
            "Microsoft.EntityFrameworkCore", "MongoDB", "Telegram", "Microsoft.AspNetCore",
            "ClyvoVet.Infrastructure", "ClyvoVet.Api");
    }

    /// <summary>
    /// Sem este, a regra acima passaria mesmo que <c>GetReferencedAssemblies</c> devolvesse
    /// sempre uma lista vazia — uma verificação que nunca falha não verifica nada.
    /// </summary>
    [Fact]
    public void Application_UsaAbstracoesDoMicrosoftExtensions_ProvaQueAVerificacaoEnxergaReferencias()
    {
        Assert.Contains("Microsoft.Extensions.Logging.Abstractions", Referencias(AssemblyDaApplication));
    }
```

- [ ] **Step 2: Escrever os testes do `EscopoDoTutor` (vermelho)**

`EscopoDoTutor` não tinha teste unitário porque dependia de `HttpContext`. Com `IUsuarioAtual` ele vira testável — teste a costura nova.
`tests/ClyvoVet.Api.Tests.Unit/EscopoDoTutorTests.cs`:

```csharp
using ClyvoVet.Application.Abstractions.Repositories;
using ClyvoVet.Application.Security;
using ClyvoVet.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Moq;

namespace ClyvoVet.Api.Tests.Unit;

public class EscopoDoTutorTests
{
    private static EscopoDoTutor Criar(
        string? flag,
        IdentidadeDoChamador? identidade,
        Mock<IAnimalRepository>? animais = null)
    {
        var configuracao = new Mock<IConfiguration>();
        configuracao.Setup(c => c["Api:EscopoPorTutor"]).Returns(flag);

        var usuario = new Mock<IUsuarioAtual>();
        usuario.Setup(u => u.Identidade).Returns(identidade);

        return new EscopoDoTutor(
            configuracao.Object,
            usuario.Object,
            (animais ?? new Mock<IAnimalRepository>()).Object);
    }

    [Fact]
    public void FiltroDeListagem_RecorteDesligado_DevolveNullSemExigirToken()
    {
        // Arrange
        var escopo = Criar(flag: null, identidade: null);

        // Act & Assert
        Assert.Null(escopo.FiltroDeListagem());
    }

    [Fact]
    public void FiltroDeListagem_RecorteLigadoSemTutorNoToken_Lanca()
    {
        // Arrange — ADMIN não tem tutor: nulo NEGA, nunca "passa sem filtro".
        var escopo = Criar("true", new IdentidadeDoChamador("u1", null, "ADMIN"));

        // Act & Assert
        Assert.Throws<SemTutorNoTokenException>(() => escopo.FiltroDeListagem());
    }

    [Fact]
    public void FiltroDeListagem_RecorteLigadoComTutor_DevolveOTutorDoToken()
    {
        // Arrange
        var escopo = Criar("true", new IdentidadeDoChamador("u1", "tutor-1", "TUTOR"));

        // Act & Assert
        Assert.Equal("tutor-1", escopo.FiltroDeListagem());
    }

    [Fact]
    public async Task AnimalEDoTutorAsync_AnimalDeOutroTutor_DevolveFalse()
    {
        // Arrange — animal de outro tutor responde "não" (vira 404 no controller, nunca 403).
        var animais = new Mock<IAnimalRepository>();
        animais.Setup(a => a.GetByIdAsync("animal-1"))
            .ReturnsAsync(new Animal { Id = "animal-1", TutorId = "tutor-2" });
        var escopo = Criar("true", new IdentidadeDoChamador("u1", "tutor-1", "TUTOR"), animais);

        // Act
        var resultado = await escopo.AnimalEDoTutorAsync("animal-1");

        // Assert
        Assert.False(resultado);
    }
}
```

- [ ] **Step 3: Rodar e ver FALHAR**

```bash
~/.dotnet/dotnet test tests/ClyvoVet.Api.Tests.Unit/ClyvoVet.Api.Tests.Unit.csproj
```

Esperado: erro de compilação (`IUsuarioAtual` não existe; `EscopoDoTutor` ainda está em `ClyvoVet.Api.Security`).

- [ ] **Step 4: Criar o projeto Application**

`src/ClyvoVet.Application/ClyvoVet.Application.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <!-- O Swagger monta a descrição dos campos a partir do XML de comentários, e os DTOs
         (com os summary) moram aqui. Sem este XML, a descrição some do Swagger em silêncio. -->
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);1591;1573</NoWarn>
  </PropertyGroup>

  <ItemGroup>
    <!-- O SaudePreditivaService tem membros internal que os testes chamam direto. -->
    <InternalsVisibleTo Include="ClyvoVet.Api.Tests.Unit" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Configuration.Abstractions" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="8.0.2" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.2" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\ClyvoVet.Domain\ClyvoVet.Domain.csproj" />
  </ItemGroup>

</Project>
```

Acrescente ao `.slnx`: `<Project Path="src/ClyvoVet.Application/ClyvoVet.Application.csproj" />`. Referências:

```bash
dotnet add src/ClyvoVet.Api/ClyvoVet.Api.csproj reference src/ClyvoVet.Application/ClyvoVet.Application.csproj
dotnet add tests/ClyvoVet.Api.Tests.Unit/ClyvoVet.Api.Tests.Unit.csproj reference src/ClyvoVet.Application/ClyvoVet.Application.csproj
dotnet add tests/ClyvoVet.Api.Tests.Integration/ClyvoVet.Api.Tests.Integration.csproj reference src/ClyvoVet.Application/ClyvoVet.Application.csproj
```

- [ ] **Step 5: Mover os arquivos**

```bash
A=src/ClyvoVet.Application
mkdir -p $A/{DTOs,Services/Interfaces,Common,Abstractions/Repositories,Abstractions/External,Security}

git mv src/ClyvoVet.Api/DTOs/Request  $A/DTOs/Request
git mv src/ClyvoVet.Api/DTOs/Response $A/DTOs/Response

for s in EventoPet Lembrete Produto SugestaoProduto SaudePreditiva WidgetSaudePreditiva; do
  git mv src/ClyvoVet.Api/Services/${s}Service.cs             $A/Services/
  git mv src/ClyvoVet.Api/Services/Interfaces/I${s}Service.cs $A/Services/Interfaces/
done

git mv src/ClyvoVet.Api/Services/DataValidationHelper.cs $A/Common/
git mv src/ClyvoVet.Api/Repositories/Interfaces/*.cs     $A/Abstractions/Repositories/
git mv src/ClyvoVet.Api/Services/Interfaces/IOciGenerativeAiClient.cs $A/Abstractions/External/
git mv src/ClyvoVet.Api/Services/Interfaces/ITelegramService.cs       $A/Abstractions/External/
git mv src/ClyvoVet.Api/Security/EscopoDoTutor.cs \
       src/ClyvoVet.Api/Security/IdentidadeDoChamador.cs \
       src/ClyvoVet.Api/Security/VinculosPendentesDeTelegram.cs $A/Security/
```

- [ ] **Step 6: `namespace` dos arquivos movidos**

```bash
A=src/ClyvoVet.Application
sed -i '' -E 's/^namespace ClyvoVet\.Api\.DTOs/namespace ClyvoVet.Application.DTOs/'                                   $A/DTOs/Request/*.cs $A/DTOs/Response/*.cs
sed -i '' -E 's/^namespace ClyvoVet\.Api\.Services\.Interfaces;/namespace ClyvoVet.Application.Services.Interfaces;/'   $A/Services/Interfaces/*.cs
sed -i '' -E 's/^namespace ClyvoVet\.Api\.Services;/namespace ClyvoVet.Application.Services;/'                         $A/Services/*.cs
sed -i '' -E 's/^namespace ClyvoVet\.Api\.Services;/namespace ClyvoVet.Application.Common;/'                           $A/Common/*.cs
sed -i '' -E 's/^namespace ClyvoVet\.Api\.Repositories\.Interfaces;/namespace ClyvoVet.Application.Abstractions.Repositories;/' $A/Abstractions/Repositories/*.cs
sed -i '' -E 's/^namespace ClyvoVet\.Api\.Services\.Interfaces;/namespace ClyvoVet.Application.Abstractions.External;/' $A/Abstractions/External/*.cs
sed -i '' -E 's/^namespace ClyvoVet\.Api\.Security;/namespace ClyvoVet.Application.Security;/'                           $A/Security/*.cs
```

- [ ] **Step 7: `using` de todo o código**

```bash
FILES=$(grep -rlE 'using ClyvoVet\.Api\.(DTOs|Repositories\.Interfaces|Services|Security)' src tests --include='*.cs' --exclude-dir=bin --exclude-dir=obj)
sed -i '' -E \
  -e 's/^using ClyvoVet\.Api\.DTOs/using ClyvoVet.Application.DTOs/' \
  -e 's/^using ClyvoVet\.Api\.Repositories\.Interfaces;/using ClyvoVet.Application.Abstractions.Repositories;/' \
  -e 's/^using ClyvoVet\.Api\.Services\.Interfaces;/using ClyvoVet.Application.Services.Interfaces;/' \
  -e 's/^using ClyvoVet\.Api\.Security;/using ClyvoVet.Application.Security;/' \
  $FILES

# `using ClyvoVet.Api.Services;` é ambíguo (Application e Infrastructure moram nele hoje): por grupo.
T=tests/ClyvoVet.Api.Tests.Unit
sed -i '' 's/^using ClyvoVet\.Api\.Services;/using ClyvoVet.Application.Services;/' \
  $T/EventoPetServiceTests.cs $T/LembreteServiceTests.cs $T/ProdutoServiceTests.cs \
  $T/SaudePreditivaServiceTests.cs $T/SugestaoProdutoServiceTests.cs $T/WidgetSaudePreditivaServiceTests.cs
sed -i '' 's/^using ClyvoVet\.Api\.Services;/using ClyvoVet.Application.Common;/' $T/DataValidationHelperTests.cs
# LembreteNotificationServiceTests e Program.cs continuam com ClyvoVet.Api.Services até a T5.
```

Agora acrescente os `using` que os arquivos usavam **sem declarar** (mesmo namespace antigo, ou *implicit usings* do SDK Web — achado A3):

```bash
A=src/ClyvoVet.Application
add_using ClyvoVet.Application.Common $A/Services/EventoPetService.cs $A/Services/LembreteService.cs $A/Services/SugestaoProdutoService.cs

# IOciGenerativeAiClient / ITelegramService agora estão em Abstractions.External
add_using ClyvoVet.Application.Abstractions.External \
  $A/Services/SaudePreditivaService.cs \
  src/ClyvoVet.Api/Services/OciGenerativeAiClient.cs src/ClyvoVet.Api/Services/TelegramService.cs \
  src/ClyvoVet.Api/Services/LembreteNotificationService.cs \
  src/ClyvoVet.Api/Controllers/TelegramController.cs src/ClyvoVet.Api/Program.cs \
  $T/SaudePreditivaServiceTests.cs $T/LembreteNotificationServiceTests.cs \
  tests/ClyvoVet.Api.Tests.Integration/TelegramEndpointsTests.cs

# O SDK Web injetava estes; o classlib não:
add_using Microsoft.Extensions.Logging $A/Services/SaudePreditivaService.cs $A/Services/WidgetSaudePreditivaService.cs
add_using Microsoft.Extensions.Configuration $A/Security/EscopoDoTutor.cs

# A Api ainda tem o ValidadorDeTokenJwt em ClyvoVet.Api.Security (some na F2):
add_using ClyvoVet.Api.Security src/ClyvoVet.Api/Middleware/IdentidadeMiddleware.cs src/ClyvoVet.Api/Program.cs $T/ValidadorDeTokenJwtTests.cs
add_using ClyvoVet.Application.Security src/ClyvoVet.Api/Security/ValidadorDeTokenJwt.cs
```

- [ ] **Step 8: `IUsuarioAtual` e o `EscopoDoTutor`**

`src/ClyvoVet.Application/Security/IUsuarioAtual.cs`:

```csharp
namespace ClyvoVet.Application.Security;

/// <summary>
/// Quem está chamando a requisição corrente.
///
/// <para>
/// Existe para a Application não conhecer <c>HttpContext</c>: o <c>EscopoDoTutor</c>
/// precisa saber o tutor do chamador, e quem sabe ler isso do pedido é a Api. Também é
/// o que torna o <c>EscopoDoTutor</c> testável sem montar um contexto HTTP.
/// </para>
/// </summary>
public interface IUsuarioAtual
{
    /// <summary>A identidade do token, ou <c>null</c> se o pedido não trouxe token válido.</summary>
    IdentidadeDoChamador? Identidade { get; }
}
```

`src/ClyvoVet.Api/Security/UsuarioAtualHttp.cs`:

```csharp
using ClyvoVet.Api.Middleware;
using ClyvoVet.Application.Security;

namespace ClyvoVet.Api.Security;

/// <summary>
/// <see cref="IUsuarioAtual"/> sobre o <c>HttpContext</c>. Lê o que o
/// <c>IdentidadeMiddleware</c> guardou; na F2 passa a ler <c>HttpContext.User</c>.
/// </summary>
public class UsuarioAtualHttp(IHttpContextAccessor acessor) : IUsuarioAtual
{
    public IdentidadeDoChamador? Identidade => acessor.HttpContext?.ObterIdentidade();
}
```

Em `src/ClyvoVet.Application/Security/EscopoDoTutor.cs`:

1. Apague `using ClyvoVet.Api.Middleware;`.
2. No construtor, troque `IHttpContextAccessor acessor,` por `IUsuarioAtual usuario,`.
3. Troque `public string? TutorId => acessor.HttpContext?.ObterIdentidade()?.TutorId;` por `public string? TutorId => usuario.Identidade?.TutorId;`.

- [ ] **Step 9: `AddApplication`**

`src/ClyvoVet.Application/ApplicationServiceExtensions.cs`:

```csharp
using ClyvoVet.Application.Security;
using ClyvoVet.Application.Services;
using ClyvoVet.Application.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace ClyvoVet.Application;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IProdutoService,              ProdutoService>();
        services.AddScoped<ISugestaoProdutoService,      SugestaoProdutoService>();
        services.AddScoped<ILembreteService,             LembreteService>();
        services.AddScoped<IEventoPetService,            EventoPetService>();
        services.AddScoped<IWidgetSaudePreditivaService, WidgetSaudePreditivaService>();
        services.AddScoped<ISaudePreditivaService,       SaudePreditivaService>();

        services.AddScoped<EscopoDoTutor>();

        // MOVA AQUI, literalmente, o comentário de Program.cs que começa em
        // "// Singleton, e nao Scoped: quem GERA o convite..." (4 linhas, commit 6302e7e:260-263).
        services.AddSingleton<VinculosPendentesDeTelegram>();

        return services;
    }
}
```

- [ ] **Step 10: `Program.cs` (mudança temporária; a T6 o reescreve)**

Em `src/ClyvoVet.Api/Program.cs`:

1. **Apague** os registros que passaram para o `AddApplication`: as 6 linhas `AddScoped<I…Service, …Service>()` (blocos das linhas 232–237 do commit `6302e7e`), `builder.Services.AddScoped<EscopoDoTutor>();` e o `AddSingleton<VinculosPendentesDeTelegram>()` **com o comentário acima dele**.
2. **Acrescente** `builder.Services.AddApplication();` no lugar dos 6 registros, e adicione `using ClyvoVet.Application;`.
3. **Registre a identidade**: logo depois de `builder.Services.AddHttpContextAccessor();` acrescente `builder.Services.AddScoped<IUsuarioAtual, UsuarioAtualHttp>();` e reescreva o comentário do acessor, que dizia "O EscopoDoTutor le a identidade…", para dizer que **quem lê a requisição corrente é o `UsuarioAtualHttp`** (o resto do comentário — "sem ele a resolução falha no primeiro request, e não no startup" — continua verdadeiro).
4. **Swagger — incluir o XML da Application** (achado A1). Troque:

```csharp
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
```

por:

```csharp
    // Um XML por projeto que tem documentação para o Swagger: os controllers estão na Api,
    // e os DTOs — com o summary de cada campo — na Application. Sem o segundo, o Swagger
    // continua respondendo 200 e a descrição dos campos some em silêncio.
    foreach (var assembly in new[] { Assembly.GetExecutingAssembly(), typeof(ProdutoRequest).Assembly })
    {
        var xmlPath = Path.Combine(AppContext.BaseDirectory, $"{assembly.GetName().Name}.xml");
        if (File.Exists(xmlPath))
            options.IncludeXmlComments(xmlPath);
    }
```

e adicione `using ClyvoVet.Application.DTOs.Request;`.

- [ ] **Step 11: Build — deixar o compilador apontar o que faltou**

```bash
dotnet build ClyvoVet-api.slnx
```

Esperado: 0 erros. Se aparecer `CS0246`/`CS0234`, é um `using` faltando no arquivo apontado; use `add_using`.
Previsões do que **pode** aparecer: `HttpClient`/`ILogger` sem `using` em serviços de aplicação; `Animal`/`ParecerConteudo` sem `using ClyvoVet.Domain.Entities`
em serviços que estavam no mesmo namespace das entidades. Avisos `CS1574` (`cref` que não resolve) não são erro, mas corrija os que aparecerem em arquivos movidos.

- [ ] **Step 12: Testes**

```bash
~/.dotnet/dotnet test tests/ClyvoVet.Api.Tests.Unit/ClyvoVet.Api.Tests.Unit.csproj
~/.dotnet/dotnet test tests/ClyvoVet.Api.Tests.Integration/ClyvoVet.Api.Tests.Integration.csproj
```

Esperado: **266** aprovados. Confira em especial: `SwaggerEndpointsTests` (o teste da T1 tem que continuar verde — é ele
que prova que o XML da Application entrou) e `EscopoPorTutorEndpointsTests` (a rede ponta a ponta do `IUsuarioAtual`).

**Efeito cosmético esperado, não é regressão:** o `<see cref="IntervaloDias"/>` do `summary` de `LembreteRequest.Recorrente` sai no Swagger com o
nome completo do tipo — hoje `ClyvoVet.Api.DTOs.Request.LembreteRequest.IntervaloDias`, depois da T4 `ClyvoVet.Application.DTOs.Request.LembreteRequest.IntervaloDias`.
Vem do namespace novo e não afeta contrato nem app; se incomodar, troque o `cref` por `<c>IntervaloDias</c>` no DTO, num commit à parte.

- [ ] **Step 13: Commit** — peça o "sim".

```bash
git add src tests ClyvoVet-api.slnx
git commit -m "refactor: extrai o projeto Application" \
  -m "DTOs, serviços de caso de uso e as interfaces de repositório e de serviços externos vão para o ClyvoVet.Application, que só conhece o Domain e as abstrações do Microsoft.Extensions. O EscopoDoTutor deixa de depender do HttpContext (IUsuarioAtual, implementada na Api). O Swagger passa a incluir o XML da Application para não perder a descrição dos campos dos DTOs."
```

---

## Task 5 — Infrastructure

**Files:**
- Create: `src/ClyvoVet.Infrastructure/ClyvoVet.Infrastructure.csproj`, `src/ClyvoVet.Infrastructure/InfrastructureServiceExtensions.cs`
- Create (teste): `tests/ClyvoVet.Api.Tests.Integration/AppDbContextTests.cs`; acrescentar a `ArquiteturaTests.cs`
- Move: `Data/*`, `Repositories/*` (concretos + `Paginacao`), `OciGenerativeAiClient`, `TelegramService`, os 2 `BackgroundService`, `TelegramHealthCheck`
- Modify: `AppDbContext.cs`, `MapaDeErro.cs`, `MapaDeErroTests.cs`, `Program.cs`, `csproj` da Api e dos testes, `.slnx`, `using` de todo o código

**Interfaces — Produz:**
- `services.AddInfrastructure(IConfiguration configuration, IHostEnvironment environment)` (extensão de `IServiceCollection`), com **exatamente** o que hoje está no `Program.cs`: teto de pool, versão fixa do MySQL, repositórios, cliente OCI, Telegram, *background services* fora de `Testing`, health checks de dependência.
- `AppDbContext.SaveChanges`/`SaveChangesAsync` lançam `RegistroEmUsoException` (com a `DbUpdateException` como causa) em vez de deixar a `DbUpdateException` vazar.
- `MapaDeErro`: `RegistroEmUsoException` → 409 + `"Registro em uso por outro cadastro."`; **nenhuma** referência ao EF na Api.

- [ ] **Step 1: Escrever as regras da Infrastructure e da Api (vermelho)**

Em `ArquiteturaTests.cs`, acrescente `using ClyvoVet.Api.Errors;` e `using ClyvoVet.Infrastructure.Data;` no topo, os campos e os testes.
(As entidades moram no Domain, então não há ambiguidade de nome com `ClyvoVet.Api.Data`, que deixa de existir nesta tarefa.)

```csharp
    private static readonly Assembly AssemblyDaInfrastructure = typeof(AppDbContext).Assembly;
    private static readonly Assembly AssemblyDaApi = typeof(MapaDeErro).Assembly;
```

```csharp
    [Fact]
    public void Infrastructure_NaoReferenciaMvcNemApi()
    {
        NaoPodeReferenciar(AssemblyDaInfrastructure, "Microsoft.AspNetCore.Mvc", "ClyvoVet.Api");
    }

    [Fact]
    public void Infrastructure_UsaEntityFramework_ProvaQueAVerificacaoEnxergaReferencias()
    {
        Assert.Contains(
            Referencias(AssemblyDaInfrastructure),
            nome => nome.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));
    }

    [Fact]
    public void Api_NaoReferenciaEntityFrameworkNemMongo()
    {
        NaoPodeReferenciar(AssemblyDaApi, "Microsoft.EntityFrameworkCore", "MongoDB");
    }

    [Fact]
    public void Api_UsaApplication_ProvaQueAVerificacaoEnxergaReferencias()
    {
        Assert.Contains("ClyvoVet.Application", Referencias(AssemblyDaApi));
    }
```

- [ ] **Step 2: Escrever os testes do `AppDbContext` (vermelho)**

Um EF InMemory não aplica chave estrangeira, então o teste **provoca a falha de integridade por um interceptador**:
é determinístico e não depende de como o provedor reage. `tests/ClyvoVet.Api.Tests.Integration/AppDbContextTests.cs`:

```csharp
using ClyvoVet.Domain.Entities;
using ClyvoVet.Domain.Enums;
using ClyvoVet.Domain.Exceptions;
using ClyvoVet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ClyvoVet.Api.Tests.Integration;

/// <summary>
/// A fronteira com o EF: quem chama o repositório só conhece <c>RegistroEmUsoException</c>.
///
/// <para>
/// Os testes de integração rodam em InMemory, que não aplica chave estrangeira; por isso a
/// falha é provocada por um interceptador que lança a mesma <c>DbUpdateException</c> que o
/// provedor relacional lançaria.
/// </para>
/// </summary>
public class AppDbContextTests
{
    private sealed class FalhaDeIntegridadeInterceptor : SaveChangesInterceptor
    {
        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData, InterceptionResult<int> result)
            => throw new DbUpdateException("FK violation", (Exception?)null);

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
            => throw new DbUpdateException("FK violation", (Exception?)null);
    }

    private static AppDbContext CriarContextoQueFalhaAoSalvar()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"AppDbContextTestDb-{Guid.NewGuid()}")
            .AddInterceptors(new FalhaDeIntegridadeInterceptor())
            .Options;
        return new AppDbContext(options);
    }

    private static Produto NovoProduto() => new()
    {
        Id = Guid.NewGuid().ToString(),
        Nome = "Ração de Teste",
        Categoria = CategoriaEnum.Racao,
        EspecieIndicada = EspecieEnum.Cachorro,
        Preco = 50m,
        Ativo = true,
        CriadoEm = DateTime.UtcNow
    };

    [Fact]
    public async Task SaveChangesAsync_FalhaDeIntegridade_LancaRegistroEmUsoExceptionComACausaOriginal()
    {
        // Arrange
        using var contexto = CriarContextoQueFalhaAoSalvar();
        contexto.Produtos.Add(NovoProduto());

        // Act
        var excecao = await Assert.ThrowsAsync<RegistroEmUsoException>(
            () => contexto.SaveChangesAsync());

        // Assert
        Assert.IsType<DbUpdateException>(excecao.InnerException);
    }

    [Fact]
    public void SaveChanges_FalhaDeIntegridade_LancaRegistroEmUsoExceptionComACausaOriginal()
    {
        // Arrange
        using var contexto = CriarContextoQueFalhaAoSalvar();
        contexto.Produtos.Add(NovoProduto());

        // Act
        var excecao = Assert.Throws<RegistroEmUsoException>(() => contexto.SaveChanges());

        // Assert
        Assert.IsType<DbUpdateException>(excecao.InnerException);
    }
}
```

- [ ] **Step 3: Rodar e ver FALHAR**

```bash
~/.dotnet/dotnet test tests/ClyvoVet.Api.Tests.Integration/ClyvoVet.Api.Tests.Integration.csproj
```

Esperado: erro de compilação (`ClyvoVet.Infrastructure` ainda não existe).

- [ ] **Step 4: Criar o projeto Infrastructure**

`src/ClyvoVet.Infrastructure/ClyvoVet.Infrastructure.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <!-- O laco de notificacao de lembretes so e testavel de forma deterministica
         chamando-o direto; passar pelo BackgroundService exigiria esperar um
         Task.Delay e o teste ficaria dependente de tempo. -->
    <InternalsVisibleTo Include="ClyvoVet.Api.Tests.Unit" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.11" />
    <PackageReference Include="Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore" Version="8.0.11" />
    <PackageReference Include="Microsoft.Extensions.Http" Version="8.0.1" />
    <PackageReference Include="Pomelo.EntityFrameworkCore.MySql" Version="8.0.2" />
    <PackageReference Include="Telegram.Bot" Version="22.10.3" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\ClyvoVet.Application\ClyvoVet.Application.csproj" />
  </ItemGroup>

</Project>
```

Esse conjunto foi compilado em protótipo com os mesmos códigos (`AddDbContext`, `UseMySql`, `AddHttpClient<,>`, `BackgroundService`,
`AddDbContextCheck`, `GetValue<uint?>`, `MySqlConnectionStringBuilder`) sem aviso `NU1605`.

`.slnx`: `<Project Path="src/ClyvoVet.Infrastructure/ClyvoVet.Infrastructure.csproj" />`. Referências:

```bash
dotnet add src/ClyvoVet.Api/ClyvoVet.Api.csproj reference src/ClyvoVet.Infrastructure/ClyvoVet.Infrastructure.csproj
dotnet add tests/ClyvoVet.Api.Tests.Unit/ClyvoVet.Api.Tests.Unit.csproj reference src/ClyvoVet.Infrastructure/ClyvoVet.Infrastructure.csproj
dotnet add tests/ClyvoVet.Api.Tests.Integration/ClyvoVet.Api.Tests.Integration.csproj reference src/ClyvoVet.Infrastructure/ClyvoVet.Infrastructure.csproj
```

- [ ] **Step 5: Mover os arquivos**

```bash
I=src/ClyvoVet.Infrastructure
mkdir -p $I/{Data,Repositories,External,Background,HealthChecks}

git mv src/ClyvoVet.Api/Data/AppDbContext.cs   $I/Data/
git mv src/ClyvoVet.Api/Data/Configurations    $I/Data/Configurations
git mv src/ClyvoVet.Api/Repositories/*.cs      $I/Repositories/     # concretos + Paginacao (as interfaces saíram na T4)
git mv src/ClyvoVet.Api/Services/OciGenerativeAiClient.cs src/ClyvoVet.Api/Services/TelegramService.cs $I/External/
git mv src/ClyvoVet.Api/Services/TelegramLinkListenerService.cs src/ClyvoVet.Api/Services/LembreteNotificationService.cs $I/Background/
git mv src/ClyvoVet.Api/HealthChecks/TelegramHealthCheck.cs $I/HealthChecks/
```

- [ ] **Step 6: `namespace` e `using`**

```bash
I=src/ClyvoVet.Infrastructure
sed -i '' -E 's/^namespace ClyvoVet\.Api\.Data/namespace ClyvoVet.Infrastructure.Data/'             $I/Data/*.cs $I/Data/Configurations/*.cs
sed -i '' -E 's/^namespace ClyvoVet\.Api\.Repositories;/namespace ClyvoVet.Infrastructure.Repositories;/' $I/Repositories/*.cs
sed -i '' -E 's/^namespace ClyvoVet\.Api\.Services;/namespace ClyvoVet.Infrastructure.External;/'    $I/External/*.cs
sed -i '' -E 's/^namespace ClyvoVet\.Api\.Services;/namespace ClyvoVet.Infrastructure.Background;/'  $I/Background/*.cs
sed -i '' -E 's/^namespace ClyvoVet\.Api\.HealthChecks;/namespace ClyvoVet.Infrastructure.HealthChecks;/' $I/HealthChecks/*.cs

FILES=$(grep -rlE 'using ClyvoVet\.Api\.(Data|Repositories)' src tests --include='*.cs' --exclude-dir=bin --exclude-dir=obj)
sed -i '' -E \
  -e 's/^using ClyvoVet\.Api\.Data/using ClyvoVet.Infrastructure.Data/' \
  -e 's/^using ClyvoVet\.Api\.Repositories;/using ClyvoVet.Infrastructure.Repositories;/' \
  $FILES
sed -i '' 's/^using ClyvoVet\.Api\.Services;/using ClyvoVet.Infrastructure.Background;/' tests/ClyvoVet.Api.Tests.Unit/LembreteNotificationServiceTests.cs

# O SDK Web injetava estes; o classlib não (achado A3):
add_using Microsoft.Extensions.Configuration  $I/External/OciGenerativeAiClient.cs
add_using Microsoft.Extensions.Logging        $I/External/OciGenerativeAiClient.cs $I/Background/TelegramLinkListenerService.cs $I/Background/LembreteNotificationService.cs
add_using Microsoft.Extensions.Hosting        $I/Background/TelegramLinkListenerService.cs $I/Background/LembreteNotificationService.cs
add_using Microsoft.Extensions.DependencyInjection $I/Background/TelegramLinkListenerService.cs $I/Background/LembreteNotificationService.cs
```

- [ ] **Step 7: A fronteira com o EF no `AppDbContext`**

Em `src/ClyvoVet.Infrastructure/Data/AppDbContext.cs`, adicione `using ClyvoVet.Domain.Exceptions;` e, **logo antes** de `protected override void OnModelCreating`, insira:

```csharp
    // A Application e a Api não conhecem o EF: a falha de integridade (chave estrangeira
    // barrando um delete, por exemplo) sobe como RegistroEmUsoException, que o MapaDeErro
    // traduz para 409. Toda DbUpdateException — inclusive a de concorrência — era 409
    // antes desta mudança, e continua sendo.
    public override async Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
        catch (DbUpdateException excecao)
        {
            throw new RegistroEmUsoException(excecao);
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        try
        {
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }
        catch (DbUpdateException excecao)
        {
            throw new RegistroEmUsoException(excecao);
        }
    }
```

(As sobrecargas sem `bool` chamam estas, então nenhum caminho de `SaveChanges` escapa.)

- [ ] **Step 8: `MapaDeErro` e seu teste**

Em `src/ClyvoVet.Api/Errors/MapaDeErro.cs`:

- Apague `using Microsoft.EntityFrameworkCore;`.
- Em `Status`, troque `DbUpdateException => StatusCodes.Status409Conflict,` por `RegistroEmUsoException => StatusCodes.Status409Conflict,`
  e ajuste o comentário acima para dizer que a exceção vem do `AppDbContext` (o resto — "é regra de negócio funcionando, não falha; a API Java responde 409" — continua valendo).
- Em `Mensagem`, troque `DbUpdateException => "Registro em uso por outro cadastro.",` por `RegistroEmUsoException e => e.Message,`
  e reescreva o comentário: a mensagem é fixa na exceção; o texto do EF (tabela e constraint) fica só na exceção interna e no log.

Em `tests/ClyvoVet.Api.Tests.Unit/MapaDeErroTests.cs`: remova `using Microsoft.EntityFrameworkCore;`, e nos dois primeiros testes
troque a construção da exceção. O primeiro passa a usar `new RegistroEmUsoException()`. O segundo — que **prova que o schema não vaza** — usa uma causa que o contém:

```csharp
        var excecao = new RegistroEmUsoException(new InvalidOperationException(
            "The DELETE statement conflicted with the REFERENCE constraint "
                + "\"fk_sugestao_produto\" on table \"t_clyvo_sugestao_produto\"."));
```

Mantenha os nomes dos testes e as asserções (`Status409Conflict`, mensagem exata, `DoesNotContain("fk_sugestao_produto")` e `DoesNotContain("t_clyvo_")`).

- [ ] **Step 9: `AddInfrastructure`**

`src/ClyvoVet.Infrastructure/InfrastructureServiceExtensions.cs` — o esqueleto abaixo mostra **onde cada bloco de `Program.cs` cai**.
Cada `// MOVA` é um corte-e-cola **literal, com os comentários** (o design §5 manda que os comentários de "porquê" acompanhem o código);
só mudam os nomes: `builder.Configuration` → `configuration`, `builder.Services` → `services`, `builder.Environment` → `environment`.
As linhas citadas são as do commit `6302e7e`.

```csharp
using ClyvoVet.Application.Abstractions.External;
using ClyvoVet.Application.Abstractions.Repositories;
using ClyvoVet.Infrastructure.Background;
using ClyvoVet.Infrastructure.Data;
using ClyvoVet.Infrastructure.External;
using ClyvoVet.Infrastructure.HealthChecks;
using ClyvoVet.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using MySqlConnector;                        // MySqlConnectionStringBuilder (transitivo via Pomelo)
using Telegram.Bot;

namespace ClyvoVet.Infrastructure;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        // MOVA: "var mysqlConnectionString = …" + o bloco "TETO DE CONEXOES EXPLICITO" até o
        // fechamento do `if (!string.IsNullOrWhiteSpace(mysqlConnectionString)) { … }`   (linhas 178–200)
        // MOVA: o bloco "VERSAO FIXA, E NAO AutoDetect." até o `AddDbContext<AppDbContext>(…)`  (linhas 202–220)

        // MOVA: os 9 `AddScoped<I…Repository, …Repository>()`                            (linhas 222–230)

        // MOVA: o comentário "OCI Generative AI via HttpClient tipado…" + `AddHttpClient<IOciGenerativeAiClient, …>` (239–244)

        // MOVA: `AddSingleton<ITelegramBotClient>(…)` e `AddSingleton<ITelegramService, TelegramService>()`      (256–258)

        // MOVA: o `if (!builder.Environment.IsEnvironment("Testing")) { AddHostedService×2 }`, sem o comentário
        //       do Singleton (esse foi para a Application na T4)                                     (264–268)
        //       → `if (!environment.IsEnvironment("Testing"))`

        // MOVA: o comentário "Health Checks — 'self' cobre liveness…" (linhas 270–284, inclusive as notas sobre
        //       o WhatsApp e a OCI) e as duas últimas chamadas da cadeia:
        //
        //   services.AddHealthChecks()
        //       .AddDbContextCheck<AppDbContext>(
        //           name: "mysql-database",
        //           failureStatus: HealthStatus.Unhealthy,
        //           tags: ["ready", "database", "external"])
        //       .AddCheck<TelegramHealthCheck>("telegram-bot", tags: ["external"]);
        //
        // O `.AddCheck("self", …)` NÃO vem para cá: fica no Program.cs (é da Api).

        return services;
    }
}
```

Em `Program.cs`, apague os mesmos blocos (agora movidos) e, **depois** do `AddHealthChecks().AddCheck("self", …)` que fica
(reduza a cadeia atual para só o `self`), acrescente:

```csharp
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);
```

**A ordem importa:** `self` é registrado *antes* de `AddInfrastructure`, para o JSON de `/health` continuar listando
`self`, `mysql-database`, `telegram-bot` na mesma ordem de hoje. Adicione `using ClyvoVet.Infrastructure;` e apague os `using` que
ficaram sem uso (`Microsoft.EntityFrameworkCore`, `MySqlConnector`, `Telegram.Bot`, `ClyvoVet.Api.Data`, `…Repositories`, `…Services`, `…HealthChecks` se só serviam ao que saiu).

- [ ] **Step 10: `csproj` da Api**

Em `src/ClyvoVet.Api/ClyvoVet.Api.csproj`:

- **Apague** o `ItemGroup` do `InternalsVisibleTo` (com o comentário do laço de notificação): ele mora na Infrastructure e na Application agora (A2).
- **Apague** as `PackageReference` que passaram para a Infrastructure: `Microsoft.EntityFrameworkCore`,
  `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore`, `Pomelo.EntityFrameworkCore.MySql`, `Telegram.Bot`.
- **Mantenha** `Microsoft.EntityFrameworkCore.Design` (com `PrivateAssets=all`): é ferramenta de `dotnet ef`, a Api é o projeto de inicialização, e a F7 depende dele.
  Mantenha também OpenTelemetry (inclui `…Instrumentation.EntityFrameworkCore`), Serilog, Swashbuckle, `Microsoft.OpenApi` e `System.IdentityModel.Tokens.Jwt`, **com seus comentários**.
  Verificado em protótipo: chamar `AddEntityFrameworkCoreInstrumentation()` e manter o `EF.Design` (com `PrivateAssets=all`) **não** faz a Api listar `Microsoft.EntityFrameworkCore` nas referências — a regra `Api_NaoReferenciaEntityFrameworkNemMongo` não vai falhar por causa deles.

- [ ] **Step 11: Build e testes**

```bash
dotnet build ClyvoVet-api.slnx
~/.dotnet/dotnet test tests/ClyvoVet.Api.Tests.Unit/ClyvoVet.Api.Tests.Unit.csproj
~/.dotnet/dotnet test tests/ClyvoVet.Api.Tests.Integration/ClyvoVet.Api.Tests.Integration.csproj
```

Esperado: **272** aprovados, incluindo as 4 regras novas, as 2 do `AppDbContext` e os 2 do `MapaDeErro` reescritos.
Confira à parte: `HealthCheckEndpointsTests` (ordem/nomes dos checks preservados) e `LembreteNotificationServiceTests` (`InternalsVisibleTo` na Infrastructure).
`Api_NaoReferenciaEntityFrameworkNemMongo` só fica verde quando **nem** `MapaDeErro` **nem** `Program.cs` citam mais o EF — é o teste que confirma que a fronteira ficou limpa.

- [ ] **Step 12: Commit** — peça o "sim".

```bash
git add src tests ClyvoVet-api.slnx
git commit -m "refactor: extrai o projeto Infrastructure" \
  -m "EF Core, repositórios, clientes OCI e Telegram, background services e health checks de dependência vão para o ClyvoVet.Infrastructure, compostos por AddInfrastructure. O AppDbContext passa a traduzir DbUpdateException em RegistroEmUsoException, então a Api não conhece mais o EF; o MapaDeErro responde o mesmo 409 e a mesma mensagem de antes."
```

---

## Task 6 — Api enxuta

**Files:**
- Create: `src/ClyvoVet.Api/Extensions/ObservabilidadeExtensions.cs`, `src/ClyvoVet.Api/Extensions/DocumentacaoApiExtensions.cs`
- Modify: `src/ClyvoVet.Api/Program.cs` (de ~410 linhas para ~150)

**Interfaces — Produz:** `builder.AddObservabilidade()` (Serilog + OpenTelemetry), `services.AddDocumentacaoApi()` e `app.UseDocumentacaoApi()` (Swagger + Swagger UI).
**Nenhum comportamento muda**: é só mover o que resta do `Program.cs` para o lugar certo. A regra "Api sem EF" já ficou verde na T5 (achado A6).

- [ ] **Step 1: `ObservabilidadeExtensions`**

`src/ClyvoVet.Api/Extensions/ObservabilidadeExtensions.cs` (mesma convenção da T5: `// MOVA` = corte-e-cola literal, com os comentários):

```csharp
using System.Reflection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

namespace ClyvoVet.Api.Extensions;

public static class ObservabilidadeExtensions
{
    private const string NomeDoServico = "ClyvoVet.Api";

    public static WebApplicationBuilder AddObservabilidade(this WebApplicationBuilder builder)
    {
        // MOVA (de Program.cs, commit 6302e7e): o bloco do Serilog — do comentário "Configuração estática
        // (em vez do padrão bootstrap-logger…" até `builder.Host.UseSerilog();`         (linhas 31–68)
        //   • `TemplateLog` vira `const` local;  • `ServiceName` → `NomeDoServico`.

        // MOVA: `builder.Services.AddOpenTelemetry()…AddPrometheusExporter());`              (linhas 293–305)
        //   • `ServiceName` → `NomeDoServico`;  • `Assembly.GetExecutingAssembly()` continua valendo
        //     (esta classe está no mesmo assembly da Api).

        return builder;
    }
}
```

- [ ] **Step 2: `DocumentacaoApiExtensions`**

`src/ClyvoVet.Api/Extensions/DocumentacaoApiExtensions.cs`:

```csharp
using System.Reflection;
using ClyvoVet.Api.Swagger;
using ClyvoVet.Application.DTOs.Request;
using Microsoft.OpenApi;                      // OpenApiInfo, OpenApiContact (Microsoft.OpenApi 2.x)
using Swashbuckle.AspNetCore.SwaggerUI;       // DocExpansion

namespace ClyvoVet.Api.Extensions;

public static class DocumentacaoApiExtensions
{
    public static IServiceCollection AddDocumentacaoApi(this IServiceCollection services)
    {
        // MOVA: `builder.Services.AddEndpointsApiExplorer();` e todo o `AddSwaggerGen(options => { … });`
        // (linhas 94–176), inclusive o laço do XML que a T4 escreveu. `builder.Services` → `services`.
        return services;
    }

    public static WebApplication UseDocumentacaoApi(this WebApplication app)
    {
        // MOVA: o comentário "Swagger sempre ativo…" + `UseSwagger();` + `UseSwaggerUI(…)`   (linhas 314–327)
        return app;
    }
}
```

- [ ] **Step 3: Reescrever o `Program.cs`**

Substitua `src/ClyvoVet.Api/Program.cs` por esta estrutura. Os blocos marcados `MANTENHA` **ficam no arquivo literalmente**, com os comentários;
o resto é o que se escreve de novo.

```csharp
using ClyvoVet.Api.Errors;
using ClyvoVet.Api.Extensions;
using ClyvoVet.Api.HealthChecks;
using ClyvoVet.Api.Middleware;
using ClyvoVet.Api.Security;
using ClyvoVet.Application;
using ClyvoVet.Application.Security;
using ClyvoVet.Domain.Exceptions;
using ClyvoVet.Infrastructure;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog e OpenTelemetry — os comentários do porquê estão em Extensions/ObservabilidadeExtensions.cs.
builder.AddObservabilidade();

// MANTENHA: o bloco "CORS ESPELHANDO O DESENHO DA API JAVA" inteiro         (linhas 70–91)

builder.Services.AddControllers();
builder.Services.AddDocumentacaoApi();

builder.Services.AddApplication();

// "self" cobre liveness. Os checks de dependência (banco e Telegram) vêm da Infrastructure, junto
// de quem os implementa, e são registrados DEPOIS deste: a ordem do JSON de /health continua
// sendo a de sempre — self, mysql-database, telegram-bot.
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("API em execução."), tags: ["live"]);

builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

// MANTENHA: o comentário "Validacao do token emitido pela API Java…" + `AddSingleton<ValidadorDeTokenJwt>();`  (245–248)
// MANTENHA: o comentário do acessor (já reescrito na T4) + `AddHttpContextAccessor();` + `AddScoped<IUsuarioAtual, UsuarioAtualHttp>();`

var app = builder.Build();

// MANTENHA: `UseMiddleware<CorrelationIdMiddleware>()` + `UseSerilogRequestLogging()`, com o comentário (309–312)

app.UseDocumentacaoApi();

// MANTENHA: `app.UseExceptionHandler(errorApp => { … });` inteiro                       (329–372)
// MANTENHA: de `app.UseHttpsRedirection();` até `app.MapControllers();`, com os comentários (374–385)
// MANTENHA: os três `MapHealthChecks` (com o comentário) e o `MapPrometheusScrapingEndpoint("/metrics")` (387–406)

app.Run();

// Necessário para o WebApplicationFactory<Program> localizar o entry point nos testes de integração.
public partial class Program { }
```

**Ordem preservada:** o pipeline continua `CorrelationId → Serilog → Swagger → ExceptionHandler → HttpsRedirection → Identidade → CORS → Authorization → Controllers → Health → Prometheus`.
Trocar a ordem de qualquer um desses muda comportamento (ex.: o `CorrelationIdMiddleware` precisa envolver o `UseSerilogRequestLogging`).

- [ ] **Step 4: Build e testes**

```bash
dotnet build ClyvoVet-api.slnx
~/.dotnet/dotnet test tests/ClyvoVet.Api.Tests.Unit/ClyvoVet.Api.Tests.Unit.csproj
~/.dotnet/dotnet test tests/ClyvoVet.Api.Tests.Integration/ClyvoVet.Api.Tests.Integration.csproj
wc -l src/ClyvoVet.Api/Program.cs      # esperado: ~150 (era 411)
```

Esperado: **272** aprovados, nenhum a menos. Se `Program.cs` acusar `CS0104` (tipo ambíguo), é um `using` duplicado de namespace — apague o desnecessário.

- [ ] **Step 5: Commit** — peça o "sim".

```bash
git add src
git commit -m "refactor: enxuga o Program.cs e a Api" \
  -m "O Program.cs passa a só orquestrar: Serilog e OpenTelemetry vão para AddObservabilidade e o Swagger para AddDocumentacaoApi/UseDocumentacaoApi. Os comentários que explicam o porquê acompanham o código."
```

---

## Task 7 — Reapontar, revalidar e atualizar a documentação

**Files:**
- Modify: `CLAUDE.md`, `README.md`, `docs/sprint4/README.md`, `docs/sprint4/03-plano.md`, `docs/sprint4/02-design.md`, `docs/sprint4/01-gap-analysis.md`
- Modify (**fora do Git**): `CLAUDE.local.md`

- [ ] **Step 1: Conferir as referências dos projetos de teste**

```bash
dotnet list tests/ClyvoVet.Api.Tests.Unit/ClyvoVet.Api.Tests.Unit.csproj reference
dotnet list tests/ClyvoVet.Api.Tests.Integration/ClyvoVet.Api.Tests.Integration.csproj reference
```

Esperado: cada um referencia `ClyvoVet.Api`, `ClyvoVet.Domain`, `ClyvoVet.Application` e `ClyvoVet.Infrastructure` (as tarefas T3–T5 já as adicionaram).

- [ ] **Step 2: Validação final ponta a ponta**

```bash
dotnet build ClyvoVet-api.slnx
export DOTNET_ROOT="$HOME/.dotnet"
~/.dotnet/dotnet test tests/ClyvoVet.Api.Tests.Unit/ClyvoVet.Api.Tests.Unit.csproj
~/.dotnet/dotnet test tests/ClyvoVet.Api.Tests.Integration/ClyvoVet.Api.Tests.Integration.csproj
docker build -t clyvovet-api .                     # Docker Desktop ligado
```

Esperado: **272** aprovados e `docker build` verde.

- [ ] **Step 3: A API sobe e responde**

Sem MySQL, `/health/live` (só o check `self`) tem que responder 200; `/health/ready` responde 503 (o banco não existe) — é o comportamento de sempre.

```bash
export DOTNET_ROOT="$HOME/.dotnet"
~/.dotnet/dotnet run --project src/ClyvoVet.Api/ClyvoVet.Api.csproj --urls http://localhost:5099 > "$TMPDIR/api-f1.log" 2>&1 &
API_PID=$!
sleep 8
curl -s -o /dev/null -w "live: %{http_code}\n"  http://localhost:5099/health/live
curl -s -o /dev/null -w "swagger: %{http_code}\n" http://localhost:5099/swagger/v1/swagger.json
kill $API_PID
git status --short          # esperado: vazio — o Logs/ gerado no run está ignorado (Step 8 da T2)
```

Esperado: `live: 200`, `swagger: 200`.

- [ ] **Step 4: Conferir que o Git vê renomeações, não apagar/criar**

```bash
git diff -M --summary 6302e7e..HEAD | grep -E '^ (create|delete) mode'
```

Esperado: só os arquivos **novos** de propósito (3 `csproj`, `RegistroEmUsoException`, `IUsuarioAtual`, `UsuarioAtualHttp`,
`ApplicationServiceExtensions`, `InfrastructureServiceExtensions`, 2 `Extensions/*`, 4 arquivos de teste novos e este plano) e o `ClyvoVet.slnx` removido.
Um arquivo pequeno que apareça como `delete` + `create` (a similaridade caiu abaixo de 50% por causa do `namespace`) é aceitável se `git diff -M30% --summary` o reconhecer.

- [ ] **Step 5: `CLAUDE.md`**

- Seção **Comandos**: apague a frase "Caminhos abaixo valem **até a Fase 1**…" e troque a última linha do bloco por
  `dotnet run --project src/ClyvoVet.Api/ClyvoVet.Api.csproj      # Swagger em /swagger`.
- "Linha de base": troque por `Linha de base (F1 fechada, <data de hoje, dd/mm/aaaa>): <unidade> unidade + <integração> integração = 272, todos verdes.` com os números do Step 2.
- Cabeçalho **"Arquitetura alvo (Clean Architecture, 4 projetos)"** → **"Arquitetura (Clean Architecture, 4 projetos)"**: agora é real.

- [ ] **Step 6: `README.md` da raiz (só caminhos; o README final é da F6)**

- Bloco "Estrutura de Pastas": troque a árvore por `src/{ClyvoVet.Domain, ClyvoVet.Application, ClyvoVet.Infrastructure, ClyvoVet.Api}` e `tests/{ClyvoVet.Api.Tests.Unit, ClyvoVet.Api.Tests.Integration}`, mantendo a descrição de cada pasta; `schema/` continua na raiz.
- Links: `ClyvoVet.Api/Program.cs`, `…/Middleware/CorrelationIdMiddleware.cs`, `…/appsettings.json` → `src/ClyvoVet.Api/…`.
- Seção de testes: "Dentro de `ClyvoVet.Api/`, os testes se dividem…" → "Em `tests/`…", e `dotnet test ClyvoVet.Api.Tests.Unit` → `dotnet test tests/ClyvoVet.Api.Tests.Unit`; idem `Integration`.

```bash
git grep -nE 'ClyvoVet\.Api/(Program|Middleware|appsettings|Tests)' -- README.md    # esperado: nada
```

- [ ] **Step 7: Estado das fases e correção dos docs (regra do `CLAUDE.md`: no mesmo commit)**

- `docs/sprint4/README.md`, tabela **Estado**: **F0 → ✅** (o `.gitignore` e os commits foram feitos; ver `6302e7e`) e **F1 → ✅**.
- `docs/sprint4/03-plano.md`: marque `[x]` nas tarefas T1–T7 da F1 e nos itens que sobraram na F0. Na **T6 da F1**, troque a linha da regra da Api ("vermelho enquanto…") por uma nota: a regra da Api foi para a T5 (`fases/F1-clean-architecture.md`, achado A6).
- `docs/sprint4/02-design.md` §3.3: onde diz que `InternalsVisibleTo` "passa para a Infrastructure", complete: "**e para a Application** (o `SaudePreditivaService` tem membros `internal` que os testes chamam)". §5 (`AddDocumentacaoApi`): acrescente que ele inclui o **XML da Application**, porque os DTOs moram lá.
- `docs/sprint4/01-gap-analysis.md`: linha **"Clean Architecture com camadas"** (🔴) → ✅ com a evidência (4 projetos + `ArquiteturaTests`); a linha "SOLID / Clean Code" fica 🟡 e passa a citar que o `Program.cs` foi de 411 para ~150 linhas (o resto é da F5).

- [ ] **Step 8: `CLAUDE.local.md` (fora do Git — não entra no commit)**

Atualize os dois caminhos de teste do bloco `bash` para `tests/ClyvoVet.Api.Tests.Unit/…` e `tests/ClyvoVet.Api.Tests.Integration/…`
e apague o parágrafo "Depois da Fase 1 os testes passam a morar em `tests/`…", que já foi cumprido.

- [ ] **Step 9: Commit** — peça o "sim".

```bash
git add CLAUDE.md README.md docs
git commit -m "docs: atualiza caminhos, comandos e estado para a nova estrutura" \
  -m "Fecha a F1: CLAUDE.md e README com os caminhos de src/ e tests/, tabela de estado da Sprint 4, checklist do plano e as duas correções que o design pedia (InternalsVisibleTo também na Application, XML da Application no Swagger)."
git status --short      # esperado: vazio (o CLAUDE.local.md está ignorado)
```

---

## Pronto quando (F1)

- [ ] DoD do `03-plano.md`: build sem erro; `dotnet test` no runtime 8 **272/272**, nunca menos que a fase anterior.
- [ ] `ArquiteturaTests` verdes (Domain, Application, Infrastructure, Api + 2 de sanidade).
- [ ] `docker build` verde e `GET /health/live` = 200 com a API rodando pelo novo caminho.
- [ ] `git diff -M --summary 6302e7e..HEAD` mostra os arquivos movidos como **renomeados**.
- [ ] Tabela de estado e `CLAUDE.md` atualizados no commit que fecha a fase.
- [ ] 7 commits, nenhum com linha de atribuição, nenhum `--amend`/`rebase`/`reset`, nenhum `push`.

## Auto-revisão

**Cobertura do design/plano-mestre → tarefa**

| Exigência | Onde |
|---|---|
| §3.1 layout `src/` + `tests/`, `git mv` | T2 |
| §3.3 mapa de arquivos, namespaces | "Mapa de destino" + T3/T4/T5 |
| §3.4 pacotes por projeto | csproj de T3/T4/T5 (Api: T5 Step 10) |
| §4 `ArquiteturaTests` (4 assemblies) | T3 (Domain), T4 (Application), T5 (Infrastructure e Api) |
| §5 `AddApplication` / `AddInfrastructure` / extensões da Api | T4 Step 9, T5 Step 9, T6 |
| §5 comentários de "porquê" acompanham o código | convenção `MOVA` em T5/T6 |
| §7.1 `DbUpdateException` → `RegistroEmUsoException` | T3 (tipo), T5 Steps 7–8 |
| `IUsuarioAtual` + `UsuarioAtualHttp` | T4 |
| `Dockerfile`/`.dockerignore` no mesmo commit do movimento; `docker build` | T2 Steps 7, 10 |
| `RegistroEmUsoException` com teste primeiro | T3 Steps 2–3, 7 |
| Atualizar `CLAUDE.md` e tabela de estado | T7 |
| Achados A1–A8 | T1 (A1), T4 (A1, A2, A3, A7), T5 (A2, A5, A6), T2 (A4, A8) |

**Tipos e assinaturas consistentes:** `RegistroEmUsoException(Exception? causa = null)` (T3) é a que o `AppDbContext` (T5) e os testes usam;
`IUsuarioAtual.Identidade` (T4) é o que `EscopoDoTutor` e `EscopoDoTutorTests` usam; `AddInfrastructure(IConfiguration, IHostEnvironment)` (T5) é o que o `Program.cs` (T6) chama.

**Limites honestos deste plano:** os blocos `MOVA`/`MANTENHA` dependem de números de linha do commit `6302e7e` — confira pelo texto do comentário-âncora se o arquivo tiver mudado.
As listas de `using` a acrescentar são previsões feitas lendo o código; o `dotnet build` é quem decide, e a instrução em cada passo diz o que fazer quando ele discordar.
Não foi rodado nada disto contra o repositório. O que foi **verificado por protótipo** (projetos descartáveis, fora do repo):
o `.xml` de um projeto referenciado chega a `bin`, ao `bin` dos testes e ao `publish` (A1); o conjunto de pacotes da Application e da Infrastructure compila sem `NU1605` (A3);
o compilador só grava referências realmente usadas — a "Api" de teste não lista o EF, a Infrastructure lista (base dos `ArquiteturaTests`); e o OpenTelemetry EF não conta como referência ao EF.
Também foi conferido por leitura que nenhum serviço de aplicação cita classe concreta da Infrastructure (só `IOciGenerativeAiClient` e `ITelegramService`). O resto é leitura do código.
