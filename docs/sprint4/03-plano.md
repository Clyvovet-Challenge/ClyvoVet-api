# Plano-mestre — Sprint 4

> **Para quem for executar:** leia [`02-design.md`](02-design.md) primeiro. Este plano
> estabelece as **fases, as tarefas e os critérios de conclusão**. Ao iniciar cada fase,
> registre o passo a passo detalhado dela (com código) em `docs/sprint4/fases/FN-*.md`, usando
> a skill `superpowers:writing-plans`, **sempre a partir do código como ele estiver naquele
> momento** — cada fase depende da forma que o código assumiu nas anteriores. Execução:
> `superpowers:executing-plans` ou `superpowers:subagent-driven-development`.

**Meta:** fechar os gaps do rubric ([`01-gap-analysis.md`](01-gap-analysis.md)) sem quebrar
produção nem o contrato com o app.

**Arquitetura:** Clean Architecture em 4 projetos (Domain, Application, Infrastructure, Api),
JWT com `[Authorize]`, HATEOAS aditivo, MongoDB para o cache do parecer de IA.

**Stack:** .NET 8, ASP.NET Core, EF Core 8.0.11 + Pomelo 8.0.2 (MySQL), MongoDB.Driver,
Serilog, OpenTelemetry, xUnit, coverlet.

**Spec:** [`02-design.md`](02-design.md)

## Restrições globais

Valem para toda tarefa de toda fase.

- Alvo `net8.0`; EF Core `8.0.11`; Pomelo `8.0.2`; `Swashbuckle.AspNetCore 10.1.7`;
  `Microsoft.OpenApi 2.12.2`.
- Cobertura de linhas de Domain + Application **≥ 70%** (meta nossa; F5).
- **Contrato com o app:** erro mantém `error` (e `referencia` em falha de servidor);
  listagem devolve **array JSON** por padrão.
- Autorização: **sem `FallbackPolicy`**; chave JWT = `Convert.FromBase64String`; só
  `tipo = access`; `tutorId` nulo nega; recurso alheio = 404.
- Não escrever em `animal`/`tutor`; **sem migrations EF** até a ADR-005.
- Git: commit só após "sim", uma mudança lógica por commit, sem `--amend`/rebase/reset,
  push e deploy só sob pedido, mensagens sem linha de atribuição.

## Definição de pronto (vale para todas as fases)

- [ ] `dotnet build` sem erros (warning novo só com justificativa).
- [ ] `dotnet test` **no runtime .NET 8** sem falhas, e com **número de casos ≥ ao da fase
      anterior** (nunca menos).
- [ ] Nenhuma mudança de comportamento observável além do que a fase declara.
- [ ] Tabela de estado do [`README`](README.md) e o `CLAUDE.md` atualizados no mesmo commit
      que fecha a fase.
- [ ] Commits feitos só depois do "sim" do dono do repositório.

## Pré-requisitos (bloqueiam o fechamento das fases)

| # | Pré-requisito | Quem | Como |
|---|---|---|---|
| **P1** ✅ | **Runtime .NET 8** para rodar os testes de integração | **resolvido em 19/09/2026** | SDK 8 instalado localmente, ao lado do SDK 10 do sistema (detalhes do ambiente ficam no `CLAUDE.local.md` de cada pessoa). O SDK 8 não lê `.slnx`, então os testes rodam pelos `.csproj`. Linha de base: **256 / 256 verdes** (gap-analysis §0). |
| **P2** | Resposta do professor sobre banco/migrations | dono do repo | Texto pronto em [ADR-005](decisoes/ADR-005-banco-e-migrations.md). Só bloqueia a F7. |
| **P3** | MongoDB hospedado (ex.: Atlas, camada gratuita) | dono do repo | **Opcional.** Só para ligar o Mongo em produção; o código funciona sem ele (design §7.2). |

---

## F0 — Organização

**Objetivo:** o repositório passa a ter instruções e plano versionados, e o ambiente está
pronto para medir.

