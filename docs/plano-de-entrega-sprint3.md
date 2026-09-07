# Plano de entrega — Sprint 3 (12/09/2026)

> Escrito em **07/09/2026**, faltando 5 dias. Consolida as decisões de uma sessão de
> entrevista sobre a arquitetura, revisadas contra o **documento oficial do
> Challenge** (`2TDS Fevereiro - Challenge 2026 - 2º Semestre.pdf`).
>
> Complementa a [auditoria-de-arquitetura.md](auditoria-de-arquitetura.md), que
> lista os achados desta API. Este documento diz **o que entra na entrega e em que
> ordem** — considerando a régua de avaliação, que a auditoria não considerava.

---

## 1. A notícia boa: esta API já atende quase toda a régua dela

A disciplina **Advanced Business Development with .NET** pede exatamente o que este
repositório já tem. Verificado no código, não presumido:

| Requisito | Pontos | Estado |
|---|---:|---|
| **Health Checks** com `Microsoft.Extensions.Diagnostics.HealthChecks`, verificando API, banco e serviços externos | 15 | ✅ `/health`, `/health/live`, `/health/ready` com `AddDbContextCheck` |
| **Logging estruturado** com Serilog ou NLog, com níveis e correlação de requisições | 10 | ✅ Serilog com `CorrelationIdMiddleware` e `X-Correlation-Id` — mas ver §2 |
| **Tracing distribuído** com OpenTelemetry ou App Insights, e métricas de desempenho | 15 | ✅ OpenTelemetry com instrumentação de ASP.NET Core, HTTP e EF Core, e exportador Prometheus |
| **Testes unitários xUnit** no padrão AAA, com Moq ou NSubstitute | 20 | ✅ 47 testes, Moq 4.20 |
| **Testes de integração** com `WebApplicationFactory`, validando fluxo HTTP completo, autenticação, sucesso e erro | 15 | ✅ 66 testes |
| **Organização**: projetos separados por camada, nomenclatura `MetodoTestado_Cenario_ResultadoEsperado`, Fixtures | 15 | ✅ `Tests.Unit` e `Tests.Integration` separados, `IntegrationTestFixture`, nomes como `GetAll_SemApiKey_RetornaUnauthorized` |
| **README atualizado**: documentar health checks, como monitorar, como rodar `dotnet test`, descrição geral | 10 | ⬜ **é o que falta** |

**São ~90 dos 100 pontos já construídos.** O trabalho restante desta disciplina é
essencialmente o README.

Isso reposiciona este repositório no plano: ele não é o gargalo. O gargalo é o
deploy, que é entregável da disciplina de **DevOps** e vive no repositório Java.

---

## 2. Um ponto de atenção que a auditoria criou

A auditoria recomendou **remover o sink de arquivo do Serilog** (§2.4), e a
recomendação foi aplicada. O motivo é sólido: no App Service o caminho é efêmero e
por instância, cada réplica escreve o seu próprio arquivo e o conteúdo some no
restart.

Mas o requisito oficial (p. 7) diz, textualmente: *"Logging Estruturado — ...
incluindo níveis de log (Information, Warning, Error), correlação de requisições e
saída para **console/arquivo**"*.

A leitura natural é "console **ou** arquivo", e o console atende. Mas são 10 pontos
apostados numa leitura.

**Resolução recomendada:** trazer o sink de arquivo de volta **condicionado a
desenvolvimento**. Assim ele existe no código — visível para quem corrige, e
demonstrável rodando localmente — e não roda no App Service, onde não serviria para
nada. Atende os dois lados sem escolher entre eles.

---

## 3. O papel desta API no deploy

A disciplina de DevOps exige o deploy de **uma** das duas APIs, e a escolhida é a
Java. Mas as duas vão para a nuvem, porque o app móvel precisa das duas para
funcionar de verdade — e manter uma local e outra pública cria configuração dupla
de URL no app, que é o tipo de coisa que falha durante a gravação.

| Recurso | Valor |
|---|---|
| Assinatura | `2tdspw-rm562312-pedrooliveira` — **nova**, não a que hospedou a entrega anterior |
| Região | `brazilsouth` — verificado por CLI que App Service Linux e MySQL Burstable coexistem lá |
| App Service Plan | **B2** Linux, **compartilhado** com a API Java |
| Web App | runtime `DOTNETCORE:8.0` |
| Banco | MySQL Flexible Server `Standard_B1ms`, provisionado **vazio** |
| Instâncias | **uma. Autoscale DESLIGADO** — ver §5 |

O schema continua sendo criado pelo **Flyway do repositório Java**, da V1 à V9, no
primeiro boot dele. Esta API não tem migrations, por decisão registrada no ADR-002:
migrations EF aqui seriam uma terceira fonte de verdade para o mesmo banco.

