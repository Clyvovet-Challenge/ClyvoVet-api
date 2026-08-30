# ClyvoVet — Guia de Schema e Execução da API .NET

## Pré-requisitos

- **Oracle SQL Developer** conectado ao banco **Oracle XE (FIAP)**
- **.NET 8 SDK** instalado
- Scripts da **API Java** já executados — `t_clyvo_tutor` e `t_clyvo_animal`
  precisam existir **antes** deste schema para que os endpoints de Lembrete e
  Sugestão de Produto funcionem via API

> **Por que `t_clyvo_tutor` é necessária?**  
> O `AnimalRepository` usa `.Include(a => a.Tutor)`, e isso gera um JOIN com
> `t_clyvo_tutor`. Faltando essa tabela, qualquer validação de `animalId`
> na API acaba lançando `ORA-00942` e retornando HTTP 500.

---

## Convenção de nomes de tabelas

Todas as tabelas usam o prefixo `t_clyvo_`:

| Tabela                         | Gerenciada por | Depende de                                |
|--------------------------------|----------------|-------------------------------------------|
| `T_CLYVO_TUTOR`                | API Java       | —                                         |
| `T_CLYVO_ANIMAL`               | API Java       | `T_CLYVO_TUTOR`                           |
| `T_CLYVO_VETERINARIO`          | API Java       | —                                         |
| `T_CLYVO_CLINICA`              | API Java       | —                                         |
| `T_CLYVO_EVENTO_CLINICO`       | API Java       | —                                         |
| `T_CLYVO_CONSULTA`             | API Java       | `T_CLYVO_ANIMAL`, `T_CLYVO_VETERINARIO`  |
| **`T_CLYVO_PRODUTO`**          | **API .NET**   | —                                         |
| **`T_CLYVO_EVENTO_PET`**       | **API .NET**   | —                                         |
| **`T_CLYVO_LEMBRETE`**         | **API .NET**   | `T_CLYVO_ANIMAL` (Java)                   |
| **`T_CLYVO_SUGESTAO_PRODUTO`** | **API .NET**   | `T_CLYVO_ANIMAL` (Java), `T_CLYVO_PRODUTO` |

---

## Arquivos deste diretório

| Arquivo                        | O que faz                                                                   |
|--------------------------------|-----------------------------------------------------------------------------|
| `01_criar_tabelas_dotnet.sql`  | Cria `fn_uuid`, dropa e recria as 4 tabelas .NET com constraints e triggers |
| `02_seed_dotnet.sql`           | Insere dados de exemplo em dois blocos isolados (veja abaixo)               |
| `03_drop_tabelas_dotnet.sql`   | Remove **apenas** as 4 tabelas .NET, preservando as Java                    |

### Como o seed está organizado

| Bloco | Tabelas | Commita se… |
|-------|---------|-------------|
| **Bloco 1** | `t_clyvo_produto`, `t_clyvo_evento_pet` | Sempre — sem FK Java |
| **Bloco 2** | `t_clyvo_lembrete`, `t_clyvo_sugestao_produto` | Só se existir um `animal_id` real em `t_clyvo_animal` |

Caso a tabela Java não exista, o Bloco 2 é encerrado com um aviso, e **os dados do Bloco 1 ficam intactos**.

---

## Ordem de execução

> Abra cada arquivo no Oracle SQL Developer e pressione **F5 (Run Script)** para executar

### Deploy completo (primeira vez)

```
[Scripts Java] → 01_criar_tabelas_dotnet.sql → 02_seed_dotnet.sql
```

### Limpar e recriar somente as tabelas .NET

```
03_drop_tabelas_dotnet.sql → 01_criar_tabelas_dotnet.sql → 02_seed_dotnet.sql
```

---

## Como rodar a API .NET 8

### Via terminal (recomendado)

```bash
cd ClyvoVet.Api
dotnet run
```

### Via Visual Studio / Rider