- [x] `CLAUDE.md` na raiz e `docs/sprint4/` escritos (README, requisitos, gap-analysis,
      design, plano, 5 ADRs).
- [ ] Revisão do dono do repositório.
- [x] **P1** resolvido; linha de base real registrada em `01-gap-analysis.md` §0
      (163 unidade + 93 integração = **256 ✅**, runtime 8.0.31).
- [ ] `.gitignore`: acrescentar `CLAUDE.local.md` (preferências pessoais) e
      `.claude/settings.local.json`; e `git rm --cached .claude/settings.local.json` — hoje
      ele está **versionado com caminhos do Windows de outro integrante**, e arquivo `local`
      não deve ser compartilhado. **Pedir "sim" antes**: mexe em arquivo de outra pessoa.
- [ ] Commits sugeridos:
  - `docs: organiza a Sprint 4 (CLAUDE.md, requisitos, gap-analysis, design e plano)`
  - `chore: tira .claude/settings.local.json do versionamento`

**Pronto quando:** tudo commitado e a linha de base do runtime 8 registrada.

---

## F1 — Clean Architecture em 4 projetos

**Objetivo:** camadas reais, verificadas por teste, **sem mudar comportamento**.
**Spec:** design §3, §4, §5, §7.1. **Decisão:** ADR-001.
**Risco principal:** `Dockerfile`/deploy (design §10). **Regra:** cada tarefa fecha com build e
testes verdes **antes** do commit.

### T1 — Preparar o terreno
- [ ] `git ls-files ClyvoVet.Api/publish ClyvoVet.Api/Logs` — se houver algo versionado que
      não devia (o `publish/` apareceu na listagem), tirar do índice em commit próprio
      (`git rm -r --cached`).
- [ ] Limpar saídas de build: `rm -rf ClyvoVet.Api/{bin,obj} ClyvoVet.Api/*/{bin,obj}`.

### T2 — Mover para `src/` e `tests/` (só movimento, zero lógica)
- [ ] `mkdir -p src tests`
- [ ] `git mv ClyvoVet.Api src/ClyvoVet.Api`
- [ ] `git mv src/ClyvoVet.Api/ClyvoVet.Api.Tests.Unit tests/ClyvoVet.Api.Tests.Unit`
- [ ] `git mv src/ClyvoVet.Api/ClyvoVet.Api.Tests.Integration tests/ClyvoVet.Api.Tests.Integration`
- [ ] Ajustar: `ClyvoVet-api.slnx` (3 caminhos); `ProjectReference` dos dois projetos de
      teste (passa a ser `..\..\src\ClyvoVet.Api\ClyvoVet.Api.csproj`); remover os 8 itens
      `Remove` do `csproj` da Api; `Dockerfile` (copiar `src/` e restaurar
      `src/ClyvoVet.Api/ClyvoVet.Api.csproj`; `WORKDIR /src/src/ClyvoVet.Api`);
      `.dockerignore` (`**/ClyvoVet.Api.Tests.*/` → `tests/`).
- [ ] **Verificar:** `dotnet build`, `dotnet test` (mesmo total da linha de base) e
      `docker build -t clyvovet-api .` (exige o Docker ligado).
- [ ] Commit: `refactor: move os projetos para src/ e tests/`

### T3 — Domain
- [ ] **Teste primeiro:** criar `tests/…Unit/ArquiteturaTests.cs` com a regra do Domain
      (design §4) e ver falhar (o projeto ainda não existe).
- [ ] `dotnet new classlib -n ClyvoVet.Domain -o src/ClyvoVet.Domain -f net8.0`; adicionar à
      solução; **nenhum pacote**.
- [ ] `git mv` de `Models/`→`Entities/`, `Enums/`, `Exceptions/`; trocar o `namespace` de cada
      arquivo movido e os `using` no restante (o compilador aponta os que faltarem).
