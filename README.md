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

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Oracle Database XE acessível (FIAP ou local)
- Oracle SQL Developer (para rodar os scripts de schema)

---

### 1. Clone o repositório

```bash
git clone https://github.com/pedrinzz10/ClyvoVet-api.git
cd ClyvoVet-api
```

---

### 2. Configure a connection string

Edite `ClyvoVet.Api/appsettings.json` com as credenciais do banco Oracle:

```json
{
  "ConnectionStrings": {
    "OracleConnection": "User Id=SEU_USUARIO;Password=SUA_SENHA;Data Source=oracle.fiap.com.br:1521/XEPDB1;"
  }
}
```

> **FIAP Oracle XE** — substitua `SEU_USUARIO` e `SUA_SENHA` pelas credenciais fornecidas.  
> Para Oracle local: `Data Source=localhost:1521/XEPDB1`

---

### 3. Prepare o banco de dados

Abra o **Oracle SQL Developer**, conecte ao banco e execute os scripts abaixo **na ordem**, usando **F5 (Run Script)** em cada um:

| Ordem | Arquivo | O que faz |
|-------|---------|-----------|
| 1º | `schema/01_criar_tabelas_dotnet.sql` | Cria todas as tabelas, função `fn_uuid()` e triggers |
| 2º | `schema/02_seed_dotnet.sql` | Insere dados de exemplo prontos para teste |

> O script `01` dropa e recria tudo com segurança — pode ser executado várias vezes.  
> O script `02` cria automaticamente um tutor e animal de seed caso as tabelas Java estejam vazias.

---

### 4. Restaure os pacotes e execute

```bash
cd ClyvoVet.Api
dotnet restore
dotnet run
```

O terminal exibirá as URLs disponíveis:

```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5191
      Now listening on: https://localhost:7225
```

---

### 5. Acesse o Swagger

| Protocolo | URL |
|-----------|-----|
| HTTP | http://localhost:5191/swagger |
| HTTPS | https://localhost:7225/swagger |

> O Swagger está **sempre ativo** em qualquer ambiente (incluindo Docker/produção).

---

### Configurando HTTPS (certificado de desenvolvimento)

Se o navegador exibir `ERR_CONNECTION_RESET` ao acessar HTTPS:

```bash
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```

Confirme quando o Windows solicitar. Reinicie a aplicação e tente novamente.

---

### Executando via Visual Studio / Rider

Abra `ClyvoVet-api.slnx` e pressione **F5** (com debug) ou **Ctrl+F5** (sem debug).  
O navegador abrirá automaticamente na página do Swagger.

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

## Guia de Testes

> Siga a ordem abaixo para testar todos os endpoints sem depender de dados externos.  
> Acesse `http://localhost:5191/swagger` e use os exemplos de JSON prontos abaixo.

---

### Passo 1 — Listar produtos (confirma conexão com Oracle)

```
GET /api/v1/produtos
```

**Resultado esperado:** lista com os 5 produtos do seed (Ração Golden, Whiskas, Frontline, Coleira Seresto, Consulta Veterinária).

---

### Passo 2 — Criar novo produto

```
POST /api/v1/produtos
```
```json
{
  "nome": "Tapete Higiênico Premium 30un",
  "descricao": "Tapete absorvente para treinamento de filhotes.",
  "categoria": 2,
  "preco": 34.90,
  "especieIndicada": 0,
  "ativo": true
}
```

**Resultado esperado:** `201 Created` com o produto criado. **Copie o `id` retornado** para usar nos próximos passos.

---

### Passo 3 — Buscar produto por ID

```
GET /api/v1/produtos/{id}
```

Substitua `{id}` pelo ID copiado no Passo 2.  
**Resultado esperado:** `200 OK` com os dados do produto.

---

### Passo 4 — Atualizar produto

```
PUT /api/v1/produtos/{id}
```
```json
{
  "nome": "Tapete Higiênico Premium 50un",
  "descricao": "Versão maior com 50 unidades.",
  "categoria": 2,
  "preco": 54.90,
  "especieIndicada": 0,
  "ativo": true
}
```

**Resultado esperado:** `200 OK` com os dados atualizados.

---

### Passo 5 — Listar eventos pet (sem dependência Java)

```
GET /api/v1/eventos-pet
```

**Resultado esperado:** lista com os 4 eventos do seed (Feira de Adoção, Vacinação Antirrábica, Workshop Primeiros Socorros, Castração Solidária).

---

### Passo 6 — Filtrar eventos por cidade

