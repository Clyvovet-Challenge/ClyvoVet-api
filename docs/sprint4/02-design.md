# Design — Sprint 4

> **Status:** aprovado em conversa, em 19/09/2026. **Sem decisão pendente:** o professor
> liberou o MySQL com migrations em 20/09/2026 ([ADR-005](decisoes/ADR-005-banco-e-migrations.md)).
> As evidências do estado atual estão em [`01-gap-analysis.md`](01-gap-analysis.md).

## 1. Objetivo e escopo

Fechar as lacunas do rubric **sem derrubar a produção (Render) e sem quebrar o contrato com
o app móvel**. Toda mudança precisa ser verificável por build, teste ou requisição.

**Dentro do escopo:** o rubric de [`00-requisitos.md`](00-requisitos.md).
**Fora do escopo** (ver §11): CI/CD, features novas de domínio, mudanças na API Java, deploy
de um MongoDB hospedado.

## 2. Decisões

| # | Decisão | Registro |
|---|---|---|
| 1 | Clean Architecture em **4 projetos reais** | [ADR-001](decisoes/ADR-001-clean-architecture-4-projetos.md) |
| 2 | MongoDB guarda o **cache do parecer de IA** | [ADR-002](decisoes/ADR-002-mongodb-cache-parecer-ia.md) |
| 3 | **`AddJwtBearer` + `[Authorize]`**, sem `FallbackPolicy` | [ADR-003](decisoes/ADR-003-jwt-bearer-e-authorize.md) |
| 4 | HATEOAS **sem quebrar** o array das listagens | [ADR-004](decisoes/ADR-004-hateoas-sem-quebrar-o-app.md) |
| 5 | Banco e migrations | [ADR-005](decisoes/ADR-005-banco-e-migrations.md) — **MySQL liberado pelo professor (20/09/2026)** |

## 3. Estrutura da solução

### 3.1 Layout de pastas

```
ClyvoVet-api/
├── ClyvoVet-api.slnx
├── src/
│   ├── ClyvoVet.Domain/
│   ├── ClyvoVet.Application/
│   ├── ClyvoVet.Infrastructure/
│   └── ClyvoVet.Api/
├── tests/
│   ├── ClyvoVet.Api.Tests.Unit/
│   └── ClyvoVet.Api.Tests.Integration/
├── docs/   schema/   azure/   scripts/
```

Atualmente os dois projetos de teste ficam **dentro** de `ClyvoVet.Api/`, e o `csproj` da Api
precisa de oito linhas `Remove` só para não compilá-los. A estrutura `src/`+`tests/` elimina
essa necessidade. A migração usa `git mv`, preservando o histórico dos arquivos.

### 3.2 Dependências (só para dentro)

```
Api ──► Application ──► Domain
 └────► Infrastructure ─► Application
```

`Infrastructure` referencia `Application` (para implementar as interfaces) e, por
transitividade, `Domain`. `Api` referencia `Application` e `Infrastructure` (esta só para
compor o DI em `Program.cs`).

### 3.3 O que vai para cada projeto

