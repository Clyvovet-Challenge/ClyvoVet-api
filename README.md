# ClyvoVet API

API RESTful desenvolvida em **.NET 8** para gestão veterinária. Permite gerenciar produtos, lembretes de saúde animal, sugestões de produtos e eventos pet.

---

## Tecnologias

| Tecnologia | Versão | Uso |
|---|---|---|
| .NET | 8.0 | Framework principal |
| ASP.NET Core | 8.0 | Web API |
| Entity Framework Core | 8.0.11 | ORM |
| Oracle EF Core | 8.21.121 | Driver do banco de dados |
| Swashbuckle (Swagger) | 10.1.7 | Documentação interativa |

---

## Estrutura do Projeto

```
ClyvoVet.Api/
├── Controllers/          → Recebe requisições HTTP e retorna respostas
├── Services/             → Regras de negócio
│   └── Interfaces/
├── Repositories/         → Acesso ao banco de dados
│   └── Interfaces/
├── DTOs/                 → Objetos de entrada e saída da API
│   ├── Request/
│   └── Response/
├── Models/               → Entidades do banco de dados
├── Data/                 → DbContext e configurações do EF Core
│   └── Configurations/
├── Enums/                → Tipos enumerados
├── Exceptions/           → Exceções customizadas
└── Program.cs            → Ponto de entrada e configuração da aplicação
```

---

## Arquitetura em Camadas

O projeto segue uma arquitetura em camadas onde cada camada tem uma responsabilidade única e se comunica apenas com a camada imediatamente abaixo dela.

```
Requisição HTTP
      ↓
  Controller          → Recebe e responde requisições HTTP
      ↓
   Service            → Executa as regras de negócio
      ↓
  Repository          → Executa queries no banco de dados
      ↓
  AppDbContext        → Comunica com o Oracle via EF Core
      ↓
 Banco de Dados (Oracle)
```

### Controllers

Responsáveis apenas por receber a requisição, chamar o service correspondente e retornar o status HTTP correto. Não contêm nenhuma lógica de negócio.

Todos os controllers seguem o padrão:
- Herdam de `ControllerBase`
- Decorados com `[ApiController]` e `[Route("api/v1/[controller]")]`
- Recebem o service via injeção de dependência no construtor
- Todos os métodos são `async`

### Services

Contêm toda a regra de negócio da aplicação. São responsáveis por:
- Validar dados de entrada (lançando `BadRequestException` quando inválido)
- Buscar entidades (lançando `NotFoundException` quando não encontrado)
- Mapear DTOs para entidades e vice-versa
- Delegar a persistência para os repositories

### Repositories

Responsáveis exclusivamente pelo acesso ao banco de dados. Encapsulam as queries do EF Core e expõem métodos específicos para cada operação necessária. Os services nunca acessam o `DbContext` diretamente.

### Models

Representam as tabelas do banco de dados. São as entidades que o EF Core mapeia e persiste. Não contêm regras de negócio.

### DTOs (Data Transfer Objects)

Separam o contrato da API do modelo interno do banco de dados.

- **Request:** objetos recebidos nas requisições. Contêm anotações de validação (`[Required]`, `[MaxLength]`, etc.)
- **Response:** objetos retornados nas respostas. Expõem apenas os campos necessários para o cliente

### Exceptions

Exceções customizadas usadas pelos services para sinalizar erros de domínio. O middleware global as captura e converte para o status HTTP correspondente.

| Exceção | Status HTTP |
|---|---|
| `NotFoundException` | 404 Not Found |
| `BadRequestException` | 400 Bad Request |
| Qualquer outra | 500 Internal Server Error |

---

## Models e Relacionamentos

### Tutor
Representa o dono do animal.

| Campo | Tipo | Descrição |
|---|---|---|
| Id | string | Identificador único |
| Nome | string | Nome completo |
| Cpf | string | CPF (único) |
| Email | string? | E-mail de contato |
| Telefone | string? | Telefone de contato |
| Endereco | string? | Endereço |
| Ativo | bool | Se o cadastro está ativo |
| CriadoEm | DateTime | Data de criação |

Relacionamentos: um tutor possui muitos **Animais**.

---

### Animal
Representa o pet cadastrado.

| Campo | Tipo | Descrição |
|---|---|---|
| Id | string | Identificador único |
| Nome | string | Nome do animal |
| Especie | string | Espécie (ex: Cachorro) |
| Raca | string? | Raça |
| DataNascimento | DateTime? | Data de nascimento |
| Sexo | string? | Sexo |
| Castrado | bool | Se é castrado |
| Ativo | bool | Se o cadastro está ativo |
| CriadoEm | DateTime | Data de criação |
| TutorId | string | FK para Tutor |

Relacionamentos: pertence a um **Tutor**, possui muitas **Consultas**.

---

### Veterinario
Representa o profissional veterinário.

