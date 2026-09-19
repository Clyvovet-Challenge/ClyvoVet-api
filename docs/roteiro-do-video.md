# Roteiro do vídeo — API .NET

Este repositório atende **duas** disciplinas com o mesmo vídeo.

| Disciplina | Peso do vídeo | O que ele tem de provar |
|---|---|---|
| **DevOps Tools & Cloud Computing** | **80 dos 100 pontos** | o deploy acontecendo, seguindo o README |
| **Advanced Business Development with .NET** | — | a API e o domínio funcionando |

> O vídeo da entrega anterior está disponível em
> `https://www.youtube.com/watch?v=8R_eru120m8`. Ele **não serve** para esta
> Sprint: a infraestrutura mudou (o banco agora é compartilhado com a API Java), e
> *"vídeo incompatível com o que foi entregue"* é a penalidade mais alta da régua.
> É preciso gravar de novo.

---

> ## ⚠️ Antes de planejar
>
> **O vídeo de DevOps não é uma demo da API — é o deploy sendo executado.** A
> régua exige *"deploy seguindo **exatamente** os passos descritos no
> README.md"*. O que se grava é a execução, não o resultado final.
>
> **O item 9.3 exige CRUD em duas tabelas relacionadas, comprovado por `SELECT`
> no banco.** O par natural aqui é **`t_clyvo_produto` ← `t_clyvo_sugestao_produto`**,
> ligadas por `produto_id`. Ambas são o núcleo desta API e têm CRUD completo —
> `POST`, `GET`, `PUT`, `DELETE` nos dois controllers.
>
> `t_clyvo_tutor` e `t_clyvo_animal` aparecem via FK, mas pertencem **à API
> Java**: a .NET apenas as lê. Não use essas como as "duas tabelas" desta
> disciplina.

---

## Antes de apertar o REC

