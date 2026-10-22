# Ensaio geral — a API completa, ponta a ponta

> O objetivo da entrega é o projeto "completo e funcional", com todas as funcionalidades integradas
> e funcionando em conjunto. Este registro mostra que isso foi executado, e não só descrito:
> a API subiu contra um MySQL e um MongoDB de verdade e respondeu a um roteiro de 79 checagens
> ponta a ponta.

**Data:** 27/09/2026 · **Código:** branch `sprint-4`, a partir do commit `4f4285a` · **Resultado: 79 PASS, 0 FAIL, 0 SKIP**

## Ambiente

Tudo local, nada na nuvem. Os segredos (senha do banco, segredo JWT, chaves) foram gerados na hora
só para o ensaio e passados por variável de ambiente, sem gravar nada em arquivo versionado.

| Peça | Como |
|---|---|
| API | `ASPNETCORE_ENVIRONMENT=Production` (logs em JSON, sem user-secrets), runtime .NET 8, `http://127.0.0.1:5191` |
| MySQL | `mysql:8.0` (8.0.46) em Docker, só em `127.0.0.1`, **com o schema real**: as migrations Flyway `V1` a `V20` da API Java (`db/migration/mysql`), aplicadas em ordem |
| MongoDB | `mongod` 8.3 local (`127.0.0.1:27099`), banco `clyvovet_ensaio` |
| Autenticação | access tokens HS256 montados como a API Java monta (mesmo emissor, público e claims), com um segredo de ensaio |
| Recorte por tutor | `Api__EscopoPorTutor=true`, para exercitar o 404 de recurso alheio e o 403 do Telegram |
| OCI Generative AI / bot do Telegram | **sem credenciais**: a saúde preditiva responde pelas regras, e o envio real pelo Telegram fica de fora |

O schema veio das migrations da API Java, e não do `schema/script_bd.sql`, porque é ele que a
produção usa. O `script_bd.sql` não cria `t_clyvo_raca`, `t_clyvo_base_doencas` nem
`t_clyvo_parecer_ia`, que a saúde preditiva usa.

## Roteiro

[`test_api.sh`](../test_api.sh), na raiz do repositório:

```bash
API_URL=http://127.0.0.1:5191 API_KEY=… TOKEN=<tutor> TOKEN_ADMIN=<admin> TOKEN_REFRESH=<refresh> \
ANIMAL_ID=<animal do tutor> TUTOR_ID=<tutor> OUTRO_ANIMAL_ID=<animal de outro tutor> OUTRO_TUTOR_ID=<outro tutor> \
./test_api.sh
```

Os ids usados são os do seed da API Java: o tutor `…0001` (Lucas), dono do animal `…0001` (Bolinha),
e a tutora `…0002` (Maria), dona do animal `…0002` (Mimi).

## Resultado por área

| Área | Checagens | Passou | O que cobre |
|---|---:|---:|---|
| 🛒 Produtos | 11 | 11 | listagem e filtros, paginação inválida (400), CRUD com token da equipe, 404, validações (400) |
| 🐾 Eventos pet | 11 | 11 | listagem e filtros, CRUD, data no passado e campo obrigatório (400), 404 |
| 🔔 Lembretes | 13 | 13 | listagem e filtros, criação com status forçado para Pendente, atualização, data no passado (400), animal inexistente (404) |
| 💡 Sugestões de produto | 12 | 12 | CRUD, `nomeAnimal`/`nomeProduto` na resposta, referências inexistentes (404), **PUT sem `dataSugestao` mantém a data** (bug corrigido nesta entrega) |
| 🔐 Autenticação, HATEOAS e integrações | 24 | 24 | ver a tabela abaixo |
| 🗑️ Limpeza | 8 | 8 | DELETE (204) dos registros criados e 404 ao buscá-los de novo |
| **Total** | **79** | **79** | |

**Autenticação, HATEOAS e integrações (24):**

