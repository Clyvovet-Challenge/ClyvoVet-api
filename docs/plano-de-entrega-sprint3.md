# Plano de entrega — Sprint 3 (12/09/2026)

> Redigido em **07/09/2026**, a 5 dias do prazo. Reúne as decisões tomadas numa
> sessão de entrevista sobre a arquitetura, conferidas contra o **documento oficial
> do Challenge** (`2TDS Fevereiro - Challenge 2026 - 2º Semestre.pdf`).
>
> Serve de complemento à [auditoria-de-arquitetura.md](auditoria-de-arquitetura.md),
> que lista os achados desta API. Aqui está definido **o que entra na entrega e em
> qual ordem** — levando em conta a régua de avaliação, algo que a auditoria não
> considerava.

---

## 1. A notícia boa: esta API já atende quase toda a régua dela

A disciplina **Advanced Business Development with .NET** exige exatamente o que
este repositório já entrega. Confirmado no código, não presumido:

| Requisito | Pontos | Estado |
|---|---:|---|
| **Health Checks** com `Microsoft.Extensions.Diagnostics.HealthChecks`, verificando API, banco e serviços externos | 15 | ✅ `/health`, `/health/live`, `/health/ready` com `AddDbContextCheck` |
| **Logging estruturado** com Serilog ou NLog, com níveis e correlação de requisições | 10 | ✅ Serilog com `CorrelationIdMiddleware` e `X-Correlation-Id` — mas ver §2 |
| **Tracing distribuído** com OpenTelemetry ou App Insights, e métricas de desempenho | 15 | ✅ OpenTelemetry com instrumentação de ASP.NET Core, HTTP e EF Core, e exportador Prometheus |
| **Testes unitários xUnit** no padrão AAA, com Moq ou NSubstitute | 20 | ✅ 47 testes, Moq 4.20 |
| **Testes de integração** com `WebApplicationFactory`, validando fluxo HTTP completo, autenticação, sucesso e erro | 15 | ✅ 66 testes |
| **Organização**: projetos separados por camada, nomenclatura `MetodoTestado_Cenario_ResultadoEsperado`, Fixtures | 15 | ✅ `Tests.Unit` e `Tests.Integration` separados, `IntegrationTestFixture`, nomes como `GetAll_SemApiKey_RetornaUnauthorized` |
| **README atualizado**: documentar health checks, como monitorar, como rodar `dotnet test`, descrição geral | 10 | ✅ feito — e o buraco era outro, ver §2.1 |

**Cerca de 90 dos 100 pontos já estão construídos.** O que resta fazer nesta
disciplina se resume, essencialmente, ao README.

Isso muda o posicionamento deste repositório no plano: ele não é o gargalo. O
gargalo é o deploy, entregável da disciplina de **DevOps**, que vive no repositório
Java.

---

## 2.1 O README: o buraco não era o que a régua descrevia

A régua exige um "README atualizado" e vale 10 pontos; a leitura inicial deste
plano assumia que o documento simplesmente não existia. **Ele existia, com 2.024
linhas**, e já cobria os quatro itens exigidos: health checks, como monitorar,
`dotnet test` e a descrição geral. Os 10 pontos não estavam em branco.

O problema era diferente, e mais sério: **boa parte do README descrevia um projeto
que já não é este.** Conferido arquivo por arquivo:

| O que o README afirmava | O que o código faz | Consequência |
|---|---|---|
| Configurar `ConnectionStrings:OracleConnection` | `Program.cs` lê `ConnectionStrings:DefaultConnection` | quem seguisse o Passo 2 terminava com a API sem conexão, e a chave antiga é **ignorada em silêncio** |
| Preparar o banco pelo SQL Developer, rodando `schema/01_*.sql` a `06_*.sql` | esses scripts são Oracle (`VARCHAR2`, `NUMBER`, `fn_clyvo_uuid`) e não rodam no MySQL | o Passo 3 inteiro era impossível de executar |
| "Todo ID sai do Oracle pela função `fn_uuid()` no trigger. O código C# **nunca** gera UUID" | os repositórios fazem `Guid.NewGuid().ToString()`, e o mapeamento é `ValueGeneratedNever()` | a afirmação era o **oposto** do código |
| `Oracle.EntityFrameworkCore` como provider | `Pomelo.EntityFrameworkCore.MySql` 8.0.2 | tabela de tecnologias errada |
| "A API já está publicada e no ar 24/7 no Render" | `clyvovet-api.onrender.com` não responde — a conexão nem se estabelece | link morto anunciado como serviço ativo |
| "Duas APIs independentes — cada uma no seu próprio container Docker — dividem o mesmo banco **Oracle XE**" | as duas são publicadas nativas em App Service, sobre MySQL gerenciado | **contradiz a entrega de DevOps**, onde app em container e banco em container valem −40 cada |
| "103 testes (46 + 57)" | 116 (47 + 69) | contagem desatualizada |
| Passo 5 do deploy: aplicar `schema/script_bd.sql` no banco da nuvem | na Sprint 3 o banco nasce vazio e o Flyway da Java cria o schema | **quebra a API Java**: tabelas sem `flyway_schema_history`, e o Flyway recusa migrar schema não vazio desconhecido |

