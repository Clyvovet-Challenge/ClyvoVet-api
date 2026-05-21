# ClyvoVet API — .NET

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat&logo=dotnet&logoColor=white)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-8.0-0078D4?style=flat&logo=microsoft&logoColor=white)
![Entity Framework Core](https://img.shields.io/badge/Entity_Framework_Core-8.0-68217A?style=flat&logo=nuget&logoColor=white)
![Oracle](https://img.shields.io/badge/Oracle_Database-XE-F80000?style=flat&logo=oracle&logoColor=white)
![Swagger](https://img.shields.io/badge/Swagger-OpenAPI-85EA2D?style=flat&logo=swagger&logoColor=black)

---

## Sobre o Projeto

A **ClyvoVet API** é uma API RESTful desenvolvida em ASP.NET Core como parte do **Challenge FIAP 2026 — projeto Petrack / Clyvo Vet**.

Esta API é responsável pelo **domínio de engajamento** da plataforma, gerenciando:

- Catálogo de produtos recomendados para pets
- Sugestões personalizadas de produtos por animal
- Lembretes de saúde e cuidados para tutores
- Eventos pet públicos como campanhas de vacinação e feiras

A aplicação faz parte de uma **arquitetura com duas APIs independentes** consumindo o mesmo banco Oracle:

| API | Responsabilidade |
|-----|-----------------|
| **.NET (este projeto)** | Produtos, sugestões, lembretes, eventos |
| **Java (parceiro)** | Tutores, animais, clínicas, consultas, pagamentos |

---

## Tecnologias Utilizadas

- .NET 8
- ASP.NET Core Web API
- Entity Framework Core 8
- Oracle.EntityFrameworkCore
- Swagger / Swashbuckle
- Oracle Database XE

---

## Estrutura de Pastas

```
ClyvoVet.Api/
├── Controllers/      → Recebem requisições HTTP e delegam ao Service
├── Services/         → Contêm as regras de negócio
│   └── Interfaces/
├── Repositories/     → Acesso ao banco de dados via EF Core
│   └── Interfaces/
├── Models/           → Entidades mapeadas para as tabelas Oracle
├── DTOs/             → Objetos de transferência de dados
│   ├── Request/      → Dados recebidos nas requisições
│   └── Response/     → Dados retornados nas respostas
├── Enums/            → Enumerações dos valores aceitos pelo banco
├── Data/             → DbContext e configurações do EF Core
│   └── Configurations/
├── Exceptions/       → Exceções customizadas (NotFoundException, BadRequestException)
└── Program.cs        → Ponto de entrada, DI e middleware
```

---

## Como Instalar e Executar

### Pré-requisitos

- .NET 8 SDK instalado
- Oracle Database XE rodando localmente ou em nuvem
- Oracle SQL Developer (opcional, para visualizar o banco)

### Passo a passo

**1. Clone o repositório:**

```bash
git clone https://github.com/SEU_USUARIO/clyvovet_dotnet.git
cd clyvovet_dotnet
```

**2. Configure a connection string no `appsettings.json`:**

```json
{
  "ConnectionStrings": {
    "OracleConnection": "User Id=SEU_USUARIO;Password=SUA_SENHA;Data Source=localhost:1521/XEPDB1;"
  }
}
```

**3. Restaure os pacotes:**

```bash
dotnet restore
```

**4. Execute a aplicação:**

```bash
cd ClyvoVet.Api
dotnet run
```

**5. Acesse o Swagger:**

Após subir, o terminal exibirá as URLs disponíveis. Acesse:

```
http://localhost:{porta}/swagger
https://localhost:{porta}/swagger
```

> O Swagger só está disponível no ambiente `Development` (padrão ao rodar via `dotnet run`).

---

### Configurando HTTPS (certificado de desenvolvimento)

Caso o navegador exiba `ERR_CONNECTION_RESET` ao acessar a URL HTTPS, instale e confie no certificado de desenvolvimento do .NET:

```bash
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```

Confirme a instalação quando o Windows solicitar. Após isso, reinicie a aplicação e acesse `https://localhost:{porta}/swagger`.

---

## Regras de Negócio

### ProdutoService

- **Listar:** aceita filtros opcionais por `categoria` e `especieIndicada`, com paginação. Ordenado por nome.
- **Buscar por ID:** retorna 404 se não existir.
- **Criar:** `Id` e `CriadoEm` são gerados automaticamente. Preço não pode ser negativo.
- **Atualizar:** verifica se o produto existe (404 caso contrário). Todos os campos são substituídos.
- **Deletar:** retorna 404 se não existir.

---

### EventoPetService

- **Listar:** filtra por `cidade` (comparação exata, case-insensitive), `tipo` e `especieAlvo`. Ordenado por `DataInicio`.
- **Criar:**
  - `DataInicio` não pode ser no passado.
  - `DataFim` (opcional) não pode ser anterior à `DataInicio`.
- **Atualizar:**
  - Verifica existência (404).
  - Só rejeita `DataInicio` se ela **foi alterada** para uma data no passado — eventos já iniciados podem ser editados normalmente.
  - `DataFim` ainda não pode ser anterior à `DataInicio`.
- **Deletar:** retorna 404 se não existir.

---

### LembreteService

- **Listar:** filtra por `animalId`, `status` e `tipo`, com paginação. Ordenado por `AgendadoEm`. Retorna o nome do animal junto.
- **Criar:**
  - Verifica se o `AnimalId` existe (404 se não existir).
  - `AgendadoEm` não pode ser no passado (comparado em UTC).
  - `Status` é **sempre** `Pendente` na criação, independente do que o cliente enviar.
- **Atualizar:**
  - Verifica existência do lembrete (404).
  - Verifica se o `AnimalId` existe (404).
  - `AgendadoEm` não pode ser no passado.
  - `Status` pode ser alterado livremente no update (ex: para `Enviado` ou `Cancelado`).
- **Deletar:** retorna 404 se não existir.

---

### SugestaoProdutoService

- **Listar:** filtra por `animalId`, com paginação. Ordenado por `DataSugestao` decrescente (mais recentes primeiro). Retorna nome do animal e do produto.
- **Criar:**
  - Verifica se o `AnimalId` existe (404).
  - Verifica se o `ProdutoId` existe (404).
  - `DataSugestao` é opcional: se não enviada, assume a data de hoje.
- **Atualizar:**
  - Verifica existência da sugestão (404).
  - Verifica se o `AnimalId` existe (404).
  - Verifica se o `ProdutoId` existe (404).
  - `DataSugestao` segue a mesma regra do criar.
- **Deletar:** retorna 404 se não existir.

---

### Observação sobre Animais

Nenhum serviço desta API cria ou gerencia animais diretamente. O `AnimalRepository` é usado **apenas para validar existência** em `LembreteService` e `SugestaoProdutoService`. Os animais são gerenciados pela API Java parceira, que compartilha o mesmo banco Oracle.

---

## Documentação das Rotas

### Produtos

| Método | Rota | Descrição | Status |
|--------|------|-----------|--------|
| GET | `/api/v1/produtos` | Lista produtos com filtros e paginação | 200 |
| GET | `/api/v1/produtos/{id}` | Busca produto por ID | 200, 404 |
| POST | `/api/v1/produtos` | Cria novo produto | 201, 400 |
| PUT | `/api/v1/produtos/{id}` | Atualiza produto existente | 200, 400, 404 |
| DELETE | `/api/v1/produtos/{id}` | Remove produto | 204, 404 |

**Parâmetros de query — `GET /api/v1/produtos`:**

| Parâmetro | Tipo | Padrão | Valores aceitos |
|-----------|------|--------|-----------------|
| `page` | int | 1 | — |
| `pageSize` | int | 10 | — |
| `categoria` | enum | — | `RACAO`, `MEDICAMENTO`, `ACESSORIO`, `SERVICO`, `OUTRO` |
| `especieIndicada` | enum | — | `CACHORRO`, `GATO`, `PASSARO`, `REPTIL`, `ROEDOR`, `TODOS`, `OUTRO` |

---

### Sugestões de Produto

| Método | Rota | Descrição | Status |
|--------|------|-----------|--------|
| GET | `/api/v1/sugestoes-produto` | Lista sugestões com filtros e paginação | 200 |
| GET | `/api/v1/sugestoes-produto/{id}` | Busca sugestão por ID | 200, 404 |
| POST | `/api/v1/sugestoes-produto` | Cria nova sugestão | 201, 400, 404 |
| PUT | `/api/v1/sugestoes-produto/{id}` | Atualiza sugestão existente | 200, 400, 404 |
| DELETE | `/api/v1/sugestoes-produto/{id}` | Remove sugestão | 204, 404 |

**Parâmetros de query — `GET /api/v1/sugestoes-produto`:**

| Parâmetro | Tipo | Padrão | Valores aceitos |
|-----------|------|--------|-----------------|
| `page` | int | 1 | — |
| `pageSize` | int | 10 | — |
| `animalId` | string | — | UUID do animal |

---

### Lembretes

| Método | Rota | Descrição | Status |
|--------|------|-----------|--------|
| GET | `/api/v1/lembretes` | Lista lembretes com filtros e paginação | 200 |
| GET | `/api/v1/lembretes/{id}` | Busca lembrete por ID | 200, 404 |
| POST | `/api/v1/lembretes` | Cria novo lembrete | 201, 400, 404 |
| PUT | `/api/v1/lembretes/{id}` | Atualiza lembrete existente | 200, 400, 404 |
| DELETE | `/api/v1/lembretes/{id}` | Remove lembrete | 204, 404 |

**Parâmetros de query — `GET /api/v1/lembretes`:**

| Parâmetro | Tipo | Padrão | Valores aceitos |
|-----------|------|--------|-----------------|
| `page` | int | 1 | — |
| `pageSize` | int | 10 | — |
| `animalId` | string | — | UUID do animal |
| `status` | enum | — | `PENDENTE`, `ENVIADO`, `CANCELADO` |
| `tipo` | enum | — | `VACINA`, `MEDICAMENTO`, `CONSULTA`, `HIGIENE`, `OUTRO` |

---

### Eventos Pet

| Método | Rota | Descrição | Status |
|--------|------|-----------|--------|
| GET | `/api/v1/eventos-pet` | Lista eventos com filtros e paginação | 200 |
| GET | `/api/v1/eventos-pet/{id}` | Busca evento por ID | 200, 404 |
| POST | `/api/v1/eventos-pet` | Cria novo evento | 201, 400 |
| PUT | `/api/v1/eventos-pet/{id}` | Atualiza evento existente | 200, 400, 404 |
| DELETE | `/api/v1/eventos-pet/{id}` | Remove evento | 204, 404 |

**Parâmetros de query — `GET /api/v1/eventos-pet`:**

| Parâmetro | Tipo | Padrão | Valores aceitos |
|-----------|------|--------|-----------------|
| `page` | int | 1 | — |
| `pageSize` | int | 10 | — |
| `cidade` | string | — | Nome da cidade |
| `tipo` | enum | — | `VACINACAO`, `FEIRA`, `CASTRACAO`, `WORKSHOP`, `OUTRO` |
| `especieAlvo` | enum | — | `CACHORRO`, `GATO`, `PASSARO`, `REPTIL`, `ROEDOR`, `TODOS`, `OUTRO` |

---

## Exemplos de Requisição

### POST `/api/v1/produtos`

```json
{
  "nome": "Ração Premium Adulto",
  "descricao": "Ração completa para cães adultos de porte médio",
  "categoria": "Racao",
  "preco": 89.90,
  "especieIndicada": "Cachorro",
  "ativo": true
}
```

### POST `/api/v1/sugestoes-produto`

```json
{
  "animalId": "uuid-do-animal-aqui",
  "produtoId": "uuid-do-produto-aqui",
  "justificativa": "Recomendado pelo veterinário após consulta",
  "ativo": true
}
```

### POST `/api/v1/lembretes`

```json
{
  "animalId": "uuid-do-animal-aqui",
  "titulo": "Vacina Antirrábica",
  "descricao": "Reforço anual da vacina antirrábica",
  "tipo": "Vacina",
  "agendadoEm": "2025-12-01T10:00:00",
  "recorrente": true,
  "status": "Pendente"
}
```

### POST `/api/v1/eventos-pet`

```json
{
  "titulo": "Campanha de Vacinação Gratuita",
  "descricao": "Vacinação antirrábica gratuita para cães e gatos",
  "tipo": "Vacinacao",
  "rua": "Av. Paulista",
  "numero": "1000",
  "bairro": "Bela Vista",
  "cidade": "São Paulo",
  "estado": "SP",
  "cep": "01310-100",
  "dataInicio": "2025-11-15",
  "dataFim": "2025-11-15",
  "especieAlvo": "Todos",
  "organizador": "Prefeitura de São Paulo",
  "gratuito": true,
  "linkInscricao": "https://exemplo.com/inscricao",
  "ativo": true
}
```

---

## Integrantes do Grupo

| Nome | RM |
|------|----|
| — | — |
| — | — |
| — | — |
| — | — |
| — | — |

---

## Licença

Distribuído sob a licença MIT. Consulte o arquivo `LICENSE` para mais informações.