Abra `ClyvoVet-api.slnx` → **F5** (debug) ou **Ctrl+F5** (sem debug).

---

## Acessando o Swagger

| Perfil      | URL Swagger                           |
|-------------|---------------------------------------|
| HTTP        | http://localhost:5191/swagger         |
| HTTPS       | https://localhost:7225/swagger        |
| IIS Express | https://localhost:44396/swagger       |

---

## Endpoints disponíveis

> **Base path:** `/api/v1/`

### 🛒 Produtos — `api/v1/produtos`

| Método | Rota                    | Descrição                                                        |
|--------|-------------------------|--------------------------------------------------------------------|
| GET    | `/api/v1/produtos`      | Lista produtos (paginado; query: `page`, `pageSize`, `categoria`, `especieIndicada`) |
| GET    | `/api/v1/produtos/{id}` | Busca produto por ID                                             |
| POST   | `/api/v1/produtos`      | Cadastra novo produto                                            |
| PUT    | `/api/v1/produtos/{id}` | Atualiza produto existente                                       |
| DELETE | `/api/v1/produtos/{id}` | Remove produto                                                   |

### 🐾 Eventos Pet — `api/v1/eventos-pet`

| Método | Rota                          | Descrição                                                        |
|--------|-------------------------------|--------------------------------------------------------------------|
| GET    | `/api/v1/eventos-pet`         | Lista eventos (query: `page`, `pageSize`, `cidade`, `tipo`, `especieAlvo`) |
| GET    | `/api/v1/eventos-pet/{id}`    | Busca evento por ID                                              |
| POST   | `/api/v1/eventos-pet`         | Cadastra novo evento *(data de início não pode ser no passado)*  |
| PUT    | `/api/v1/eventos-pet/{id}`    | Atualiza evento existente                                        |
| DELETE | `/api/v1/eventos-pet/{id}`    | Remove evento                                                    |

### 🔔 Lembretes — `api/v1/lembretes`

> ⚠️ Exige `animalId` válido em `t_clyvo_animal` **e** a existência de `t_clyvo_tutor`.

| Método | Rota                       | Descrição                                                        |
|--------|----------------------------|--------------------------------------------------------------------|
| GET    | `/api/v1/lembretes`        | Lista lembretes (query: `page`, `pageSize`, `animalId`, `status`, `tipo`) |
| GET    | `/api/v1/lembretes/{id}`   | Busca lembrete por ID                                            |
| POST   | `/api/v1/lembretes`        | Cria lembrete *(status forçado a `Pendente`; data deve ser futura)* |
| PUT    | `/api/v1/lembretes/{id}`   | Atualiza lembrete *(data deve ser futura)*                       |
| DELETE | `/api/v1/lembretes/{id}`   | Remove lembrete                                                  |

### 💡 Sugestões de Produto — `api/v1/sugestoes-produto`

> ⚠️ Exige `animalId` válido em `t_clyvo_animal` **e** a existência de `t_clyvo_tutor`.

| Método | Rota                              | Descrição                                                        |
|--------|-----------------------------------|--------------------------------------------------------------------|
| GET    | `/api/v1/sugestoes-produto`       | Lista sugestões (query: `page`, `pageSize`, `animalId`)          |
| GET    | `/api/v1/sugestoes-produto/{id}`  | Busca sugestão por ID                                            |
| POST   | `/api/v1/sugestoes-produto`       | Cria sugestão *(valida `animalId` e `produtoId`)*                |
| PUT    | `/api/v1/sugestoes-produto/{id}`  | Atualiza sugestão                                                |
| DELETE | `/api/v1/sugestoes-produto/{id}`  | Remove sugestão                                                  |

---

## Fluxo de teste sugerido

> Siga a sequência a seguir para validar as dependências passo a passo.

**1. Listar produtos (sem dependência Java)**
```
GET /api/v1/produtos
```
→ Retorna os 5 produtos do seed. Confirma a conexão com o Oracle.