| Hoje (`ClyvoVet.Api/`) | Destino | Namespace |
|---|---|---|
| `Models/*` (inclui `ParecerConteudo`) | Domain/Entities | `ClyvoVet.Domain.Entities` |
| `Enums/*` | Domain/Enums | `ClyvoVet.Domain.Enums` |
| `Exceptions/*` (+ nova `RegistroEmUsoException`) | Domain/Exceptions | `ClyvoVet.Domain.Exceptions` |
| `DTOs/Request`, `DTOs/Response` | Application/DTOs | `ClyvoVet.Application.DTOs.*` |
| `Services/Interfaces/I{Evento,Lembrete,Produto,SugestaoProduto,SaudePreditiva,WidgetSaudePreditiva}Service` | Application/Services/Interfaces | `ClyvoVet.Application.Services.Interfaces` |
| `Services/{Evento,Lembrete,Produto,SugestaoProduto,SaudePreditiva,WidgetSaudePreditiva}Service` | Application/Services | `ClyvoVet.Application.Services` |
| `Services/DataValidationHelper` | Application/Common | `ClyvoVet.Application.Common` |
| `Repositories/Interfaces/*` | Application/Abstractions/Repositories | `ClyvoVet.Application.Abstractions.Repositories` |
| `IOciGenerativeAiClient`, `ITelegramService` | Application/Abstractions/External | `ClyvoVet.Application.Abstractions.External` |
| `Security/{EscopoDoTutor, IdentidadeDoChamador, VinculosPendentesDeTelegram}` (+ `SemTutorNoTokenException`, que está dentro do `EscopoDoTutor.cs`) | Application/Security | `ClyvoVet.Application.Security` |
| *(novo)* `IUsuarioAtual` | Application/Security | idem |
| `Data/AppDbContext`, `Data/Configurations/*` | Infrastructure/Data | `ClyvoVet.Infrastructure.Data` |
| `Repositories/*Repository.cs`, `Paginacao` | Infrastructure/Repositories | `ClyvoVet.Infrastructure.Repositories` |
| `Services/{OciGenerativeAiClient, TelegramService}` | Infrastructure/External | `ClyvoVet.Infrastructure.External` |
| `Services/{TelegramLinkListenerService, LembreteNotificationService}` | Infrastructure/Background | `ClyvoVet.Infrastructure.Background` |
| `HealthChecks/TelegramHealthCheck` | Infrastructure/HealthChecks | `ClyvoVet.Infrastructure.HealthChecks` |
| `Controllers`, `Filters`, `Middleware/CorrelationIdMiddleware`, `Swagger`, `Errors`, `HealthChecks/HealthCheckJsonWriter`, `Program.cs`, `appsettings*`, `Properties` | Api | `ClyvoVet.Api.*` |
| `Security/ValidadorDeTokenJwt`, `Middleware/IdentidadeMiddleware` | Api na F1; **removidos na F2** | — |

`ClyvoVet.Api.Tests.Unit` e `ClyvoVet.Api.Tests.Integration` mantêm os nomes; só passam a
referenciar os projetos certos. `InternalsVisibleTo("ClyvoVet.Api.Tests.Unit")`, hoje na Api
por causa do laço de notificação, passa para a Infrastructure junto com o serviço.

### 3.4 Pacotes por projeto

| Projeto | Pacotes |
|---|---|
| Domain | nenhum |
| Application | `Microsoft.Extensions.{Configuration,Logging,DependencyInjection}.Abstractions` |
| Infrastructure | EF Core 8.0.11, `Pomelo…MySql` 8.0.2, `HealthChecks.EntityFrameworkCore`, `Telegram.Bot` 22.10.3, `Microsoft.Extensions.Http`, hosting/health abstractions; **F4:** `MongoDB.Driver` |
| Api | Swashbuckle 10.1.7, `Microsoft.OpenApi` 2.12.2, Serilog, OpenTelemetry; **F2:** `Microsoft.AspNetCore.Authentication.JwtBearer` 8.0.11; **F5:** `Serilog.Formatting.Compact` |

## 4. Como as regras de camada são verificadas

Referência de projeto **sozinha não é suficiente**: pacotes fluem de `Infrastructure` para
`Api` por transitividade, então a `Api` compilaria usando tipos do EF sem qualquer aviso. Daí
a existência do `ArquiteturaTests` (projeto de unidade, reflexão pura, sem biblioteca extra),
que falha quando:

| Assembly | Não pode referenciar |
|---|---|
| `ClyvoVet.Domain` | qualquer assembly `ClyvoVet.*` ou `Microsoft.*` |
| `ClyvoVet.Application` | `Microsoft.EntityFrameworkCore*`, `MongoDB*`, `Telegram*`, `Microsoft.AspNetCore*`, `ClyvoVet.Infrastructure`, `ClyvoVet.Api` |
| `ClyvoVet.Infrastructure` | `Microsoft.AspNetCore.Mvc*`, `ClyvoVet.Api` |
| `ClyvoVet.Api` | `Microsoft.EntityFrameworkCore*`, `MongoDB*` |

O compilador só registra no assembly as referências efetivamente usadas pelo código, então o
teste enxerga o uso real, não a intenção. Consequência direta: `MapaDeErro` deixa de poder
citar `DbUpdateException` (§7.1).

## 5. Composição e injeção de dependência