As duas últimas linhas são as que custam pontos, e nenhuma delas é "documentação
desatualizada" num sentido inofensivo: uma contradiz a arquitetura declarada na
entrega, e a outra é uma instrução que, se seguida, impede a outra API de subir.

A seção de deploy da entrega anterior **não foi removida** — ela registra o vídeo
daquela sprint, e apagá-la equivaleria a reescrever o histórico de outra pessoa. Ela
recebeu um aviso no topo informando que o procedimento vigente está no repositório
da API Java, além de um aviso específico no passo 5.

---

## 2. Um ponto de atenção que a auditoria criou

A auditoria recomendou **remover o sink de arquivo do Serilog** (§2.4), e essa
recomendação foi aplicada. A justificativa é sólida: no App Service o caminho é
efêmero e específico de cada instância — cada réplica grava seu próprio arquivo, e
o conteúdo desaparece a cada restart.

Só que o requisito oficial (p. 7) afirma, textualmente: *"Logging Estruturado — ...
incluindo níveis de log (Information, Warning, Error), correlação de requisições e
saída para **console/arquivo**"*.

A leitura mais natural é "console **ou** arquivo", e o console já atende. Mas são 10
pontos apostados nessa interpretação.

**Resolução recomendada:** reintroduzir o sink de arquivo, porém **condicionado ao
ambiente de desenvolvimento**. Assim ele passa a existir no código — visível para
quem revisa, e demonstrável em execução local — sem rodar no App Service, onde não
teria utilidade. Atende os dois lados, sem forçar uma escolha entre eles.

---

## 3. O papel desta API no deploy

A disciplina de DevOps exige o deploy de **apenas uma** das duas APIs, e a
escolhida foi a Java. Ainda assim, as duas sobem para a nuvem, porque o app móvel
depende de ambas para funcionar de verdade — manter uma local e outra pública
criaria configuração dupla de URL no app, exatamente o tipo de coisa que falha
durante a gravação.

| Recurso | Valor |
|---|---|
| Assinatura | `2tdspw-rm562312-pedrooliveira` — **nova**, não a que hospedou a entrega anterior |
| Região | `brazilsouth` — verificado por CLI que App Service Linux e MySQL Burstable coexistem lá |
| App Service Plan | **B2** Linux, **compartilhado** com a API Java |
| Web App | runtime `DOTNETCORE:8.0` |
| Banco | MySQL Flexible Server `Standard_B1ms`, provisionado **vazio** |
| Instâncias | **uma. Autoscale DESLIGADO** — ver §5 |

O schema segue sendo criado pelo **Flyway do repositório Java**, da V1 à V9, logo no
primeiro boot dele. Esta API não possui migrations, conforme decisão registrada no
ADR-002: migrations EF aqui criariam uma terceira fonte de verdade para o mesmo
banco.

**Consequência de ordem no deploy:** a API Java precisa subir **antes** desta,
senão as seis tabelas `t_clyvo_*` que ela consome ainda não existiriam. No
ambiente local isso é resolvido via `depends_on: service_healthy`; na Azure, depende
da ordem de execução do script.

---

## 4. O que este repositório precisa entregar

Em ordem de prioridade.

1. **README atualizado** — os 10 pontos que faltam nesta disciplina: endpoints de
   health check, como monitorar, `dotnet test`, e a descrição geral com as novas
   funcionalidades.
2. **Sink de arquivo condicionado ao ambiente de desenvolvimento** (§2).
3. **Pacotes vulneráveis** — `Microsoft.OpenApi` 2.4.1 e `Microsoft.Bcl.Memory`
   9.0.0, ambos classificados como gravidade alta (achado §2.10 da auditoria).
   Confirmar antes se o `Swashbuckle.AspNetCore` 10.1.7 é compatível com a versão
   nova do primeiro pacote.
4. **Varredura de segredo exposto no código-fonte** — a régua de DevOps desconta
   **−20** por *"deixar dados sensíveis expostos (usuário, senha e tokens) no
   código fonte"*, e os placeholders do `appsettings.json` precisam ser conferidos
   um a um.
