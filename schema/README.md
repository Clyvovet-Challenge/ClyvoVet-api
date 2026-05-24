# ClyvoVet — Guia de Schema e Execução da API .NET

## Pré-requisitos

- **Oracle SQL Developer** conectado ao banco **Oracle XE (FIAP)**
- **.NET 8 SDK** instalado
- Scripts da **API Java** executados **antes** dos deste diretório  
  *(a tabela `t_clyvo_animal` precisa existir para as FKs de Lembrete e Sugestão)*

---

## Convenção de nomes de tabelas

Todas as tabelas usam o prefixo `t_clyvo_`:

| Tabela                       | Gerenciada por | Depende de                     |
|------------------------------|----------------|-------------------------------|
| `T_CLYVO_TUTOR`              | API Java       | —                             |
| `T_CLYVO_ANIMAL`             | API Java       | `T_CLYVO_TUTOR`               |
| `T_CLYVO_VETERINARIO`        | API Java       | —                             |
| `T_CLYVO_CLINICA`            | API Java       | —                             |
| `T_CLYVO_EVENTO_CLINICO`     | API Java       | —                             |
| `T_CLYVO_CONSULTA`           | API Java       | `T_CLYVO_ANIMAL`, `T_CLYVO_VETERINARIO` |
| **`T_CLYVO_PRODUTO`**        | **API .NET**   | —                             |
| **`T_CLYVO_EVENTO_PET`**     | **API .NET**   | —                             |
| **`T_CLYVO_LEMBRETE`**       | **API .NET**   | `T_CLYVO_ANIMAL` (Java)       |
| **`T_CLYVO_SUGESTAO_PRODUTO`** | **API .NET** | `T_CLYVO_ANIMAL` (Java), `T_CLYVO_PRODUTO` (.NET) |

---

## Arquivos deste diretório

| Arquivo                        | O que faz                                                         |
|--------------------------------|-------------------------------------------------------------------|
| `01_criar_tabelas_dotnet.sql`  | Cria a função `fn_uuid`, dropa e recria as 4 tabelas .NET com triggers |
| `02_seed_dotnet.sql`           | Insere dados de exemplo (5 produtos, 4 eventos, 3 lembretes, 3 sugestões) |
| `03_drop_tabelas_dotnet.sql`   | Remove **apenas** as 4 tabelas .NET, preservando as tabelas Java  |

---

## Ordem de execução

> Abra cada arquivo no **Oracle SQL Developer** e pressione **F5 (Run Script)**

### Primeira vez (deploy completo)

```
[Script Java 01 e 02] → 01_criar_tabelas_dotnet.sql → 02_seed_dotnet.sql
```

### Para limpar e recriar as tabelas .NET

```
03_drop_tabelas_dotnet.sql → 01_criar_tabelas_dotnet.sql → 02_seed_dotnet.sql
```

> **Atenção:** `03_drop_tabelas_dotnet.sql` usa `CASCADE CONSTRAINTS` — ele remove
> as FKs entre tabelas .NET automaticamente, sem tocar nas tabelas Java.

---

## Como rodar a API .NET 8

### Via terminal (recomendado)

```bash
cd ClyvoVet.Api
dotnet run
```

### Via Visual Studio / Rider

Abra `ClyvoVet-api.slnx` e pressione **F5** (debug) ou **Ctrl+F5** (sem debug).

---

## Acessando o Swagger

Após iniciar a API, abra no navegador:

| Perfil      | URL Swagger                           |
|-------------|---------------------------------------|
| HTTP        | http://localhost:5191/swagger         |
| HTTPS       | https://localhost:7225/swagger        |
| IIS Express | https://localhost:44396/swagger       |

---

## Endpoints disponíveis

### 🛒 Produtos — `/api/produto`

| Método | Rota                | Descrição                                              |
|--------|---------------------|--------------------------------------------------------|
| GET    | `/api/produto`      | Lista produtos (paginado; filtros: `categoria`, `especieIndicada`) |
| GET    | `/api/produto/{id}` | Busca produto por ID                                   |
| POST   | `/api/produto`      | Cadastra novo produto                                  |
| PUT    | `/api/produto/{id}` | Atualiza produto existente                             |
| DELETE | `/api/produto/{id}` | Remove produto                                         |

### 💡 Sugestões de Produto — `/api/sugestao-produto`