| Checagem | Esperado | Obtido |
|---|---|---|
| Sem Bearer · sem `X-Api-Key` · token inválido · **refresh token** | 401 | 401 · 401 · 401 · 401 |
| `POST /produtos` com token de TUTOR | 403 | 403 |
| `X-Total-Count` e `Link` (`rel="next"`) na listagem | presentes | presentes (`X-Total-Count: 6`) |
| `ordenarPor=preco&direcao=desc` · campo não ordenável | 200 e decrescente · 400 | 200 e decrescente · 400 |
| `Accept: application/vnd.clyvovet.hateoas+json` | envelope `{itens, total, _links}` | envelope |
| `GET /lembretes/{id}` | `_links.self` | presente |
| Saúde preditiva e widget do próprio animal | 200 | 200 (parecer com `origem=REGRAS`) |
| Saúde preditiva, widget e lembrete de **animal de outro tutor** | 404 | 404 · 404 · 404 |
| Telegram: convite e vínculo do próprio tutor · convite de outro tutor | 200 · 403 | 200 (`vinculado=false`) · 403 |
| `POST /telegram/enviar` sem a chave do Telegram | 401 | 401 |
| `/health/live` · `/health/ready` · `/metrics` | 200 | 200 · 200 · 200 |

**MongoDB:** depois do roteiro, a coleção `pareceres_ia` tinha o parecer do Bolinha (`_id` = id do
animal, `origem: REGRAS`, `validoAte` = 7 dias depois) e o índice TTL `ttl_valido_ate`
(`expireAfterSeconds: 0`), conferidos no `mongosh`.

**Health check completo (`/health`):** `self`, `mysql-database` e `mongo` Healthy. O
`telegram-bot` fica Unhealthy (`invalid token`) porque o bot usado é um placeholder. Por isso o
status geral é Unhealthy, mas o `/health/ready` responde 200: o Telegram está fora da tag
`ready` de propósito, e é esse o comportamento documentado.

**Logs:** nenhuma entrada `Error`. Os `Warning` são os esperados: os testes negativos (400/404),
o ouvinte do Telegram sem bot real e o redirecionamento HTTPS sem porta HTTPS local.

## O que ficou de fora, e por quê

- **Parecer redigido pela OCI Generative AI:** sem credenciais da OCI, a API usou o *fallback*
  por regras, que é o comportamento projetado. A chamada real à OCI não foi exercitada.
- **Envio real de mensagem no Telegram:** exige um bot e um `chatId` de verdade. Foram exercitados
  a chave própria do envio (401), o convite e o vínculo, e o 403 para outro tutor.
- **App móvel e login pela API Java:** os tokens foram montados com o mesmo formato e segredo da API
  Java, sem passar pelo login dela.

## O que o ensaio encontrou

1. **O próprio roteiro não rodava mais.** O `test_api.sh`, escrito antes do JWT, não mandava o Bearer, usava
   `head -n -1`, que o macOS não tem, datas fixas que já estão ficando no passado e um título de
   1 caractere, que a validação atual recusa. Ele também montava JSON dentro de `"$(… "{a,b}" …)"`,
   e o bash 3.2 do macOS faz *brace expansion* ali, então o `curl` recebia só um pedaço do corpo.
   Todos esses problemas eram do script, não da API, e foram corrigidos no script.
2. **O recorte por tutor depende de uma chave de configuração.** O 404 para recurso de outro tutor
   só vale com `Api__EscopoPorTutor=true`; o padrão é desligado (ver a
   [auditoria](auditoria-de-arquitetura.md)). O README passou a dizer isso na seção de autenticação.
   *Depois do review de 27/09/2026 o padrão passou a ser **ligado**; `false` desliga.*
3. **Dois detalhes de ambiente, sem relação com o código:**
   - Um MySQL local sem SSL precisa de `AllowPublicKeyRetrieval=True` na connection string, por
     causa do `caching_sha2_password`. Na Azure a conexão usa SSL.
   - Forçar uma *collation* no servidor (`--collation-server`) quebra a migration `V14` da API Java
     com erro de FK incompatível, então o MySQL deve subir com a *collation* padrão.
