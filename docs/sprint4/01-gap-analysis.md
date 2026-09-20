# Gap-analysis — rubric da Sprint 4 × código atual

> Levantamento feito em **19/09/2026**, na branch `sprint-4` (base `777a113`), com base
> na leitura do código, e não do README. Legenda: 🟢 atende · 🟡 atende parcialmente · 🔴 não atende.

## 0. Linha de base (build e testes)

| Verificação | Resultado |
|---|---|
| `dotnet build` | ✅ compila (com o SDK 8 e também com o SDK 10) |
| Testes de unidade | ✅ **163 / 163** passam |
| Testes de integração | ✅ **93 / 93** passam |
| **Total** | ✅ **256 / 256** — linha de base oficial da Sprint 4 (medida em 19/09/2026, runtime 8.0.31) |

**Histórico da medição.** Na primeira tentativa, usando apenas o runtime **.NET 10** na
máquina com `DOTNET_ROLL_FORWARD=Major`, o resultado foi 26 de 93 na integração: o
`Mvc.Testing 8.0.11` acabava rodando junto com o ASP.NET 10, e toda resposta serializada
falhava com `The PipeWriter 'ResponseBodyPipeWriter' does not implement
PipeWriter.UnflushedBytes`. Isso era **um artefato do ambiente, não um defeito do código** —
o problema desapareceu ao instalar o runtime 8 (pré-requisito P1 do [plano](03-plano.md),
resolvido com um SDK 8 instalado localmente). Como o SDK 8 não lê `.slnx`, os testes passam
a rodar apontando diretamente para os dois `.csproj`.

> Correção de um número mencionado antes em conversa: "215 testes" veio de um
> `grep` em `[Fact]`/`[Theory]`. O número real de casos é **256** (163 + 93).

## 1. Arquitetura e código — 30 pts

| Item | Estado | Evidência | Fase |
|---|---|---|---|
| Clean Architecture com camadas | 🔴 | `ClyvoVet-api.slnx` lista 3 projetos: a Api (com tudo dentro, só separado por pasta) e 2 de teste. Não há Domain/Application/Infrastructure. | F1 |
| SOLID / Clean Code | 🟡 | Interface por serviço e por repositório; controllers finos. Mas o `Program.cs` tem 411 linhas e faz tudo (logging, CORS, Swagger, banco, DI, health, OTel). Não há revisão nem evidência documentada dos princípios. | F1, F5 |
| Injeção de dependência | 🟢 | Tudo registrado por interface em `Program.cs:222-268`. Falta só extrair para `AddApplication`/`AddInfrastructure`. | F1 |
| Exceções globais | 🟡 | Existe e é testado (`Errors/MapaDeErro.cs`), mas o handler é um lambda de 40 linhas em `Program.cs:329-372`, e o `MapaDeErro` conhece `DbUpdateException` (EF) — vazamento de infraestrutura. | F1, F2 |

## 2. API RESTful — 20 pts

| Item | Estado | Evidência | Fase |
|---|---|---|---|
| Swagger/OpenAPI | 🟢 | `Program.cs:96-176`, XML comments, filtros de tag e de chave. Falta o esquema Bearer. | F2 |
| Paginação | 🟡 | `Repositories/Paginacao.cs` (à prova de estouro). **Sem total**: as listagens devolvem `IEnumerable<T>` (`ILembreteService.GetAllAsync`), então não há como montar `last`. | F3 |
| Ordenação | 🔴 | Controllers só recebem `page`, `pageSize` e filtros (ex.: `LembreteController.GetAll`). A ordem é um `OrderBy` fixo em cada repositório. | F3 |
| Filtros | 🟢 | `animalId`, `status`, `tipo` (lembretes); há filtros equivalentes nos outros recursos. | — |
| HATEOAS | 🔴 | Zero ocorrências de `_links`/`HATEOAS` no código. | F3 |
| JWT / Identity | 🟡 | `Security/ValidadorDeTokenJwt.cs` + `Middleware/IdentidadeMiddleware.cs` **leem** o token da API Java, mas o middleware é "inerte" (não rejeita nada) e não há `AddAuthentication` nem `[Authorize]`. Quem protege as rotas hoje é a `X-Api-Key` (`[TypeFilter(typeof(ApiKeyFilterAttribute))]`). | F2 |

## 3. Persistência — 20 pts

| Item | Estado | Evidência | Fase |
|---|---|---|---|
| EF Core com **migrações** | 🔴 | Não existe pasta `Migrations`. Provider é **MySQL** (`Pomelo.EntityFrameworkCore.MySql 8.0.2`, `Program.cs:219-220`); o rubric diz **Oracle ou SQL Server**, e em 20/09/2026 o professor liberou manter o MySQL. A auditoria de 06/09 (§6) proibiu migrations aqui porque o schema é da API Java (Flyway); a F7 as cria só para as `t_clyvo_*`. | F7 — **liberada ([ADR-005](decisoes/ADR-005-banco-e-migrations.md))** |
| MongoDB | 🔴 | Zero ocorrências. | F4 |
| Repository | 🟢 | 9 interfaces em `Repositories/Interfaces/` e 9 implementações concretas. | — |

## 4. Monitoramento, observabilidade e testes — 20 pts

| Item | Estado | Evidência | Fase |
|---|---|---|---|
| Health checks | 🟢 | `/health`, `/health/live`, `/health/ready` (`Program.cs:390-403`); checks de `self`, banco e Telegram. Faltará o do Mongo. | F4 |
| Logging estruturado | 🟡 | Serilog com `CorrelationId` e propriedades (`Program.cs:34-66`), mas a saída do console é **texto** (template). Em produção o esperado é JSON. | F5 |
| Testes AAA (xUnit) | 🟢 | 13 classes de teste de unidade + 12 de integração; 256 casos. | — |
| Cobertura de Domínio e Aplicação | 🟡 | `coverlet.collector 6.0.4` instalado nos dois projetos de teste; **nenhuma medição existe** e não há meta declarada. Com as camadas ainda misturadas, "Domínio e Aplicação" nem é mensurável. | F5 (depende de F1) |

## 5. Documentação — 10 pts

| Item | Estado | Evidência | Fase |
|---|---|---|---|
| README completo | 🟡 | 2.126 linhas, tem Arquitetura (l. 243), Testes (l. 638), Rotas (l. 738) e Integrantes (l. 2114). Mas abre com 200 linhas do deploy Azure da Sprint 3 e tem ~800 linhas de guia de testes manuais no meio. | F6 |
| Diagrama de arquitetura | 🟡 | Só existe `docs/arquitetura-azure.svg` (**infra**). Falta o diagrama de camadas da solução. | F6 |
| Swagger exportado/descrito | 🟡 | Rotas descritas no README; não há `openapi.json` exportado. | F6 |

## 6. Onde estão os pontos

| Bloco | Pts | Situação |
|---|---|---|
| Arquitetura e código | 30 | 🔴 F1 + F2 |
| API RESTful | 20 | 🟡 F2 + F3 |
| Persistência | 20 | 🔴 F4 (+ F7 se o professor exigir) |
| Monitoramento e testes | 20 | 🟡 F4 + F5 |
| Documentação | 10 | 🟡 F6 |

**Risco de penalidade:** nenhum, no momento (o projeto compila, há testes e há README).
O risco real é **introduzir** algum durante a F1, que altera todos os arquivos — por isso
cada passo termina com build e testes verdes antes de qualquer commit.