- `Application`: `services.AddApplication()` — registra serviços e `EscopoDoTutor`.
- `Infrastructure`: `services.AddInfrastructure(configuration, environment)` — `DbContext`
  (com o teto de conexões e a versão fixa do MySQL que hoje estão no `Program.cs`),
  repositórios, cliente OCI, Telegram, *background services* (**não** registrados no ambiente
  `Testing`, como hoje) e health checks de dependência.
- `Api`: `Program.cs` só orquestra e delega a extensões em `Api/Extensions/`:
  `AddDocumentacaoApi`, `AddAutenticacaoJwt` (F2) e `AddObservabilidade`.
- **Mantido igual:** todos os comentários de "por que" do `Program.cs` atual (versão fixa do
  MySQL, teto de pool, CORS, sink de arquivo só em Development). Eles acompanham o código
  para o novo lugar; não são reescritos.

## 6. API

### 6.1 Tratamento global de exceções (F2)

- O lambda de `UseExceptionHandler` vira `TratadorGlobalDeExcecoes : IExceptionHandler`
  (Api), registrado com `AddExceptionHandler` + `AddProblemDetails`.
- A **decisão de status e mensagem continua no `MapaDeErro`** (já testado). O tratador só
  escreve a resposta.
- **Contrato preservado:** o corpo mantém `error` e, em falha de servidor, `referencia` — o
  app lê esses campos. Somam-se os campos padrão de `ProblemDetails` (`status`, `title`,
  `traceId`).
- Log inalterado: `Warning` para 404/400/403 de negócio, `Error` para o resto.

### 6.2 Autenticação e autorização (F2) — [ADR-003](decisoes/ADR-003-jwt-bearer-e-authorize.md)

**Token (emitido pela API Java, validado aqui):**

| Claim | Uso |
|---|---|
| `sub` | id do usuário |
| `tipo` | só `access` é aceito; `refresh` é recusado |
| `tutorId` | dono dos dados; nulo para ADMIN/VETERINARIO |
| `perfil` | `ADMIN`, `VETERINARIO` ou `TUTOR` → vira o *role* |

**Validação** (mesmos parâmetros de hoje, agora no pipeline padrão): HS256, chave =
`Convert.FromBase64String(Jwt:Secret)`, emissor e público validados, `MapInboundClaims = false`,
`RoleClaimType = "perfil"`, `NameClaimType = "sub"`. O emissor e o público têm os valores
que hoje estão fixos no `ValidadorDeTokenJwt` e passam a ser chaves `Jwt:Emissor` e
`Jwt:Publico` (mesmos valores como padrão).

**Políticas:**

| Política | Regra |
|---|---|
| `Autenticado` | usuário autenticado **e** `tipo = access` |
| `Equipe` | `Autenticado` + perfil `ADMIN` ou `VETERINARIO` |

**Aplicação:**

| Endpoints | Política |
|---|---|
| Lembretes, Eventos Pet, Sugestões de Produto, Saúde Preditiva, Widget | `Autenticado` |
| Produtos — `GET` | `Autenticado` |
| Produtos — `POST`/`PUT`/`DELETE` | `Equipe` — **confirmar na F2 se o app não escreve produto**; se escrever, cai para `Autenticado` e isso é registrado na ADR-003 |
| Telegram | inalterado (chaves próprias) |
| `/health*`, `/metrics`, `/swagger` | anônimos — **sem `FallbackPolicy`** |

A `X-Api-Key` permanece como segunda camada (defesa em profundidade). `EscopoDoTutor`
continua recortando por dono, agora lendo a identidade por `IUsuarioAtual` (Application),
implementado na Api sobre `HttpContext.User`. `IdentidadeMiddleware` e `ValidadorDeTokenJwt`
são removidos.

**Alavanca de reversão:** `Auth:ExigirToken` (padrão `true`). Em `false`, as políticas
passam a autorizar sem token. Existe porque a produção está no ar e o segredo
`Jwt__Secret` **precisa estar configurado no Render antes do deploy**; sem ele, todo
endpoint protegido responde 401. Sem segredo a aplicação **sobe** (não morre no boot), loga
um `Warning` e responde 401 — falha visível, nunca queda de processo.

**Swagger:** ganha o esquema `Bearer` (http/bearer/JWT) ao lado do `ApiKey`.

### 6.3 Paginação com total e ordenação (F3)

