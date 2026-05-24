# ClyvoVet — Guia de Schema e Execução da API .NET

## Pré-requisito

- **Oracle SQL Developer** conectado ao banco **Oracle XE (FIAP)**
- **.NET 8 SDK** instalado (para rodar a API)
- Os scripts da API Java (tabelas `t_clyvo_tutor`, `t_clyvo_animal`, etc.) devem ter sido executados **antes** dos scripts desta API

---

## Convenção de nomes de tabelas

Todas as tabelas do banco usam o prefixo `t_clyvo_`:

| Entidade        | Tabela Oracle              | Gerenciada por |
|-----------------|----------------------------|----------------|
| Tutor           | `T_CLYVO_TUTOR`            | API Java       |
| Animal          | `T_CLYVO_ANIMAL`           | API Java       |
| Veterinário     | `T_CLYVO_VETERINARIO`      | API Java       |
| Clínica         | `T_CLYVO_CLINICA`          | API Java       |
| Evento Clínico  | `T_CLYVO_EVENTO_CLINICO`   | API Java       |
| Consulta        | `T_CLYVO_CONSULTA`         | API Java       |
| Produto         | `T_CLYVO_PRODUTO`          | **API .NET**   |
| Sugestão Produto| `T_CLYVO_SUGESTAO_PRODUTO` | **API .NET**   |
| Lembrete        | `T_CLYVO_LEMBRETE`         | **API .NET**   |
| Evento Pet      | `T_CLYVO_EVENTO_PET`       | **API .NET**   |

---

## Ordem de execução dos scripts SQL

> Abra cada arquivo no Oracle SQL Developer e pressione **F5 (Run Script)**

| Ordem | Arquivo                        | Descrição                                               |
|-------|--------------------------------|---------------------------------------------------------|
| 1º    | `01_criar_tabelas_java.sql`    | Cria as tabelas gerenciadas pela API Java               |
| 2º    | `02_criar_tabelas_dotnet.sql`  | Cria as 4 tabelas gerenciadas pela API .NET             |
| 3º    | `03_drop_tabelas_dotnet.sql`   | Remove apenas as tabelas .NET (usado para re-deploy)    |

> **Atenção:** execute sempre os scripts Java antes dos .NET, pois `T_CLYVO_LEMBRETE`
> e `T_CLYVO_SUGESTAO_PRODUTO` referenciam `T_CLYVO_ANIMAL` via chave estrangeira.

---

## Como rodar a API .NET 8

### Via terminal

```bash
cd ClyvoVet.Api
dotnet run
```

### Via Visual Studio / Rider

Abra a solution `ClyvoVet-api.slnx` e pressione **F5** (Debug) ou **Ctrl+F5** (Sem debug).

---

## Acessando o Swagger

Após iniciar a API, abra no navegador:

| Perfil    | URL do Swagger                              |
|-----------|---------------------------------------------|
| HTTP      | http://localhost:5191/swagger               |
| HTTPS     | https://localhost:7225/swagger              |
| IIS Express | https://localhost:44396/swagger           |

---

## Endpoints disponíveis

### 🛒 Produtos — `/api/produto`

| Método | Rota                 | Descrição                              |
|--------|----------------------|----------------------------------------|
| GET    | `/api/produto`       | Lista produtos (paginado, filtros opcionais) |
| GET    | `/api/produto/{id}`  | Busca produto por ID                   |
| POST   | `/api/produto`       | Cadastra novo produto                  |
| PUT    | `/api/produto/{id}`  | Atualiza produto existente             |
| DELETE | `/api/produto/{id}`  | Remove produto                         |

### 💡 Sugestões de Produto — `/api/sugestao-produto`

| Método | Rota                          | Descrição                              |
|--------|-------------------------------|----------------------------------------|
| GET    | `/api/sugestao-produto`       | Lista sugestões (paginado, filtros opcionais) |
| GET    | `/api/sugestao-produto/{id}`  | Busca sugestão por ID                  |
| POST   | `/api/sugestao-produto`       | Cria nova sugestão                     |
| PUT    | `/api/sugestao-produto/{id}`  | Atualiza sugestão existente            |
| DELETE | `/api/sugestao-produto/{id}`  | Remove sugestão                        |

### 🔔 Lembretes — `/api/lembrete`

| Método | Rota                    | Descrição                              |
|--------|-------------------------|----------------------------------------|
| GET    | `/api/lembrete`         | Lista lembretes (paginado, filtros opcionais) |
| GET    | `/api/lembrete/{id}`    | Busca lembrete por ID                  |
| POST   | `/api/lembrete`         | Cria novo lembrete                     |
| PUT    | `/api/lembrete/{id}`    | Atualiza lembrete existente            |
| DELETE | `/api/lembrete/{id}`    | Remove lembrete                        |

### 🐾 Eventos Pet — `/api/evento-pet`

| Método | Rota                      | Descrição                              |
|--------|---------------------------|----------------------------------------|
| GET    | `/api/evento-pet`         | Lista eventos (paginado, filtros opcionais) |
| GET    | `/api/evento-pet/{id}`    | Busca evento por ID                    |
| POST   | `/api/evento-pet`         | Cadastra novo evento                   |
| PUT    | `/api/evento-pet/{id}`    | Atualiza evento existente              |
| DELETE | `/api/evento-pet/{id}`    | Remove evento                          |

---

## Fluxo sugerido para testar

> Use o Swagger UI para executar os passos abaixo em ordem:

1. **Criar um Produto**
   - `POST /api/produto` com nome, categoria, preço e espécie indicada
   - Guarde o `id` retornado

2. **Criar uma Sugestão de Produto**
   - `POST /api/sugestao-produto` informando `produtoId` (passo 1) e um `animalId` válido da API Java
   - Guarde o `id` retornado

3. **Criar um Lembrete**
   - `POST /api/lembrete` informando um `animalId` válido, título, tipo e data de agendamento
   - O campo `status` é sempre criado como `PENDENTE` independente do valor enviado

4. **Criar um Evento Pet**
   - `POST /api/evento-pet` com título, tipo, datas de início/fim e localização

> **Nota sobre `animalId`:** o `animal_id` deve corresponder a um registro existente
> na tabela `T_CLYVO_ANIMAL` (criada pela API Java). Se o animal não existir,
> a operação falhará por violação de chave estrangeira.

---

## Regras de negócio importantes

- `Produto.Preco` não pode ser negativo
- `Lembrete.Status` é sempre `PENDENTE` na criação, independente do valor enviado no request
- `Lembrete.AgendadoEm` deve ser uma data futura
- Eventos Pet já iniciados ainda podem ser editados (sem restrição de data no update)
