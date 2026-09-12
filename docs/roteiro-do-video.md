# Roteiro do vídeo — API .NET

Este repositório responde por **duas** disciplinas no mesmo vídeo.

| Disciplina | Peso do vídeo | O que ele tem de provar |
|---|---|---|
| **DevOps Tools & Cloud Computing** | **80 dos 100 pontos** | o deploy acontecendo, seguindo o README |
| **Advanced Business Development with .NET** | — | a API e o domínio funcionando |

> O vídeo da entrega anterior está em
> `https://www.youtube.com/watch?v=8R_eru120m8`. Ele **não serve** para esta
> Sprint: a infraestrutura mudou (banco agora compartilhado com a API Java) e
> *"vídeo incompatível com o que foi entregue"* é a penalidade mais cara da
> régua. Grave de novo.

---

> ## ⚠️ Antes de planejar
>
> **O vídeo de DevOps não é demo de API — é o deploy acontecendo.** A régua
> exige *"deploy seguindo **exatamente** os passos descritos no README.md"*.
> Grava-se a execução, não o resultado.
>
> **O item 9.3 pede CRUD em duas tabelas relacionadas, provado por `SELECT` no
> banco.** Aqui o par é natural: **`t_clyvo_produto` ← `t_clyvo_sugestao_produto`**,
> ligadas por `produto_id`. As duas são o núcleo desta API e ambas têm CRUD
> completo — `POST`, `GET`, `PUT`, `DELETE` nos dois controllers.
>
> `t_clyvo_tutor` e `t_clyvo_animal` aparecem por FK, mas são **da API Java**: a
> .NET só lê. Não as use como as "duas tabelas" desta disciplina.

---

## Antes de apertar o REC

| | |
|---|---|
| ☐ | `az login` feito e assinatura certa selecionada |
| ☐ | `azure/00-variaveis.sh` revisado — nomes únicos em toda a Azure |
| ☐ | Terminal com fonte grande (16pt+) |
| ☐ | Postman com as requisições montadas, ou o `test_api.sh` à mão |
| ☐ | Cliente MySQL aberto numa segunda janela, já conectado |
| ☐ | Os recursos **ainda não existirem** — o vídeo mostra criando |

---

## O roteiro

### Parte 1 — Provisionar (≈3 min)

| Cena | Comando | Narração |
|---|---|---|
| Abertura | — | "PetTrack API, ASP.NET Core 8, publicada em Azure App Service sobre MySQL Flexible Server" |
| Recursos | `bash azure/01-criar-recursos.sh` | "grupo de recursos e o banco gerenciado — **PaaS, nada em container**" |
| App Service | `bash azure/02-criar-app-service.sh` | "o app roda direto no App Service; o `Dockerfile` do repositório não participa da publicação" |
| Deploy | `bash azure/03-deploy.sh` | "publica o build do `dotnet publish`" |
| Portal | — | 10 segundos no portal da Azure, mostrando os recursos criados |
| Swagger | abrir `/swagger` na URL publicada | "a API no ar, respondendo pela internet" |

> **A frase que vale ponto:** *"nem o app nem o banco estão em container — os
> dois são serviços gerenciados da Azure"*. É exatamente o que a régua penaliza
> quando é falso, e vale dizer quando é verdade.

### Parte 2 — CRUD com prova no banco (≈4 min) — *o item 9.3*

Duas janelas lado a lado: Postman/Swagger de um lado, MySQL do outro.

| # | Operação | Endpoint | Prove no banco |
|---|---|---|---|
| 1 | **CREATE** produto | `POST /api/v1/produtos` | `SELECT id, nome, preco FROM t_clyvo_produto ORDER BY id DESC LIMIT 1;` |
| 2 | **CREATE** sugestão | `POST /api/v1/sugestoes-produto` com o `produtoId` acima | `SELECT s.id, p.nome FROM t_clyvo_sugestao_produto s JOIN t_clyvo_produto p ON p.id = s.produto_id;` — **o JOIN evidencia o relacionamento** |
| 3 | **READ** | `GET /api/v1/produtos` e `GET /api/v1/sugestoes-produto` | a mesma linha na resposta |
| 4 | **UPDATE** | `PUT /api/v1/produtos/{id}` mudando o preço | `SELECT` **antes e depois**, na mesma tela |
| 5 | **DELETE** | `DELETE /api/v1/sugestoes-produto/{id}` | o `SELECT` volta vazio |

> **Narre o que o `SELECT` prova.** *"a linha saiu do banco"* vale mais que
> *"a API respondeu 204"* — é a diferença que o item 9.3 cobra.

### Parte 3 — Advanced Business Development (≈1,5 min)

O que esta API tem além do CRUD, e que só aparece se você mostrar:

- **Lembretes** (`/api/v1/lembretes`) — a funcionalidade que o app consome
- **Saúde preditiva** (`/api/v1/widget-saude-preditiva`) — o parecer por
  raça/idade. Diga se veio da **OCI** ou das **regras locais**: hoje sai por
  `origem: REGRAS`, e afirmar IA sem a credencial configurada seria falso
- **Telegram** (`/api/v1/telegram`) — o canal de notificação
- **Duas camadas de credencial**: `X-Api-Key` prova que a chamada veio do app;
  `Bearer` prova quem a fez. Mostre um `401` sem a chave e um `200` com ela —
  dois segundos, e evidencia segurança

---

## O que **não** fazer

- **Não corte o meio do deploy.** Corte no meio sugere que não funcionou.
- **Não mostre segredo**: `Api:ApiKey`, senha do MySQL, connection string. Se
  vazar no vídeo, troque a credencial antes de entregar.
- **Não rode `azure/04-destruir-recursos.sh`** no vídeo nem depois dele —
  recurso apagado equivale a entrega em localhost. Só **após a correção**.
- **Não reaproveite o vídeo anterior.** A infraestrutura descrita nele é outra.

---

## Depois de subir

1. Link do YouTube no `README.md`, substituindo o da entrega anterior.
2. PDF com nome completo e RM de todos, link do GitHub e link do YouTube — e
   nada além disso.
3. Vídeo **não listado**, nunca privado.