- `ConsultaPaginada(int Page, int PageSize, string? OrdenarPor, DirecaoOrdenacao Direcao)` e
  `PaginaDeResultados<T>(IReadOnlyList<T> Itens, int Total, int Page, int PageSize)` na
  Application. Os serviços passam a devolver `PaginaDeResultados<T>`; o controller decide a
  representação (§6.4). Isso também elimina a assinatura de 6–7 parâmetros de
  `GetAllAsync`.
- Query string: `ordenarPor` (nome camelCase de uma propriedade do recurso) e `direcao`
  (`asc` | `desc`, padrão `asc`). Sem `ordenarPor`, **vale a ordem de hoje** de cada
  repositório. `direcao` sem `ordenarPor` é ignorada.
- **Lista branca por recurso**, declarada no repositório (`Dictionary<string,
  Expression<Func<TEntidade, object>>>`). Campo fora dela → `BadRequestException` →
  400 `{ "error": "Campo de ordenação 'x' inválido. Permitidos: a, b, c." }`. Nunca se
  monta ordenação a partir de texto livre.
- Os filtros existentes (`animalId`, `status`, `tipo`…) **não mudam**; nenhum filtro novo é
  inventado (YAGNI: o rubric pede que existam, e existem).
- `Paginacao.Aplicar` (à prova de estouro) continua sendo o único ponto de recorte.

### 6.4 HATEOAS (F3) — [ADR-004](decisoes/ADR-004-hateoas-sem-quebrar-o-app.md)

**Por item (aditivo, sempre ligado):** os DTOs de resposta herdam `RespostaHateoas` e ganham
`_links` — um objeto indexado pela relação, no estilo HAL:

```json
{ "id": "…", "titulo": "…", "_links": {
    "self":     { "href": "/api/v1/lembretes/…", "method": "GET" },
    "atualizar":{ "href": "/api/v1/lembretes/…", "method": "PUT" },
    "excluir":  { "href": "/api/v1/lembretes/…", "method": "DELETE" },
    "colecao":  { "href": "/api/v1/lembretes",   "method": "GET" } } }
```

Acrescentar uma propriedade a um objeto JSON não quebra quem só lê os campos que conhece.

| Recurso | Relações por item |
|---|---|
| Lembrete | `self`, `atualizar`, `excluir`, `colecao`, `saudePreditiva` |
| Evento Pet | `self`, `atualizar`, `excluir`, `colecao` |
| Produto | `self`, `atualizar`, `excluir`, `colecao` |
| Sugestão de Produto | `self`, `atualizar`, `excluir`, `colecao`, `saudePreditiva` |
| Saúde Preditiva `GET {animalId}` | `self`, `widget`, `lembretes`, `sugestoes` |
| Widget `GET {animalId}` | `self`, `saudePreditiva` |
| Telegram `GET link/{tutorId}` | `self`, `vinculo` |
| Telegram `GET vinculo/{tutorId}` | `self`, `link`, `desvincular` |

A cobertura é de **todos os `GET` de consulta**, inclusive os dois do Telegram
(`TelegramLinkResponse` e `TelegramVinculoResponse` também herdam `RespostaHateoas`).
Os `href` saem do `LinkGenerator` a partir do **nome da ação** — nenhuma rota escrita à mão
em string, então renomear uma rota não deixa link morto. `GeradorDeLinks` mora na Api.

**Por coleção:** o corpo continua **array**. A navegação vai em cabeçalhos:
`Link: <…?page=2&pageSize=10>; rel="next"` (também `first`, `prev`, `last`) e
`X-Total-Count`. Os links preservam filtros, `ordenarPor` e `direcao`.

**Envelope opcional:** com `Accept: application/vnd.clyvovet.hateoas+json` a listagem devolve

```json
{ "itens": [ … ], "page": 1, "pageSize": 10, "total": 42,
  "_links": { "self": {…}, "first": {…}, "prev": {…}, "next": {…}, "last": {…} } }
```

`prev` e `next` só aparecem quando existem. Os `GET` de listagem declaram os dois tipos de
mídia no Swagger.

## 7. Persistência

### 7.1 EF Core / MySQL

