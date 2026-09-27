# CLAUDE.md — ClyvoVet API (.NET)

Instruções para quem trabalha neste repositório com o Claude Code. Vale para todas as sessões.
Se algo aqui ficar velho, **corrija este arquivo no mesmo commit que mudou a realidade**.

## O que é

API REST de gestão veterinária (ASP.NET Core 8, EF Core, MySQL). É a metade .NET do
ClyvoVet; a metade Java (`clyvovet-backend-java`) é dona do schema e da autenticação.
Disciplina: **Advanced Business Development with .NET** (FIAP, 2TDS).

## Onde estamos: Sprint 4 (branch `sprint-4`)

A Sprint 4 consolida tudo das Sprints 1–3 e é avaliada por um rubric de 100 pontos.
**Antes de mexer em qualquer coisa, leia `docs/sprint4/README.md`** — ele aponta o rubric,
o gap-analysis, o design, o plano em fases e as decisões (ADRs).

Regra de trabalho: **uma fase por vez**, na ordem de `docs/sprint4/03-plano.md`.
Não adiante trabalho de fases futuras, mesmo que pareça óbvio.

## Comandos

```bash
dotnet build ClyvoVet-api.slnx
dotnet test  ClyvoVet-api.slnx
dotnet run --project src/ClyvoVet.Api/ClyvoVet.Api.csproj      # Swagger em /swagger
```

- **Precisa do runtime .NET 8** para rodar os testes de integração. Com só o .NET 10
  instalado, ~67 deles falham com `PipeWriter ... UnflushedBytes` — é o ambiente, não o
  código. Não use `DOTNET_ROLL_FORWARD=Major` para "validar": o resultado engana.
- O **SDK 8 não lê `.slnx`**: quem testa com ele roda cada `.csproj` de teste em vez da
  solução. Os detalhes de ambiente de cada pessoa ficam no `CLAUDE.local.md` (fora do Git).
- Linha de base (review pós-F7, 27/09/2026): **340 unidade + 222 integração = 562, todos verdes**
  (+ 8 testes de Mongo real, pulados sem `MONGO_TEST_URI`).
- `MONGO_TEST_URI=mongodb://localhost:27017 dotnet test …` roda também os testes contra um MongoDB real.
- `scripts/cobertura.sh` mede a cobertura de linhas de Domain + Application e **falha abaixo de 90%**
  (hoje 98,8%). Use `DOTNET=~/.dotnet/dotnet` quando o `dotnet` do PATH não tiver o runtime 8.
- `scripts/exportar-swagger.sh` regera `docs/swagger/openapi-v1.json` a partir da própria API.
  **Rode depois de mudar rota, DTO ou a descrição do Swagger**, e commite o JSON junto.
- `scripts/gerar-script-migrations.sh` regera `schema/ef/migrations-idempotente.sql` a partir das
  migrations. **Rode depois de mudar uma entidade ou config do EF (e criar a migration)**, e commite o SQL junto.
- `test_api.sh` é o roteiro ponta a ponta contra uma API no ar (pede `TOKEN`, `API_KEY` etc.;
  ver o cabeçalho). O ensaio geral e a receita do ambiente estão em `docs/ensaio-geral.md`.
- Segredos locais: `dotnet user-secrets` (o projeto já tem `UserSecretsId`). Em produção,
  variáveis de ambiente do Render. **Nunca** credencial em arquivo versionado.

## Arquitetura (Clean Architecture, 4 projetos)

```
Api ──► Application ──► Domain
 └────► Infrastructure ─► Application
```

| Projeto | Contém | Não pode conhecer |
|---|---|---|
| `ClyvoVet.Domain` | entidades, enums, exceções de negócio | nada (zero pacotes) |
| `ClyvoVet.Application` | serviços (casos de uso), DTOs, **interfaces** de repositório e de serviços externos | EF, MongoDB, ASP.NET, Telegram |
| `ClyvoVet.Infrastructure` | EF Core, repositórios concretos, MongoDB, clientes OCI/Telegram, background services, health checks de dependência | ASP.NET MVC |
| `ClyvoVet.Api` | controllers, middlewares, Swagger, JWT, HATEOAS, tratamento global de exceções, `Program.cs` | EF, MongoDB (só via Infrastructure) |

As regras são **verificadas por teste** (`ArquiteturaTests`). Se o teste reclamar, a
arquitetura está errada — não o teste.

## Convenções

- **Idioma:** nomes de domínio, comentários e mensagens em português (como no código atual).
  Infraestrutura e termos técnicos ficam em inglês (`Repository`, `Handler`, `Middleware`).