| Método | Rota                         | Descrição                                              |
|--------|------------------------------|--------------------------------------------------------|
| GET    | `/api/sugestao-produto`      | Lista sugestões (paginado; filtros: `animalId`, `ativo`) |
| GET    | `/api/sugestao-produto/{id}` | Busca sugestão por ID                                  |
| POST   | `/api/sugestao-produto`      | Cria nova sugestão *(requer `animalId` válido)*        |
| PUT    | `/api/sugestao-produto/{id}` | Atualiza sugestão existente                            |
| DELETE | `/api/sugestao-produto/{id}` | Remove sugestão                                        |

### 🔔 Lembretes — `/api/lembrete`

| Método | Rota                 | Descrição                                              |
|--------|----------------------|--------------------------------------------------------|
| GET    | `/api/lembrete`      | Lista lembretes (paginado; filtros: `animalId`, `tipo`, `status`) |
| GET    | `/api/lembrete/{id}` | Busca lembrete por ID                                  |
| POST   | `/api/lembrete`      | Cria lembrete *(requer `animalId` válido; status forçado a `PENDENTE`)* |
| PUT    | `/api/lembrete/{id}` | Atualiza lembrete                                      |
| DELETE | `/api/lembrete/{id}` | Remove lembrete                                        |

### 🐾 Eventos Pet — `/api/evento-pet`

| Método | Rota                    | Descrição                                              |
|--------|-------------------------|--------------------------------------------------------|
| GET    | `/api/evento-pet`       | Lista eventos (paginado; filtros: `cidade`, `tipo`, `especieAlvo`) |
| GET    | `/api/evento-pet/{id}`  | Busca evento por ID                                    |
| POST   | `/api/evento-pet`       | Cadastra novo evento                                   |
| PUT    | `/api/evento-pet/{id}`  | Atualiza evento existente                              |
| DELETE | `/api/evento-pet/{id}`  | Remove evento                                          |

---

## Fluxo de teste sugerido

> Execute os passos na ordem para validar todas as dependências.

**1. Listar produtos do seed**
```
GET /api/produto
```
→ Deve retornar os 5 produtos inseridos pelo seed.

**2. Criar um novo produto**
```json
POST /api/produto
{
  "nome": "Tapete Higienico 30un",
  "descricao": "Tapete descartavel com atrativo olfativo.",
  "categoria": "Acessorio",
  "preco": 34.90,
  "especieIndicada": "Cachorro",
  "ativo": true
}
```

**3. Listar eventos pet**
```
GET /api/evento-pet
```
→ Deve retornar os 4 eventos do seed.

**4. Criar lembrete** *(substitua `animalId` por um ID real de `T_CLYVO_ANIMAL`)*
```json
POST /api/lembrete
{
  "animalId": "<id-real-de-t_clyvo_animal>",
  "titulo": "Banho e tosa mensal",
  "tipo": "Higiene",
  "agendadoEm": "2026-07-10T10:00:00",
  "recorrente": true
}
```
→ `status` será sempre `PENDENTE` independente do valor enviado.

**5. Criar sugestão de produto** *(use `animalId` e `produtoId` reais)*
```json
POST /api/sugestao-produto
{
  "animalId": "<id-real-de-t_clyvo_animal>",
  "produtoId": "<id-de-produto-existente>",
  "justificativa": "Recomendado pelo veterinario na ultima consulta.",
  "dataSugestao": "2026-05-24"
}
```

---

## Regras de negócio relevantes

| Regra | Onde se aplica |
|-------|----------------|
| `Produto.Preco` não pode ser negativo | `POST /PUT /api/produto` |
| `Lembrete.Status` é sempre `PENDENTE` na criação | `POST /api/lembrete` |
| `Lembrete.AgendadoEm` deve ser data futura | `POST /api/lembrete` |
| Eventos já iniciados ainda podem ser editados | `PUT /api/evento-pet` |
| `animalId` deve existir em `T_CLYVO_ANIMAL` | Lembrete e SugestaoProduto |

---

## Valores válidos dos enums (como gravados no banco)

| Campo             | Valores aceitos                                             |
|-------------------|-------------------------------------------------------------|
| `categoria`       | `RACAO`, `MEDICAMENTO`, `ACESSORIO`, `SERVICO`, `OUTRO`    |
| `especieIndicada` / `especieAlvo` | `CACHORRO`, `GATO`, `PASSARO`, `REPTIL`, `ROEDOR`, `TODOS`, `OUTRO` |
| `tipo` (Lembrete) | `VACINA`, `MEDICAMENTO`, `CONSULTA`, `HIGIENE`, `OUTRO`    |
| `status`          | `PENDENTE`, `ENVIADO`, `CANCELADO`                         |
| `tipo` (EventoPet)| `VACINACAO`, `FEIRA`, `CASTRACAO`, `WORKSHOP`, `OUTRO`     |