O EF Core e o MySQL compartilhado **continuam** (o professor liberou o MySQL para o rubric
"Oracle ou SQL Server"; ver ADR-005). Mudança de fronteira: hoje `MapaDeErro` responde 409 para `DbUpdateException`.
A Application e a Api não podem conhecer o EF, então `AppDbContext.SaveChangesAsync` passa a
capturar `DbUpdateException` e relançar **`RegistroEmUsoException`** (Domain). O
`MapaDeErro` mapeia essa exceção para o mesmo 409 e a mesma mensagem de sempre
("Registro em uso por outro cadastro."). **Comportamento idêntico, fronteira limpa.**

### 7.2 MongoDB (F4) — [ADR-002](decisoes/ADR-002-mongodb-cache-parecer-ia.md)

- `ParecerIaMongoRepository : IParecerIaRepository`. **A interface, a entidade e o
  `SaudePreditivaService` não mudam.**
- Coleção `pareceres_ia`; `_id` = `animalId` (um parecer por animal, como o
  `UNIQUE(animal_id)` de hoje). `Salvar` é *upsert*.
- `Conteudo` (hoje um JSON em `string`) é gravado como **subdocumento BSON** via
  `BsonDocument.Parse` e devolvido como texto por `ToJson()` — o serviço continua
  desserializando o que sempre desserializou.
- **Índice TTL** em `validoAte` (`expireAfterSeconds: 0`), criado de forma idempotente no
  boot. O serviço continua checando `ValidoAte` no código (o monitor de TTL do Mongo roda
  a cada ~60 s, então o índice é limpeza, não regra).
- **Cache nunca derruba a feature:** falha do Mongo na leitura vira *cache miss* e na
  escrita vira `Warning`; a saúde preditiva segue respondendo pelo caminho de regras.
- **Seleção por configuração:** `Mongo:ConnectionString` presente → Mongo; ausente → o
  `ParecerIaRepository` atual (EF/MySQL). Protege a produção enquanto não existir Mongo
  hospedado, e mantém o ambiente `Testing` sem dependência externa.
- Configuração: `Mongo:ConnectionString`, `Mongo:Database` (padrão `clyvovet`).
- Health check `mongo` na tag `external`, **fora** de `ready` (o mesmo raciocínio do
  Telegram: instabilidade num cache não tira a API de rotação).
- `docker-compose.yml` na raiz com `mongo:7` para desenvolvimento.
- **Testes:** o mapeamento entidade↔documento é testado sem servidor. Os testes contra um
  Mongo real são marcados e só rodam com `MONGO_TEST_URI` definida; sem ela, são pulados
  com a razão explícita. Rodar: `docker run -p 27017:27017 mongo:7` e
  `MONGO_TEST_URI=mongodb://localhost:27017 dotnet test`.
- A tabela `t_clyvo_parecer_ia` (Flyway V15 da Java) continua existindo; fica sem uso
  quando o Mongo está ligado.

### 7.3 Migrations (F7)

O professor liberou o MySQL em 20/09/2026, então vale o caminho F7-A da
[ADR-005](decisoes/ADR-005-banco-e-migrations.md): migration *baseline* só das `t_clyvo_*`.

## 8. Observabilidade e testes

- **Logging (F5):** em `Production`, saída de console em **JSON**
  (`CompactJsonFormatter`); em `Development`, mantém o template legível atual. `CorrelationId`,
  `Application` e `MachineName` seguem como propriedades. Sink de arquivo continua só em
  `Development`.
- **Health checks:** os três endpoints atuais + check do Mongo (§7.2).
- **Testes:** xUnit, AAA, mantendo os 256 casos e somando os de cada fase — ordenação e
  lista branca, HATEOAS (item, cabeçalhos, envelope), autorização (401 sem token, 401 com
  refresh, 403 de perfil, 200 válido, rotas de infra 200 sem token), mapeamento do Mongo,
  `ArquiteturaTests`, tradução de `DbUpdateException`.
- **Cobertura (F5):** `scripts/cobertura.sh` roda `dotnet test` com coleta do `coverlet`,
  gera o relatório (ReportGenerator) e **falha se a cobertura de linhas de Domain +
  Application ficar abaixo de 70%**. O 70% é escolha nossa — o rubric não fixa número. A F5
  mede a linha de base primeiro e adiciona testes até bater a meta.
- **Qualidade de código (F5):** revisão de SOLID e Clean Code nas 4 camadas com a skill
  `code-review` (responsabilidades misturadas, métodos longos, duplicação, dependência de
  implementação em vez de interface). Correções em commits próprios; o que for aceito como
  está fica anotado com o motivo. O README ganha a seção "Princípios aplicados", com um
  exemplo **real** do código por princípio.