**2. Criar novo produto**
```json
POST /api/v1/produtos
{
  "nome": "Tapete Higienico 30un",
  "categoria": "Acessorio",
  "preco": 34.90,
  "especieIndicada": "Cachorro",
  "ativo": true
}
```

**3. Listar eventos pet (sem dependência Java)**
```
GET /api/v1/eventos-pet
```
→ Retorna os 4 eventos do seed.

**4. Criar evento pet**
```json
POST /api/v1/eventos-pet
{
  "titulo": "Pet Run — Corrida com seu cao",
  "tipo": "Outro",
  "cidade": "Sao Paulo",
  "estado": "SP",
  "dataInicio": "2026-09-15",
  "especieAlvo": "Cachorro",
  "gratuito": false,
  "ativo": true
}
```

**5. Criar lembrete** *(exige `animalId` real de `t_clyvo_animal`)*
```json
POST /api/v1/lembretes
{
  "animalId": "<id-real-de-t_clyvo_animal>",
  "titulo": "Banho e tosa mensal",
  "tipo": "Higiene",
  "agendadoEm": "2026-07-10T10:00:00",
  "recorrente": true
}
```
→ O `status` sempre vem `Pendente`, não importa o valor enviado.

**6. Criar sugestão** *(exige `animalId` real e `produtoId` existente)*
```json
POST /api/v1/sugestoes-produto
{
  "animalId": "<id-real-de-t_clyvo_animal>",
  "produtoId": "<id-retornado-pelo-GET-de-produtos>",
  "justificativa": "Recomendado pelo veterinario na ultima consulta."
}
```

---

## O que cada endpoint valida (regras de negócio)

| Endpoint | Regra | HTTP |
|----------|-------|------|
| `POST /produtos` | `preco` não pode ser negativo | 400 |
| `POST /lembretes` | `agendadoEm` deve ser data futura | 400 |
| `PUT /lembretes/{id}` | `agendadoEm` deve ser data futura | 400 |
| `POST /lembretes` | `status` é forçado a `Pendente` (ignora o valor enviado) | — |
| `POST /eventos-pet` | `dataInicio` não pode ser no passado | 400 |
| `PUT /eventos-pet/{id}` | `dataInicio` só pode ser alterada para data futura | 400 |
| `POST /eventos-pet` | `dataFim` deve ser ≥ `dataInicio` | 400 |
| `POST /lembretes` | `animalId` deve existir em `t_clyvo_animal` | 404 |
| `POST /sugestoes-produto` | `animalId` deve existir em `t_clyvo_animal` | 404 |
| `POST /sugestoes-produto` | `produtoId` deve existir em `t_clyvo_produto` | 404 |
| Qualquer `GET /{id}` | Retorna 404 se o recurso não existir | 404 |

---

## Valores válidos dos enums (como enviados no JSON e gravados no banco)

| Campo                        | Valores JSON (C#)                                              | Gravado no Oracle           |
|------------------------------|----------------------------------------------------------------|-----------------------------|
| `categoria`                  | `Racao`, `Medicamento`, `Acessorio`, `Servico`, `Outro`        | `RACAO`, `MEDICAMENTO`, … |
| `especieIndicada` / `especieAlvo` | `Cachorro`, `Gato`, `Passaro`, `Reptil`, `Roedor`, `Todos`, `Outro` | `CACHORRO`, `GATO`, … |
| `tipo` (Lembrete)            | `Vacina`, `Medicamento`, `Consulta`, `Higiene`, `Outro`        | `VACINA`, `MEDICAMENTO`, … |
| `status`                     | `Pendente`, `Enviado`, `Cancelado`                             | `PENDENTE`, `ENVIADO`, …  |
| `tipo` (EventoPet)           | `Vacinacao`, `Feira`, `Castracao`, `Workshop`, `Outro`         | `VACINACAO`, `FEIRA`, …   |
