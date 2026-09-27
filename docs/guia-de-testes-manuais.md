# Guia de Testes Manuais

> Movido do [README da raiz](../README.md) na Sprint 4. A única mudança é o aviso de autenticação logo abaixo, que passou a citar o JWT.


> **54 testes** manuais, rodados contra o banco MySQL real, todos passando.  
> Acesse **`http://localhost:5191/swagger`**, siga a ordem indicada e reaproveite os JSONs já prontos.  
> Legenda dos ícones: ✅ sucesso &nbsp;|&nbsp; ❌ erro esperado (validação)  
> ⚠️ Os endpoints principais exigem a `X-Api-Key` (desde a Sprint 3) **e** o access token `Bearer` da API Java (desde a Sprint 4) — clique em **"Authorize"** no Swagger e informe os dois antes de começar (veja a seção [🔐 Autenticação](../README.md#-autenticação)).

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