- [ ] **Teste primeiro:** `RegistroEmUsoExceptionTests` (mensagem/tipo) → criar
      `RegistroEmUsoException` em `Domain/Exceptions`.
- [ ] Build + testes verdes. Commit: `refactor: extrai o projeto Domain`

### T4 — Application
- [ ] Regra de dependência do Application em `ArquiteturaTests` (vermelho).
- [ ] Criar projeto; pacotes só de `Microsoft.Extensions.*.Abstractions`.
- [ ] Mover DTOs, interfaces e serviços de caso de uso, `DataValidationHelper`,
      `Repositories/Interfaces`→`Abstractions/Repositories`, `IOciGenerativeAiClient` e
      `ITelegramService`→`Abstractions/External`, `EscopoDoTutor`,
      `IdentidadeDoChamador`, `VinculosPendentesDeTelegram`.
- [ ] **`IUsuarioAtual`** (Application): `IdentidadeDoChamador? Identidade { get; }`.
      `EscopoDoTutor` passa a depender dele em vez de `IHttpContextAccessor`; implementação
      `UsuarioAtualHttp` na Api lendo `HttpContext.Items` (como hoje — a F2 troca por claims).
- [ ] `AddApplication()`.
- [ ] Build + testes verdes. Commit: `refactor: extrai o projeto Application`

### T5 — Infrastructure
- [ ] Regra de dependência da Infrastructure em `ArquiteturaTests` (vermelho).
- [ ] Criar projeto com EF, Pomelo, HealthChecks.EFCore, Telegram.Bot, Http, hosting.
- [ ] Mover `Data/`, repositórios concretos + `Paginacao`, `OciGenerativeAiClient`,
      `TelegramService`, os dois `BackgroundService`, `TelegramHealthCheck`.
      `InternalsVisibleTo("ClyvoVet.Api.Tests.Unit")` vai junto.
- [ ] **Teste primeiro:** em `tests/…Integration`, "SaveChanges com violação de integridade
      lança `RegistroEmUsoException`" → `AppDbContext.SaveChangesAsync` captura
      `DbUpdateException` e relança. `MapaDeErro` passa a mapear `RegistroEmUsoException`
      → 409 e a mesma mensagem ("Registro em uso por outro cadastro."); ajustar
      `MapaDeErroTests`.
- [ ] `AddInfrastructure(configuration, environment)` recebendo o que hoje está no
      `Program.cs` (teto de pool, versão fixa do MySQL, DI de repositórios/clientes,
      *background services* fora de `Testing`, health checks).
- [ ] Build + testes verdes. Commit: `refactor: extrai o projeto Infrastructure`

### T6 — Api enxuta
- [ ] Regra da Api em `ArquiteturaTests`: não referenciar EF nem MongoDB (vermelho enquanto o
      `MapaDeErro`/`Program.cs` ainda tocar no EF).
- [ ] `Program.cs` só orquestra; extensões em `src/ClyvoVet.Api/Extensions/`
      (`AddDocumentacaoApi`, `AddObservabilidade`). **Os comentários de "porquê" vão junto.**
- [ ] Tirar do `csproj` da Api os pacotes que passaram para a Infrastructure.
- [ ] Build + testes verdes. Commit: `refactor: enxuga o Program.cs e a Api`

### T7 — Reapontar testes, `Dockerfile` e docs
- [ ] `ProjectReference` dos testes para os projetos certos; namespaces dos testes.
- [ ] `Dockerfile` copiando `src/` inteiro (já cobre os 4 projetos); `docker build` verde.
- [ ] Atualizar `CLAUDE.md` (seção **Comandos** com os caminhos novos) e os caminhos no
      `README.md`.
- [ ] Commit: `docs: atualiza caminhos e comandos para a nova estrutura`

**Pronto quando:** DoD + `ArquiteturaTests` verdes + `docker build` verde + `dotnet run`
sobe e `GET /health/live` responde 200 + `git diff --stat -M` mostra os arquivos como
**renomeados**, não apagados/criados.