- **Comentários explicam o porquê**, não o quê. O código atual tem bons exemplos
  (`Paginacao.cs`, `ChaveDoJwt.cs`): siga esse padrão, sem inflar.
- **Testes:** xUnit, padrão **AAA** (Arrange/Act/Assert), nome `Metodo_Cenario_Resultado`.
  Bug corrigido = teste de regressão junto.
- **Rotas:** `/api/v1/<recurso-no-plural>`; sem verbos na URL.
- **Contrato com o app móvel não quebra:** as respostas de erro mantêm `error` (e
  `referencia` em falha de servidor); as listagens continuam devolvendo **array JSON** por
  padrão. O app é outro repositório e não muda junto.
- Commits em português, no estilo convencional: `feat:`, `fix:`, `refactor:`, `test:`,
  `docs:`, `chore:`. A mensagem **nunca cita o Claude** (ver a seção "Git" abaixo).

## Git — regras do dono do repositório

- **Só commite depois de um "sim" explícito**, e uma mudança lógica por commit ("aos poucos").
- **Nunca** `--amend`, `rebase`, `reset --hard` nem force-push. Erro no commit anterior
  → commit novo. O histórico é append-only, inclusive o que veio antes deste projeto.
- **Push e deploy só quando pedido**, confirmando exatamente o que sobe.
- Não crie branches além de `sprint-4` sem combinar.
- **O nome do Claude não pode aparecer em commit nem em descrição de PR**: nada de
  `Co-Authored-By`, `Generated with`, "feito com o Claude" ou qualquer menção a IA ou
  Anthropic. O autor é quem commita. Se uma ferramenta acrescentar isso sozinha, **não
  commite**: entregue o comando `git commit` pronto para o dono do repositório rodar.

## O que NÃO fazer (decisões já tomadas — cada uma tem motivo em `docs/`)

- **Não escrever em `animal` nem `tutor`.** Esta API só lê; a Java é dona dessas tabelas.
- **A migration cobre só as 7 tabelas que a API grava** (produto, sugestao_produto, lembrete,
  evento_pet, predisposicao_saude, tutor_telegram, parecer_ia). `animal`, `tutor`, `raca` e
  `base_doencas` são da Java e ficam fora com `ExcludeFromMigrations()` (ADR-005).
- **Nada de `Database.Migrate()` no boot.** Banco compartilhado: Flyway da Java primeiro, depois
  `schema/ef/baseline-banco-compartilhado.sql`, depois o script idempotente. O script do EF num
  banco vazio, antes do Flyway, impede a Java de subir.
- **Não usar `FallbackPolicy` global** de autorização: derrubaria `/health`, `/metrics`,
  `/swagger` e os webhooks, e health check quebrado tira a app de rotação no Render.
- **JWT: a chave é o base64 *decodificado* do segredo** (`Convert.FromBase64String`), igual
  à Java. `Encoding.UTF8.GetBytes(segredo)` produz outra chave e daria 401 em tudo, sem log.
- **Só access token vale** (`tipo = access`). O refresh de 7 dias não é credencial aqui.
- **`tutorId` nulo nega, nunca "passa sem filtro"** (ADMIN e VETERINARIO não têm tutor).
- **Recurso de outro tutor responde 404, não 403** — a existência já é informação.
  Exceção de propósito: no Telegram o `tutorId` da rota é o **do próprio chamador** (vincular o
  próprio chat), não um recurso alheio; se não for o dele, a resposta é **403** (`PermiteTutor`).
- **Nunca `AllowAnyOrigin`** no CORS.
- Não mexer em `TINYINT` das tabelas `t_clyvo_*` (Pomelo mapeia `bool` para `tinyint(1)`).

## Mapa de documentação

| Arquivo | Para quê |
|---|---|
| `docs/sprint4/README.md` | Porta de entrada da Sprint 4 |
| `docs/sprint4/00-requisitos.md` | Rubric oficial, literal |
| `docs/sprint4/01-gap-analysis.md` | O que já atende e o que falta, com evidência |
| `docs/sprint4/02-design.md` | Design aprovado |
| `docs/sprint4/03-plano.md` | Fases, tarefas e critérios de pronto |
| `docs/sprint4/decisoes/` | ADRs — por que cada decisão foi tomada |
| `docs/auditoria-de-arquitetura.md` | Auditoria da Sprint 3 (contexto do banco compartilhado) |
| `README.md` | README final da entrega (é entregável, tem rubric próprio) |
