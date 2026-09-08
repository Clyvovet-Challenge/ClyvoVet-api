# Auditoria de arquitetura — o que esta API precisa corrigir

> Levantado em **06/09/2026** sobre o código, `Program.cs`, configurations do EF,
> Dockerfile e scripts `azure/` — não sobre o README. Relatório completo dos dois
> backends, com comparação de arquiteturas e matriz de riscos:
> [claude.ai/code/artifact/79da68b6-2d7d-41b1-82c2-aac5fc68652a](https://claude.ai/code/artifact/79da68b6-2d7d-41b1-82c2-aac5fc68652a)
>
> Este arquivo é o recorte do que **esta API** é dona. O recorte da API Java está
> em `clyvovet-backend-java/docs/11-auditoria-de-arquitetura.md`.

---

## 1. A decisão de arquitetura

**Banco compartilhado com a API Java, que é a dona do schema.**

A decisão se apoia num fato do código, não em preferência:

> **Cada tabela tem exatamente um escritor.**
> Esta API lê `animal` e `tutor` e nunca escreve nelas — `AnimalRepository.cs`
> expõe apenas `GetByIdAsync` e `GetByTutorIdAsync`, e não há nenhuma escrita nos
> `DbSet<Animal>` / `DbSet<Tutor>` em todo o projeto.

Por isso o risco clássico de banco compartilhado — dois serviços disputando a
mesma linha — não se aplica aqui. As alternativas (consumir a Java por HTTP, ou
bancos separados com mensageria) foram avaliadas e descartadas: exigiriam cliente
HTTP com retry e circuit breaker, ou replicação de dados que esta API não é dona,
e nenhuma das duas coisas existe hoje.

### Divisão de propriedade

| | |
|---|---|
| **Esta API é dona do conteúdo de** | `t_clyvo_produto`, `t_clyvo_sugestao_produto`, `t_clyvo_lembrete`, `t_clyvo_evento_pet`, `t_clyvo_predisposicao_saude`, `t_clyvo_tutor_telegram` |
| **Lê, nunca escreve** | `animal`, `tutor` |
| **Não toca** | `usuario`, `autorizacao_acesso`, `acesso_historico` e o resto do núcleo clínico |
| **Define o schema** | A API Java, via Flyway. Desde a `V8__tabelas_dotnet.sql`, isso inclui as seis tabelas acima |

**O que isso muda na prática:** as tabelas desta API continuam sendo dela — o que
mudou é onde a **definição** vive. O provisionamento passa a ter um caminho só.

---

## 2. Achados desta API

Ordenados por gravidade. Cada um cita o arquivo que justifica a conclusão.

### 2.1 ✅ Não havia autorização por usuário — CORRIGIDO, e desligável

`Filters/ApiKeyFilterAttribute.cs` compara o header `X-Api-Key` com `Api:ApiKey`.
Não existe usuário, papel, claim ou token em nenhum ponto da API.

Em `Controllers/LembreteController.cs:33`, `animalId` é **filtro opcional de
query** (`[FromQuery] string? animalId = null`), não escopo de autorização.

> Quem tiver a chave lê, edita e apaga **os lembretes de todos os animais de todos
> os tutores**.

**Agravante:** no app móvel a chave chega por `EXPO_PUBLIC_DOTNET_API_KEY`.
Variáveis `EXPO_PUBLIC_*` são embutidas no bundle JavaScript em tempo de build e
são extraíveis de qualquer APK. Na prática, **a chave é pública**.

**Corrigido em três camadas, todas desligáveis por app setting.** A chave estática
virou defesa em profundidade em vez de ser a única barreira.

| Camada | Onde | Padrão | Interruptor |
|---|---|---|---|
| claim `tutorId` no access token | API Java | ligada, aditiva | — |
| validar o token e identificar | aqui | **inerte** | `Jwt__Secret` ausente |
| recortar por dono | aqui | **desligada** | `Api__EscopoPorTutor=false` |

Cobre `lembretes`, `sugestoes-produto` e `widget-saude-preditiva`: listagem
recortada pelo tutor, 404 em recurso de outro tutor, e escrita bloqueada em animal
alheio — inclusive o `POST`, que é o único endpoint que **agenda uma notificação**.

**A correção proposta acima estava certa no destino e errada em quatro detalhes**,
todos levantados por um painel de revisão antes de virar código. Vale o registro
porque nenhum deles daria erro de compilação:

1. **`AddJwtBearer()` não foi usado.** Ele traria o modelo de `[Authorize]` e
   `FallbackPolicy` junto, e uma policy global derrubaria `/health`, `/metrics`,
   `/swagger` e os webhooks de uma vez — health check quebrado tira a aplicação de
   rotação no App Service. Em vez dele, `System.IdentityModel.Tokens.Jwt`, que
   **já estava no grafo** (o Twilio o traz em 8.3.1) e só lê o token. Há três
   testes provando que as rotas de infraestrutura seguem respondendo 200 sem token
   com o recorte ligado.

2. **A chave sai do base64 decodificado, não dos bytes da string.** A Java faz
   `Keys.hmacShaKeyFor(Decoders.BASE64.decode(segredo))`. O idioma de todo tutorial
   ASP.NET é `Encoding.UTF8.GetBytes(segredo)` — com o **mesmo valor** de
   configuração, a chave é **outra**, e o sintoma seria 401 em cem por cento das
   chamadas, sem nada no log. Os dois lados travam o contrato por teste.

3. **Só access token.** A Java gera access e refresh pelo mesmo método — mesma
   chave, mesmo formato, muda a claim `tipo` e a validade. Sem checá-la, o refresh
   de **sete dias**, que fica em disco no aparelho, viraria credencial válida aqui.

4. **`tutorId` nulo nega, nunca "passa sem filtro".** ADMIN e VETERINARIO não têm
   tutor. O `if (tutorId != null) query.Where(...)` devolveria a base inteira
   justamente para os perfis mais poderosos.

**Key Vault não entrou.** O segredo chega como App Setting, que já o mantém fora do
código-fonte; Key Vault não é exigido por nenhuma das duas disciplinas e cada
recurso a mais é um recurso a explicar na avaliação oral.

**Impacto fora daqui, ainda em aberto:** o app precisa passar a mandar
`Authorization` para esta API. Não é aditivo — o cliente `.NET` dele não tem
refresh, então mandar o Bearer sem mais nada faria as chamadas falharem 15 minutos
depois do login. Enquanto isso não estiver pronto, `Api__EscopoPorTutor` fica em
`false` e nada muda.

### 2.2 ✅ `schema/script_bd.sql` diverge do schema real — CORRIGIDO

Este arquivo é o entregável de DDL da disciplina e é o que o
`azure/01-criar-recursos.sh` instrui a aplicar. A PARTE 1 dele é **cópia manual**
das migrations da API Java — o próprio README registra que *"mudando o schema de
lá, essa cópia precisa acompanhar."*

Ela não acompanhou. Sete colunas do lado Java mudaram de `TINYINT` para `INT`
(porque o `NumericBooleanConverter` faz o atributo chegar ao JDBC como `Integer`, e
o `ddl-auto=validate` reprova `TINYINT` contra `INTEGER`). O arquivo ainda tem os
`TINYINT` antigos:

| Linha | Coluna |
|---|---|
| 179 | `usuario.ativo` |
| 200 | `servico.ativo` |
| 252 | `alerta_clinico.ativo` |
| ~264 | `animal.castrado` |
| ~267 | `animal.resumo_seguranca_ativo` |
| 303 | `acesso_historico.nivel` |
| 305 | `acesso_historico.emergencial` |

> Um banco provisionado por este arquivo **impede a API Java de subir**. Esta API
> funciona normalmente — ela não valida schema —, então o problema só aparece
> quando alguém tentar publicar a Java.

**Não mexer nas linhas 388, 399 e 416**: essas são das tabelas `t_clyvo_*`, onde o
Pomelo mapeia `bool` para `tinyint(1)`. Ali `TINYINT` está correto. A divergência
não é de convenção — é que cada tabela segue o ORM que a usa.

**Correção, duas opções:**

1. **Trocar as sete linhas para `INT`.** Processo idêntico, mesmo comando, o vídeo
   de entrega continua fiel. É a opção mínima.
2. ~~**Substituir o arquivo pelo `documentos/script_bd.sql` gerado pela API Java.**~~
   **Esta recomendação estava errada e é retirada.** Aquele arquivo é gerado a
   partir de `db/migration/oracle/`, então é DDL **Oracle** — `VARCHAR2`, sem
   `ENGINE=InnoDB`. O banco desta API é MySQL. Substituir direto produziria um
   script que não roda.
   A versão correta da ideia é gerar um equivalente MySQL a partir de
   `db/migration/mysql/`, e está registrada abaixo como pendência.

**Pendência separada:** o `script_bd.sql` cria as tabelas sem popular
`flyway_schema_history`. Se a API Java for publicada contra esse mesmo banco, o
Flyway vê banco populado sem histórico e a V1 falha. Resolve-se com
`baseline-on-migrate=true` e `baseline-version=8` no perfil da Java, ou
provisionando o banco pelo Flyway desde o início. Não é urgente, mas precisa estar
decidido antes do deploy da Java.

### 2.3 🔴 Dois `BackgroundService` rodam em toda instância

`Program.cs:155-156` registra `TelegramLinkListenerService` e
`LembreteNotificationService` incondicionalmente fora do ambiente `Testing`.

| Serviço | O que acontece com 3 réplicas |
|---|---|
| `LembreteNotificationService` | o tutor recebe a mesma notificação **3 vezes** |
| `TelegramLinkListenerService` | três `getUpdates` concorrentes; a API do Telegram entrega cada update a um consumidor e o comportamento fica não determinístico |

**Correção:** eleição de líder, ou mover para uma fila com consumidor único, ou
extrair para um Azure Function com timer.

**Para a entrega:** manter o App Service em **instância única** elimina o problema
inteiro. É a decisão certa para o prazo.

### 2.4 ✅ Serilog grava em disco local — CORRIGIDO

`Program.cs:38` — `.WriteTo.File("Logs/clyvovet-api-.log")`.

No App Service esse caminho é efêmero e por instância: cada réplica escreve seu
próprio arquivo, ninguém os agrega, e o conteúdo some no próximo restart. O sink
de console já existe e é o que o App Service captura.

**Correção:** remover o sink de arquivo, ou condicioná-lo a desenvolvimento. Uma
linha. É a única dependência de armazenamento local em qualquer das duas APIs.

### 2.5 ✅ Connection pool no padrão do driver — CORRIGIDO

A connection string não traz parâmetro de pool. Vale o padrão do MySqlConnector:
**100 conexões por instância**.

A API Java opera com o padrão do HikariCP, **10**. Esta API pode abrir **dez vezes
mais conexões** — sendo a que tem menos endpoints (24 contra 74) e menos tráfego.
Não é dimensionamento, é o padrão que ninguém tocou.

**Correção:** `Maximum Pool Size=15` na connection string, e o equivalente do lado
Java. Confirmar o teto real com `SHOW VARIABLES LIKE 'max_connections'` antes de
escalar — o servidor é `Standard_B1ms`, tier Burstable.

### 2.6 ✅ CORS não configurado — CORRIGIDO

Não há `AddCors` nem `UseCors` em `Program.cs`. A API Java configura por
`CLYVOVET_CORS_ORIGENS`.

Não afeta o app nativo, que não faz CORS. Afeta o Expo web, se ele for
demonstrado.

**Correção:** espelhar o desenho da Java — origens por variável de ambiente, nunca
`AllowAnyOrigin` junto de credenciais.

### 2.7 ✅ `DbSet` mortos apontando para tabelas inexistentes — CORRIGIDO

`AppDbContext` declara `DbSet<Veterinario>` e `DbSet<Consulta>`, mapeados em
`VeterinarioConfiguration.cs` e `ConsultaConfiguration.cs` para
`t_clyvo_veterinario` e `t_clyvo_consulta` — tabelas que **nenhum script cria**.

São inertes: nenhum arquivo em `Repositories/`, `Services/` ou `Controllers/` os
referencia, e sem migrations o EF nunca valida o modelo contra o banco. Só mordem
se alguém rodar `dotnet ef`.

**Correção:** remover as entidades, as configurations e os `DbSet`. Faxina, não
bug.

### 2.8 🟢 Sem pipeline de CI

Não há `.github/workflows`, `azure-pipelines.yml` nem equivalente. Os testes de
integração — que valem 50 pontos na disciplina — **nunca rodam automaticamente**.
O deploy é manual via `azure/03-deploy.sh`.

**Correção:** workflow que roda `dotnet test` em cada push. O `IntegrationTestFixture`
usa EF InMemory e semeia sozinho, então não precisa de banco no CI.

### 2.9 🟢 Comparação de chave não é de tempo constante

`ApiKeyFilterAttribute.cs:17` usa `!=` entre strings. Tecnicamente suscetível a
timing attack.

Severidade baixa e cai para irrelevante depois de §2.1, porque a chave deixa de
ser o único mecanismo. Registrado por completude.

---

### 2.10 🟠 `Microsoft.OpenApi` 2.4.1 tem vulnerabilidade conhecida

Achado novo, encontrado ao compilar — não estava na auditoria original porque ela
leu o código, não o resultado do build.

São **dois** pacotes, e `dotnet list package --vulnerable --include-transitive`
mostra os dois:

| Pacote | Versão | Gravidade | Como chega |
|---|---|---|---|
| `Microsoft.OpenApi` | 2.4.1 | **Alta** — [GHSA-v5pm-xwqc-g5wc](https://github.com/advisories/GHSA-v5pm-xwqc-g5wc) | direto, `ClyvoVet.Api.csproj:36` |
| `Microsoft.Bcl.Memory` | 9.0.0 | **Alta** — [GHSA-73j8-2gch-69rq](https://github.com/advisories/GHSA-73j8-2gch-69rq) | **transitivo** |

O `dotnet build` só avisa do primeiro (`NU1903`); o transitivo aparece apenas com
`--include-transitive`, e é por isso que ele passou despercebido.

**Correção:** subir `Microsoft.OpenApi`, confirmando antes que o
`Swashbuckle.AspNetCore` 10.1.7 aceita a versão nova — os dois andam juntos e o
Swagger é entregável da disciplina. O `Microsoft.Bcl.Memory` sai por quem o traz;
descobrir com `dotnet nuget why`.

**Vale rodar isto no CI (§2.8)**, quando ele existir: `dotnet list package
--vulnerable --include-transitive` falhando o build é barato e pega a próxima.

### 2.11 🟢 `Castrado` declara `TINYINT(1)`, mas a coluna é `INT`

`AnimalConfiguration.cs` declara `.HasColumnType("TINYINT(1)")` para `Castrado`.
A coluna real, do lado da API Java, é **`INT`** — e precisa ser: o
`NumericBooleanConverter` do JPA entrega `Integer` ao JDBC, e o `ddl-auto=validate`
reprova `TINYINT` contra `INTEGER`, impedindo aquela API de subir.

**Não quebra nada hoje**, e isso foi verificado: com o mapeamento em `TINYINT(1)`
contra a coluna `INT`, `GET /widget-saude-preditiva/{id}` — que carrega a entidade
`Animal` inteira — responde **200**. O MySqlConnector converte o inteiro devolvido
para `bool?` sem reclamar, e esta API só **lê** essa coluna.

O problema é de outra natureza: o mapeamento **documenta um tipo que a coluna não
tem**, e reintroduz exatamente a confusão `TINYINT`/`INT` que custou uma rodada de
diagnóstico do lado Java. Quem ler este arquivo para descobrir o tipo da coluna vai
ler errado.

**Correção:** remover o `.HasColumnType("TINYINT(1)")` e deixar o Pomelo inferir,
como estava, ou declarar `INT`. É decisão de quem escreveu — vale alinhar antes de
mexer.

> A regra que evita a próxima: **`TINYINT` nas tabelas `t_clyvo_*` de conteúdo
> desta API, `INT` nas colunas booleanas que a API Java escreve.** Cada tabela
> segue o ORM que a usa; `animal` é da Java.

---

## 3. O que já foi corrigido

| Correção | Commit |
|---|---|
| `ToTable` apontava para `animal` e `tutor`, renomeadas pela V9 do repo Java. A API subia e falhava só na consulta, porque o EF não valida schema no boot | `fix(ef): acompanha o rename das tabelas do schema compartilhado` |
| Entidades mortas `Veterinario`/`Consulta`: eram inertes até a V9 criar `t_clyvo_veterinario` de verdade, com outro formato (§2.7) | `refactor: remove as entidades mortas Veterinario e Consulta` |
| `script_bd.sql` com 7 colunas `TINYINT` e 13 tabelas sem prefixo — provisionaria um banco onde a API Java não sobe (§2.2) | `fix(schema): alinha o script_bd.sql ao schema real da API Java` |
| Sink de arquivo do Serilog (§2.4), pool no padrão de 100 (§2.5) e ausência de CORS (§2.6) | `fix(program): sink de disco, teto de pool e CORS` |

### Pendência que ficou desta rodada

O `schema/script_bd.sql` continua sendo **cópia manual** do schema da API Java, e
já defasou duas vezes. A correção durável é gerar um `script_bd_mysql.sql` a
partir de `db/migration/mysql/` — o `scripts/gerar-script-bd.py` do repositório
Java já faz isso para o Oracle e precisaria de uma variante.

O que impede a substituição direta hoje: este arquivo tem um bloco
`DROP TABLE IF EXISTS` no topo que o torna re-executável, e um script gerado por
concatenação de migrations não tem isso. O gerador precisaria emitir o preâmbulo
de limpeza para o arquivo servir ao mesmo propósito no vídeo de entrega.

---

## 4. Efeito colateral que precisa de dono

`t_clyvo_lembrete.animal_id` e `t_clyvo_sugestao_produto.animal_id` referenciam
`animal(id)` **sem** `ON DELETE CASCADE` — decisão deliberada da V8: apagar um
animal e apagar em silêncio o que esta API gravou sobre ele seria decisão dela,
não da migration.

Consequência: `DELETE /animais/{id}` na API Java falha com erro de integridade
enquanto existir lembrete daquele animal, e a Java **não sabe dizer** o que travou,
porque desconhece a tabela.

**Precisa de decisão entre as duas equipes:**

- a Java devolve 409 com mensagem útil; ou
- esta API expõe um endpoint de limpeza que o fluxo de exclusão chama antes; ou
- a FK ganha `ON DELETE CASCADE`, aceitando a exclusão em cascata.

---

## 5. Ordem sugerida

| # | O quê | Estado |
|---|---|---|
| 1 | Alinhar `script_bd.sql` (§2.2) | ✅ feito |
| 2 | Remover sink de arquivo (§2.4) | ✅ feito, e depois **condicionado a `Development`** — ver §2 do plano de entrega |
| 3 | Pool em 15 (§2.5) | ✅ feito |
| 4 | CORS (§2.6) | ✅ feito |
| 5 | Remover `DbSet` mortos (§2.7) | ✅ feito |
| 6 | Manter App Service em **instância única** (§2.3) | **decisão de configuração**, não código — resolve o achado inteiro |
| 7 | Subir `Microsoft.OpenApi` (§2.10) | ✅ feito — 2.4.1 → 2.12.2, e `Microsoft.Bcl.Memory` fixado em 9.0.19. `dotnet list package --vulnerable --include-transitive` volta limpo nos três projetos |
| 8 | Pipeline de CI (§2.8) | fora de escopo — o documento oficial coloca CI/CD na **Sprint 4** |
| 9 | JWT compartilhado (§2.1) | ✅ feito nas três camadas de servidor, todas desligáveis por app setting. Falta só o app mandar o `Bearer` — ver §2.1 |
| 10 | Tempo constante na chave (§2.9) | ✅ feito — `CryptographicOperations.FixedTimeEquals`, mais falha fechada quando `Api__ApiKey` não está configurada. Coberto por `ApiKeyFilterAttributeTests` |

**O que sobrou é de dois tipos.** O 6 se resolve provisionando certo. O 9 é o único
que exige coordenação entre os três repositórios, e o caminho natural é fazê-lo
junto com o deploy, quando o Key Vault existir para guardar o segredo compartilhado.

---

## 6. O que **não** fazer

Caminhos plausíveis que a auditoria descartou com base no código:

- **Não** criar migrations EF nesta API. Introduziria uma terceira fonte de
  verdade para o mesmo schema — o problema que a V8 acabou de resolver.
- **Não** voltar a mapear `t_clyvo_animal` / `t_clyvo_tutor` próprias. Era isso que
  fazia o `animalId` devolvido pela Java não existir para esta API.
- **Não** passar a escrever em `animal` ou `tutor`. É a única mudança capaz de
  introduzir conflito de escrita num banco que hoje não tem nenhum.