| | |
|---|---|
| ☐ | **O repositório [`clyvovet-backend-java`](https://github.com/Clyvovet-Challenge/clyvovet-backend-java) clonado ao lado deste** — é de lá que a infraestrutura sobe |
| ☐ | `az login` feito e assinatura certa selecionada |
| ☐ | `azure/00-variaveis.sh` **do repo Java** revisado — nomes únicos em toda a Azure |
| ☐ | Os segredos exportados no ambiente, **nunca em arquivo versionado** (passo 2 do README de lá) |
| ☐ | Terminal com fonte grande (16pt+) |
| ☐ | Postman com as requisições montadas, ou o `test_api.sh` à mão |
| ☐ | Cliente MySQL aberto numa segunda janela, já conectado |
| ☐ | Os recursos **ainda não existirem** — o vídeo mostra criando |

---

## O roteiro

### Parte 1 — Provisionar as DUAS APIs (≈5 min)

> **Os scripts `azure/01` a `04` DESTE repositório não aparecem no vídeo.** Eles
> pertencem ao caminho da entrega **anterior**, e o README deste repositório já
> avisa isso logo no início: *"este repositório não provisiona mais nada
> sozinho"*. Na Sprint 3 existe **uma única** infraestrutura, levantada pelos
> scripts `azure/00` a `09` do repositório
> [`clyvovet-backend-java`](https://github.com/Clyvovet-Challenge/clyvovet-backend-java)
> — Resource Group, MySQL, o plano compartilhado e **as duas** APIs, incluindo
> esta.
>
> É por isso que o vídeo mostra as duas subindo: não é uma escolha de roteiro, é
> como a infraestrutura foi de fato montada.

Grave seguindo os seis passos do README de lá, nesta ordem:

| # | Passo (README do repo Java) | Comando | Narração |
|---|---|---|---|
| — | Abertura | — | "ClyvoVet API, ASP.NET Core 8. Ela e a API Java do time dividem um plano do App Service e **o mesmo banco**" |
| 1 | Clonar o repositório | `git clone …/clyvovet-backend-java.git` | "a infraestrutura das duas vive num repositório só" |
| 2 | Definir os segredos **no ambiente** | `export …` | **não mostre os valores na tela.** "os segredos entram por variável de ambiente — nada disso está versionado" |
| 3 | Conferir o que a assinatura oferece | `bash azure/00-descobrir-recursos.sh` | "a assinatura acadêmica bloqueia algumas regiões; o script diz quais estão liberadas" |
| 4 | Criar os recursos, um por vez | `01-resource-group` → `02-banco-mysql` → `03-plano-app-service` → `04-webapp-java` → `05-webapp-dotnet` → `06-configuracoes` | "MySQL gerenciado e App Service — **PaaS, nada em container**. Dois webapps, um plano" |
| 5 | Publicar as aplicações | `bash azure/07-deploy-java.sh` **e depois** `bash azure/08-deploy-dotnet.sh` | "a Java primeiro, e a ordem importa — ver abaixo" |
| 6 | Verificar de ponta a ponta | `bash azure/09-verificar.sh` | "o script bate na saúde das duas e prova que o schema existe" |

> ### ⚠️ A ordem do passo 5 não é uma preferência — é o que evita quebrar tudo
>
> O banco nasce **vazio**, e é o **Flyway da API Java, no primeiro boot dela**,
> quem cria o schema. Se o `schema/script_bd.sql` deste repositório for aplicado
> antes disso, o banco fica com as tabelas mas **sem a `flyway_schema_history`**
> — e o Flyway se recusa a migrar um schema não vazio que ele não reconhece. A
> API Java simplesmente não sobe, e não existe conserto rápido no meio de uma
> gravação.
>
> **Java primeiro. Sempre.**

O `09-verificar.sh` encerra o bloco por conta própria: ele consulta
`{java}/actuator/health`, `{dotnet}/health/live` e `{dotnet}/health/ready`, e em
seguida faz um cadastro real pela API Java — a prova de que o Flyway criou o
schema. Deixe a saída completa aparecer na tela.

Em seguida, 10 segundos mostrando os recursos no **portal da Azure**, e o
`/swagger` desta API aberto na URL publicada.

> **A frase que garante o ponto:** *"nem o app nem o banco estão em container —
> os dois são serviços gerenciados da Azure"*. É exatamente o que a régua
> penaliza quando é falso, e vale a pena dizer quando é verdadeiro.

### Parte 2 — CRUD com prova no banco (≈4 min) — *o item 9.3*

Duas janelas lado a lado: Postman/Swagger de um lado, MySQL do outro.

| # | Operação | Endpoint | Prove no banco |
|---|---|---|---|
| 1 | **CREATE** produto | `POST /api/v1/produtos` | `SELECT id, nome, preco FROM t_clyvo_produto ORDER BY id DESC LIMIT 1;` |
| 2 | **CREATE** sugestão | `POST /api/v1/sugestoes-produto` com o `produtoId` acima | `SELECT s.id, p.nome FROM t_clyvo_sugestao_produto s JOIN t_clyvo_produto p ON p.id = s.produto_id;` — **o JOIN evidencia o relacionamento** |
| 3 | **READ** | `GET /api/v1/produtos` e `GET /api/v1/sugestoes-produto` | a mesma linha na resposta |
| 4 | **UPDATE** | `PUT /api/v1/produtos/{id}` mudando o preço | `SELECT` **antes e depois**, na mesma tela |
| 5 | **DELETE** | `DELETE /api/v1/sugestoes-produto/{id}` | o `SELECT` volta vazio |

> **Explique o que o `SELECT` está provando.** Dizer *"a linha saiu do banco"*
> vale mais do que *"a API respondeu 204"* — essa é a diferença que o item 9.3
> exige.

### Parte 3 — Advanced Business Development (≈1,5 min)

O que esta API oferece além do CRUD só fica visível se for mostrado:

- **Lembretes** (`/api/v1/lembretes`) — a funcionalidade que o app consome
- **Saúde preditiva** (`/api/v1/widget-saude-preditiva`) — o parecer por
  raça/idade. Informe se ele veio da **OCI** ou das **regras locais**: hoje sai
  como `origem: REGRAS`, e afirmar que é IA sem a credencial configurada seria
  incorreto
- **Telegram** (`/api/v1/telegram`) — o canal de notificação
- **Duas camadas de credencial**: `X-Api-Key` comprova que a chamada partiu do
  app; `Bearer` comprova quem a fez. Mostre um `401` sem a chave e um `200` com
  ela — leva dois segundos e evidencia a segurança

---

## O que **não** fazer

- **Não corte no meio do deploy.** Um corte no meio sugere que algo não
  funcionou.
- **Não exiba nenhum segredo**: `Api:ApiKey`, senha do MySQL, connection string.
  Se algum vazar no vídeo, troque a credencial antes de entregar.
- **Não execute script de destruição** — nem o `azure/04-destruir-recursos.sh`
  deste repositório, nem o `azure/99-destruir.sh` do repo Java — durante o vídeo
  ou depois dele. Recurso apagado equivale a uma entrega em localhost. Só
  **depois da correção**.
- **Não aplique `schema/script_bd.sql` deste repositório.** Veja o aviso do
  passo 5: isso impede a API Java de subir.
- **Não reaproveite o vídeo anterior.** A infraestrutura descrita nele é outra.

---

## Depois de subir

1. Atualizar o link do YouTube no `README.md`, substituindo o da entrega
   anterior.
2. Gerar um PDF com nome completo e RM de todos os integrantes, o link do
   GitHub e o link do YouTube — sem mais nada além disso.
3. Publicar o vídeo como **não listado**, nunca como privado.
