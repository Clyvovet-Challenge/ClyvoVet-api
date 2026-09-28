# Guia de Testes Manuais

> **54 passos** manuais pelo Swagger, com o status e a mensagem que a API devolve em cada um.  
> Acesse **`http://localhost:5191/swagger`**, siga a ordem indicada e reaproveite os JSONs já prontos.  
> Legenda dos ícones: ✅ sucesso &nbsp;|&nbsp; ❌ erro esperado (validação) &nbsp;|&nbsp; 🔑 conta a usar  
>
> Os resultados esperados foram conferidos contra o código em 27/09/2026 (perfis, recorte por tutor,
> mensagens de erro). O roteiro automatizado que cobre o mesmo terreno é o [`test_api.sh`](../test_api.sh).

---

### Antes de começar — duas contas e os IDs

Os endpoints exigem a `X-Api-Key` **e** o access token `Bearer` emitido pela API Java no login
(veja [🔐 Autenticação](../README.md#-autenticação)). No Swagger, clique em **"Authorize"** e informe os dois.
**Nenhuma conta sozinha roda o guia inteiro**, e isso é regra da API, não falha:

| 🔑 Conta | Para quê | Por quê |
|---|---|---|
| **Equipe**: `admin@clyvovet.com` (ADMIN) ou `camila.ferreira@vetcare.com.br` (VETERINARIO) | Blocos 1 e 2 e os deletes de produto e evento (T49–T51, T53) | Criar, editar e apagar produto e evento é só da equipe; um TUTOR recebe `403` |
| **Tutor**: `lucas.santos@email.com` (TUTOR) | Blocos 3 e 4 e os deletes de lembrete e sugestão (T47, T48, T52, T54) | Com o recorte por tutor (ligado por padrão), lembretes e sugestões são do tutor do token; ADMIN e VETERINARIO não têm tutor e recebem `403` |

As senhas estão na seção [Quem faz o login](../README.md#quem-faz-o-login-a-api-java) do README.
Para trocar de conta, faça login de novo na API Java e troque o Bearer no **"Authorize"**.

Rode no cliente `mysql` (ou no MySQL Workbench) para pegar os IDs:

```sql
-- animal_id: precisa ser um animal DO TUTOR logado. Animal de outro tutor responde 404.
SELECT a.id, a.nome
  FROM t_clyvo_animal a
  JOIN t_clyvo_usuario u ON u.tutor_id = a.tutor_id
 WHERE u.email = 'lucas.santos@email.com'
 LIMIT 1;

-- produto_id do seed (necessário nos testes de Sugestão)
SELECT id, nome FROM t_clyvo_produto LIMIT 1;
```

> Guarde os dois UUIDs: eles entram no lugar de `{ANIMAL_ID}` e `{PRODUTO_ID}` nos testes a seguir.

---

## 🛒 BLOCO 1 — Produtos

> 🔑 **Conta da equipe.** As leituras (T01–T05, T07, T09) funcionam com qualquer conta; criar e editar, não.

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
{ "error": "Produto com id id-que-nao-existe não encontrado." }
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

> 🔑 **Conta da equipe.** Igual ao Bloco 1: qualquer conta lê, só a equipe cria e edita.

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
  "dataInicio": "2027-03-10",
  "dataFim": "2027-03-10",
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
  "dataInicio": "2027-03-10",
  "dataFim": "2027-03-11",
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
{ "error": "A data de início do evento não pode ser no passado." }
```

---

### T21 — Criar evento sem título (campo obrigatório)
```
POST /api/v1/eventos-pet
```
```json
{
  "tipo": 0,
  "dataInicio": "2027-03-10",
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
{ "error": "Evento com id id-que-nao-existe não encontrado." }
```

---

## 🔔 BLOCO 3 — Lembretes

> 🔑 **Conta do tutor** (`lucas.santos@email.com`). Com a conta da equipe, todas as rotas deste bloco respondem `403`.  
> A partir do T26, os testes usam o `{ANIMAL_ID}` do SQL do início, que precisa ser um animal desse tutor.

---

### T23 — Listar todos os lembretes
```
GET /api/v1/lembretes
```

✅ **Esperado:** `200 OK`: array só com os lembretes dos animais do tutor logado (pode vir `[]` se ele ainda não tiver nenhum). O total vem no cabeçalho `X-Total-Count`.

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
  "agendadoEm": "2027-03-20T14:00:00",
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

### T31 — Atualizar lembrete: o `status` do corpo é ignorado
```
PUT /api/v1/lembretes/{id do T28}
```
```json
{
  "animalId": "{ANIMAL_ID}",
  "titulo": "Consulta de Rotina",
  "descricao": "Checkup anual completo com hemograma.",
  "tipo": 2,
  "agendadoEm": "2027-03-20T14:00:00",
  "recorrente": false,
  "status": 1
}
```

✅ **Esperado:** `200 OK`, com a descrição atualizada e `"status": 0` (Pendente), **não** `1`.

> Quem marca um lembrete como `Enviado` é o serviço que dispara a notificação, não o cliente: editar um
> lembrete não o dá por enviado. Mudar o `agendadoEm` volta o status para `Pendente`, para ele disparar
> de novo; mantendo a data, o status fica como estava.

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
{ "error": "A data do lembrete não pode ser no passado." }
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
  "agendadoEm": "2027-03-20T10:00:00",
  "recorrente": false,
  "status": 0
}
```

❌ **Esperado:** `404 Not Found`
```json
{ "error": "Animal 00000000-0000-0000-0000-000000000000 nao encontrado." }
```

> É a mesma resposta de um animal que existe mas é de **outro tutor**: a API não conta a quem não é
> dono se o animal existe.

---

### T34 — Criar lembrete sem campo obrigatório (animalId)
```
POST /api/v1/lembretes
```
```json
{
  "titulo": "Sem Animal",
  "tipo": 0,
  "agendadoEm": "2027-03-20T10:00:00",
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
{ "error": "Lembrete com id id-que-nao-existe não encontrado." }
```

---

## 💡 BLOCO 4 — Sugestões de Produto

> 🔑 **Conta do tutor**, como no Bloco 3.  
> A partir do T37, os testes usam o `{ANIMAL_ID}` (animal desse tutor) e o `{PRODUTO_ID}` do SQL do início.

---

### T36 — Listar todas as sugestões
```
GET /api/v1/sugestoes-produto
```

✅ **Esperado:** `200 OK`: array só com as sugestões dos animais do tutor logado (pode vir `[]`).

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
{ "error": "Produto com id 00000000-0000-0000-0000-000000000000 não encontrado." }
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
{ "error": "Animal 00000000-0000-0000-0000-000000000000 nao encontrado." }
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
{ "error": "Sugestão com id id-que-nao-existe não encontrada." }
```

---

## 🗑️ BLOCO 5 — Delete e Confirmação

> Siga a ordem abaixo para limpar os registros criados durante os testes.  
> 🔑 T47, T48, T52 e T54 com a **conta do tutor**; T49, T50, T51 e T53 com a **conta da equipe**.

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

> **Resultado esperado ao final:** cada um dos 54 passos devolve o status e a mensagem indicados, e os
> registros criados durante o guia foram apagados.
