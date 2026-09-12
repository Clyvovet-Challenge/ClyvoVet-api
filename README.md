# ClyvoVet API — .NET

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat&logo=dotnet&logoColor=white)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-8.0-0078D4?style=flat&logo=microsoft&logoColor=white)
![Entity Framework Core](https://img.shields.io/badge/Entity_Framework_Core-8.0-68217A?style=flat&logo=nuget&logoColor=white)
![MySQL](https://img.shields.io/badge/MySQL-8.0-4479A1?style=flat&logo=mysql&logoColor=white)
![Swagger](https://img.shields.io/badge/Swagger-OpenAPI-85EA2D?style=flat&logo=swagger&logoColor=black)
![Serilog](https://img.shields.io/badge/Serilog-Structured_Logging-1B1F26?style=flat)
![OpenTelemetry](https://img.shields.io/badge/OpenTelemetry-Tracing_%26_Metrics-425CC7?style=flat&logo=opentelemetry&logoColor=white)
![xUnit](https://img.shields.io/badge/xUnit-Testes_Automatizados-512BD4?style=flat)

## ☁️ Sprint DevOps Tools & Cloud Computing — Deploy na Azure

> ### ⚠️ Registro da entrega anterior — o procedimento vigente é outro
>
> O passo a passo desta seção foi o da **entrega anterior**, com a infraestrutura
> daquele momento (região `chilecentral`, scripts `azure/01` a `azure/04` deste
> repositório, schema aplicado por `schema/script_bd.sql`). Ele fica registrado
> porque documenta o que o vídeo daquela entrega mostra.
>
> **Para provisionar a infraestrutura da Sprint 3, siga o README do repositório
> [`clyvovet-backend-java`](https://github.com/Clyvovet-Challenge/clyvovet-backend-java),
> não este.** Lá os scripts `azure/00` a `azure/09` sobem, numa região só, o
> Resource Group, o MySQL, o plano compartilhado e **as duas** APIs — esta
> inclusive. Este repositório não provisiona mais nada sozinho.
>
> **Um ponto não é preferência, é quebra:** na Sprint 3 o banco é criado **vazio**,
> e quem cria o schema é o **Flyway da API Java**, no primeiro boot dela. Aplicar
> `schema/script_bd.sql` antes disso deixa o banco com as tabelas mas sem a
> `flyway_schema_history` — e o Flyway recusa migrar um schema não vazio que ele
> não conhece, então a API Java simplesmente não sobe. Ver o passo 5 abaixo.

> Esta seção registra a entrega da disciplina **DevOps Tools & Cloud Computing**: a mesma API (ClyvoVet .NET) apresentada no restante deste README, aqui publicada num **Azure App Service** e ligada a um **Azure Database for MySQL Flexible Server compartilhado com a API Java** do time (Tutor, Animal, Clínica, etc.). O passo a passo a seguir reproduz exatamente o que foi feito no vídeo de entrega.

> 📋 **Roteiro da gravação desta Sprint:** [`docs/roteiro-do-video.md`](docs/roteiro-do-video.md).
> O vídeo abaixo é o da entrega **anterior** e descreve outra infraestrutura.

**🎥 Vídeo de apresentação:** [assista aqui](https://www.youtube.com/watch?v=8R_eru120m8)

**Integrantes:**

| Nome | RM |
|---|---|
| Fabrício Henrique Pereira | RM563237 |
| Henrique Sinkevicius Maran | RM562977 |
| Leonardo José Pereira | RM563065 |
| Miguel Henrique Oliveira Dias | RM565492 |
| Pedro Henrique de Oliveira | RM562312 |

### Descrição da Solução

Construída em ASP.NET Core 8, a ClyvoVet API gerencia o catálogo de produtos/serviços veterinários e também as sugestões de produto feitas para cada animal, duas tabelas ligadas entre si (`t_clyvo_produto` ← `t_clyvo_sugestao_produto`), ambas com CRUD completo. Para esta entrega, ela roda num **Azure App Service** (Linux, sem container) e grava os dados num **Azure Database for MySQL Flexible Server** — o mesmo banco que a API Java do time usa (Tutor, Animal, Clínica, Veterinário, etc.) —, o que deixa o app Mobile do grupo consumir as duas APIs sobre os mesmos dados.

### Benefícios para o Negócio

- **Catálogo centralizado**: preço, categoria e espécie indicada de cada produto/serviço da clínica passam a viver num só lugar, no lugar de planilhas soltas ou anotações em papel.
- **Sugestão de produto rastreável**: toda sugestão feita a um tutor guarda justificativa e data, o que dá à clínica um histórico de recomendações por animal (ex.: antipulgas sugerido, ração indicada).
- **Integração real entre os sistemas do time**: .NET e Java apontam para o mesmo banco, então um animal já cadastrado na API Java pode receber lembretes e sugestões de produto pela API .NET sem precisar de um segundo cadastro.
- **Escalabilidade sem gerenciar servidor**: por rodar em PaaS (App Service + banco gerenciado), a clínica dispensa infraestrutura própria — disponibilidade, backup e patch do banco ficam por conta da Azure.

### Banco de Dados em Nuvem

- **Motor:** MySQL 8.0, via **Azure Database for MySQL Flexible Server** (nada de H2, nada de container).
- **DDL completo:** [`schema/script_bd.sql`](schema/script_bd.sql) traz o schema inteiro (tabelas da API Java somadas às nossas), com colunas, chaves primárias/estrangeiras, comentários e uma carga inicial de dados relevante. **Na Sprint 3 ele é documentação, não ferramenta de provisionamento** — serve para ler e conferir o modelo; quem cria as tabelas no banco de nuvem é o Flyway da API Java. As tabelas `tutor`/`animal`/etc. reproduzem as migrations Flyway reais do repositório da API Java (`clyvovet-backend-java`); mudando o schema de lá, essa cópia precisa acompanhar.
- **Tabelas do CRUD (núcleo da solução, avaliado nesta entrega):** `t_clyvo_produto` e `t_clyvo_sugestao_produto`, ligadas por `produto_id`. Já `tutor` e `animal` são da API Java — entram aqui apenas via FK/JOIN, para leitura, e o .NET nunca escreve nelas.

### Arquitetura escolhida: Opção 2 — App Service + Banco PaaS

Nenhuma parte desta entrega roda em container — nem o app, nem o banco: tudo fica em serviços gerenciados da Azure, provisionados via **Azure CLI**:

| Recurso | Serviço Azure | Criado por |
|---|---|---|
| Grupo de recursos | Resource Group | `azure/01-criar-recursos.sh` |
| Banco de dados | Azure Database for MySQL Flexible Server | `azure/01-criar-recursos.sh` |
| Plano de aplicativo | App Service Plan (Linux, B1) | `azure/02-criar-app-service.sh` |
| Aplicativo web | App Service (.NET 8, runtime nativo, sem container) | `azure/02-criar-app-service.sh` |

![Arquitetura da solução na Azure](docs/arquitetura-azure.svg)

> Documentação técnica complementar em [`docs/`](docs/) — incluindo a
> [auditoria de arquitetura](docs/auditoria-de-arquitetura.md) de 06/09/2026, que
> registra por que o banco é compartilhado com a API Java e o que esta API ainda
> precisa corrigir.

### Pré-requisitos

| Ferramenta | Para que serve |
|---|---|
| [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli) | Criar todos os recursos (obrigatório pelo edital) |
| Conta ativa na Azure (`az login`) | Ter uma subscription onde criar os recursos |
| [.NET SDK 8](https://dotnet.microsoft.com/download/dotnet/8.0) | `dotnet publish` local antes do deploy |
| Cliente `mysql` (MySQL Shell ou `mysql-client`) | Aplicar `schema/script_bd.sql` no banco recém-criado |
| Git | Clonar o repositório |

### Passo a passo — do zero até a API rodando na Azure

**1. Clonar o repositório**

```bash
git clone https://github.com/Clyvovet-Challenge/ClyvoVet-api.git
cd ClyvoVet-api
git checkout devops-sprint3-azure
```

**2. Login na Azure**

```bash
az login
```

**3. Ajustar as variáveis (se necessário)**

Abra `azure/00-variaveis.sh` e confira/ajuste `SUBSCRIPTION`, os nomes de recursos (precisam ser únicos em toda a Azure) e a região (`LOCATION`). Nesta entrega usamos `chilecentral` — certas assinaturas acadêmicas bloqueiam outras regiões via Azure Policy (erro `RequestDisallowedByAzure`), e mesmo dentro das regiões liberadas o MySQL Flexible Server às vezes devolve `InternalServerError` (instabilidade pontual do serviço, não da conta — já vimos isso acontecer em `brazilsouth`, `eastus2`, `southcentralus` e `canadacentral`); nesse caso, tente outra região liberada na sua assinatura.

**4. Criar o Resource Group + banco MySQL**

```bash
export MYSQL_PASSWORD='DefinaUmaSenhaForte123!'
bash azure/01-criar-recursos.sh
```

O comando provisiona o Resource Group, o servidor MySQL Flexible Server e o banco `clyvovet`, além de liberar seu IP atual no firewall.

**5. Aplicar o schema no banco**

> ⚠️ **Este passo não vale para a Sprint 3.** Lá o banco nasce vazio e o schema é
> criado pelo Flyway da API Java. Rodar o comando abaixo contra o banco da Sprint 3
> impede a API Java de subir, pelo motivo explicado no início desta seção.

```bash
mysql -h <MYSQL_SERVER>.mysql.database.azure.com -u clyvovetadmin -p$MYSQL_PASSWORD --ssl-mode=REQUIRED clyvovet < schema/script_bd.sql
```

(o script `01` já imprime esse mesmo comando, com os valores corretos, ao final da execução)

**6. Criar o App Service e configurar os segredos**

```bash
export API_KEY='GereUmaChaveAleatoria'
export TELEGRAM_BOT_TOKEN='...' TELEGRAM_API_KEY='...' TELEGRAM_BOT_USERNAME='...'
# OCI Generative AI (saude preditiva). Opcional: sem estas variaveis a API
# responde pelo fallback deterministico.
export OCI_TENANCY_OCID='...' OCI_USER_OCID='...' OCI_FINGERPRINT='...'
export OCI_PRIVATE_KEY_PEM="$(cat ~/.oci/clyvovet_api_key.pem)"
export OCI_REGION='us-chicago-1' OCI_COMPARTMENT_OCID='...'
bash azure/02-criar-app-service.sh
```

Todos os segredos entram como **App Settings** do App Service; em nenhum momento ficam gravados no código-fonte.

**7. Publicar o app**

Todo script carrega `00-variaveis.sh`, e esse arquivo exige `MYSQL_PASSWORD` no ambiente mesmo quando o script não toca no banco — abrindo um terminal novo depois do passo 4, exporte a senha outra vez antes de rodar:

```bash
export MYSQL_PASSWORD='DefinaUmaSenhaForte123!'
bash azure/03-deploy.sh
```

O script executa `dotnet publish`, compacta o resultado em zip e publica com `az webapp deploy`, exibindo a URL da API ao terminar.

**8. Validar**

```bash
curl https://<APP_NAME>.azurewebsites.net/health
```

A resposta esperada traz `"status": "Healthy"` em cada verificação.

### Testando o CRUD

Acesse `https://<APP_NAME>.azurewebsites.net/swagger`, clique em **Authorize** e informe a `Api__ApiKey` configurada no passo 6.

| Operação | Endpoint | Tabela afetada |
|---|---|---|
| Consultar | `GET /api/v1/produtos` | `t_clyvo_produto` |
| Inserir | `POST /api/v1/produtos` | `t_clyvo_produto` |
| Atualizar | `PUT /api/v1/produtos/{id}` | `t_clyvo_produto` |
| Excluir | `DELETE /api/v1/produtos/{id}` | `t_clyvo_produto` |
| Consultar | `GET /api/v1/sugestoes-produto` | `t_clyvo_sugestao_produto` |
| Inserir | `POST /api/v1/sugestoes-produto` | `t_clyvo_sugestao_produto` |
| Atualizar | `PUT /api/v1/sugestoes-produto/{id}` | `t_clyvo_sugestao_produto` |
| Excluir | `DELETE /api/v1/sugestoes-produto/{id}` | `t_clyvo_sugestao_produto` |

Para confirmar cada operação direto no banco (como pedido no vídeo), abra um shell interativo via `mysql` (o mesmo comando do passo 5, tirando o `< schema/script_bd.sql`) e execute:

```sql
SELECT * FROM t_clyvo_produto ORDER BY criado_em DESC;
SELECT * FROM t_clyvo_sugestao_produto ORDER BY criado_em DESC;
```

### Removendo os recursos (depois da correção)

```bash
export MYSQL_PASSWORD='DefinaUmaSenhaForte123!'   # mesma observação do passo 7
bash azure/04-destruir-recursos.sh
```

O script remove o Resource Group inteiro, com tudo o que há dentro dele. **Atenção:** caso o banco já esteja de fato compartilhado com a API Java em produção, alinhe com o time antes de executar — ele apaga o banco de todo mundo, não só o nosso.

---

## 🌐 API em produção

A hospedagem da Sprint 3 é **Azure App Service**, provisionada pelos scripts do
repositório da API Java (`azure/05-webapp-dotnet.sh` e `azure/08-deploy-dotnet.sh`).
Os endereços que o deploy publica:

- **Base URL:** `https://app-clyvovet-dotnet-rm562312.azurewebsites.net`
- **Swagger:** `.../swagger`
- **Health Check:** `.../health`, `.../health/live`, `.../health/ready`

> **Os links viram clicáveis quando o deploy da Sprint 3 rodar** — enquanto isso,
> os endereços acima são o destino, não um serviço no ar.
>
> A instância anterior ficava no plano Free do **Render**, em
> `clyvovet-api.onrender.com`. Ela **não responde mais** (verificado: a conexão nem
> se estabelece), e por isso saiu daqui em vez de continuar anunciada como "no ar
> 24/7" — README que aponta para URL morta custa mais do que README sem URL.

---

## Sobre o Projeto

A **ClyvoVet API** é uma API RESTful feita em **ASP.NET Core 8**, criada dentro do **Challenge FIAP 2026 — projeto Clyvo Vet**. Dentro da plataforma veterinária, ela cobre o **domínio de engajamento**, cuidando de:

- Catálogo de produtos e serviços veterinários
- Sugestões personalizadas de produtos por animal
- Lembretes de saúde e cuidados para tutores
- Eventos pet públicos (campanhas de vacinação, feiras, workshops)
- **Saúde Preditiva com IA generativa** — parecer de riscos e recomendações por animal, redigido pela **OCI Generative AI** sobre uma base agregada de doenças por espécie/raça (datasets Dryad com DOI), com fallback determinístico e cache por animal
- **Widget de Saúde Preditiva** (por regras) — o antecessor, mantido no ar: aponta condições relevantes para a espécie/raça/idade
- **Envio de mensagens no Telegram** — bot próprio; é o canal único de mensagem (lembretes e saúde preditiva). O WhatsApp/Twilio saiu do escopo na Sprint 3

A **Sprint 3** somou à API uma camada completa de observabilidade e testes automatizados:

- **Health Checks** (`/health`, `/health/live`, `/health/ready`) que checam se a conexão com o **MySQL** está realmente funcionando.
- **Logging estruturado** via Serilog (console sempre; arquivo em desenvolvimento), correlacionando requisições através do header `X-Correlation-Id`.
- **Distributed tracing e métricas** com OpenTelemetry (spans exportados no console e endpoint `/metrics` em formato Prometheus).
- **124 testes automatizados** (55 unitários + 69 de integração), abrangendo a camada de Aplicação (Services), o filtro de API Key, o fluxo HTTP completo (Controllers → banco em memória) e a geração do documento OpenAPI.

---

## Arquitetura

Duas APIs independentes dividem **um único banco MySQL**, e nenhuma das duas roda
em container: as duas são publicadas como aplicação nativa em **Azure App Service
Linux**, sobre um **Azure Database for MySQL Flexible Server** gerenciado. É a
Opção 2 exigida pela disciplina de DevOps, onde app em container e banco em
container são penalizados. (O `Dockerfile` na raiz continua servindo ao
desenvolvimento local; ele não produz o artefato publicado.)

| API | Responsabilidade | Tabelas gerenciadas |
|-----|-----------------|---------------------|
| **.NET (este projeto)** | Engajamento e catálogo | `t_clyvo_produto`, `t_clyvo_sugestao_produto`, `t_clyvo_lembrete`, `t_clyvo_evento_pet`, `t_clyvo_predisposicao_saude` |
| **Java (parceira)** | Clínica e cadastro | `t_clyvo_tutor`, `t_clyvo_animal`, `t_clyvo_clinica`, `t_clyvo_veterinario`, `t_clyvo_evento_clinico`, `t_clyvo_pagamento` |

> A API .NET **lê** as tabelas de animal e tutor da API Java para validar FKs e enriquecer as respostas — mas **nunca escreve** nelas.

---

## Tecnologias

| Tecnologia | Versão | Uso |
|------------|--------|-----|
| .NET / ASP.NET Core | 8.0 | Framework da API |
| Entity Framework Core | 8.0.11 | ORM (Database-First, sem migrations) |
| Pomelo.EntityFrameworkCore.MySql | 8.0.2 | Provider MySQL para EF Core |
| Swashbuckle.AspNetCore | 10.1.7 | Geração do Swagger / OpenAPI |
| Microsoft.OpenApi | 2.12.2 | Modelos OpenAPI (namespace atualizado na v2) |
| MySQL | 8.0 | Banco de dados (Azure Database for MySQL Flexible Server na nuvem) |
| Microsoft.Extensions.Diagnostics.HealthChecks | 8.0.11 | Health Checks (`/health`, `/health/live`, `/health/ready`) |
| Serilog.AspNetCore | 10.0.0 | Logging estruturado (console sempre; arquivo em `Development`) |
| OpenTelemetry (.NET SDK) | 1.18.0 | Distributed tracing + métricas de desempenho |
| OpenTelemetry.Exporter.Prometheus.AspNetCore | 1.18.0-beta.1 | Endpoint `/metrics` no formato Prometheus |
| xUnit + Moq | 2.9.3 / 4.20.72 | Testes unitários (padrão AAA) |
| Microsoft.AspNetCore.Mvc.Testing | 8.0.11 | Testes de integração via `WebApplicationFactory` |
| Microsoft.EntityFrameworkCore.InMemory | 8.0.11 | Banco em memória usado nos testes de integração |
| Telegram.Bot | 22.10.3 | Envio de mensagens no Telegram (bot próprio) |

---

## Estrutura de Pastas

```
ClyvoVet-api/
├── ClyvoVet.Api/
│   ├── Controllers/           → Recebem requisições HTTP e delegam ao Service
│   ├── Services/               → Regras de negócio
│   │   └── Interfaces/
│   ├── Repositories/          → Acesso ao banco via EF Core
│   │   └── Interfaces/
│   ├── Models/                 → Entidades mapeadas nas tabelas MySQL
│   ├── DTOs/
│   │   ├── Request/           → Dados recebidos nas requisições (POST/PUT)
│   │   └── Response/          → Dados retornados nas respostas
│   ├── Enums/                 → Enumerações dos valores aceitos pelo banco
│   ├── Data/
│   │   ├── AppDbContext.cs    → DbContext principal
│   │   └── Configurations/    → Fluent API (mapeamento tabela ↔ modelo)
│   ├── Exceptions/            → NotFoundException, BadRequestException
│   ├── HealthChecks/          → Formatação JSON do resultado do Health Check
│   ├── Middleware/            → CorrelationIdMiddleware (rastreio de requisições nos logs)
│   ├── Properties/
│   │   └── launchSettings.json
│   ├── appsettings.json       → Connection string MySQL (placeholder) + níveis de log
│   ├── Program.cs             → DI, Swagger, Health Checks, Serilog, OpenTelemetry, middleware de erros
│   ├── Logs/                   → Log em arquivo do Serilog, só em `Development` (não versionado)
│   ├── ClyvoVet.Api.Tests.Unit/         → Testes unitários (Services, mocks via Moq)
│   └── ClyvoVet.Api.Tests.Integration/  → Testes de integração (WebApplicationFactory + EF Core InMemory)
└── schema/
    ├── 01_criar_tabelas_dotnet.sql             → DDL das 4 tabelas originais + triggers + fn_uuid()
    ├── 02_seed_dotnet.sql                       → Dados de exemplo para os endpoints originais
    ├── 03_drop_tabelas_dotnet.sql               → Remove as 6 tabelas .NET
    ├── 04_criar_tabela_predisposicao_dotnet.sql → DDL da tabela do Widget de Saúde Preditiva
    ├── 05_seed_predisposicao_dotnet.sql         → 42 predisposições reais por espécie/raça/idade
    ├── 06_criar_tabela_tutor_telegram_dotnet.sql → DDL da tabela de vínculo Tutor ↔ Telegram
    └── README.md                                → Guia do schema
```

---

## Como Executar

### Pré-requisitos

| Ferramenta | Versão mínima | Para que serve |
|------------|--------------|----------------|
| [.NET SDK](https://dotnet.microsoft.com/download/dotnet/8.0) | 8.0 | Compilar e rodar a API |
| MySQL | 8.0+ | Banco de dados |
| Cliente `mysql` | Qualquer | Criar o banco e aplicar `schema/script_bd.sql` |
| Git | Qualquer | Clonar o repositório |

---

### Passo 1 — Clonar o repositório

```bash
git clone https://github.com/Clyvovet-Challenge/ClyvoVet-api.git
cd ClyvoVet-api
```

**Verifique se o clone funcionou:**

```bash
ls
# Deve listar: ClyvoVet.Api/  schema/  README.md  ClyvoVet-api.slnx  ...
```

> **Erro: `git: command not found`**  
> Significa que o Git não está instalado na máquina. Instale a partir de [git-scm.com](https://git-scm.com), ou use "Code → Download ZIP" direto no GitHub.

> **Erro: `Repository not found`**  
> Verifique se a URL está correta e se o repositório está público.

---

### Passo 2 — Configurar a connection string

> **Se você tem um clone antigo:** a chave mudou. Era `ConnectionStrings:OracleConnection`,
> apontando para o Oracle da FIAP; hoje o projeto usa **MySQL** via
> `Pomelo.EntityFrameworkCore.MySql`, e o `Program.cs` lê
> **`ConnectionStrings:DefaultConnection`**. Um secret antigo com o nome velho é
> simplesmente ignorado, e a API sobe reclamando de connection string ausente.

Por ficar versionado no repositório, `ClyvoVet.Api/appsettings.json` guarda apenas um **placeholder** — evite colocar sua senha real ali, sob risco de subir a credencial sem perceber. O caminho recomendado é o **User Secrets** do .NET: ele mantém a connection string **fora da pasta do projeto**, num arquivo local que o `git` nunca enxerga:

```bash
cd ClyvoVet.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Port=3306;Database=clyvovet;Uid=root;Pwd=SUA_SENHA_LOCAL;"
```

> Na primeira vez, se o projeto ainda não tem um `UserSecretsId`, rode antes: `dotnet user-secrets init`.

Em ambiente de desenvolvimento, a API lê o User Secrets sozinha, então não sobra mais nada para editar. Para ver o que foi salvo:

```bash
dotnet user-secrets list
```

**Alternativa (menos segura):** colocar a connection string direto no `appsettings.json` local. Funciona do mesmo jeito, mas assim que a senha real estiver ali, **evite comitar** o arquivo — cheque com `git status` antes de qualquer commit.

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=clyvovet;Uid=root;Pwd=SUA_SENHA_LOCAL;"
  }
}
```

**Formato da connection string MySQL:**

| Parte | Exemplo | Descrição |
|-------|---------|-----------|
| `Server` | `localhost` | Host do MySQL |
| `Port` | `3306` | Porta (o padrão do MySQL) |
| `Database` | `clyvovet` | Nome do banco |
| `Uid` / `Pwd` | `root` / sua senha | Credenciais |
| `SslMode` | `Required` | **Obrigatório na Azure**; dispensável em `localhost` |

**Contra o banco da Azure** (o mesmo que a API Java usa), acrescente o TLS — o MySQL Flexible Server recusa a conexão sem ele:

```
Server=<SERVIDOR>.mysql.database.azure.com;Port=3306;Database=clyvovet;Uid=clyvovetadmin;Pwd=<SENHA>;SslMode=Required;
```

> **Erro: `Unable to connect to any of the specified MySQL hosts`**
> O MySQL não está no ar ou o host/porta estão errados. Confira com `mysql -h localhost -u root -p`.

> **Erro: `Access denied for user`**
> Usuário ou senha incorretos no `Uid`/`Pwd`.

> **Erro: `Unknown database 'clyvovet'`**
> O banco ainda não existe. É o Passo 3.

> **Erro na Azure: `The SSL connection could not be established`**
> Faltou `SslMode=Required;` na connection string.

---

### Passo 3 — Preparar o banco de dados

> ⚠️ **Só para desenvolvimento local.** No banco da Azure **não execute nada disto**:
> lá o banco nasce vazio e quem cria o schema é o **Flyway da API Java**, no primeiro
> boot dela. Aplicar DDL antes deixa as tabelas sem a `flyway_schema_history`, e o
> Flyway então recusa migrar um schema não vazio que ele não conhece — a API Java
> não sobe.

#### 3.1 — Criar o banco e aplicar o schema

```bash
mysql -u root -p -e "CREATE DATABASE IF NOT EXISTS clyvovet CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;"
mysql -u root -p clyvovet < schema/script_bd.sql
```

[`schema/script_bd.sql`](schema/script_bd.sql) é o schema completo em **MySQL**, nas duas partes:

| Parte | Tabelas | Quem escreve |
|---|---|---|
| **1** | `t_clyvo_tutor`, `t_clyvo_animal`, `t_clyvo_clinica`, `t_clyvo_veterinario`, … | a **API Java** — aqui elas existem para as FKs e os JOINs; esta API só lê |
| **2** | `t_clyvo_produto`, `t_clyvo_sugestao_produto`, `t_clyvo_lembrete`, `t_clyvo_evento_pet`, `t_clyvo_predisposicao_saude`, `t_clyvo_tutor_telegram` | esta API |

O arquivo já inclui a carga inicial: produtos, eventos pet, um tutor e um animal de
exemplo, as predisposições de saúde do Widget e a tabela de vínculo com o Telegram.
Não há passo separado para nenhum deles.

#### 3.2 — Conferir

```bash
mysql -u root -p clyvovet -e "
SELECT TABLE_NAME, TABLE_ROWS FROM information_schema.TABLES
 WHERE TABLE_SCHEMA = 'clyvovet' AND TABLE_NAME LIKE 't_clyvo_%'
 ORDER BY TABLE_NAME;"
```

Se `t_clyvo_produto` e `t_clyvo_predisposicao_saude` aparecerem com linhas, o seed
entrou. Sem as predisposições, `GET /api/v1/widget-saude-preditiva/{animalId}`
responde normalmente, mas sempre com a lista vazia.

> **Os arquivos `schema/01_*.sql` a `schema/06_*.sql` são do tempo do Oracle** —
> `VARCHAR2`, `NUMBER`, triggers `BEFORE INSERT` e a função `fn_clyvo_uuid`. Eles
> não rodam no MySQL e ficam apenas como registro histórico. O único script vigente
> é o `script_bd.sql`.

---

### Passo 4 — Restaurar pacotes

Na raiz do projeto:

```bash
cd ClyvoVet.Api
dotnet restore
```

Output esperado:

```
Restaurando pacotes de C:\...\ClyvoVet.Api.csproj...
  Determinando projetos a serem restaurados...
  Todos os projetos estão atualizados.
```

> **Erro: `dotnet: command not found`**  
> O .NET SDK não está instalado, ou não está no PATH. Instale a partir de [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/8.0) e reinicie o terminal em seguida.

> **Erro: `NETSDK1045: The current .NET SDK does not support targeting .NET 8.0`**  
> A versão do SDK instalada não é compatível. Rode `dotnet --version` para ver qual está ativa — é preciso ter **8.0.x ou superior**.

> **Erro: `Unable to load the service index for source https://api.nuget.org`**  
> Falta acesso à internet para baixar os pacotes. Verifique sua conexão, ou configure um proxy NuGet se estiver numa rede corporativa.

---

### Passo 5 — Executar a API

```bash
dotnet run
```

Output esperado:

```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5191
      Now listening on: https://localhost:7225
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

> **Erro: `Failed to bind to address http://localhost:5191: address already in use`**  
> A porta 5191 já está em uso por outro processo. Encerre esse processo:
> ```bash
> # Windows
> netstat -ano | findstr :5191
> taskkill /PID <numero_do_pid> /F
> ```
> Ou mude a porta em `Properties/launchSettings.json`.

> **Erro: `Unable to connect to any of the specified MySQL hosts` na primeira requisição**  
> A connection string está errada — volte ao Passo 2. Mesmo com credenciais inválidas a aplicação sobe normalmente; o erro só se manifesta na primeira chamada ao banco.

> **Erro: `Table 'clyvovet.t_clyvo_produto' doesn't exist`**  
> O banco existe mas está vazio: falta aplicar `schema/script_bd.sql` (Passo 3).

> **A API sobe mas retorna `500` em todos os endpoints**  
> Confira os logs no terminal — é ali que o erro real aparece. As causas mais frequentes:
> - Connection string incorreta (usuário, senha ou service name)
> - Tabelas ainda não criadas (rode o script `01` primeiro)
> - Tipo de dado não mapeado no EF Core

---

### Passo 6 — Acessar o Swagger

Com a API rodando, abra no navegador:

| Perfil | URL |
|--------|-----|
| HTTP (recomendado para testes) | http://localhost:5191/swagger |
| HTTPS | https://localhost:7225/swagger |

Todos os endpoints aparecem no Swagger, agrupados por controller.

> **O Swagger fica sempre ativo** — não é restrito ao ambiente `Development`, funcionando em Docker, servidor e produção.

> **Erro: `ERR_CONNECTION_RESET` ou `ERR_SSL_PROTOCOL_ERROR` no HTTPS**  
> O certificado de desenvolvimento ainda não é confiável. Rode:
> ```bash
> dotnet dev-certs https --clean
> dotnet dev-certs https --trust
> ```
> Confirme na janela que o Windows abrir e reinicie a aplicação. Persistindo o problema, use a URL HTTP.

> **Swagger abre mas mostra "Failed to fetch" ao executar endpoints**  
> Sinal de que o Swagger está tentando HTTPS com um certificado inválido. Clique em "Servers" no topo e escolha a URL HTTP (`http://localhost:5191`).

> **Página em branco ou `404` ao acessar `/swagger`**  
> A aplicação está no ar, mas o Swagger não foi servido. Confira, em `Program.cs`, se `app.UseSwagger()` e `app.UseSwaggerUI()` estão **fora** de qualquer bloco `if (app.Environment.IsDevelopment())`.

---

### Alternativa — Executar via Visual Studio ou Rider

1. Abra `ClyvoVet-api.slnx` na IDE
2. Selecione o perfil `http` no dropdown de execução
3. Pressione **F5** (com debug) ou **Ctrl+F5** (sem debug)
4. O navegador abre sozinho no Swagger

> **Visual Studio:** se o navegador abrir em `weatherforecast`, ou numa página em branco, confira se `launchUrl`, em `Properties/launchSettings.json`, está definido como `"swagger"`.

---

### Verificação rápida — API funcionando

Com a API no ar, faça uma requisição de teste:

```bash
curl http://localhost:5191/api/v1/produtos
```

**Resposta esperada:** um array JSON com os produtos do seed. Vindo `[]`, o banco está conectado mas o seed não rodou; vindo `{"error": "Erro interno no servidor."}`, há algo errado na connection string — confira os logs do terminal.

---

## Monitoramento e Observabilidade

### Health Checks

A API expõe três endpoints de Health Check, usando `Microsoft.Extensions.Diagnostics.HealthChecks`:

| Endpoint | O que verifica | Uso |
|----------|----------------|-----|
| `GET /health` | Todos os checks (visão geral) | Diagnóstico manual, painel de monitoramento |
| `GET /health/live` | Apenas se o processo da API está de pé (`self`) | Liveness probe (ex.: Kubernetes, Docker healthcheck) |
| `GET /health/ready` | Conectividade real com o MySQL (`Database.CanConnectAsync()`) | Readiness probe |

Além do MySQL, `GET /health` também confere a Telegram Bot API (`telegram-bot`, via `GetMe`), fora da tag `ready` de propósito: uma instabilidade nela não deve tirar a API inteira de rotação, já que Produto, Lembrete, EventoPet e Sugestão de Produto seguem funcionando sem Telegram. A OCI Generative AI **não** tem sonda: ela é opcional por design (fallback determinístico), e uma sonda a transformaria em dependência.

Cada resposta traz um JSON com o status geral, a duração total e o detalhe de cada verificação:

```bash
curl http://localhost:5191/health
```

```json
{
  "status": "Healthy",
  "totalDurationMs": 1317.72,
  "checks": [
    { "name": "self", "status": "Healthy", "durationMs": 0.31, "tags": ["live"] },
    { "name": "oracle-database", "status": "Healthy", "durationMs": 16.52, "tags": ["ready", "database", "external"] },
    { "name": "telegram-bot", "status": "Healthy", "durationMs": 1316.33, "description": "Bot @clyvovet_notificacoes_bot respondendo.", "tags": ["external"] }
  ]
}
```

Ficando algum desses serviços inacessível (connection string errada, token inválido, sem internet etc.), o `status` daquele check passa a `"Unhealthy"` e o campo `error` traz a exceção correspondente.

### Logging Estruturado (Serilog)

- Configurado em [`Program.cs`](ClyvoVet.Api/Program.cs). O **console** é sempre ativo — é dele que a Azure lê, no "Log stream" e no Application Insights.
- O **arquivo** (`Logs/clyvovet-api-*.log`, rotação diária, retenção de 7 dias) entra **somente em `Development`**. O motivo é operacional: no App Service esse caminho é efêmero e por instância, cada réplica escreveria o seu próprio arquivo, ninguém os agrega e o conteúdo some no restart — seria a única dependência de armazenamento local da API. Localmente ele serve, e é onde dá para demonstrá-lo. O ambiente da suíte é `Testing`, então os testes também não deixam rastro em disco.
- Toda linha de log carrega um **Correlation ID** por requisição, gerado pelo [`CorrelationIdMiddleware`](ClyvoVet.Api/Middleware/CorrelationIdMiddleware.cs) — ou herdado do header `X-Correlation-Id` quando o cliente manda um valor que passa na validação de tamanho/formato — e devolvido também na resposta.
- São usados três níveis: `Information` para requisições HTTP concluídas, `Warning` para erros de negócio esperados (404/400) e `Error` para exceções não tratadas (500).
- Os níveis mínimos por categoria são ajustáveis em [`appsettings.json`](ClyvoVet.Api/appsettings.json), na seção `"Serilog"`.

### Tracing e Métricas (OpenTelemetry)

- **Tracing:** ASP.NET Core, `HttpClient` e Entity Framework Core vêm instrumentados automaticamente — cada requisição gera uma árvore de spans exportada para o **console**.
- **Métricas:** disponíveis em formato Prometheus via `GET /metrics`, cobrindo tempo de resposta, contagem de requisições e taxa de erros por rota/status code.

```bash
curl http://localhost:5191/metrics
```

---

## Testes Automatizados

Dentro de `ClyvoVet.Api/`, os testes se dividem em dois projetos, seguindo o padrão **AAA (Arrange, Act, Assert)** e a convenção de nomes `MetodoTestado_Cenario_ResultadoEsperado`:

| Projeto | O que testa | Ferramentas |
|---------|-------------|-------------|
| `ClyvoVet.Api.Tests.Unit` | Camada de Aplicação (`Services/`) com os repositórios mockados, e o `ApiKeyFilterAttribute` | xUnit + Moq |
| `ClyvoVet.Api.Tests.Integration` | Fluxo HTTP completo (Controller → Service → Repository → banco) | xUnit + `WebApplicationFactory` + EF Core InMemory |

### Rodando os testes

```bash
cd ClyvoVet.Api
dotnet test ClyvoVet.Api.Tests.Unit
dotnet test ClyvoVet.Api.Tests.Integration
```

Ou os dois juntos, direto da raiz do repositório:

```bash
dotnet test ClyvoVet-api.slnx
```

**Resultado esperado:** `124` testes passando (`55` unitários e `69` de integração).

### Detalhes dos testes de integração

- A API inteira sobe em memória via `WebApplicationFactory<Program>`, o que **troca o MySQL real por um banco EF Core InMemory** — assim, `dotnet test` roda sem precisar de banco nenhum, nem local nem na nuvem.
- `SwaggerEndpointsTests` cobre a geração do documento OpenAPI: que `/swagger/v1/swagger.json` responde, que ele é um documento válido com rotas, e que uma rota protegida por `X-Api-Key` continua declarando o requisito de segurança. Existe porque o Swagger é entregável avaliado e falha nele é 500 em tempo de execução, não erro de compilação — foi o que permitiu subir o `Microsoft.OpenApi` para corrigir a vulnerabilidade GHSA-v5pm-xwqc-g5wc sem apostar que nada quebrou.
- A maior parte dos testes usa uma **Collection Fixture** (`IntegrationTestFixture` + `[CollectionDefinition]`) que sobe a API **uma única vez** para a suíte inteira, semeando um Tutor, um Animal e um Produto de teste. Já os testes do Widget de Saúde Preditiva sobem uma instância própria, separada, por dependerem de um Animal com raça e idade específicas.

---

## Schema do Banco de Dados

### Todas as tabelas (banco compartilhado)

| Tabela | Responsável | Depende de |
|--------|-------------|------------|
| `T_CLYVO_TUTOR` | API Java | — |
| `T_CLYVO_ANIMAL` | API Java | `T_CLYVO_TUTOR` |
| `T_CLYVO_CLINICA` | API Java | — |
| `T_CLYVO_VETERINARIO` | API Java | `T_CLYVO_CLINICA` |
| `T_CLYVO_EVENTO_CLINICO` | API Java | `T_CLYVO_ANIMAL`, `T_CLYVO_VETERINARIO` |
| `T_CLYVO_PAGAMENTO` | API Java | `T_CLYVO_EVENTO_CLINICO` |
| **`T_CLYVO_PRODUTO`** | **API .NET** | — |
| **`T_CLYVO_EVENTO_PET`** | **API .NET** | — |
| **`T_CLYVO_LEMBRETE`** | **API .NET** | `T_CLYVO_ANIMAL` |
| **`T_CLYVO_SUGESTAO_PRODUTO`** | **API .NET** | `T_CLYVO_ANIMAL`, `T_CLYVO_PRODUTO` |
| **`T_CLYVO_PREDISPOSICAO_SAUDE`** | **API .NET** | — (catálogo de referência, sem FK) |
| **`T_CLYVO_TUTOR_TELEGRAM`** | **API .NET** | — (`tutor_id` validado via API, sem FK) |

> Ainda que pertença à API Java, a `T_CLYVO_TUTOR` é indispensável: o `AnimalRepository` faz `.Include(a => a.Tutor)`, e sem essa tabela a API dispara `Table 'clyvovet.t_clyvo_tutor' doesn't exist` em qualquer endpoint de lembrete ou sugestão.

---

### Geração de IDs (UUID)

Todo ID é um UUID gerado **em C#**, no repositório, antes do `INSERT`. O banco não
participa: as colunas `id` são `VARCHAR(36)` simples, sem trigger, sem `DEFAULT`, e o
mapeamento diz isso explicitamente com `ValueGeneratedNever()`.

```csharp
// Repositories/ProdutoRepository.cs
produto.Id = Guid.NewGuid().ToString();
```

```csharp
// Data/Configurations/ProdutoConfiguration.cs
builder.Property(p => p.Id)
    .HasColumnName("id")
    .HasColumnType("VARCHAR(36)")
    .ValueGeneratedNever();
```

> **Isto mudou com a migração para MySQL.** No Oracle, o ID vinha da função
> `fn_uuid()` chamada num trigger `BEFORE INSERT`, e o EF Core o lia de volta com
> `RETURNING`. Gerar do lado da aplicação tem uma vantagem prática aqui: o ID existe
> antes de a linha ser gravada, então dá para montar respostas e relacionamentos sem
> um segundo *round-trip* ao banco.

---

### Scripts disponíveis

> **Os `01_` a `06_` abaixo são do tempo do Oracle** e não rodam no MySQL
> (`VARCHAR2`, `NUMBER`, triggers, `fn_clyvo_uuid`). Ficam como registro. O script
> vigente, e único aplicável, é o **`schema/script_bd.sql`**.

| Arquivo | Quando usar |
|---------|-------------|
| `schema/01_criar_tabelas_dotnet.sql` | Primeira vez ou para recriar tudo do zero |
| `schema/02_seed_dotnet.sql` | Após o `01` — insere produtos, eventos, lembretes e sugestões de exemplo |
| `schema/03_drop_tabelas_dotnet.sql` | Para limpar apenas as tabelas .NET (preserva as Java) |
| `schema/04_criar_tabela_predisposicao_dotnet.sql` | Cria a tabela `T_CLYVO_PREDISPOSICAO_SAUDE` (widget de saúde preditiva) |
| `schema/05_seed_predisposicao_dotnet.sql` | Após o `04` — insere as 42 predisposições de saúde por espécie/raça/idade |
| `schema/06_criar_tabela_tutor_telegram_dotnet.sql` | Cria a tabela `T_CLYVO_TUTOR_TELEGRAM` (vínculo tutor ↔ bot do Telegram) |

---

## Documentação das Rotas

> **Base path:** `/api/v1/`  
> Toda resposta de endpoint vem em `application/json`.

### 🔐 Autenticação

Os endpoints principais (`/produtos`, `/lembretes`, `/eventos-pet`, `/sugestoes-produto`) exigem o header `X-Api-Key` — sem ele, ou com valor incorreto, a API responde `401 Unauthorized`.

```bash
dotnet user-secrets set "Api:ApiKey" "SUA_CHAVE_AQUI"
```

```bash
curl http://localhost:5191/api/v1/produtos -H "X-Api-Key: SUA_CHAVE_AQUI"
```

No Swagger (`/swagger`), clique em **"Authorize"** (canto superior direito) e informe a chave uma única vez — a partir daí ela é aplicada automaticamente em toda chamada feita por ali.

> O **envio** do Telegram segue o mesmo mecanismo, só que com chave própria (`Telegram:ApiKey`), porque manda mensagem para qualquer `chatId`. As ações de **vínculo** do tutor (`link`, `vinculo`) usam esta `Api:ApiKey` — detalhes na seção correspondente, mais abaixo.

#### Esta API não tem usuário nem perfil

Procurando as **credenciais de teste do tutor, do veterinário ou do admin**? Elas não existem aqui. Esta API se autentica por **chave de serviço**, não por pessoa: quem chama é o app, e a chave é a mesma para todo mundo. Não há login, não há JWT, não há papel — e por isso também não há o que separar por perfil nas respostas.

Os quatro perfis (`TUTOR`, `VETERINARIO`, `ADMIN_CLINICA`, `ADMIN`) vivem na **API Java**, que é a dona do cadastro, das sessões e das autorizações. As contas de desenvolvimento estão documentadas lá:

| E-mail | Senha | Perfil |
|---|---|---|
| `lucas.santos@email.com` | `tutor12345` | `TUTOR` |
| `maria.oliveira@email.com` | `tutor12345` | `TUTOR` |
| `camila.ferreira@vetcare.com.br` | `vet12345` | `VETERINARIO` |
| `gestor.vetcare@clyvovet.com` | `gestor12345` | `ADMIN_CLINICA` |
| `admin@clyvovet.com` | `admin12345` | `ADMIN` |

Semeadas pelo `DevDataSeeder` nos perfis Spring `dev`, `h2`, `oracle` e `local` — nunca em produção. No stack do Docker isso exige `SPRING_PROFILES_ACTIVE=mysql,local` no serviço `java-api`; com `mysql` sozinho o banco sobe sem usuário e todo login devolve 401.

---

### 🛒 Produtos — `/api/v1/produtos`

Trata do catálogo de produtos e serviços veterinários (`T_CLYVO_PRODUTO`).

| Método | Rota | Descrição | Status |
|--------|------|-----------|--------|
| GET | `/api/v1/produtos` | Lista produtos com filtros e paginação | 200, 400 |
| GET | `/api/v1/produtos/{id}` | Busca produto por ID | 200, 404 |
| POST | `/api/v1/produtos` | Cadastra novo produto | 201, 400 |
| PUT | `/api/v1/produtos/{id}` | Atualiza produto existente | 200, 400, 404 |
| DELETE | `/api/v1/produtos/{id}` | Remove produto | 204, 404 |

**Query params — GET `/api/v1/produtos`**

| Parâmetro | Tipo | Padrão | Descrição |
|-----------|------|--------|-----------|
| `page` | int | 1 | Número da página |
| `pageSize` | int | 10 | Itens por página (máx. 100) |
| `categoria` | enum | — | `Racao` \| `Medicamento` \| `Acessorio` \| `Servico` \| `Outro` |
| `especieIndicada` | enum | — | `Cachorro` \| `Gato` \| `Passaro` \| `Reptil` \| `Roedor` \| `Todos` \| `Outro` \| `Bovino` \| `Equino`. **Traz junto os marcados `Todos`** |
| `porteIndicado` | enum | — | `Pequeno` \| `Medio` \| `Grande` \| `Todos`. **Traz junto os marcados `Todos`** |
| `ativo` | bool | — | `true` só ativos, `false` só inativos. Omitido, traz os dois |

> **`especieIndicada` inclui os universais.** Pedir `Cachorro` devolve os produtos de cachorro **e** os marcados `Todos` — consulta de rotina, banho, o que não é específico de espécie. Antes o filtro usava igualdade exata, e perguntar "o que serve para um cachorro" escondia justamente o que serve para qualquer animal. Para ver só os universais, peça `especieIndicada=Todos`.

> **`porteIndicado` (coluna `porte_indicado`, migration `V16`).** Espécie sozinha não resolvia o caso mais comum da categoria mais comum: ração de cachorro pequeno e de cachorro grande são produtos diferentes, com outra formulação e outra granulometria, e ambos apareciam para qualquer cachorro. Coleira, casinha e dosagem de antipulgas têm o mesmo problema. O default é `TODOS`, o que preserva o comportamento anterior à coluna — produto que não declara porte continua servindo a qualquer animal e continua aparecendo. Os valores espelham `t_clyvo_animal.porte` (`PEQUENO`/`MEDIO`/`GRANDE`), em maiúscula, porque é assim que o `AnimalMapper` do Java grava e é assim que os dois lados se comparam — no Oracle, divergir na caixa faria o casamento falhar em silêncio.

> **`ativo` passou a ser lido.** O campo existia no modelo, no banco e no `PUT`, e nenhuma consulta o consultava: desativar um produto não o tirava de lugar nenhum. O parâmetro é opcional para não mudar o comportamento de quem já chamava sem ele — a vitrine do app pede `ativo=true`, e a gestão da clínica omite, porque precisa enxergar o que desativou para poder reativar. O mesmo vale para `GET /api/v1/sugestoes-produto`.

> ⚠️ **Enum por nome vale no query param, não no corpo.** `?categoria=Racao` funciona; no JSON do `POST`/`PUT` o valor precisa ser o **ordinal** (`"categoria": 0`), porque a API não registra `JsonStringEnumConverter`. Mandar o nome no corpo devolve 400 com `The JSON value could not be converted`.

**Request — POST / PUT**

```json
{
  "nome": "Ração Golden Adulto 15kg",
  "descricao": "Ração premium para cães adultos, rica em proteínas e ômega-3.",
  "categoria": 0,
  "preco": 189.90,
  "especieIndicada": 0,
  "ativo": true
}
```

**Response — GET / POST / PUT**

```json
{
  "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "nome": "Ração Golden Adulto 15kg",
  "descricao": "Ração premium para cães adultos, rica em proteínas e ômega-3.",
  "categoria": 0,
  "preco": 189.90,
  "especieIndicada": 0,
  "ativo": true,
  "criadoEm": "2026-05-24T10:30:00"
}
```

---

### 🐾 Eventos Pet — `/api/v1/eventos-pet`

Trata dos eventos públicos para pets (`T_CLYVO_EVENTO_PET`), sem depender de FK com as tabelas Java.

| Método | Rota | Descrição | Status |
|--------|------|-----------|--------|
| GET | `/api/v1/eventos-pet` | Lista eventos com filtros e paginação | 200, 400 |
| GET | `/api/v1/eventos-pet/{id}` | Busca evento por ID | 200, 404 |
| POST | `/api/v1/eventos-pet` | Cadastra novo evento | 201, 400 |
| PUT | `/api/v1/eventos-pet/{id}` | Atualiza evento existente | 200, 400, 404 |
| DELETE | `/api/v1/eventos-pet/{id}` | Remove evento | 204, 404 |

**Query params — GET `/api/v1/eventos-pet`**

| Parâmetro | Tipo | Padrão | Descrição |
|-----------|------|--------|-----------|
| `page` | int | 1 | Número da página |
| `pageSize` | int | 10 | Itens por página (máx. 100) |
| `cidade` | string | — | Filtra por cidade (case-insensitive) |
| `tipo` | enum | — | `Vacinacao` \| `Feira` \| `Castracao` \| `Workshop` \| `Outro` |
| `especieAlvo` | enum | — | `Cachorro` \| `Gato` \| `Passaro` \| `Reptil` \| `Roedor` \| `Todos` \| `Outro` \| `Bovino` \| `Equino` |

**Request — POST / PUT**

```json
{
  "titulo": "Feira de Adoção Responsável",
  "descricao": "Feira com cães e gatos disponíveis para adoção. Microchipagem gratuita.",
  "tipo": 1,
  "rua": "Av. Paulista",
  "numero": "1578",
  "bairro": "Bela Vista",
  "cidade": "São Paulo",
  "estado": "SP",
  "cep": "01310-200",
  "dataInicio": "2026-08-10",
  "dataFim": "2026-08-11",
  "especieAlvo": 5,
  "organizador": "ONG Amigo Fiel",
  "gratuito": true,
  "linkInscricao": "https://amigofiel.org.br/feira",
  "ativo": true
}
```

**Response — GET / POST / PUT**

```json
{
  "id": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
  "titulo": "Feira de Adoção Responsável",
  "descricao": "Feira com cães e gatos disponíveis para adoção. Microchipagem gratuita.",
  "tipo": 1,
  "rua": "Av. Paulista",
  "numero": "1578",
  "bairro": "Bela Vista",
  "cidade": "São Paulo",
  "estado": "SP",
  "cep": "01310-200",
  "dataInicio": "2026-08-10",
  "dataFim": "2026-08-11",
  "especieAlvo": 5,
  "organizador": "ONG Amigo Fiel",
  "gratuito": true,
  "linkInscricao": "https://amigofiel.org.br/feira",
  "ativo": true,
  "criadoEm": "2026-05-24T10:30:00"
}
```

---

### 🔔 Lembretes — `/api/v1/lembretes`

Trata dos lembretes de cuidados vinculados a um animal (`T_CLYVO_LEMBRETE`).  
⚠️ Exige `animalId` válido em `T_CLYVO_ANIMAL` (e que `T_CLYVO_TUTOR` já exista).

| Método | Rota | Descrição | Status |
|--------|------|-----------|--------|
| GET | `/api/v1/lembretes` | Lista lembretes com filtros e paginação | 200, 400 |
| GET | `/api/v1/lembretes/{id}` | Busca lembrete por ID | 200, 404 |
| POST | `/api/v1/lembretes` | Cria novo lembrete | 201, 400, 404 |
| PUT | `/api/v1/lembretes/{id}` | Atualiza lembrete existente | 200, 400, 404 |
| DELETE | `/api/v1/lembretes/{id}` | Remove lembrete | 204, 404 |

**Query params — GET `/api/v1/lembretes`**

| Parâmetro | Tipo | Padrão | Descrição |
|-----------|------|--------|-----------|
| `page` | int | 1 | Número da página |
| `pageSize` | int | 10 | Itens por página (máx. 100) |
| `animalId` | string | — | UUID do animal (filtra por animal específico) |
| `status` | enum | — | `Pendente` \| `Enviado` \| `Cancelado` |
| `tipo` | enum | — | `Vacina` \| `Medicamento` \| `Consulta` \| `Higiene` \| `Outro` |

**Request — POST / PUT**

```json
{
  "animalId": "<uuid-do-animal>",
  "titulo": "Antibiótico — 10 dias",
  "descricao": "Uma dose por dia, sempre no mesmo horário.",
  "tipo": 1,
  "agendadoEm": "2026-09-15T10:00:00",
  "intervaloDias": 1,
  "repetirAte": "2026-09-25T23:59:00",
  "status": 0
}
```

> **Atenção:** na criação, o `status` é **sempre forçado para `Pendente` (0)**, seja qual for o valor enviado.  
> `agendadoEm` precisa ser uma data/hora **futura**.

**A repetição (V18)**

`intervaloDias` é quem decide se o lembrete volta: nulo ou ausente = dispara uma
vez. `repetirAte` fecha a série — é o "de x dia até y dia", com `agendadoEm` no
começo. Nulo = repete sem fim previsto, que é o caso do antipulgas mensal.

| Campo | Regra |
|---|---|
| `intervaloDias` | 1 a 365. Fora disso, **400** |
| `repetirAte` | exige `intervaloDias`; sem ele, **400** |
| `repetirAte` | não pode ser anterior a `agendadoEm`; se for, **400** |
| `recorrente` | **derivado** de `intervaloDias`. Aceito no corpo e ignorado |

> **`recorrente` não é fonte de verdade.** Ele existia antes da V18, era gravado,
> lido e mapeado em oito lugares — e **nada no sistema agia sobre ele**: um
> lembrete "recorrente" disparava uma vez e virava `Enviado`. A coluna ficou por
> compatibilidade (o app a lê), e agora a API a calcula a partir do intervalo. Um
> corpo com `"recorrente": true` e sem `intervaloDias` grava `false`.

**Como a série anda:** ao notificar um lembrete com intervalo, a API **não cria
linha nova** — ela empurra `agendadoEm` para frente e mantém o status `Pendente`.
Quando a próxima data passa de `repetirAte`, aí marca `Enviado`: a série
terminou. Clonar a cada disparo custaria 36 linhas por ano num lembrete mensal, e
uma lista em que o tutor veria trinta e seis vezes o mesmo antipulgas.

O avanço pula de uma vez para a próxima data **futura**. Se a API ficou fora do ar
por um mês, um lembrete diário está trinta dias atrasado; avançar um intervalo por
ciclo faria o tutor receber trinta mensagens iguais para se atualizar.

**Response — GET / POST / PUT**

```json
{
  "id": "c3d4e5f6-a7b8-9012-cdef-123456789012",
  "animalId": "d4e5f6a7-b8c9-0123-defa-234567890123",
  "nomeAnimal": "Rex",
  "titulo": "Vacina Antirrábica — Reforço Anual",
  "descricao": "Aplicar a vacina antirrábica no pet shop da rua central.",
  "tipo": 0,
  "agendadoEm": "2026-09-15T10:00:00",
  "recorrente": true,
  "intervaloDias": 1,
  "repetirAte": "2026-09-25T23:59:00",
  "status": 0,
  "criadoEm": "2026-05-24T10:30:00"
}
```

---

### 💡 Sugestões de Produto — `/api/v1/sugestoes-produto`

Trata das sugestões de produto vinculadas a um animal (`T_CLYVO_SUGESTAO_PRODUTO`).  
⚠️ Exige `animalId` válido em `T_CLYVO_ANIMAL` e `produtoId` válido em `T_CLYVO_PRODUTO`.

| Método | Rota | Descrição | Status |
|--------|------|-----------|--------|
| GET | `/api/v1/sugestoes-produto` | Lista sugestões com filtros e paginação | 200, 400 |
| GET | `/api/v1/sugestoes-produto/{id}` | Busca sugestão por ID | 200, 404 |
| POST | `/api/v1/sugestoes-produto` | Cria nova sugestão | 201, 400, 404 |
| PUT | `/api/v1/sugestoes-produto/{id}` | Atualiza sugestão existente | 200, 400, 404 |
| DELETE | `/api/v1/sugestoes-produto/{id}` | Remove sugestão | 204, 404 |

**Query params — GET `/api/v1/sugestoes-produto`**

| Parâmetro | Tipo | Padrão | Descrição |
|-----------|------|--------|-----------|
| `page` | int | 1 | Número da página |
| `pageSize` | int | 10 | Itens por página (máx. 100) |
| `animalId` | string | — | UUID do animal (filtra por animal específico) |

**Request — POST / PUT**

```json
{
  "animalId": "<uuid-do-animal>",
  "produtoId": "<uuid-do-produto>",
  "justificativa": "Animal com infestação de pulgas. Uso mensal de antipulgas tópico recomendado pelo veterinário.",
  "dataSugestao": "2026-05-24",
  "ativo": true
}
```

> Omitindo `dataSugestao`, assume-se a data de hoje.

**Response — GET / POST / PUT**

```json
{
  "id": "e5f6a7b8-c9d0-1234-efab-345678901234",
  "animalId": "d4e5f6a7-b8c9-0123-defa-234567890123",
  "nomeAnimal": "Rex",
  "produtoId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "nomeProduto": "Frontline Plus Antipulgas 10-20kg",
  "justificativa": "Animal com infestação de pulgas. Uso mensal de antipulgas tópico recomendado pelo veterinário.",
  "dataSugestao": "2026-05-24",
  "ativo": true,
  "criadoEm": "2026-05-24T10:30:00"
}
```

---

### 🩺 Widget de Saúde Preditiva — `/api/v1/widget-saude-preditiva`

> ⚠️ Feature extra, fora do escopo avaliado da Sprint 3.

Esse card compara os dados do animal (espécie, raça e idade) com um catálogo de predisposições de saúde (`T_CLYVO_PREDISPOSICAO_SAUDE`) e, encontrando alguma condição relevante, sugere marcar uma consulta.

| Método | Rota | Descrição | Status |
|--------|------|-----------|--------|
| GET | `/api/v1/widget-saude-preditiva/{animalId}` | Retorna as predisposições de saúde do animal | 200, 404 |

**Response — GET**

```json
{
  "animalId": "d4e5f6a7-b8c9-0123-defa-234567890123",
  "nomeAnimal": "Rex",
  "especie": "Cachorro",
  "raca": "Labrador",
  "idadeAnos": 7.2,
  "sugerirAgendamentoConsulta": true,
  "predisposicoes": [
    {
      "doenca": "Displasia de quadril",
      "recomendacao": "Manter peso ideal e avaliação ortopédica periódica a partir da meia-idade.",
      "idadeMinimaAnos": 5,
      "fonteReferencia": "VetCompass (RVC) - Labrador Retrievers under primary veterinary care in the UK"
    }
  ]
}
```

**Regras de negócio**

- A comparação de raça é tolerante: ignora maiúsculas/minúsculas e casa substrings nos dois sentidos, então pequenas variações de digitação na raça cadastrada ainda encontram o catálogo.
- Um `idadeMinimaAnos` nulo ou `0` vale para qualquer idade; nos demais casos, a idade do animal precisa ser maior ou igual ao mínimo — e um animal sem data de nascimento cadastrada nunca bate com um mínimo acima de zero.
- Não reconhecendo a espécie do animal, o widget devolve a lista de predisposições vazia, sem erro algum.
- `sugerirAgendamentoConsulta` vira `true` assim que aparece ao menos uma predisposição — o widget só sugere; marcar a consulta fica para outra etapa.
- Um `animalId` inexistente resulta em 404.

---

### 🤖 Saúde Preditiva com IA — `/api/v1/saude-preditiva`

O parecer de riscos e recomendações que a home do app mostra por animal. O desenho, em uma frase: os **fatos** vêm da base agregada de doenças (`t_clyvo_base_doencas`, contagens de casos por espécie/raça extraídas de datasets [Dryad](https://datadryad.org) com DOI); a **OCI Generative AI** apenas redige e prioriza em cima deles; o resultado fica em **cache por animal** (`t_clyvo_parecer_ia`, 7 dias); e quando a OCI está indisponível ou sem credencial, as mesmas linhas geram um **parecer determinístico**. A home nunca depende da nuvem para abrir.

| Método | Rota | Descrição | Status |
|--------|------|-----------|--------|
| GET | `/api/v1/saude-preditiva/{animalId}` | Parecer de riscos e recomendações do animal | 200 |

**Response — GET** (campos principais)

```json
{
  "animalId": "d88454bc-53b4-4da0-89e8-570f14e95c02",
  "nomeAnimal": "Bolinha",
  "origem": "IA",
  "modelo": "meta.llama-3.3-70b-instruct",
  "resumo": "Atenção preventiva a mastocitoma e displasia coxofemoral.",
  "baseLimitada": false,
  "riscos": [
    { "doenca": "Mastocitoma", "categoria": "ONCOLOGICA", "nivel": "ALTO", "justificativa": "306 casos na raça na base de referência." }
  ],
  "recomendacoes": ["Checkup anual com palpação de pele e linfonodos."],
  "disclaimer": "Orientação preventiva gerada a partir de bases de referência. Não é diagnóstico e não substitui consulta veterinária."
}
```

- `origem` diz quem redigiu: `IA` (OCI) ou `REGRAS` (fallback determinístico). O app mostra a diferença ao tutor.
- `baseLimitada` avisa quando a base cobre pouco a espécie (aves/répteis dos datasets são fauna selvagem; roedores não têm dados).
- Se o tutor tem o Telegram vinculado, um parecer **novo** também dispara o resumo por mensagem.
- Exige o `X-Api-Key` principal e respeita o escopo por tutor (animal alheio responde 404).

**Configuração da OCI** (tudo por variável de ambiente ou `user-secrets` — nunca no código):

```bash
dotnet user-secrets set "Oci:TenancyOcid" "ocid1.tenancy.oc1..."
dotnet user-secrets set "Oci:UserOcid" "ocid1.user.oc1..."
dotnet user-secrets set "Oci:Fingerprint" "aa:bb:cc:..."
dotnet user-secrets set "Oci:PrivateKeyPath" "C:/chaves/clyvovet_api_key.pem"
dotnet user-secrets set "Oci:Region" "us-chicago-1"
dotnet user-secrets set "Oci:GenAi:CompartmentOcid" "ocid1.compartment.oc1..."
```

O modelo default é `meta.llama-3.3-70b-instruct` (`Oci:GenAi:ModelId` muda; `Oci:GenAi:ApiFormat` aceita `GENERIC`/`COHERE`). A chave de API se cria na console da OCI em **Identity → My profile → API keys**; a região precisa oferecer o serviço Generative AI. **Sem nada disso configurado a rota continua funcionando** — o parecer sai com `origem: "REGRAS"`.

> O WhatsApp (Twilio) saiu do escopo nesta sprint: o Telegram é o canal único de mensagens, e a marcação de consultas acontece apenas no app — o bot só dispara lembretes e os resumos de saúde preditiva.


---

### ✈️ Telegram — `/api/v1/telegram`

> ⚠️ Feature extra, fora do escopo avaliado da Sprint 3.

O canal de mensagens da plataforma, com bot próprio no [Telegram](https://core.telegram.org/bots/api) — ponto único de disparo (lembretes e resumos de saúde preditiva), testável de ponta a ponta de graça. Desde esta sprint é o ÚNICO canal: o WhatsApp/Twilio saiu do escopo, e a marcação de consultas acontece apenas no app.

| Método | Rota | Descrição | Chave | Status |
|--------|------|-----------|-------|--------|
| POST | `/api/v1/telegram/enviar` | Envia uma mensagem de Telegram para o `chatId` informado | `Telegram:ApiKey` | 204 |
| GET | `/api/v1/telegram/link/{tutorId}` | Gera o deep link (`t.me/<bot>?start=<convite>`) para o tutor vincular sua conta ao bot | `Api:ApiKey` | 200 |
| GET | `/api/v1/telegram/vinculo/{tutorId}` | Diz se o tutor já tem conversa ligada, e desde quando | `Api:ApiKey` | 200 |
| DELETE | `/api/v1/telegram/vinculo/{tutorId}` | Desliga as notificações deste tutor | `Api:ApiKey` | 204 |

#### Duas chaves, e o motivo

O filtro de chave deixou de ser do controller e passou a ser **por ação**, porque as ações aqui têm risco muito diferente.

`enviar` manda **qualquer mensagem para qualquer `chatId`**. Uma chave que abre isso não pode viajar dentro de um aplicativo distribuído: quem extraísse o bundle passaria a escrever, como se fosse a clínica, para todo tutor cujo `chatId` descobrisse. Ela continua sendo a `Telegram:ApiKey`, de serviço para serviço.

As outras três são do próprio tutor sobre o próprio vínculo, e o app precisa delas para a tela existir. Usam a `Api:ApiKey` — a mesma que o app já carrega para os lembretes — com o `EscopoDoTutor` impedindo que um tutor mexa no vínculo de outro. Exigir a chave de envio nelas obrigaria a embarcar a chave de envio, que é exatamente o que o parágrafo acima proíbe.

**Response — GET `/vinculo/{tutorId}`**

```json
{ "vinculado": true, "desde": "2026-09-10T14:32:11Z" }
```

O `chatId` **não** entra na resposta: o app não faz nada com ele — quem envia é esta API —, e devolvê-lo só ampliaria o estrago de um token vazado.

Tutor sem vínculo responde **200 com `vinculado: false`**, não 404: não ter vínculo é o estado em que todo tutor começa, e tratá-lo como erro obrigaria o app a desenhar a tela normal a partir de uma exceção. Pelo mesmo motivo, `DELETE` responde 204 mesmo quando não havia vínculo — o pedido é "que este tutor não receba mais", e esse estado passa a valer nos dois casos.

**Request — POST**

```json
{
  "chatId": 123456789,
  "mensagem": "Seu pet tem um lembrete de vacina agendado para amanhã."
}
```

**Configuração**

1. Crie um bot falando com **[@BotFather](https://t.me/BotFather)** no Telegram: mande `/newbot` e siga as instruções — ele devolve um **token** no formato `123456:ABC-DEF...`.
2. Para receber mensagens, o destinatário precisa mandar `/start` ao bot pelo menos uma vez.
3. O `chatId` de cada destinatário sai de `https://api.telegram.org/bot<TOKEN>/getUpdates`, consultado depois do `/start`.

```bash
dotnet user-secrets set "Telegram:BotToken" "SEU_BOT_TOKEN"
dotnet user-secrets set "Telegram:ApiKey" "SUA_CHAVE_AQUI"
dotnet user-secrets set "Telegram:BotUsername" "seu_bot_username"
```

**Vínculo tutor ↔ Telegram**

Como `Tutor` é uma tabela da API Java, não dá para adicionar uma coluna `chatId` nela. O vínculo `TutorId → ChatId` fica então numa tabela própria (`T_CLYVO_TUTOR_TELEGRAM`), preenchida assim:

1. O frontend chama `GET /api/v1/telegram/link/{tutorId}` (com o `tutorId` do tutor já logado) e recebe o deep link de volta.
2. Ao clicar no link, o tutor abre o Telegram, que manda `/start {convite}` ao bot automaticamente.
3. Um serviço em background na API .NET consulta o Telegram (`getUpdates`) e, ao detectar esse `/start`, troca o convite pelo tutor e grava o vínculo `TutorId → ChatId` na tabela.

**O que vai no `start=` é um convite, e não o `tutorId`**

O parâmetro carregava o próprio `tutorId`, e o ouvinte gravava o vínculo para qualquer id que chegasse — sem verificar que quem mandou é o dono dele. O bot é público por natureza, e o `tutorId` nunca foi segredo: ele viaja em `/auth/me` e no corpo de cada animal. Bastava digitar `/start <uuid alheio>` para passar a receber, no próprio celular, os lembretes daquele tutor; `/meusanimais` e `/meuslembretes` devolviam os pets e os lembretes dele; e como o vínculo é sobrescrito, o dono de verdade parava de receber qualquer notificação — inclusive pelo WhatsApp, porque o envio ao Telegram tinha "sucesso" e a alternativa nem chegava a ser tentada.

Hoje o `start=` leva um convite de **256 bits**, sorteado por `RandomNumberGenerator`, que só existe porque alguém autenticado pediu um link para aquele tutor. Ele vale **uma vez** e por **15 minutos** (`VinculosPendentesDeTelegram`). Saber o `tutorId` deixou de servir para alguma coisa.

O convite fica em memória, e não no banco: a vida inteira dele são os segundos entre o app mostrar o link e o tutor tocar nele, e o processo que gera é o mesmo que consome, porque o ouvinte roda dentro da API. Em troca vale uma limitação explícita — com mais de uma instância, ou depois de um restart, um convite ainda não usado deixa de valer e o tutor pede outro.

O endpoint também exige o header `X-Api-Key` (chave própria, diferente da principal):

```bash
curl -X POST http://localhost:5191/api/v1/telegram/enviar \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: SUA_CHAVE_AQUI" \
  -d '{"chatId": 123456789, "mensagem": "Teste"}'
```

> ✅ Esse endpoint (e o fluxo completo de vínculo, `TelegramLinkListenerService` incluído) foi validado de ponta a ponta com um bot real, sem travar em trial/template. Mesmo assim, os testes automatizados (`TelegramEndpointsTests`, `TutorTelegramRepositoryTests`) usam fakes/banco em memória, para permanecerem determinísticos e livres de rede externa — pelo mesmo motivo, o `TelegramLinkListenerService` fica desativado no ambiente de `Testing`.

**Notificação automática de lembretes**

O `LembreteNotificationService` (também um `BackgroundService`, desativado em `Testing`) checa a cada 1 minuto se algum lembrete `Pendente` está vencendo na próxima hora. Encontrando um, dispara a notificação via Telegram, se o tutor já tiver vinculado a conta (`T_CLYVO_TUTOR_TELEGRAM`), e marca o lembrete como `Enviado`, para não notificar de novo. Sem vínculo, o lembrete permanece `Pendente` e o motivo vai para o log — desde a saída do WhatsApp não há segundo canal.

---

## Guia de Testes Manuais

> **54 testes** manuais, rodados contra o banco MySQL real, todos passando.  
> Acesse **`http://localhost:5191/swagger`**, siga a ordem indicada e reaproveite os JSONs já prontos.  
> Legenda dos ícones: ✅ sucesso &nbsp;|&nbsp; ❌ erro esperado (validação)  
> ⚠️ Desde a Sprint 3, os endpoints principais exigem `X-Api-Key` — clique em **"Authorize"** no Swagger antes de começar (veja a seção [🔐 Autenticação](#-autenticação)).

---

### Antes de começar — obtenha os IDs necessários

Rode no cliente `mysql` (ou no MySQL Workbench) depois de aplicar o schema:

```sql
-- animal_id (necessário nos testes de Lembrete e Sugestão)
SELECT id, nome FROM t_clyvo_animal LIMIT 1;

-- produto_id do seed (necessário nos testes de Sugestão)
SELECT id, nome FROM t_clyvo_produto LIMIT 1;
```

> Guarde os dois UUIDs — eles entram no lugar de `{ANIMAL_ID}` e `{PRODUTO_ID}` nos testes a seguir.  
> O `animalId` também pode ser pego direto na resposta do **T23** (GET /lembretes).

---

## 🛒 BLOCO 1 — Produtos

---

### T01 — Listar todos os produtos
**Confirma a conexão com o MySQL — deve devolver os produtos do seed.**

```
GET /api/v1/produtos
```

✅ **Esperado:** `200 OK` — array com os produtos cadastrados no seed.

---

### T02 — Filtrar produtos por categoria
```
GET /api/v1/produtos?categoria=Racao
```

✅ **Esperado:** `200 OK` — só os produtos com `categoria = 0` (Racao).

---

### T03 — Filtrar produtos por espécie
```
GET /api/v1/produtos?especieIndicada=Gato
```

✅ **Esperado:** `200 OK` — só produtos indicados para gatos.

---

### T04 — Paginação inválida: page = 0
```
GET /api/v1/produtos?page=0
```

❌ **Esperado:** `400 Bad Request`
```json
{ "error": "O parâmetro 'page' deve ser maior que zero." }
```

---

### T05 — Paginação inválida: pageSize acima do limite
```
GET /api/v1/produtos?pageSize=200
```

❌ **Esperado:** `400 Bad Request`
```json
{ "error": "O parâmetro 'pageSize' deve estar entre 1 e 100." }
```

---

### T06 — Criar produto ✨
```
POST /api/v1/produtos
```
```json
{
  "nome": "Shampoo Pet Neutro 500ml",
  "descricao": "Shampoo hipoalergênico para cães e gatos.",
  "categoria": 2,
  "preco": 28.90,
  "especieIndicada": 5,
  "ativo": true
}
```

✅ **Esperado:** `201 Created` — produto criado com `id` gerado pela API.

> 📋 **Guarde o `id` retornado** — será usado nos testes T07, T08 e T50.

---

### T07 — Buscar produto por ID
```
GET /api/v1/produtos/{id do T06}
```

✅ **Esperado:** `200 OK` — dados completos do produto criado em T06.

---

### T08 — Atualizar produto
```
PUT /api/v1/produtos/{id do T06}
```
```json
{
  "nome": "Shampoo Pet Neutro 1L",
  "descricao": "Versão maior com 1 litro.",
  "categoria": 2,
  "preco": 49.90,
  "especieIndicada": 5,
  "ativo": true
}
```

✅ **Esperado:** `200 OK` — produto com nome e preço já atualizados.

---

### T09 — Buscar produto com ID inexistente
```
GET /api/v1/produtos/id-que-nao-existe
```

❌ **Esperado:** `404 Not Found`
```json
{ "error": "Produto não encontrado." }
```

---

### T10 — Criar produto com preço negativo
```
POST /api/v1/produtos
```
```json
{
  "nome": "Produto Inválido",
  "categoria": 0,
  "preco": -50.00,
  "especieIndicada": 0,
  "ativo": true
}
```

❌ **Esperado:** `400 Bad Request` — erro de validação sobre preço negativo.

---

### T11 — Criar produto sem campo obrigatório (nome)
```
POST /api/v1/produtos
```
```json
{
  "categoria": 0,
  "preco": 10.00,
  "especieIndicada": 0,
  "ativo": true
}
```

❌ **Esperado:** `400 Bad Request` — erro de campo obrigatório.

---

## 🐾 BLOCO 2 — Eventos Pet

---

### T12 — Listar todos os eventos pet
```
GET /api/v1/eventos-pet
```

✅ **Esperado:** `200 OK` — array com os eventos do seed (Feira de Adoção, Vacinação etc.).

---

### T13 — Filtrar eventos por cidade
```
GET /api/v1/eventos-pet?cidade=Sao Paulo
```

✅ **Esperado:** `200 OK` — só os eventos de São Paulo.

---

### T14 — Filtrar eventos por tipo
```
GET /api/v1/eventos-pet?tipo=Vacinacao
```

✅ **Esperado:** `200 OK` — só os eventos do tipo `Vacinacao (0)`.

---

### T15 — Filtrar eventos por espécie alvo
```
GET /api/v1/eventos-pet?especieAlvo=Todos
```

✅ **Esperado:** `200 OK` — só os eventos abertos a todos os animais.

---

### T16 — Paginação inválida: page = 0
```
GET /api/v1/eventos-pet?page=0
```

❌ **Esperado:** `400 Bad Request`
```json
{ "error": "O parâmetro 'page' deve ser maior que zero." }
```

---

### T17 — Criar evento pet ✨
```
POST /api/v1/eventos-pet
```
```json
{
  "titulo": "Workshop Nutrição Pet",
  "descricao": "Palestra sobre alimentação natural para cães.",
  "tipo": 3,
  "rua": "Rua das Acácias",
  "numero": "500",
  "bairro": "Jardins",
  "cidade": "São Paulo",
  "estado": "SP",
  "cep": "01425-000",
  "dataInicio": "2026-10-05",
  "dataFim": "2026-10-05",
  "especieAlvo": 0,
  "organizador": "Dr. Pet Nutrição",
  "gratuito": false,
  "linkInscricao": "https://drpet.com.br/workshop",
  "ativo": true
}
```

✅ **Esperado:** `201 Created` — evento criado com `id` gerado pela API.

> 📋 **Guarde o `id` retornado** — será usado nos testes T18, T19 e T49.

---

### T18 — Buscar evento por ID
```
GET /api/v1/eventos-pet/{id do T17}
```

✅ **Esperado:** `200 OK` — dados completos do evento criado em T17.

---

### T19 — Atualizar evento pet
```
PUT /api/v1/eventos-pet/{id do T17}
```
```json
{
  "titulo": "Workshop Nutrição Pet — Edição Atualizada",
  "tipo": 3,
  "cidade": "São Paulo",
  "estado": "SP",
  "dataInicio": "2026-10-05",
  "dataFim": "2026-10-06",
  "especieAlvo": 0,
  "gratuito": true,
  "ativo": true
}
```

✅ **Esperado:** `200 OK` — evento com título e `gratuito` já atualizados.

---

### T20 — Criar evento com data de início no passado
```
POST /api/v1/eventos-pet
```
```json
{
  "titulo": "Evento Passado",
  "tipo": 0,
  "dataInicio": "2020-01-01",
  "especieAlvo": 5,
  "gratuito": true,
  "ativo": true
}
```

❌ **Esperado:** `400 Bad Request`
```json
{ "error": "A data de início não pode ser no passado." }
```

---

### T21 — Criar evento sem título (campo obrigatório)
```
POST /api/v1/eventos-pet
```
```json
{
  "tipo": 0,
  "dataInicio": "2026-12-01",
  "especieAlvo": 5,
  "gratuito": true,
  "ativo": true
}
```

❌ **Esperado:** `400 Bad Request` — erro de campo obrigatório.

---

### T22 — Buscar evento com ID inexistente
```
GET /api/v1/eventos-pet/id-que-nao-existe
```

❌ **Esperado:** `404 Not Found`
```json
{ "error": "Evento não encontrado." }
```

---

## 🔔 BLOCO 3 — Lembretes

> ⚠️ A partir do T28, os testes exigem `animalId` válido.  
> Pegue esse valor no T23 (campo `animalId` de qualquer lembrete do seed), ou pelo SQL do pré-requisito.

---

### T23 — Listar todos os lembretes
```
GET /api/v1/lembretes
```

✅ **Esperado:** `200 OK` — array com os lembretes do seed (Vacina V10, Vermifugação, Retorno).

> 📋 **Guarde o valor de `animalId`** de qualquer item retornado — será usado nos testes T26 e T28 em diante.

---

### T24 — Filtrar lembretes por status
```
GET /api/v1/lembretes?status=Pendente
```

✅ **Esperado:** `200 OK` — só os lembretes com `status = 0` (Pendente).

---

### T25 — Filtrar lembretes por tipo
```
GET /api/v1/lembretes?tipo=Vacina
```

✅ **Esperado:** `200 OK` — só os lembretes do tipo `Vacina (0)`.

---

### T26 — Filtrar lembretes por animal
```
GET /api/v1/lembretes?animalId={ANIMAL_ID}
```

✅ **Esperado:** `200 OK` — só os lembretes do animal informado.

---

### T27 — Paginação inválida: page negativo
```
GET /api/v1/lembretes?page=-1
```

❌ **Esperado:** `400 Bad Request`
```json
{ "error": "O parâmetro 'page' deve ser maior que zero." }
```

---

### T28 — Criar lembrete ✨
```
POST /api/v1/lembretes
```
```json
{
  "animalId": "{ANIMAL_ID}",
  "titulo": "Consulta de Rotina",
  "descricao": "Checkup anual completo com hemograma.",
  "tipo": 2,
  "agendadoEm": "2026-10-20T14:00:00",
  "recorrente": false,
  "status": 0
}
```

✅ **Esperado:** `201 Created` — lembrete criado. O campo `status` **sempre** vem `0` (Pendente), mesmo que outro valor tenha sido enviado.

> 📋 **Guarde o `id` retornado** — será usado nos testes T29 a T32 e T48.

---

### T29 — Buscar lembrete por ID
```
GET /api/v1/lembretes/{id do T28}
```

✅ **Esperado:** `200 OK` — dados completos, incluindo `nomeAnimal` preenchido pelo JOIN.

---

### T30 — Verificar que status foi forçado para Pendente
No response do T29, confirme que `"status": 0`, independente do valor enviado em T28.

✅ **Esperado:** `"status": 0` no response.

---

### T31 — Atualizar lembrete — mudar status para Enviado
```
PUT /api/v1/lembretes/{id do T28}
```
```json
{
  "animalId": "{ANIMAL_ID}",
  "titulo": "Consulta de Rotina",
  "descricao": "Checkup anual completo com hemograma.",
  "tipo": 2,
  "agendadoEm": "2026-10-20T14:00:00",
  "recorrente": false,
  "status": 1
}
```

✅ **Esperado:** `200 OK` — lembrete com `"status": 1` (Enviado).

---

### T32 — Atualizar lembrete com data no passado
```
PUT /api/v1/lembretes/{id do T28}
```
```json
{
  "animalId": "{ANIMAL_ID}",
  "titulo": "Teste Data Passada",
  "tipo": 0,
  "agendadoEm": "2020-01-01T10:00:00",
  "recorrente": false,
  "status": 0
}
```

❌ **Esperado:** `400 Bad Request`
```json
{ "error": "A data de agendamento não pode ser no passado." }
```

---

### T33 — Criar lembrete com animalId inexistente
```
POST /api/v1/lembretes
```
```json
{
  "animalId": "00000000-0000-0000-0000-000000000000",
  "titulo": "Teste Animal Inválido",
  "tipo": 0,
  "agendadoEm": "2026-12-01T10:00:00",
  "recorrente": false,
  "status": 0
}
```

❌ **Esperado:** `404 Not Found`
```json
{ "error": "Animal não encontrado." }
```

---

### T34 — Criar lembrete sem campo obrigatório (animalId)
```
POST /api/v1/lembretes
```
```json
{
  "titulo": "Sem Animal",
  "tipo": 0,
  "agendadoEm": "2026-12-01T10:00:00",
  "recorrente": false
}
```

❌ **Esperado:** `400 Bad Request` — erro de campo obrigatório.

---

### T35 — Buscar lembrete com ID inexistente
```
GET /api/v1/lembretes/id-que-nao-existe
```

❌ **Esperado:** `404 Not Found`
```json
{ "error": "Lembrete não encontrado." }
```

---

## 💡 BLOCO 4 — Sugestões de Produto

> ⚠️ A partir do T39, os testes exigem `{ANIMAL_ID}` e `{PRODUTO_ID}` válidos.  
> Pegue esses valores pelo SQL do pré-requisito, ou pelos GETs anteriores.

---

### T36 — Listar todas as sugestões
```
GET /api/v1/sugestoes-produto
```

✅ **Esperado:** `200 OK` — array com as sugestões do seed.

---

### T37 — Filtrar sugestões por animal
```
GET /api/v1/sugestoes-produto?animalId={ANIMAL_ID}
```

✅ **Esperado:** `200 OK` — só as sugestões do animal informado, da mais recente para a mais antiga.

---

### T38 — Paginação inválida: pageSize acima do limite
```
GET /api/v1/sugestoes-produto?pageSize=999
```

❌ **Esperado:** `400 Bad Request`
```json
{ "error": "O parâmetro 'pageSize' deve estar entre 1 e 100." }
```

---

### T39 — Criar sugestão de produto ✨
```
POST /api/v1/sugestoes-produto
```
```json
{
  "animalId": "{ANIMAL_ID}",
  "produtoId": "{PRODUTO_ID}",
  "justificativa": "Animal com baixa imunidade — veterinário recomendou suplemento vitamínico após hemograma.",
  "dataSugestao": "2026-05-24",
  "ativo": true
}
```

✅ **Esperado:** `201 Created` — sugestão criada com `id` gerado pela API.

> 📋 **Guarde o `id` retornado** — será usado nos testes T40, T42 e T47.

---

### T40 — Buscar sugestão por ID
```
GET /api/v1/sugestoes-produto/{id do T39}
```

✅ **Esperado:** `200 OK` — dados completos, incluindo `nomeAnimal` e `nomeProduto` preenchidos automaticamente pelo JOIN.

---

### T41 — Verificar enriquecimento do response
No response do T40, confirme a presença dos campos vindos do JOIN:

```json
{
  "nomeAnimal": "<nome do animal>",
  "nomeProduto": "<nome do produto>"
}
```

✅ **Esperado:** ambos os campos preenchidos com os nomes reais do banco.

---

### T42 — Atualizar sugestão de produto
```
PUT /api/v1/sugestoes-produto/{id do T39}
```
```json
{
  "animalId": "{ANIMAL_ID}",
  "produtoId": "{PRODUTO_ID}",
  "justificativa": "Justificativa atualizada após reavaliação clínica.",
  "dataSugestao": "2026-05-24",
  "ativo": false
}
```

✅ **Esperado:** `200 OK` — sugestão com `ativo: false` e justificativa já atualizada.

---

### T43 — Criar sugestão com produtoId inexistente
```
POST /api/v1/sugestoes-produto
```
```json
{
  "animalId": "{ANIMAL_ID}",
  "produtoId": "00000000-0000-0000-0000-000000000000",
  "ativo": true
}
```

❌ **Esperado:** `404 Not Found`
```json
{ "error": "Produto não encontrado." }
```

---

### T44 — Criar sugestão com animalId inexistente
```
POST /api/v1/sugestoes-produto
```
```json
{
  "animalId": "00000000-0000-0000-0000-000000000000",
  "produtoId": "{PRODUTO_ID}",
  "ativo": true
}
```

❌ **Esperado:** `404 Not Found`
```json
{ "error": "Animal não encontrado." }
```

---

### T45 — Criar sugestão sem campos obrigatórios
```
POST /api/v1/sugestoes-produto
```
```json
{
  "justificativa": "Sem animal e produto",
  "ativo": true
}
```

❌ **Esperado:** `400 Bad Request` — erro de campo obrigatório.

---

### T46 — Buscar sugestão com ID inexistente
```
GET /api/v1/sugestoes-produto/id-que-nao-existe
```

❌ **Esperado:** `404 Not Found`
```json
{ "error": "Sugestão de produto não encontrada." }
```

---

## 🗑️ BLOCO 5 — Delete e Confirmação

> Siga a ordem abaixo para limpar os registros criados durante os testes.

---

### T47 — Deletar sugestão criada em T39
```
DELETE /api/v1/sugestoes-produto/{id do T39}
```

✅ **Esperado:** `204 No Content` — sem body na resposta.

---

### T48 — Deletar lembrete criado em T28
```
DELETE /api/v1/lembretes/{id do T28}
```

✅ **Esperado:** `204 No Content`.

---

### T49 — Deletar evento criado em T17
```
DELETE /api/v1/eventos-pet/{id do T17}
```

✅ **Esperado:** `204 No Content`.

---

### T50 — Deletar produto criado em T06
```
DELETE /api/v1/produtos/{id do T06}
```

✅ **Esperado:** `204 No Content`.

---

### T51 — Confirmar deleção do produto
```
GET /api/v1/produtos/{id do T06}
```

❌ **Esperado:** `404 Not Found` — produto removido com sucesso.

---

### T52 — Confirmar deleção do lembrete
```
GET /api/v1/lembretes/{id do T28}
```

❌ **Esperado:** `404 Not Found` — lembrete removido com sucesso.

---

### T53 — Confirmar deleção do evento
```
GET /api/v1/eventos-pet/{id do T17}
```

❌ **Esperado:** `404 Not Found` — evento removido com sucesso.

---

### T54 — Confirmar deleção da sugestão
```
GET /api/v1/sugestoes-produto/{id do T39}
```

❌ **Esperado:** `404 Not Found` — sugestão removida com sucesso.

---

> **Resultado esperado ao final:** os 54 testes passam, cada um com o status code indicado.  
> Essa suíte rodou contra o MySQL real e fechou em **54/54 PASS**.

---

## Regras de Negócio

### Produto

| Regra | Comportamento |
|-------|---------------|
| Preço não pode ser negativo | 400 Bad Request |
| ID gerado pela API | Campo `id` ignorado no request — o repositório atribui `Guid.NewGuid()` antes do INSERT |

### Evento Pet

| Regra | Comportamento |
|-------|---------------|
| `dataInicio` não pode ser no passado (POST) | 400 Bad Request |
| `dataInicio` só pode ser alterada para data futura (PUT) | 400 Bad Request |
| Eventos já iniciados podem ser editados normalmente | Apenas mudança de `dataInicio` para passado é bloqueada |
| `dataFim` deve ser ≥ `dataInicio` | 400 Bad Request |

### Lembrete

| Regra | Comportamento |
|-------|---------------|
| `animalId` deve existir em `t_clyvo_animal` | 404 Not Found |
| `agendadoEm` deve ser data/hora futura (POST e PUT) | 400 Bad Request |
| `status` é forçado a `Pendente` na criação | Qualquer valor enviado é ignorado |
| No PUT, `status` pode ser alterado livremente | Permite marcar como `Enviado` ou `Cancelado` |

### Sugestão de Produto

| Regra | Comportamento |
|-------|---------------|
| `animalId` deve existir em `t_clyvo_animal` | 404 Not Found |
| `produtoId` deve existir em `t_clyvo_produto` | 404 Not Found |
| `dataSugestao` é opcional | Se omitido, assume a data de hoje |

### Geral

| Regra | Comportamento |
|-------|---------------|
| `page` deve ser ≥ 1 | 400 Bad Request |
| `pageSize` deve estar entre 1 e 100 | 400 Bad Request |
| Recurso não encontrado por ID | 404 Not Found com `{ "error": "mensagem" }` |
| Erro interno no servidor | 500 com `{ "error": "Erro interno no servidor." }` |

---

## Enums — Valores Aceitos

> **No JSON do body (POST/PUT):** envie o valor **inteiro** do enum.  
> **Nos query params (GET):** envie o **nome** do enum (ex.: `?categoria=Racao`).  
> **No Swagger, os valores disponíveis já aparecem num dropdown automático.**

### Categoria (Produto)

| Valor JSON | Nome | Gravado no banco |
|------------|------|-----------------|
| `0` | Racao | `RACAO` |
| `1` | Medicamento | `MEDICAMENTO` |
| `2` | Acessorio | `ACESSORIO` |
| `3` | Servico | `SERVICO` |
| `4` | Outro | `OUTRO` |

### Espécie (`especieIndicada` / `especieAlvo`)

| Valor JSON | Nome | Gravado no banco |
|------------|------|-----------------|
| `0` | Cachorro | `CACHORRO` |
| `1` | Gato | `GATO` |
| `2` | Passaro | `PASSARO` |
| `3` | Reptil | `REPTIL` |
| `4` | Roedor | `ROEDOR` |
| `5` | Todos | `TODOS` |
| `6` | Outro | `OUTRO` |
| `7` | Bovino | `BOVINO` |
| `8` | Equino | `EQUINO` |

### Tipo do Lembrete

| Valor JSON | Nome | Gravado no banco |
|------------|------|-----------------|
| `0` | Vacina | `VACINA` |
| `1` | Medicamento | `MEDICAMENTO` |
| `2` | Consulta | `CONSULTA` |
| `3` | Higiene | `HIGIENE` |
| `4` | Outro | `OUTRO` |

### Status do Lembrete

| Valor JSON | Nome | Gravado no banco |
|------------|------|-----------------|
| `0` | Pendente | `PENDENTE` |
| `1` | Enviado | `ENVIADO` |
| `2` | Cancelado | `CANCELADO` |

### Tipo do Evento Pet

| Valor JSON | Nome | Gravado no banco |
|------------|------|-----------------|
| `0` | Vacinacao | `VACINACAO` |
| `1` | Feira | `FEIRA` |
| `2` | Castracao | `CASTRACAO` |
| `3` | Workshop | `WORKSHOP` |
| `4` | Outro | `OUTRO` |

---

## Integrantes do Grupo

| Nome | RM |
|------|----|
| Fabrício Henrique Pereira| RM563237 |
| Henrique Sinkevicius Maran | RM562977 |
| Leonardo José Pereira | RM563065 |
| Miguel Henrique Oliveira Dias | RM565492 |
| Pedro Henrique de Oliveira | RM562312 |

---

## Licença

Distribuído sob licença MIT — mais detalhes no arquivo `LICENSE`.
