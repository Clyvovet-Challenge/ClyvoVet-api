# ClyvoVet API — .NET

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat&logo=dotnet&logoColor=white)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-8.0-0078D4?style=flat&logo=microsoft&logoColor=white)
![Entity Framework Core](https://img.shields.io/badge/Entity_Framework_Core-8.0-68217A?style=flat&logo=nuget&logoColor=white)
![Oracle](https://img.shields.io/badge/Oracle_Database-XE-F80000?style=flat&logo=oracle&logoColor=white)
![Swagger](https://img.shields.io/badge/Swagger-OpenAPI-85EA2D?style=flat&logo=swagger&logoColor=black)
![Serilog](https://img.shields.io/badge/Serilog-Structured_Logging-1B1F26?style=flat)
![OpenTelemetry](https://img.shields.io/badge/OpenTelemetry-Tracing_%26_Metrics-425CC7?style=flat&logo=opentelemetry&logoColor=white)
![xUnit](https://img.shields.io/badge/xUnit-Testes_Automatizados-512BD4?style=flat)

---

## Sobre o Projeto

A **ClyvoVet API** é uma API RESTful construída em **ASP.NET Core 8**, desenvolvida como parte do **Challenge FIAP 2026 — projeto Clyvo Vet**. Ela é responsável pelo **domínio de engajamento** dentro da plataforma veterinária, respondendo por:

- Catálogo de produtos e serviços veterinários
- Sugestões personalizadas de produtos por animal
- Lembretes de saúde e cuidados para tutores
- Eventos pet públicos (campanhas de vacinação, feiras, workshops)
- **Widget de Saúde Preditiva** — sugere condições de saúde relevantes pra espécie/raça/idade de um animal
- **Envio de mensagens no WhatsApp** — via Twilio, para notificar tutores

Na **Sprint 3**, foi adicionada uma camada inteira de observabilidade e testes automatizados à API:

- **Health Checks** (`/health`, `/health/live`, `/health/ready`) que verificam a conectividade real com o Oracle.
- **Logging estruturado** via Serilog (console + arquivo), com correlação de requisições pelo header `X-Correlation-Id`.
- **Distributed tracing e métricas** através do OpenTelemetry (spans exportados no console e endpoint `/metrics` no formato Prometheus).
- **74 testes automatizados** (45 unitários + 29 de integração), cobrindo a camada de Aplicação (Services) e todo o fluxo HTTP (Controllers → banco em memória).

---

## Arquitetura

O mesmo banco Oracle XE (FIAP) é compartilhado por duas APIs independentes, cada qual em seu próprio container Docker:

| API | Responsabilidade | Tabelas gerenciadas |
|-----|-----------------|---------------------|
| **.NET (este projeto)** | Engajamento e catálogo | `t_clyvo_produto`, `t_clyvo_sugestao_produto`, `t_clyvo_lembrete`, `t_clyvo_evento_pet`, `t_clyvo_predisposicao_saude` |
| **Java (parceira)** | Clínica e cadastro | `t_clyvo_tutor`, `t_clyvo_animal`, `t_clyvo_clinica`, `t_clyvo_veterinario`, `t_clyvo_evento_clinico`, `t_clyvo_pagamento` |

> Para validar FKs e enriquecer as respostas, a API .NET **lê** as tabelas de animal e tutor mantidas pela API Java — mas em nenhum momento **escreve** nelas.

---

## Tecnologias

| Tecnologia | Versão | Uso |
|------------|--------|-----|
| .NET / ASP.NET Core | 8.0 | Framework da API |
| Entity Framework Core | 8.0.11 | ORM (Database-First, sem migrations) |
| Oracle.EntityFrameworkCore | 8.21.121 | Provider Oracle para EF Core |
| Swashbuckle.AspNetCore | 10.1.7 | Geração do Swagger / OpenAPI |
| Microsoft.OpenApi | 2.4.1 | Modelos OpenAPI (namespace atualizado na v2) |
| Oracle Database XE | — | Banco de dados |
| Microsoft.Extensions.Diagnostics.HealthChecks | 8.0.11 | Health Checks (`/health`, `/health/live`, `/health/ready`) |
| Serilog.AspNetCore | 10.0.0 | Logging estruturado (console + arquivo) |
| OpenTelemetry (.NET SDK) | 1.18.0 | Distributed tracing + métricas de desempenho |
| OpenTelemetry.Exporter.Prometheus.AspNetCore | 1.18.0-beta.1 | Endpoint `/metrics` no formato Prometheus |
| xUnit + Moq | 2.9.3 / 4.20.72 | Testes unitários (padrão AAA) |
| Microsoft.AspNetCore.Mvc.Testing | 8.0.11 | Testes de integração via `WebApplicationFactory` |
| Microsoft.EntityFrameworkCore.InMemory | 8.0.11 | Banco em memória usado nos testes de integração |
| Twilio | 8.0.0 | Envio de mensagens no WhatsApp (Sandbox) |

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
│   ├── Models/                 → Entidades mapeadas nas tabelas Oracle
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
│   ├── appsettings.json       → Connection string Oracle (placeholder) + níveis de log
│   ├── Program.cs             → DI, Swagger, Health Checks, Serilog, OpenTelemetry, middleware de erros
│   ├── Logs/                   → Arquivos de log gerados pelo Serilog (não versionado)
│   ├── ClyvoVet.Api.Tests.Unit/         → Testes unitários (Services, mocks via Moq)
│   └── ClyvoVet.Api.Tests.Integration/  → Testes de integração (WebApplicationFactory + EF Core InMemory)
└── schema/
    ├── 01_criar_tabelas_dotnet.sql             → DDL das 4 tabelas originais + triggers + fn_uuid()
    ├── 02_seed_dotnet.sql                       → Dados de exemplo para os endpoints originais
    ├── 03_drop_tabelas_dotnet.sql               → Remove as 5 tabelas .NET
    ├── 04_criar_tabela_predisposicao_dotnet.sql → DDL da tabela do Widget de Saúde Preditiva
    ├── 05_seed_predisposicao_dotnet.sql         → 42 predisposições reais por espécie/raça/idade
    └── README.md                                → Guia do schema
```

---

## Como Executar

### Pré-requisitos

| Ferramenta | Versão mínima | Para que serve |
|------------|--------------|----------------|
| [.NET SDK](https://dotnet.microsoft.com/download/dotnet/8.0) | 8.0 | Compilar e rodar a API |
| Oracle Database | XE 21c+ | Banco de dados |
| Oracle SQL Developer | Qualquer | Executar os scripts SQL |
| Git | Qualquer | Clonar o repositório |

---

### Passo 1 — Clonar o repositório

```bash
git clone https://github.com/pedrinzz10/ClyvoVet-api.git
cd ClyvoVet-api
```

**Confira se o clone deu certo:**

```bash
ls
# Deve listar: ClyvoVet.Api/  schema/  README.md  ClyvoVet-api.slnx  ...
```

> **Erro: `git: command not found`**  
> O Git não está instalado nessa máquina. Baixe em [git-scm.com](https://git-scm.com) ou use "Code → Download ZIP" direto no GitHub.

> **Erro: `Repository not found`**  
> Confirme se a URL está certa e se o repositório está público.

---

### Passo 2 — Configurar a connection string

O arquivo `ClyvoVet.Api/appsettings.json`, que fica versionado no repositório, traz só um **placeholder** — não coloque sua senha real ali, senão você corre o risco de subir a credencial sem perceber. O jeito recomendado é usar o **User Secrets** do .NET, que guarda a connection string **fora da pasta do projeto**, num arquivo local que o `git` jamais enxerga:

```bash
cd ClyvoVet.Api
dotnet user-secrets set "ConnectionStrings:OracleConnection" "User Id=SEU_RM;Password=SUA_SENHA;Data Source=oracle.fiap.com.br:1521/ORCL;"
```

> Se for a primeira vez e o projeto ainda não tiver um `UserSecretsId`, rode antes: `dotnet user-secrets init`.

A API já lê o User Secrets automaticamente em ambiente de desenvolvimento, então não há nada mais para editar. Para conferir o que ficou salvo:

```bash
dotnet user-secrets list
```

**Alternativa (menos segura):** gravar a connection string diretamente no `appsettings.json` local. Funciona igual, mas uma vez que sua senha real estiver ali, **evite fazer commit** do arquivo — confira com `git status` antes de commitar.

```json
{
  "ConnectionStrings": {
    "OracleConnection": "User Id=SEU_RM;Password=SUA_SENHA;Data Source=oracle.fiap.com.br:1521/ORCL;"
  }
}
```

**Formato da connection string Oracle:**

| Parte | Exemplo | Descrição |
|-------|---------|-----------|
| `User Id` | `rmxxxxxx` | Seu RM (usuário Oracle FIAP) |
| `Password` | `fiap26` | Senha do Oracle FIAP |
| `Data Source` | `oracle.fiap.com.br:1521/ORCL` | Host:Porta/ServiceName |

> **⚠️ O service name certo pro Oracle FIAP é `ORCL`**  
> Usar `XE` ou `XEPDB1` gera `ORA-12514: TNS:listener não tem conhecimento sobre o serviço`.

**Pra Oracle local (instalação própria):**

```json
"OracleConnection": "User Id=system;Password=SUA_SENHA;Data Source=localhost:1521/XEPDB1;"
```

> **Erro: `ORA-01017: invalid username/password`**  
> Usuário ou senha errados. Confira as credenciais no portal FIAP ou redefina a senha pelo SQL Developer.

> **Erro: `ORA-12514: TNS:listener não tem conhecimento sobre o serviço`**  
> Service name errado. Vá testando `ORCL`, `XEPDB1` ou `XE` até conectar. No SQL Developer dá pra ver o service name correto na configuração de uma conexão existente.

> **Erro: `ORA-12541: TNS:no listener`** ou **`Connection refused`**  
> O servidor Oracle está inacessível. Cheque sua conexão com a internet (o servidor FIAP fica fora da rede local) ou veja se o Oracle local está de pé (`services.msc` → `OracleServiceXE`).

---

### Passo 3 — Preparar o banco de dados

Abra o **Oracle SQL Developer**, conecte com suas credenciais e rode os scripts nesta ordem:

#### 3.1 — Criar as tabelas

1. Abra `schema/01_criar_tabelas_dotnet.sql` no SQL Developer
2. Pressione **F5** (Run Script — não F9)
3. Aguarde até ver no output:

```
Table T_CLYVO_PRODUTO created.
Table T_CLYVO_EVENTO_PET created.
Table T_CLYVO_LEMBRETE created.
Table T_CLYVO_SUGESTAO_PRODUTO created.
Trigger TRG_PRODUTO_ID compiled.
...
```

> **Pode reexecutar esse script sem medo** — ele dropa as tabelas existentes antes de recriar.

> **Erro: `ORA-00942: table or view does not exist`** durante o DROP  
> Esperado na primeira execução. O script usa `EXCEPTION WHEN OTHERS THEN NULL` justamente pra ignorar esse erro — pode seguir em frente.

> **Erro: `ORA-01031: insufficient privileges`**  
> Seu usuário não tem permissão pra criar tabelas. Conecte com um usuário com privilégios de DBA ou peça ajuda ao administrador do banco.

> **Erro: `ORA-00955: name is already used by an existing object`**  
> Já existe algum objeto (trigger, função) com esse nome. Rode `schema/03_drop_tabelas_dotnet.sql` primeiro pra limpar, depois execute o `01` de novo.

#### 3.2 — Inserir dados de exemplo

1. Abra `schema/02_seed_dotnet.sql`
2. Pressione **F5**
3. Confira o output:

```
BLOCO 1 — Produtos e Eventos Pet
--- Inserindo produtos ---
[OK] 5 produtos inseridos.
--- Inserindo eventos pet ---
[OK] 4 eventos inseridos.
[COMMIT] Bloco 1 salvo.

BLOCO 2 — Tutor, Animal, Lembretes e Sugestoes
--- Resolvendo animal_id ---
[INFO] t_clyvo_animal vazia. Criando tutor e animal de seed...
[OK] Tutor de seed criado: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
[OK] Animal de seed criado: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
--- Inserindo lembretes ---
[OK] 3 lembretes inseridos.
--- Inserindo sugestoes de produto ---
[OK] 3 sugestoes inseridas.
[COMMIT] Bloco 2 salvo.
```

> **`[ERRO] Bloco 1`** — Sinal de que o script `01` ainda não rodou. Execute-o primeiro.

> **`[AVISO] Nao foi possivel acessar t_clyvo_animal`**  
> A tabela `t_clyvo_animal` não existe ainda. Rode o script `01` (que cria todas as tabelas) antes do `02`.

> **`[ERRO] Bloco 2: ORA-00001: unique constraint violated`**  
> Sinal de que o seed já rodou antes. É comum acontecer com o tutor de seed (CPF `00000000000`) — o script tolera isso, já que o Bloco 1 foi commitado e o Bloco 2 tenta localizar o tutor existente antes de criar um novo.

> **A contagem final de registros deve mostrar:**

```
TABELA                   TOTAL
------------------------ -----
T_CLYVO_ANIMAL               1
T_CLYVO_LEMBRETE             3
T_CLYVO_PRODUTO              5
T_CLYVO_EVENTO_PET           4
T_CLYVO_SUGESTAO_PRODUTO     3
T_CLYVO_TUTOR                1
```

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
> O .NET SDK não está instalado, ou não está no PATH. Baixe em [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/8.0) e reinicie o terminal depois de instalar.

> **Erro: `NETSDK1045: The current .NET SDK does not support targeting .NET 8.0`**  
> A versão do SDK instalada é incompatível. Rode `dotnet --version` pra ver qual está ativa — precisa ser **8.0.x ou superior**.

> **Erro: `Unable to load the service index for source https://api.nuget.org`**  
> Falta acesso à internet pra baixar os pacotes. Verifique a conexão ou configure um proxy NuGet, caso esteja numa rede corporativa.

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
> Outro processo já está usando a porta 5191. Encerre esse processo:
> ```bash
> # Windows
> netstat -ano | findstr :5191
> taskkill /PID <numero_do_pid> /F
> ```
> Ou troque a porta em `Properties/launchSettings.json`.

> **Erro: `ORA-12514` ou `ORA-12541` ao fazer a primeira requisição**  
> A connection string está incorreta — volte ao Passo 2. A aplicação sobe normalmente mesmo com credenciais inválidas; o erro só aparece na primeira chamada ao banco.

> **Erro: `Unable to load DLL 'oci.dll'`**  
> Falta o cliente Oracle nativo. Como o pacote `Oracle.ManagedDataAccess` é 100% gerenciado, ele **dispensa** o Oracle Client instalado — confira se o projeto está usando a versão certa do pacote (`Oracle.ManagedDataAccess.Core` ou `Oracle.EntityFrameworkCore`).

> **A API sobe mas retorna `500` em todos os endpoints**  
> Olhe os logs no terminal — é lá que aparece o erro real. As causas mais comuns:
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

O Swagger vai listar todos os endpoints agrupados por controller.

> **Swagger está sempre ativo** — não fica restrito ao ambiente `Development`. Funciona em Docker, servidor e produção.

> **Erro: `ERR_CONNECTION_RESET` ou `ERR_SSL_PROTOCOL_ERROR` no HTTPS**  
> O certificado de desenvolvimento não é confiável ainda. Rode:
> ```bash
> dotnet dev-certs https --clean
> dotnet dev-certs https --trust
> ```
> Confirme quando o Windows pedir e reinicie a aplicação. Se ainda der problema, use a URL HTTP.

> **Swagger abre mas mostra "Failed to fetch" ao executar endpoints**  
> O Swagger está tentando HTTPS com um certificado inválido. Clique em "Servers" no topo e selecione a URL HTTP (`http://localhost:5191`).

> **Página em branco ou `404` ao acessar `/swagger`**  
> A aplicação está de pé, mas o Swagger não foi servido. Confira se `app.UseSwagger()` e `app.UseSwaggerUI()`, no `Program.cs`, estão **fora** de qualquer bloco `if (app.Environment.IsDevelopment())`.

---

### Alternativa — Executar via Visual Studio ou Rider

1. Abra `ClyvoVet-api.slnx` na IDE
2. Selecione o perfil `http` no dropdown de execução
3. Pressione **F5** (com debug) ou **Ctrl+F5** (sem debug)
4. O navegador abre sozinho no Swagger

> **Visual Studio:** se o navegador abrir em `weatherforecast` ou numa página em branco, confira se `launchUrl` em `Properties/launchSettings.json` está definido como `"swagger"`.

---

### Verificação rápida — API funcionando

Depois de subir a API, faça uma requisição de teste:

```bash
curl http://localhost:5191/api/v1/produtos
```

**Resposta esperada:** um array JSON com os produtos do seed. Se vier `[]`, o banco está conectado mas o seed não rodou. Se vier `{"error": "Erro interno no servidor."}`, tem algo errado na connection string — confira os logs do terminal.

---

## Monitoramento e Observabilidade

### Health Checks

A API expõe três endpoints de Health Check, usando `Microsoft.Extensions.Diagnostics.HealthChecks`:

| Endpoint | O que verifica | Uso |
|----------|----------------|-----|
| `GET /health` | Todos os checks (visão geral) | Diagnóstico manual, painel de monitoramento |
| `GET /health/live` | Apenas se o processo da API está de pé (`self`) | Liveness probe (ex.: Kubernetes, Docker healthcheck) |
| `GET /health/ready` | Conectividade real com o Oracle (`Database.CanConnectAsync()`) | Readiness probe — como o Oracle FIAP é um serviço **externo** ao processo, esse check também cobre "disponibilidade de serviços externos" |

Cada resposta traz um JSON com o status geral, a duração total e o detalhe de cada verificação:

```bash
curl http://localhost:5191/health
```

```json
{
  "status": "Healthy",
  "totalDurationMs": 5.34,
  "checks": [
    { "name": "self", "status": "Healthy", "durationMs": 0.02, "tags": ["live"] },
    { "name": "oracle-database", "status": "Healthy", "durationMs": 4.92, "tags": ["ready", "database", "external"] }
  ]
}
```

Quando o Oracle fica inacessível (connection string errada, sem internet, etc.), o `status` muda para `"Unhealthy"` e o campo `error` de cada check traz a exceção correspondente.

### Logging Estruturado (Serilog)

- Fica configurado em [`Program.cs`](ClyvoVet.Api/Program.cs) e escreve ao mesmo tempo no **console** e num **arquivo** (`Logs/clyvovet-api-*.log`, com rotação diária e retenção dos últimos 7 dias).
- Cada linha de log carrega um **Correlation ID** por requisição, gerado pelo [`CorrelationIdMiddleware`](ClyvoVet.Api/Middleware/CorrelationIdMiddleware.cs) — ou reaproveitado do header `X-Correlation-Id` quando o cliente envia um valor que passa na validação de tamanho/formato — e devolvido também na resposta.
- Três níveis são usados: `Information` para requisições HTTP concluídas, `Warning` para erros de negócio esperados (404/400) e `Error` para exceções não tratadas (500).
- Os níveis mínimos por categoria podem ser ajustados em [`appsettings.json`](ClyvoVet.Api/appsettings.json), na seção `"Serilog"`.

### Tracing e Métricas (OpenTelemetry)

- **Tracing:** ASP.NET Core, `HttpClient` e Entity Framework Core são instrumentados automaticamente — cada requisição gera uma árvore de spans exportada para o **console**.
- **Métricas:** expostas em formato Prometheus via `GET /metrics`, cobrindo tempo de resposta, contagem de requisições e taxa de erros por rota/status code.

```bash
curl http://localhost:5191/metrics
```

---

## Testes Automatizados

Dentro de `ClyvoVet.Api/` os testes ficam divididos em dois projetos separados, seguindo o padrão **AAA (Arrange, Act, Assert)** e a convenção de nomenclatura `MetodoTestado_Cenario_ResultadoEsperado`:

| Projeto | O que testa | Ferramentas |
|---------|-------------|-------------|
| `ClyvoVet.Api.Tests.Unit` | Camada de Aplicação (`Services/`) — regras de negócio isoladas, com os repositórios mockados | xUnit + Moq |
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

**Resultado esperado:** `74` testes passando (`45` unitários + `29` de integração).

### Detalhes dos testes de integração

- Toda a API sobe em memória por meio de `WebApplicationFactory<Program>`, o que **substitui o Oracle real por um banco EF Core InMemory** — logo, rodar `dotnet test` não exige acesso ao Oracle FIAP.
- A maioria dos testes recorre a uma **Collection Fixture** (`IntegrationTestFixture` + `[CollectionDefinition]`) que sobe a API **uma única vez** para toda a suíte, semeando um Tutor, um Animal e um Produto de teste. Já os testes do Widget de Saúde Preditiva sobem uma instância própria, à parte, porque dependem de um Animal com raça e idade específicas.

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

> Mesmo pertencendo à API Java, a `T_CLYVO_TUTOR` é indispensável: o `AnimalRepository` faz `.Include(a => a.Tutor)`, e sem essa tabela a API dispara `ORA-00942` em qualquer endpoint de lembrete ou sugestão.

---

### Geração de IDs (UUID)

Todos os IDs saem do Oracle via a função `fn_uuid()`, chamada no trigger `BEFORE INSERT` de cada tabela. O código C# **nunca** gera UUIDs — o EF Core usa `RETURNING` pra ler o valor já gerado:

```sql
-- Função fn_uuid() — definida em 01_criar_tabelas_dotnet.sql
CREATE OR REPLACE FUNCTION fn_uuid RETURN VARCHAR2 IS
BEGIN
  RETURN LOWER(REGEXP_REPLACE(RAWTOHEX(SYS_GUID()),
    '([A-F0-9]{8})([A-F0-9]{4})([A-F0-9]{4})([A-F0-9]{4})([A-F0-9]{12})',
    '\1-\2-\3-\4-\5'));
END;
```

---

### Scripts disponíveis

| Arquivo | Quando usar |
|---------|-------------|
| `schema/01_criar_tabelas_dotnet.sql` | Primeira vez ou para recriar tudo do zero |
| `schema/02_seed_dotnet.sql` | Após o `01` — insere produtos, eventos, lembretes e sugestões de exemplo |
| `schema/03_drop_tabelas_dotnet.sql` | Para limpar apenas as tabelas .NET (preserva as Java) |
| `schema/04_criar_tabela_predisposicao_dotnet.sql` | Cria a tabela `T_CLYVO_PREDISPOSICAO_SAUDE` (widget de saúde preditiva) |
| `schema/05_seed_predisposicao_dotnet.sql` | Após o `04` — insere as 42 predisposições de saúde por espécie/raça/idade |

---

## Documentação das Rotas

> **Base path:** `/api/v1/`  
> Todos os endpoints retornam `application/json`.

---

### 🛒 Produtos — `/api/v1/produtos`

Cuida do catálogo de produtos e serviços veterinários (`T_CLYVO_PRODUTO`).

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
| `especieIndicada` | enum | — | `Cachorro` \| `Gato` \| `Passaro` \| `Reptil` \| `Roedor` \| `Todos` \| `Outro` \| `Bovino` \| `Equino` |

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

Cuida dos eventos públicos para pets (`T_CLYVO_EVENTO_PET`). Sem dependência de FK com as tabelas Java.

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

Cuida dos lembretes de cuidados vinculados a um animal (`T_CLYVO_LEMBRETE`).  
⚠️ Exige um `animalId` válido em `T_CLYVO_ANIMAL` (e a existência de `T_CLYVO_TUTOR`).

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
  "titulo": "Vacina Antirrábica — Reforço Anual",
  "descricao": "Aplicar a vacina antirrábica no pet shop da rua central.",
  "tipo": 0,
  "agendadoEm": "2026-09-15T10:00:00",
  "recorrente": true,
  "status": 0
}
```

> **Atenção:** o `status` é **sempre forçado a `Pendente` (0)** na criação, não importa o valor enviado.  
> `agendadoEm` precisa ser uma data/hora **futura**.

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
  "status": 0,
  "criadoEm": "2026-05-24T10:30:00"
}
```

---

### 💡 Sugestões de Produto — `/api/v1/sugestoes-produto`

Cuida das sugestões de produto vinculadas a um animal (`T_CLYVO_SUGESTAO_PRODUTO`).  
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

> Se `dataSugestao` for omitido, assume a data de hoje.

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

Esse card cruza os dados do animal (espécie, raça e idade) com um catálogo de predisposições de saúde (`T_CLYVO_PREDISPOSICAO_SAUDE`) e, ao encontrar alguma condição relevante, sugere agendar uma consulta.

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

- A comparação de raça é flexível: maiúsculas e minúsculas são ignoradas e substrings casam nos dois sentidos, de modo que pequenas variações de digitação na raça cadastrada ainda batem com o catálogo.
- Um `idadeMinimaAnos` nulo ou `0` serve para qualquer idade; fora isso, a idade do animal tem que ser maior ou igual ao mínimo — e um animal sem data de nascimento cadastrada jamais bate com um mínimo acima de zero.
- Se a espécie do animal não for reconhecida, o widget simplesmente devolve a lista de predisposições vazia, sem lançar erro.
- `sugerirAgendamentoConsulta` retorna `true` assim que pelo menos uma predisposição é encontrada — o widget se limita a sugerir; criar a consulta fica por conta de outra etapa.
- Um `animalId` que não existe resulta em 404.

---

### 📱 WhatsApp — `/api/v1/whatsapp`

> ⚠️ Feature extra, fora do escopo avaliado da Sprint 3.

Funciona como o único ponto de disparo de mensagens no WhatsApp, usando o [Twilio](https://www.twilio.com/whatsapp) (WhatsApp Sandbox). Outras partes do sistema — lembretes, sugestões, etc. — se apoiam nele para notificar o tutor sem duplicar a lógica de envio.

| Método | Rota | Descrição | Status |
|--------|------|-----------|--------|
| POST | `/api/v1/whatsapp/enviar` | Envia uma mensagem de WhatsApp para o número informado | 204 |

**Request — POST**

```json
{
  "telefone": "+5511999999999",
  "mensagem": "Seu pet tem um lembrete de vacina agendado para amanhã."
}
```

**Configuração**

Requer três chaves em `Twilio` (via `dotnet user-secrets`, nunca no `appsettings.json` versionado):

```bash
dotnet user-secrets set "Twilio:AccountSid" "SEU_ACCOUNT_SID"
dotnet user-secrets set "Twilio:AuthToken" "SEU_AUTH_TOKEN"
dotnet user-secrets set "Twilio:NumeroSandbox" "whatsapp:+1XXXXXXXXXX"
```

O [Console do Twilio](https://console.twilio.com) disponibiliza o Account SID, o Auth Token e o número do sandbox em **Messaging → Try out WhatsApp**. Antes de poder receber qualquer mensagem, o destinatário precisa dar o "join" no sandbox pelo próprio WhatsApp.

O endpoint também exige uma **API key própria** no header `X-Api-Key` — sem ela, retorna `401`:

```bash
dotnet user-secrets set "WhatsApp:ApiKey" "SUA_CHAVE_AQUI"
```

```bash
curl -X POST http://localhost:5191/api/v1/whatsapp/enviar \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: SUA_CHAVE_AQUI" \
  -d '{"telefone":"+5511999999999","mensagem":"Teste"}'
```

> ⚠️ **Limitação conhecida:** numa conta **trial** do Twilio, qualquer mensagem enviada via API exige um `ContentSid` (template pré-aprovado); mandar texto livre (`Body`) é rejeitado com o erro `21654 ContentSid Required`, mesmo dentro de uma janela de sessão ativa. Tentar criar ou consultar templates pela Content API também esbarra num `403` em conta trial (`This feature is not available on a Trial account`). Na prática, isso quer dizer que **o endpoint funciona normalmente numa conta Twilio paga/produção**, mas testá-lo de ponta a ponta não foi possível com uma conta trial gratuita. O código está pronto; falta só uma conta Twilio com upgrade feito para validar de verdade.
>
> É por isso que o teste de integração do endpoint (`WhatsAppEndpointsTests`) troca o `IWhatsAppService` real por um fake: ele confirma que o controller recebe a requisição, aciona o serviço com os dados corretos e devolve `204`, sem depender do Twilio de fato.

---

## Guia de Testes Manuais

> **54 testes** conferidos com Oracle real, todos passando.  
> Acesse **`http://localhost:5191/swagger`**, siga a sequência indicada e reaproveite os JSONs já prontos.  
> Legenda dos ícones: ✅ sucesso &nbsp;|&nbsp; ❌ erro esperado (validação)

---

### Antes de começar — obtenha os IDs necessários

Execute no **Oracle SQL Developer** depois de rodar o seed:

```sql
-- animal_id (necessário nos testes de Lembrete e Sugestão)
SELECT id, nome FROM t_clyvo_animal WHERE ROWNUM = 1;

-- produto_id do seed (necessário nos testes de Sugestão)
SELECT id, nome FROM t_clyvo_produto WHERE ROWNUM = 1;
```

> Guarde os dois UUIDs — eles substituem `{ANIMAL_ID}` e `{PRODUTO_ID}` nos testes abaixo.  
> Também dá pra pegar o `animalId` direto no response do **T23** (GET /lembretes).

---

## 🛒 BLOCO 1 — Produtos

---

### T01 — Listar todos os produtos
**Confirma a conexão com o Oracle. Deve devolver os produtos do seed.**

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

✅ **Esperado:** `201 Created` — produto criado com `id` gerado pelo Oracle.

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

✅ **Esperado:** `201 Created` — evento criado com `id` gerado pelo Oracle.

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

> ⚠️ A partir do T28, os testes exigem um `animalId` válido.  
> Pegue esse valor no T23 (campo `animalId` de qualquer lembrete do seed) ou pelo SQL do pré-requisito.

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
> Pegue esses valores pelo SQL do pré-requisito ou pelos GETs anteriores.

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

✅ **Esperado:** `201 Created` — sugestão criada com `id` gerado pelo Oracle.

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

> Execute na ordem abaixo para limpar os registros criados durante os testes.

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

> **Resultado esperado ao final:** todos os 54 testes passam, cada um com o status code indicado.  
> Essa suíte rodou contra Oracle real e fechou em **54/54 PASS**.

---

## Regras de Negócio

### Produto

| Regra | Comportamento |
|-------|---------------|
| Preço não pode ser negativo | 400 Bad Request |
| ID gerado pelo Oracle | Campo `id` ignorado no request — gerado via `fn_uuid()` na trigger |

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
> **Nos query params (GET):** envie o **nome** do enum (ex: `?categoria=Racao`).  
> **O Swagger exibe os valores disponíveis com dropdown automático.**

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
| Leonardo José Pereira | RM563065 |
| Miguel Henrique Oliveira Dias | RM565492 |
| Pedro Henrique de Oliveira | RM562312 |

---

## Licença

Distribuído sob a licença MIT. Consulte o arquivo `LICENSE` para mais informações.