5. **`Castrado` declarado como `TINYINT(1)`** (achado §2.11) — a coluna real é
   `INT`. Isso não quebra nada, mas documenta o tipo errado; vale alinhar com quem
   escreveu.
6. ✅ **JWT compartilhado com a API Java** (§2.1 da auditoria — era a falha mais
   grave). Implementado **antes** do deploy, e não depois, justamente porque foi
   entregue totalmente desligado: as três camadas do servidor já existem, e o
   comportamento com as flags em `false` é idêntico, byte a byte, ao de antes —
   comprovado pelos 69 testes de integração antigos, que não enviam `Authorization`
   e continuam passando sem alteração.

   Cobre `lembretes`, `sugestoes-produto` e `widget-saude-preditiva`. Os dois
   interruptores são app settings, então revertê-los na Azure é um único comando,
   sem redeploy — **primeiro `Api__EscopoPorTutor=false`**, e só depois o segredo;
   fazer na ordem inversa deixaria a API exigindo identidade sem conseguir lê-la, o
   pior estado possível.

   **Ainda falta o lado do app**, e essa parte não é meramente aditiva: o cliente
   `.NET` do app não tem refresh, então enviar o `Bearer` sozinho faria as chamadas
   falharem 15 minutos após o login — e o 401 apareceria como "Serviço de
   lembretes indisponível" em vez de renovar a sessão.
7. **Fora do escopo desta sprint:** pipeline de CI. O documento oficial situa
   CI/CD como requisito da **Sprint 4**, não desta.

---

## 5. Instância única, autoscale desligado

Dois componentes deste repositório impedem o *scale-out*: os dois
`BackgroundService` registrados em `Program.cs`:

| Serviço | O que acontece com 3 réplicas |
|---|---|
| `LembreteNotificationService` | o tutor recebe a mesma notificação **3 vezes** |
| `TelegramLinkListenerService` | três `getUpdates` concorrentes; o Telegram entrega cada update a um consumidor e o comportamento fica não determinístico |

Com uma única instância, nenhum dos dois representa problema — e essa é a
configuração correta para a entrega. A correção definitiva passaria por eleição de
líder, fila com consumidor único, ou migração para Azure Function com timer.
Nenhuma delas está prevista para esta sprint.

Este registro é deliberadamente explícito porque o sintoma de um erro aqui **não
aparece em log de erro**: aparece como o tutor recebendo a mesma notificação três
vezes.

---

## 6. O que **não** fazer

- **Não** criar migrations EF nesta API. Isso reintroduziria a terceira fonte de
  verdade que a V8 do repositório Java eliminou. Antes dela, as tabelas `t_clyvo_*`
  nasciam de SQL avulso e **não existiriam na nuvem**, já que no App Service só o
  Flyway é executado.
- **Não** voltar a mapear `t_clyvo_animal` / `t_clyvo_tutor` como entidades
  próprias. Era exatamente isso que fazia o `animalId` devolvido pela Java deixar
  de existir para esta API.
- **Não** passar a escrever em `t_clyvo_animal` ou `t_clyvo_tutor`. É a única
  mudança capaz de gerar conflito de escrita num banco que hoje não tem nenhum, e
  derrubaria o ADR-001.
- **Não** uniformizar o tipo booleano entre as duas partes do schema: `TINYINT` nas
  tabelas `t_clyvo_*` de conteúdo desta API, já que o Pomelo mapeia `bool` para
  `tinyint(1)`; **`INT`** nas colunas booleanas escritas pela API Java, pois o
  `NumericBooleanConverter` entrega `Integer` ao JDBC e o `ddl-auto=validate`
  rejeita `TINYINT` contra `INTEGER`. Cada tabela acompanha o ORM que a usa.
- **Não** containerizar o app nem o banco. Na Opção 2 escolhida pela disciplina de
  DevOps, cada um custa **−40**.
- **Não** montar o pipeline de CI/CD agora. Isso é assunto da Sprint 4.

---

## 7. Onde estão as outras peças

| Repositório | Documento |
|---|---|
| `clyvovet-backend-java` | `docs/12-plano-de-entrega-sprint3.md` — o plano completo, com a régua de DevOps, os scripts `az` e o cronograma |
| `2tdspw-challenge-clyvovet-challenge` | `spec/12-auditoria-de-arquitetura.md` — o recorte do app móvel |

O cronograma se aplica aos três repositórios, e a data decisiva é **09/09**: se o
deploy não estiver funcionando e verificado até então, não sobrará tempo para
gravar o vídeo de DevOps, que vale 80 dos 100 pontos daquela disciplina.