---

## F2 — Exceções globais e JWT

**Objetivo:** autenticação/autorização padrão do ASP.NET e tratador global de exceções.
**Spec:** design §6.1, §6.2. **Decisão:** ADR-003.

- [ ] **T1 — Prova de compatibilidade.** Adicionar `Microsoft.AspNetCore.Authentication.JwtBearer
      8.0.11` e rodar os testes de token que já existem
      (`ValidadorDeTokenJwtTests`, `EscopoPorTutorEndpointsTests`). Se falhar por conflito
      com `IdentityModel 8.3.1`, fixar a versão exigida e registrar na ADR-003. **Nada de
      refatoração antes disto passar.**
- [ ] **T2 — `AddAutenticacaoJwt`** (teste primeiro, `AutenticacaoJwtTests`): token válido →
      200; sem token → 401; `tipo=refresh` → 401; expirado → 401; chave errada → 401;
      perfil `TUTOR` em rota `Equipe` → 403. Chaves `Jwt:Secret`, `Jwt:Emissor`,
      `Jwt:Publico` (emissor/público com os valores hoje fixos no `ValidadorDeTokenJwt`),
      `Auth:ExigirToken` (padrão `true`).
- [ ] **T3 — `IUsuarioAtual` sobre `HttpContext.User`**; remover `IdentidadeMiddleware` e
      `ValidadorDeTokenJwt`; migrar `ValidadorDeTokenJwtTests` para os testes novos sem
      perder nenhum caso (incluindo `ChaveDerivaDoBase64`).
- [ ] **T4 — `[Authorize]`** conforme design §6.2. **Confirmar com o app** se ele escreve
      produto (se sim, `Produto` cai para `Autenticado`). Testes: `/health*`, `/metrics`,
      `/swagger` → 200 **sem token**; rotas protegidas → 401 sem token; `Auth:ExigirToken=false`
      → 200 sem token.
- [ ] **T5 — Swagger:** esquema `Bearer` ao lado do `ApiKey`.
- [ ] **T6 — `TratadorGlobalDeExcecoes` + `ProblemDetails`** (teste primeiro): mesmos
      status de hoje (404/400/403/409/500); corpo com `error`, e `referencia` só no 500;
      `MapaDeErroTests` verdes.
- [ ] **T7 — Docs:** ADR-003 → *Implementada*; seção "Autenticação" no README; atualizar o
      comentário do `csproj` que justificava não usar `JwtBearer`; **checklist de deploy**
      (definir `Jwt__Secret` — mesmo valor da Java — no Render *antes* do deploy).

**Pronto quando:** DoD + os testes acima verdes + nenhuma referência restante a
`IdentidadeMiddleware`/`ValidadorDeTokenJwt`.

---

## F3 — Ordenação, paginação com total e HATEOAS

**Objetivo:** listagens completas e navegáveis, sem quebrar o app.
**Spec:** design §6.3, §6.4. **Decisão:** ADR-004.

- [ ] **T1** `ConsultaPaginada`, `PaginaDeResultados<T>`, `DirecaoOrdenacao` (Application) + testes.
- [ ] **T2** Repositórios dos 4 recursos com listagem (Lembrete, Evento Pet, Produto,
      Sugestão): **lista branca** de ordenação + total (`CountAsync`). Testes por recurso:
      ordena asc/desc; campo fora da lista → `BadRequestException`; sem `ordenarPor` mantém
      a ordem de hoje. A lista de campos de cada recurso sai das propriedades escalares do
      respectivo DTO de resposta — escrita no passo a passo da fase.
- [ ] **T3** Serviços devolvem `PaginaDeResultados<T>`; atualizar os testes de serviço.
- [ ] **T4** Controllers: `ordenarPor`/`direcao`; 400 com `error` para campo inválido; corpo
      **array**; cabeçalhos `X-Total-Count` e `Link` (`first`/`prev`/`next`/`last`, preservando
      filtros e ordenação).