| Campo | Tipo | Descrição |
|---|---|---|
| Id | string | Identificador único |
| Nome | string | Nome completo |
| Crmv | string | Registro CRMV (único) |
| Email | string? | E-mail profissional |
| Especialidade | string? | Área de especialidade |
| Ativo | bool | Se o cadastro está ativo |
| CriadoEm | DateTime | Data de criação |

Relacionamentos: possui muitas **Consultas**.

---

### Consulta
Representa um atendimento veterinário.

| Campo | Tipo | Descrição |
|---|---|---|
| Id | string | Identificador único |
| DataHora | DateTime | Data e hora da consulta |
| Status | string | Status da consulta |
| Motivo | string? | Motivo da consulta |
| Observacoes | string? | Observações do veterinário |
| Valor | decimal | Valor cobrado |
| CriadoEm | DateTime | Data de criação |
| AnimalId | string | FK para Animal |
| VeterinarioId | string | FK para Veterinario |

Relacionamentos: pertence a um **Animal** e a um **Veterinario**.

---

### Produto
Representa um produto do catálogo (ração, medicamento, acessório, etc.).

| Campo | Tipo | Descrição |
|---|---|---|
| Id | string | Identificador único |
| Nome | string | Nome do produto |
| Descricao | string? | Descrição detalhada |
| Categoria | CategoriaEnum | Categoria do produto |
| Preco | decimal? | Preço sugerido |
| EspecieIndicada | EspecieEnum | Espécie para qual é indicado |
| Ativo | bool | Se o produto está ativo |
| CriadoEm | DateTime | Data de criação |

Relacionamentos: pode aparecer em muitas **SugestoesProduto**.

---

### Lembrete
Representa um lembrete de saúde para um animal (vacina, medicamento, consulta, etc.).

| Campo | Tipo | Descrição |
|---|---|---|
| Id | string | Identificador único |
| AnimalId | string | FK para Animal |
| Titulo | string | Título do lembrete |
| Descricao | string? | Descrição detalhada |
| Tipo | TipoLembreteEnum | Tipo do lembrete |
| AgendadoEm | DateTime | Data/hora agendada |
| Recorrente | bool | Se o lembrete se repete |
| Status | StatusLembreteEnum | Status atual |
| CriadoEm | DateTime | Data de criação |

Relacionamentos: pertence a um **Animal**.

---

### SugestaoProduto
Representa uma recomendação de produto para um animal específico.

| Campo | Tipo | Descrição |
|---|---|---|
| Id | string | Identificador único |
| AnimalId | string | FK para Animal |
| ProdutoId | string | FK para Produto |
| Justificativa | string? | Motivo da sugestão |
| DataSugestao | DateOnly | Data da sugestão |
| Ativo | bool | Se a sugestão está ativa |
| CriadoEm | DateTime | Data de criação |

Relacionamentos: pertence a um **Animal** e a um **Produto**.

---

### EventoPet
Representa um evento relacionado ao mundo pet (feiras, vacinações, castrações, etc.).

| Campo | Tipo | Descrição |
|---|---|---|
| Id | string | Identificador único |
| Titulo | string | Nome do evento |
| Descricao | string? | Descrição |
| Tipo | TipoEventoPetEnum | Tipo do evento |
| Rua, Numero, Bairro, Cidade, Estado, Cep | string? | Endereço completo |
| DataInicio | DateOnly | Data de início |
| DataFim | DateOnly? | Data de encerramento |
| EspecieAlvo | EspecieEnum | Espécie para qual o evento é direcionado |
| Organizador | string? | Nome do organizador |
| Gratuito | bool | Se o evento é gratuito |
| LinkInscricao | string? | URL para inscrição |
| Ativo | bool | Se o evento está ativo |
| CriadoEm | DateTime | Data de criação |

---

## Enums

### CategoriaEnum
Categorias de produtos disponíveis no catálogo.

| Valor | Descrição |
|---|---|
| `Racao` | Alimentos para pets |
| `Medicamento` | Remédios e vacinas |
| `Acessorio` | Coleiras, brinquedos, etc. |
| `Servico` | Banho, tosa, etc. |
| `Outro` | Outros tipos |

### EspecieEnum
Espécies de animais suportadas.

| Valor |
|---|
| `Cachorro` |
| `Gato` |
| `Passaro` |
| `Reptil` |
| `Roedor` |
| `Todos` |
| `Outro` |

### TipoLembreteEnum
Tipos de lembretes de saúde.

| Valor |
|---|
| `Vacina` |
| `Medicamento` |
| `Consulta` |
| `Higiene` |
| `Outro` |

### StatusLembreteEnum
Ciclo de vida de um lembrete.

| Valor | Descrição |
|---|---|
| `Pendente` | Ainda não enviado |
| `Enviado` | Notificação disparada |
| `Cancelado` | Lembrete cancelado |

### TipoEventoPetEnum
Tipos de eventos pet.

| Valor |
|---|
| `Vacinacao` |
| `Feira` |
| `Castracao` |
| `Workshop` |
| `Outro` |

---

## Endpoints