- **Ensaio geral (F6):** o rubric pede as funcionalidades das Sprints 1–3 "integradas e
  funcionando em conjunto", e os testes verdes não provam isso sozinhos. A F6 percorre, com
  token válido, todos os endpoints contra um ambiente completo (MySQL + MongoDB) e registra
  o resultado em `docs/sprint4/ensaio-geral.md`.

## 9. Documentação (F6)

- `README.md` reorganizado: visão geral → arquitetura (diagrama de camadas em **Mermaid**,
  que o GitHub renderiza) → tecnologias → estrutura de pastas → princípios aplicados (SOLID, com exemplos reais) → como executar (local, Docker,
  Mongo) → autenticação → endpoints (com exemplos de `ordenarPor`, `Link` e envelope) →
  observabilidade → testes e cobertura → integrantes → licença.
- O conteúdo do deploy Azure da Sprint 3 vai para `docs/deploy-azure-sprint3.md`; o guia de
  testes manuais para `docs/guia-de-testes-manuais.md`. **Nada é apagado**, só realocado.
- `docs/swagger/openapi-v1.json` exportado por `scripts/exportar-swagger.sh`.
- `docs/arquitetura-azure.svg` permanece (é o diagrama de infra).

## 10. Riscos

| Risco | Impacto | Mitigação |
|---|---|---|
| Mover projetos quebra o build do Render (`Dockerfile` copia `ClyvoVet.Api/…`) | Deploy fora do ar | F1 atualiza `Dockerfile` e `.dockerignore` **no mesmo commit** do movimento e valida com `docker build`. O Render só rebuilda com push, e push só sob pedido. |
| `Jwt__Secret` ausente no Render após ligar `[Authorize]` | 401 em toda rota protegida | Alavanca `Auth:ExigirToken`; checklist de deploy na F2; o app sobe mesmo sem o segredo. |
| `JwtBearer 8.0.x` com `IdentityModel 8.3.1` (o grafo atual está unificado em 8.3.1) | Erro de compatibilidade binária | F2 começa com uma **prova de compatibilidade** (pacote + testes de token existentes) antes de qualquer refatoração. Se falhar, fixa a versão que o `JwtBearer` exige e adapta os testes. |
| Refatoração mecânica muda comportamento sem querer | Regressão | Cada passo da F1 fecha com os 256 casos verdes; nenhum arquivo muda de lógica, só de lugar e namespace. |
| Testar no runtime errado (o SDK 10 do sistema roda `net8.0` com roll-forward e a integração falha por artefato de ambiente) | Falso vermelho, ou falso verde | P1 resolvido: SDK 8 instalado localmente, testes rodam pelos `.csproj`. Nunca usar `DOTNET_ROLL_FORWARD` para validar. |
| Mongo indisponível em produção | Perda de cache | Falha vira *cache miss*; sem `Mongo:ConnectionString` o EF atual continua respondendo. |
| Testes de Mongo real não rodam sem servidor | Cobertura da persistência NoSQL menor no CI local | Documentado; mapeamento testado sem servidor; comando de execução no README. |

## 11. Fora de escopo

- **CI/CD** (GitHub Actions): não consta no rubric desta disciplina. Registrado como possível
  melhoria futura; um workflow de `dotnet build` + `dotnet test` evitaria a penalidade de
  −20 por "não compilar" e pode ser adicionado depois, caso o dono do repositório queira.
- Funcionalidade nova de domínio, mudanças no repositório Java, e o **deploy** de qualquer
  parte (Render, Azure, Atlas): somente mediante pedido explícito.

## 12. Pendências externas (não dependem de código)

1. ~~**Professor** — resposta pendente sobre a ADR-005 (§7.3)~~ — **resolvido** em 20/09/2026:
   MySQL liberado.
2. ~~**Ambiente** — instalar o runtime .NET 8~~ — **resolvido** em 19/09/2026 (P1 do plano).
3. **Mongo hospedado** — necessário apenas se o dono do repositório optar por manter o Mongo
   ativo em produção (ex.: MongoDB Atlas, camada gratuita); criar a conta e o cluster é
   decisão dele, e o código funciona normalmente sem isso (§7.2).