- [ ] **T5** `Link`, `RespostaHateoas`, `GeradorDeLinks` (`LinkGenerator` + nome da ação);
      `_links` nos DTOs de item conforme design §6.4 — **inclusive nos dois `GET` do
      Telegram** (`link/{tutorId}` e `vinculo/{tutorId}`). Teste de regressão do contrato:
      **sem** `Accept` especial, corpo continua array e os campos antigos intactos.
- [ ] **T6** Envelope com `Accept: application/vnd.clyvovet.hateoas+json` (`itens`, `page`,
      `pageSize`, `total`, `_links`); `prev`/`next` só quando existem.
- [ ] **T7** Swagger documenta parâmetros e os dois tipos de mídia; README ganha exemplos
      `curl`.

**Pronto quando:** DoD + testes de contrato/HATEOAS verdes + `curl` de exemplo funcionando
contra a API local.

---

## F4 — MongoDB

**Objetivo:** NoSQL integrado, sem risco para a feature de saúde preditiva.
**Spec:** design §7.2. **Decisão:** ADR-002.

- [ ] **T1** `MongoDB.Driver` na Infrastructure; `docker-compose.yml` com `mongo:7`; chaves
      `Mongo:ConnectionString`, `Mongo:Database`.
- [ ] **T2** Mapeamento entidade↔documento (`Conteudo` string ↔ subdocumento BSON) com
      testes **sem servidor**.
- [ ] **T3** `ParecerIaMongoRepository` (`GetByAnimalIdAsync`, `SalvarAsync` = *upsert*);
      falha na leitura → *cache miss*; falha na escrita → `Warning`. Testes contra Mongo
      real marcados e **pulados sem `MONGO_TEST_URI`**, com a razão no relatório.
- [ ] **T4** Índice TTL em `validoAte` criado de forma idempotente no boot.
- [ ] **T5** Seleção por configuração no `AddInfrastructure`; `Testing` continua sem Mongo.
- [ ] **T6** `MongoHealthCheck` (tag `external`, fora de `ready`) + teste.
- [ ] **T7** README: seção NoSQL e como rodar os testes com Mongo.
- [ ] **Verificação manual** (com Docker ligado): `docker compose up -d mongo`, chamar a
      saúde preditiva duas vezes e ver o documento em `pareceres_ia` (a segunda vem do cache).

**Pronto quando:** DoD + a verificação manual acima + `ArquiteturaTests` ainda verdes
(Application sem `MongoDB*`).

---

## F5 — Observabilidade, testes e qualidade de código

**Objetivo:** logs estruturados em produção, cobertura medida e defendida, e SOLID/Clean
Code verificados no código.
**Spec:** design §8.

- [ ] **T1** `Serilog.Formatting.Compact`; JSON no console fora de `Development`; teste de
      configuração; template legível mantido em `Development`.
- [ ] **T2** `scripts/cobertura.sh` (coleta, relatório, falha abaixo de 70% em Domain +
      Application). **Medir a linha de base primeiro** e registrar em `01-gap-analysis.md`.
- [ ] **T3** Escrever testes até bater a meta, priorizando o que a medição mostrar mais
      descoberto (AAA, `Metodo_Cenario_Resultado`).
