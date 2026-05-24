# ClyvoVet API — .NET

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat&logo=dotnet&logoColor=white)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-8.0-0078D4?style=flat&logo=microsoft&logoColor=white)
![Entity Framework Core](https://img.shields.io/badge/Entity_Framework_Core-8.0-68217A?style=flat&logo=nuget&logoColor=white)
![Oracle](https://img.shields.io/badge/Oracle_Database-XE-F80000?style=flat&logo=oracle&logoColor=white)
![Swagger](https://img.shields.io/badge/Swagger-OpenAPI-85EA2D?style=flat&logo=swagger&logoColor=black)

---

## Sobre o Projeto

A **ClyvoVet API** é uma API RESTful desenvolvida em **ASP.NET Core 8** como parte do **Challenge FIAP 2026 — projeto Clyvo Vet**. Ela compõe o **domínio de engajamento** da plataforma veterinária, responsável por:

- Catálogo de produtos e serviços veterinários
- Sugestões personalizadas de produtos por animal
- Lembretes de saúde e cuidados para tutores
- Eventos pet públicos (campanhas de vacinação, feiras, workshops)

---

## Arquitetura

A plataforma usa **duas APIs independentes** compartilhando o mesmo banco Oracle XE (FIAP), cada uma em seu próprio container Docker:

| API | Responsabilidade | Tabelas gerenciadas |
|-----|-----------------|---------------------|
| **.NET (este projeto)** | Engajamento e catálogo | `t_clyvo_produto`, `t_clyvo_sugestao_produto`, `t_clyvo_lembrete`, `t_clyvo_evento_pet` |
| **Java (parceira)** | Clínica e cadastro | `t_clyvo_tutor`, `t_clyvo_animal`, `t_clyvo_clinica`, `t_clyvo_veterinario`, `t_clyvo_evento_clinico`, `t_clyvo_pagamento` |

> A API .NET **lê** as tabelas da API Java (animal e tutor) para validar FKs e enriquecer respostas, mas **nunca escreve** nelas.

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

---

## Estrutura de Pastas

```
ClyvoVet-api/
├── ClyvoVet.Api/
│   ├── Controllers/          → Recebem requisições HTTP e delegam ao Service
│   ├── Services/             → Regras de negócio
│   │   └── Interfaces/
│   ├── Repositories/         → Acesso ao banco via EF Core
│   │   └── Interfaces/
│   ├── Models/               → Entidades mapeadas nas tabelas Oracle
│   ├── DTOs/
│   │   ├── Request/          → Dados recebidos nas requisições (POST/PUT)
│   │   └── Response/         → Dados retornados nas respostas
│   ├── Enums/                → Enumerações dos valores aceitos pelo banco
│   ├── Data/
│   │   ├── AppDbContext.cs   → DbContext principal
│   │   └── Configurations/   → Fluent API (mapeamento tabela ↔ modelo)
│   ├── Exceptions/           → NotFoundException, BadRequestException
│   ├── Properties/
│   │   └── launchSettings.json
│   ├── appsettings.json      → Connection string Oracle
│   └── Program.cs            → DI, Swagger, middleware de erros
└── schema/
    ├── 01_criar_tabelas_dotnet.sql  → DDL completo + triggers + fn_uuid()
    ├── 02_seed_dotnet.sql           → Dados de exemplo para todos os endpoints
    ├── 03_drop_tabelas_dotnet.sql   → Remove apenas as 4 tabelas .NET
    └── README.md                    → Guia do schema
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

**Verifique se o clone funcionou:**

```bash
ls
# Deve listar: ClyvoVet.Api/  schema/  README.md  ClyvoVet-api.slnx  ...
```

> **Erro: `git: command not found`**  
> Git não está instalado. Baixe em [git-scm.com](https://git-scm.com) ou use o botão "Code → Download ZIP" no GitHub.

> **Erro: `Repository not found`**  
> Verifique se a URL está correta e se o repositório é público.

---

### Passo 2 — Configurar a connection string

Abra o arquivo `ClyvoVet.Api/appsettings.json` e substitua as credenciais:

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
| `User Id` | `rm562312` | Seu RM (usuário Oracle FIAP) |
| `Password` | `fiap26` | Senha do Oracle FIAP |
| `Data Source` | `oracle.fiap.com.br:1521/ORCL` | Host:Porta/ServiceName |

> **⚠️ Service name correto para o Oracle FIAP: `ORCL`**  
> Usar `XE` ou `XEPDB1` causa `ORA-12514: TNS:listener não tem conhecimento sobre o serviço`.

**Para Oracle local (instalação própria):**

```json
"OracleConnection": "User Id=system;Password=SUA_SENHA;Data Source=localhost:1521/XEPDB1;"
```

> **Erro: `ORA-01017: invalid username/password`**  
> Usuário ou senha incorretos. Verifique as credenciais no portal FIAP ou redefina a senha no SQL Developer.

> **Erro: `ORA-12514: TNS:listener não tem conhecimento sobre o serviço`**  
> Service name errado. Tente `ORCL`, `XEPDB1` ou `XE` até conectar. No SQL Developer, você pode ver o service name correto na configuração da sua conexão existente.

> **Erro: `ORA-12541: TNS:no listener`** ou **`Connection refused`**  
> O servidor Oracle está inacessível. Verifique sua conexão com a internet (o servidor FIAP é externo) ou se o Oracle local está iniciado (`services.msc` → `OracleServiceXE`).

---

### Passo 3 — Preparar o banco de dados

Abra o **Oracle SQL Developer**, conecte com suas credenciais e execute os scripts na ordem:

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

> **Este script é seguro para reexecutar** — ele dropa as tabelas existentes antes de recriar.

> **Erro: `ORA-00942: table or view does not exist`** durante o DROP  
> Normal na primeira execução. O script usa `EXCEPTION WHEN OTHERS THEN NULL` para ignorar esse erro — continue.

> **Erro: `ORA-01031: insufficient privileges`**  
> Seu usuário não tem permissão para criar tabelas. Conecte com um usuário que tenha privilégios de DBA ou peça ao administrador do banco.

> **Erro: `ORA-00955: name is already used by an existing object`**  
> Algum objeto (trigger, função) já existe com o mesmo nome. Execute `schema/03_drop_tabelas_dotnet.sql` primeiro para limpar, depois rode o `01` novamente.

#### 3.2 — Inserir dados de exemplo

1. Abra `schema/02_seed_dotnet.sql`
2. Pressione **F5**
3. Aguarde e confira o output:

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

> **`[ERRO] Bloco 1`** — O script `01` não foi executado antes. Execute-o primeiro.

> **`[AVISO] Nao foi possivel acessar t_clyvo_animal`**  
> A tabela `t_clyvo_animal` não existe. Execute o script `01` (que cria todas as tabelas) antes do `02`.

> **`[ERRO] Bloco 2: ORA-00001: unique constraint violated`**  
> O seed já foi executado antes. Isso pode acontecer com o tutor de seed (CPF `00000000000`). O script é tolerante a isso — o Bloco 1 já foi commitado e o Bloco 2 tenta encontrar o tutor existente antes de criar um novo.

> **Contagem de registros ao final deve mostrar:**

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
> O .NET SDK não está instalado ou não está no PATH. Baixe em [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/8.0) e reinicie o terminal após instalar.

> **Erro: `NETSDK1045: The current .NET SDK does not support targeting .NET 8.0`**  
> Versão do SDK incompatível. Execute `dotnet --version` para ver a versão instalada. É necessário **8.0.x ou superior**.

> **Erro: `Unable to load the service index for source https://api.nuget.org`**  
> Sem acesso à internet para baixar os pacotes. Verifique sua conexão ou configure um proxy NuGet se estiver em rede corporativa.

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
> A porta 5191 está ocupada por outro processo. Encerre o processo que usa a porta:
> ```bash
> # Windows
> netstat -ano | findstr :5191
> taskkill /PID <numero_do_pid> /F
> ```
> Ou altere a porta em `Properties/launchSettings.json`.

> **Erro: `ORA-12514` ou `ORA-12541` ao fazer a primeira requisição**  
> A connection string está errada. Veja o Passo 2 para corrigir. A aplicação sobe mesmo com credenciais inválidas — o erro aparece só na primeira chamada ao banco.

> **Erro: `Unable to load DLL 'oci.dll'`**  
> O cliente Oracle nativo não está instalado. O pacote `Oracle.ManagedDataAccess` é 100% gerenciado e **não precisa** do Oracle Client instalado — verifique se o projeto usa a versão correta do pacote (`Oracle.ManagedDataAccess.Core` ou `Oracle.EntityFrameworkCore`).

> **A API sobe mas retorna `500` em todos os endpoints**  
> Verifique os logs no terminal — o erro real aparece lá. As causas mais comuns são:
> - Connection string incorreta (usuário, senha ou service name)
> - Tabelas não criadas (execute o script `01` primeiro)
> - Tipo de dado não mapeado no EF Core

---

### Passo 6 — Acessar o Swagger

Com a API rodando, abra no navegador:

| Perfil | URL |
|--------|-----|
| HTTP (recomendado para testes) | http://localhost:5191/swagger |
| HTTPS | https://localhost:7225/swagger |

A interface do Swagger exibirá todos os endpoints agrupados por controller.

> **Swagger está sempre ativo** — não é restrito ao ambiente `Development`. Funciona em Docker, servidor e produção.

> **Erro: `ERR_CONNECTION_RESET` ou `ERR_SSL_PROTOCOL_ERROR` no HTTPS**  
> Certificado de desenvolvimento não confiado. Execute:
> ```bash
> dotnet dev-certs https --clean
> dotnet dev-certs https --trust
> ```
> Confirme a instalação quando o Windows solicitar e reinicie a aplicação. Se o problema persistir, use a URL HTTP.

> **Swagger abre mas mostra "Failed to fetch" ao executar endpoints**  
> O Swagger está tentando usar HTTPS mas o certificado não é válido. Clique em "Servers" no topo do Swagger e selecione a URL HTTP (`http://localhost:5191`).

> **Página em branco ou `404` ao acessar `/swagger`**  
> A aplicação está rodando mas o Swagger não foi servido. Verifique se o `app.UseSwagger()` e `app.UseSwaggerUI()` estão no `Program.cs` **fora** de qualquer bloco `if (app.Environment.IsDevelopment())`.

---

### Alternativa — Executar via Visual Studio ou Rider

1. Abra `ClyvoVet-api.slnx` na IDE
2. Selecione o perfil `http` no dropdown de execução
3. Pressione **F5** (com debug) ou **Ctrl+F5** (sem debug)
4. O navegador abrirá automaticamente no Swagger

> **Visual Studio:** se o navegador abrir em `weatherforecast` ou página em branco, verifique se `launchUrl` em `Properties/launchSettings.json` está definido como `"swagger"`.

---

### Verificação rápida — API funcionando

Após subir, faça uma requisição de teste:

```bash
curl http://localhost:5191/api/v1/produtos
```

**Resposta esperada:** array JSON com os produtos do seed. Se retornar `[]`, o banco está conectado mas o seed não foi executado. Se retornar `{"error": "Erro interno no servidor."}`, há um problema na connection string — verifique os logs do terminal.

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

> `T_CLYVO_TUTOR` é necessária mesmo sendo da API Java, pois o `AnimalRepository` faz `.Include(a => a.Tutor)` — sem ela a API lança `ORA-00942` em qualquer endpoint de lembrete ou sugestão.

---

### Geração de IDs (UUID)

Todos os IDs são gerados pelo Oracle via função `fn_uuid()` chamada no trigger `BEFORE INSERT` de cada tabela. A API **nunca** gera UUIDs no código C# — o EF Core usa `RETURNING` para ler o valor gerado:

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

---

## Documentação das Rotas

> **Base path:** `/api/v1/`  
> Todos os endpoints retornam `application/json`.

---

### 🛒 Produtos — `/api/v1/produtos`

Gerencia o catálogo de produtos e serviços veterinários (`T_CLYVO_PRODUTO`).

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
| `especieIndicada` | enum | — | `Cachorro` \| `Gato` \| `Passaro` \| `Reptil` \| `Roedor` \| `Todos` \| `Outro` |

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

Gerencia eventos públicos para pets (`T_CLYVO_EVENTO_PET`). Não tem dependência de FK com as tabelas Java.

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
| `especieAlvo` | enum | — | `Cachorro` \| `Gato` \| `Passaro` \| `Reptil` \| `Roedor` \| `Todos` \| `Outro` |

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

Gerencia lembretes de cuidados vinculados a um animal (`T_CLYVO_LEMBRETE`).  
⚠️ Requer `animalId` válido em `T_CLYVO_ANIMAL` (e `T_CLYVO_TUTOR` existindo).

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

> **Atenção:** `status` é **sempre forçado a `Pendente` (0)** na criação, independente do valor enviado.  
> `agendadoEm` deve ser uma data/hora **futura**.

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

Gerencia sugestões de produto vinculadas a um animal (`T_CLYVO_SUGESTAO_PRODUTO`).  
⚠️ Requer `animalId` válido em `T_CLYVO_ANIMAL` e `produtoId` válido em `T_CLYVO_PRODUTO`.

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

> `dataSugestao` é opcional — se omitido, assume a data de hoje.

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

## Guia de Testes Manuais

> **54 testes** verificados com Oracle real — todos passam.  
> Acesse **`http://localhost:5191/swagger`**, siga a ordem e use os JSONs prontos.  
> Ícones de resultado esperado: ✅ sucesso &nbsp;|&nbsp; ❌ erro esperado (validação)

---

### Antes de começar — obtenha os IDs necessários

Execute no **Oracle SQL Developer** após rodar o seed:

```sql
-- animal_id (necessário nos testes de Lembrete e Sugestão)
SELECT id, nome FROM t_clyvo_animal WHERE ROWNUM = 1;

-- produto_id do seed (necessário nos testes de Sugestão)
SELECT id, nome FROM t_clyvo_produto WHERE ROWNUM = 1;
```

> Guarde esses dois UUIDs — você vai substituir `{ANIMAL_ID}` e `{PRODUTO_ID}` nos testes abaixo.  
> Alternativamente, você pode obter o `animalId` no response do **T23** (GET /lembretes).

---

## 🛒 BLOCO 1 — Produtos

---

### T01 — Listar todos os produtos
**Confirma conexão com Oracle. Deve retornar os produtos do seed.**

```
GET /api/v1/produtos
```

✅ **Esperado:** `200 OK` — array com os produtos cadastrados no seed.

---

### T02 — Filtrar produtos por categoria
```
GET /api/v1/produtos?categoria=Racao
```

✅ **Esperado:** `200 OK` — apenas produtos com `categoria = 0` (Racao).

---

### T03 — Filtrar produtos por espécie
```
GET /api/v1/produtos?especieIndicada=Gato
```

✅ **Esperado:** `200 OK` — apenas produtos para gatos.

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

> 📋 **Copie o `id` retornado** — usado nos testes T07, T08 e T50.

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

✅ **Esperado:** `200 OK` — produto com nome e preço atualizados.

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

✅ **Esperado:** `200 OK` — array com eventos do seed (Feira de Adoção, Vacinação etc.).

---

### T13 — Filtrar eventos por cidade
```
GET /api/v1/eventos-pet?cidade=Sao Paulo
```

✅ **Esperado:** `200 OK` — apenas eventos de São Paulo.

---

### T14 — Filtrar eventos por tipo
```
GET /api/v1/eventos-pet?tipo=Vacinacao
```

✅ **Esperado:** `200 OK` — apenas eventos do tipo `Vacinacao (0)`.

---

### T15 — Filtrar eventos por espécie alvo
```
GET /api/v1/eventos-pet?especieAlvo=Todos
```

✅ **Esperado:** `200 OK` — apenas eventos para todos os animais.

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

> 📋 **Copie o `id` retornado** — usado nos testes T18, T19 e T49.

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

✅ **Esperado:** `200 OK` — evento com título e `gratuito` atualizados.

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

> ⚠️ Os testes T28 em diante exigem um `animalId` válido.  
> Obtenha-o no T23 (campo `animalId` de qualquer lembrete do seed) ou pelo SQL do pré-requisito.

---

### T23 — Listar todos os lembretes
```
GET /api/v1/lembretes
```

✅ **Esperado:** `200 OK` — array com os lembretes do seed (Vacina V10, Vermifugação, Retorno).

> 📋 **Copie o valor de `animalId`** de qualquer item retornado — usado nos testes T26 e T28 em diante.

---

### T24 — Filtrar lembretes por status
```
GET /api/v1/lembretes?status=Pendente
```

✅ **Esperado:** `200 OK` — apenas lembretes com `status = 0` (Pendente).

---

### T25 — Filtrar lembretes por tipo
```
GET /api/v1/lembretes?tipo=Vacina
```

✅ **Esperado:** `200 OK` — apenas lembretes do tipo `Vacina (0)`.

---

### T26 — Filtrar lembretes por animal
```
GET /api/v1/lembretes?animalId={ANIMAL_ID}
```

✅ **Esperado:** `200 OK` — apenas lembretes do animal especificado.

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

✅ **Esperado:** `201 Created` — lembrete criado. O campo `status` **sempre** será `0` (Pendente), mesmo que outro valor seja enviado.

> 📋 **Copie o `id` retornado** — usado nos testes T29 a T32 e T48.

---

### T29 — Buscar lembrete por ID
```
GET /api/v1/lembretes/{id do T28}
```

✅ **Esperado:** `200 OK` — dados completos incluindo `nomeAnimal` preenchido pelo JOIN.

---

### T30 — Verificar que status foi forçado para Pendente
No response do T29, confira que `"status": 0` independente do valor enviado em T28.

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

> ⚠️ Os testes T39 em diante exigem `{ANIMAL_ID}` e `{PRODUTO_ID}` válidos.  
> Obtenha-os pelo SQL do pré-requisito ou pelos GETs anteriores.

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

✅ **Esperado:** `200 OK` — apenas sugestões do animal especificado, ordenadas da mais recente para a mais antiga.

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

> 📋 **Copie o `id` retornado** — usado nos testes T40, T42 e T47.

---

### T40 — Buscar sugestão por ID
```
GET /api/v1/sugestoes-produto/{id do T39}
```

✅ **Esperado:** `200 OK` — dados completos incluindo `nomeAnimal` e `nomeProduto` preenchidos automaticamente pelo JOIN.

---

### T41 — Verificar enriquecimento do response
No response do T40, confirme que os campos de JOIN estão presentes:

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

✅ **Esperado:** `200 OK` — sugestão com `ativo: false` e justificativa atualizada.

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

> **Resultado esperado ao final:** todos os 54 testes passam com os status codes indicados.  
> Esta suite foi executada com Oracle real e obteve **54/54 PASS**.

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