**Consequência de ordem no deploy:** a API Java precisa subir **antes** desta, ou as
seis tabelas `t_clyvo_*` que esta consome ainda não existem. No ambiente local isso
está resolvido por `depends_on: service_healthy`; na Azure, é ordem de execução do
script.

---

## 4. O que este repositório precisa entregar

Em ordem.

1. **README atualizado** — os 10 pontos que faltam da disciplina: endpoints de
   health check, como monitorar, `dotnet test`, e a descrição geral com as
   funcionalidades novas.
2. **Sink de arquivo condicionado a desenvolvimento** (§2).
3. **Pacotes vulneráveis** — `Microsoft.OpenApi` 2.4.1 e `Microsoft.Bcl.Memory`
   9.0.0, ambos de gravidade alta (achado §2.10 da auditoria). Confirmar antes que
   o `Swashbuckle.AspNetCore` 10.1.7 aceita a versão nova do primeiro.
4. **Varredura de segredo exposto no código-fonte** — a régua de DevOps desconta
   **−20** por *"deixar dados sensíveis expostos (usuário, senha e tokens) no
   código fonte"*, e o `appsettings.json` tem placeholders que precisam ser
   conferidos um por um.
5. **`Castrado` declarado como `TINYINT(1)`** (achado §2.11) — a coluna real é
   `INT`. Não quebra, mas documenta o tipo errado. Alinhar com quem escreveu.
6. **Se sobrar tempo:** validar o JWT que a Java emite, e recortar `GET /lembretes`
   pelo tutor autenticado (§2.1 da auditoria — o furo mais grave). Entra **depois**
   do deploy verificado, para que uma quebra tenha causa óbvia.
7. **Fora de escopo nesta sprint:** pipeline de CI. O documento oficial coloca
   CI/CD como requisito da **Sprint 4**, não desta.

---

## 5. Instância única, autoscale desligado

Dois componentes deste repositório impedem *scale-out*, e são os dois
`BackgroundService` registrados em `Program.cs`:

| Serviço | O que acontece com 3 réplicas |
|---|---|
| `LembreteNotificationService` | o tutor recebe a mesma notificação **3 vezes** |
| `TelegramLinkListenerService` | três `getUpdates` concorrentes; o Telegram entrega cada update a um consumidor e o comportamento fica não determinístico |

Com uma instância, nenhum dos dois é problema — e é a configuração certa para a
entrega. A correção definitiva é eleição de líder, fila com consumidor único, ou
mover para Azure Function com timer. Nenhuma delas nesta sprint.

O registro é explícito porque o sintoma de errar aqui **não aparece em log de
erro**: aparece como tutor recebendo notificação triplicada.

---

## 6. O que **não** fazer

- **Não** criar migrations EF nesta API. Reintroduziria a terceira fonte de verdade
  que a V8 do repositório Java eliminou. Antes dela, as tabelas `t_clyvo_*` nasciam
  de SQL avulso e **não existiriam na nuvem**, porque no App Service só o Flyway
  roda.
- **Não** voltar a mapear `t_clyvo_animal` / `t_clyvo_tutor` próprias. Era isso que
  fazia o `animalId` devolvido pela Java não existir para esta API.
- **Não** passar a escrever em `t_clyvo_animal` ou `t_clyvo_tutor`. É a única
  mudança capaz de introduzir conflito de escrita num banco que hoje não tem
  nenhum, e derrubaria o ADR-001.
- **Não** uniformizar o tipo booleano entre as duas partes do schema. `TINYINT` nas
  tabelas `t_clyvo_*` de conteúdo desta API, porque o Pomelo mapeia `bool` para
  `tinyint(1)`; **`INT`** nas colunas booleanas que a API Java escreve, porque o
  `NumericBooleanConverter` entrega `Integer` ao JDBC e o `ddl-auto=validate`
  reprova `TINYINT` contra `INTEGER`. Cada tabela segue o ORM que a usa.
- **Não** containerizar o app nem o banco. Na Opção 2 escolhida pela disciplina de
  DevOps, cada um vale **−40**.
- **Não** montar pipeline de CI/CD agora. É Sprint 4.

---

## 7. Onde estão as outras peças

| Repositório | Documento |
|---|---|
| `clyvovet-backend-java` | `docs/12-plano-de-entrega-sprint3.md` — o plano completo, com a régua de DevOps, os scripts `az` e o cronograma |
| `2tdspw-challenge-clyvovet-challenge` | `spec/12-auditoria-de-arquitetura.md` — o recorte do app móvel |

O cronograma vale para os três repositórios, e a data que decide tudo é **09/09**:
se o deploy não estiver de pé e verificado até lá, não sobra folga para gravar o
vídeo de DevOps, que vale 80 dos 100 pontos daquela disciplina.