```
GET /api/v1/eventos-pet?cidade=Sao Paulo
```

**Resultado esperado:** apenas os eventos de São Paulo.

---

### Passo 7 — Criar novo evento pet

```
POST /api/v1/eventos-pet
```
```json
{
  "titulo": "Pet Run — Corrida com seu Cão",
  "descricao": "Corrida divertida de 5km com pets. Premiação para os 3 primeiros.",
  "tipo": 4,
  "rua": "Parque Ibirapuera",
  "numero": "s/n",
  "bairro": "Moema",
  "cidade": "São Paulo",
  "estado": "SP",
  "cep": "04094-000",
  "dataInicio": "2026-09-20",
  "dataFim": "2026-09-20",
  "especieAlvo": 0,
  "organizador": "Pet Run Brasil",
  "gratuito": false,
  "linkInscricao": "https://petrunbrasil.com.br/inscricao",
  "ativo": true
}
```

**Resultado esperado:** `201 Created`.

---

### Passo 8 — Obter `animalId` do seed para os próximos testes

Execute no Oracle SQL Developer:

```sql
SELECT id, nome FROM t_clyvo_animal WHERE ROWNUM = 1;
```

**Copie o `id` retornado** — você vai precisar dele nos Passos 9 a 13.

Alternativamente, o Bloco 2 do seed imprime o `animal_id` criado no `DBMS_OUTPUT`.

---

### Passo 9 — Listar lembretes

```
GET /api/v1/lembretes
```

**Resultado esperado:** lista com os 3 lembretes do seed (Vacina V10, Vermifugação, Retorno Dermatologia).

---

### Passo 10 — Criar novo lembrete

```
POST /api/v1/lembretes
```
```json
{
  "animalId": "<cole-o-uuid-do-animal-aqui>",
  "titulo": "Banho e Tosa Mensal",
  "descricao": "Agendar no Pet Shop Clyvo — ligar com 2 dias de antecedência.",
  "tipo": 3,
  "agendadoEm": "2026-07-15T09:00:00",
  "recorrente": true,
  "status": 0
}
```

> O campo `status` será **sempre** `Pendente (0)` na criação, mesmo que outro valor seja enviado.

**Resultado esperado:** `201 Created`. **Copie o `id` do lembrete** para os próximos passos.

---

### Passo 11 — Atualizar status do lembrete

```
PUT /api/v1/lembretes/{id}
```
```json
{
  "animalId": "<cole-o-uuid-do-animal-aqui>",
  "titulo": "Banho e Tosa Mensal",
  "descricao": "Agendar no Pet Shop Clyvo — ligar com 2 dias de antecedência.",
  "tipo": 3,
  "agendadoEm": "2026-07-15T09:00:00",
  "recorrente": true,
  "status": 1
}
```

**Resultado esperado:** `200 OK` com `status: 1` (Enviado).

---

### Passo 12 — Listar sugestões de produto

```
GET /api/v1/sugestoes-produto
```

**Resultado esperado:** lista com as 3 sugestões do seed.

---

### Passo 13 — Criar nova sugestão de produto

Primeiro copie o `id` de um produto listado no Passo 1, depois:

```
POST /api/v1/sugestoes-produto
```
```json
{
  "animalId": "<cole-o-uuid-do-animal-aqui>",
  "produtoId": "<cole-o-uuid-do-produto-aqui>",
  "justificativa": "Veterinário recomendou troca de ração por sensibilidade alimentar diagnosticada na consulta de maio.",
  "dataSugestao": "2026-05-24",
  "ativo": true
}
```

**Resultado esperado:** `201 Created` com `nomeAnimal` e `nomeProduto` preenchidos automaticamente.

---

### Passo 14 — Testar erro de validação (400)

```
POST /api/v1/produtos
```
```json
{
  "nome": "Produto Inválido",
  "categoria": 0,
  "preco": -10.00,
  "especieIndicada": 0,
  "ativo": true
}
```

**Resultado esperado:** `400 Bad Request` com mensagem de erro sobre preço negativo.

---

### Passo 15 — Testar 404

```
GET /api/v1/produtos/id-que-nao-existe
```

**Resultado esperado:** `404 Not Found` com `{ "error": "Produto não encontrado." }`.

---

### Passo 16 — Deletar recursos criados (limpeza)

```
DELETE /api/v1/lembretes/{id}
DELETE /api/v1/produtos/{id}
DELETE /api/v1/eventos-pet/{id}
DELETE /api/v1/sugestoes-produto/{id}
```

**Resultado esperado:** `204 No Content` para cada um.

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