Todos os endpoints seguem o padrão `/api/v1/{recurso}`.

### Produto — `/api/v1/produto`

| Método | Rota | Descrição | Status |
|---|---|---|---|
| GET | `/api/v1/produto` | Lista produtos com paginação e filtros | 200 |
| GET | `/api/v1/produto/{id}` | Busca produto por ID | 200 / 404 |
| POST | `/api/v1/produto` | Cria um novo produto | 201 / 400 |
| PUT | `/api/v1/produto/{id}` | Atualiza um produto existente | 200 / 404 / 400 |
| DELETE | `/api/v1/produto/{id}` | Remove um produto | 204 / 404 |

**Filtros do GET lista:** `page`, `pageSize`, `categoria` (CategoriaEnum), `especieIndicada` (EspecieEnum)

---

### Lembrete — `/api/v1/lembrete`

| Método | Rota | Descrição | Status |
|---|---|---|---|
| GET | `/api/v1/lembrete` | Lista lembretes com paginação e filtros | 200 |
| GET | `/api/v1/lembrete/{id}` | Busca lembrete por ID | 200 / 404 |
| POST | `/api/v1/lembrete` | Cria um novo lembrete | 201 / 400 |
| PUT | `/api/v1/lembrete/{id}` | Atualiza um lembrete existente | 200 / 404 / 400 |
| DELETE | `/api/v1/lembrete/{id}` | Remove um lembrete | 204 / 404 |

**Filtros do GET lista:** `page`, `pageSize`, `animalId`, `status` (StatusLembreteEnum), `tipo` (TipoLembreteEnum)

---

### SugestaoProduto — `/api/v1/sugestaoproduto`

| Método | Rota | Descrição | Status |
|---|---|---|---|
| GET | `/api/v1/sugestaoproduto` | Lista sugestões com paginação e filtros | 200 |
| GET | `/api/v1/sugestaoproduto/{id}` | Busca sugestão por ID | 200 / 404 |
| POST | `/api/v1/sugestaoproduto` | Cria uma nova sugestão | 201 / 400 |
| PUT | `/api/v1/sugestaoproduto/{id}` | Atualiza uma sugestão existente | 200 / 404 / 400 |
| DELETE | `/api/v1/sugestaoproduto/{id}` | Remove uma sugestão | 204 / 404 |

**Filtros do GET lista:** `page`, `pageSize`, `animalId`

---

### EventoPet — `/api/v1/eventopet`

| Método | Rota | Descrição | Status |
|---|---|---|---|
| GET | `/api/v1/eventopet` | Lista eventos com paginação e filtros | 200 |
| GET | `/api/v1/eventopet/{id}` | Busca evento por ID | 200 / 404 |
| POST | `/api/v1/eventopet` | Cria um novo evento | 201 / 400 |
| PUT | `/api/v1/eventopet/{id}` | Atualiza um evento existente | 200 / 404 / 400 |
| DELETE | `/api/v1/eventopet/{id}` | Remove um evento | 204 / 404 |

**Filtros do GET lista:** `page`, `pageSize`, `cidade`, `tipo` (TipoEventoPetEnum), `especieAlvo` (EspecieEnum)

---

## Tratamento de Erros

O tratamento de erros é centralizado em um middleware global configurado no `Program.cs`. Os services lançam exceções customizadas e o middleware as intercepta antes de retornar ao cliente, garantindo que nenhum stack trace seja exposto.

**Formato padrão de resposta de erro:**
```json
{
  "error": "Mensagem descritiva do erro"
}
```

| Situação | Exceção | HTTP |
|---|---|---|
| Recurso não encontrado | `NotFoundException` | 404 |
| Dados inválidos (regra de negócio) | `BadRequestException` | 400 |
| Body inválido (validação de DTO) | Automático via `[ApiController]` | 400 |
| Erro inesperado | `Exception` | 500 |

---

## Configuração e Execução

### Pré-requisitos

- .NET 8 SDK
- Oracle Database acessível

### String de conexão

Configure a string de conexão no `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "OracleConnection": "User Id=seu_usuario;Password=sua_senha;Data Source=seu_host:1521/seu_service"
  }
}
```

### Executando o projeto

```bash
dotnet restore
dotnet run --project ClyvoVet.Api
```

### Swagger

Com a aplicação rodando em ambiente de desenvolvimento, a documentação interativa estará disponível em:

```
https://localhost:{porta}/swagger
```

---

## Injeção de Dependência

Todos os serviços e repositórios são registrados com tempo de vida **Scoped** (uma instância por requisição HTTP):

```
IProdutoRepository        → ProdutoRepository
ISugestaoProdutoRepository → SugestaoProdutoRepository
ILembreteRepository       → LembreteRepository
IEventoPetRepository      → EventoPetRepository
IAnimalRepository         → AnimalRepository

IProdutoService           → ProdutoService
ISugestaoProdutoService   → SugestaoProdutoService
ILembreteService          → LembreteService
IEventoPetService         → EventoPetService
```