- [ ] **T4** Revisar health checks (todos com teste de integração de status e formato).
- [ ] **T5 — Revisão de SOLID e Clean Code** (rubric 1: "Princípios SOLID e Clean Code
      evidentes no código"). Rodar a skill `code-review` nas 4 camadas e procurar: classe ou
      método com mais de uma responsabilidade, método longo, duplicação, dependência de
      implementação em vez de interface, e `switch`/`if` que exigem editar código para
      acrescentar um caso. Corrigir o que aparecer, **um commit por correção**, com os
      testes verdes. O que for aceito como está, com o motivo, fica anotado no
      `01-gap-analysis.md`.
- [ ] **T6** README: seção **"Princípios aplicados"**, com **um exemplo real do código por
      princípio** (arquivo e linha) — só entra o que existir de fato. Candidatos a
      confirmar no código: S — um serviço por recurso; O — acrescentar um campo ordenável é
      uma entrada na lista branca, sem alterar `Paginacao`; L — `ParecerIaMongoRepository` e
      `ParecerIaRepository` intercambiáveis por `IParecerIaRepository`; I — interfaces
      pequenas por recurso; D — interfaces em Application implementadas em Infrastructure
      (o `ArquiteturaTests` prova). Mais as seções "Observabilidade" e "Testes e cobertura"
      com os comandos reais.

**Pronto quando:** DoD + `scripts/cobertura.sh` sai com código 0, o número real está
documentado, e a revisão de SOLID/Clean Code está feita e registrada.

---

## F6 — Documentação final

**Objetivo:** o README que o rubric avalia, completo e navegável.
**Spec:** design §9.

- [ ] **T1** Diagrama de camadas em **Mermaid** (Api → Application → Domain;
      Infrastructure → Application; bancos e serviços externos).
- [ ] **T2** Reorganizar o `README.md` na ordem do design §9. **Realocar, nunca apagar:**
      Azure da Sprint 3 → `docs/deploy-azure-sprint3.md`; guia de testes manuais →
      `docs/guia-de-testes-manuais.md`. Conferir que nenhum link relativo quebrou.
- [ ] **T3** `scripts/exportar-swagger.sh` → `docs/swagger/openapi-v1.json`.
- [ ] **T4** Conferir seções exigidas pelo rubric: visão geral, arquitetura, endpoints,
      instalação, testes, **integrantes**.
- [ ] **T5 — Ensaio geral** (enunciado: "funcionalidades das Sprints 1, 2 e 3, integradas e
      funcionando em conjunto"). Subir a API contra um ambiente completo (MySQL + MongoDB) e
      percorrer, **com token válido**, os endpoints das Sprints 1–3: produtos, eventos pet,
      lembretes, sugestões, saúde preditiva, widget e Telegram. Conferir os status esperados
      (200/201/204/4xx), `_links`, `Link`/`X-Total-Count`, ordenação, 401 sem token e
      `/health*` e `/metrics`. Registrar o resultado (o que passou e o que não) em
      `docs/sprint4/ensaio-geral.md`. O ambiente exato (compose local ou o serviço
      publicado) é definido na abertura da fase. Avaliar o `test_api.sh` da raiz como ponto
      de partida do roteiro; ele e os exemplos `curl` do README precisam passar a mandar o
      token, se hoje só usam `X-Api-Key`.
- [ ] **T6** Revisão final contra o rubric: reabrir `01-gap-analysis.md`, atualizar a coluna
      de estado e listar o que sobrou.

**Pronto quando:** DoD + todo item do rubric mapeado para evidência no repositório + ensaio
geral registrado em `docs/sprint4/ensaio-geral.md`.

---

## F7 — Migrations EF (condicional)

**Bloqueada por [ADR-005](decisoes/ADR-005-banco-e-migrations.md)** — só começa depois de
**P2**. O conteúdo depende da resposta:

- **MySQL aceito:** migration *baseline* só das `t_clyvo_*`, com `animal`/`tutor` fora das
  migrations; script idempotente como entregável; provar em banco vazio.
- **SQL Server ou Oracle exigidos:** **não é uma fase, é um redesenho** (o schema
  compartilhado com a Java deixa de servir). Volta ao brainstorming, com spec própria,
  antes de qualquer código.

---

## Ordem e dependências

```
P1 ──► F0 ──► F1 ──► F2 ──► F3 ──► F4 ──► F5 ──► F6
                               (F4 é independente de F2/F3)
P2 ──► F7   (paralela às demais; não bloqueia F1–F6)
```

A F1 precede todas as outras porque **move todos os arquivos**: implementar qualquer feature
antes dela geraria conflito em cada fase subsequente.
