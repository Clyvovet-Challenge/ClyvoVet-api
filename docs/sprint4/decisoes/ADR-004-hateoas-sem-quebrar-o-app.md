# ADR-004 — HATEOAS sem quebrar o app

**Status:** aceita · **Data:** 19/09/2026 · **Fase:** F3

## Contexto

O rubric exige "HATEOAS implementado nos endpoints de consulta". A solução óbvia seria
envolver cada listagem num envelope `{ itens, _links }`. **Só que as listagens desta API
devolvem um array JSON puro** (`IEnumerable<T>`, ex.: `ILembreteService.GetAllAsync`), e é
esse array que o app móvel — repositório à parte, que não muda junto — consome. Um envelope
quebraria o aplicativo.

Há ainda outro obstáculo: sem o **total** de itens, não é possível construir o link `last`;
atualmente nenhum serviço devolve esse total.

## Decisão

Adotar três camadas compatíveis entre si (design §6.4):

1. **Por item, sempre presente e aditiva:** `_links` dentro de cada objeto, indexado pela
   relação (`self`, `atualizar`, `excluir`, `colecao`, …). Adicionar uma propriedade não
   quebra quem só lê os campos já conhecidos.
2. **Por coleção, preservando o corpo:** o array continua como está; a navegação passa a
   viver no cabeçalho `Link` (RFC 8288: `first`/`prev`/`next`/`last`) e em `X-Total-Count`.
3. **Envelope opcional via negociação de conteúdo:**
   `Accept: application/vnd.clyvovet.hateoas+json` retorna `{ itens, page, pageSize, total,
   _links }`. Quem não pedir explicitamente, não recebe.

Os `href` são gerados pelo `LinkGenerator` a partir do nome da ação — nenhuma rota é
escrita manualmente. Os serviços passam a retornar `PaginaDeResultados<T>` (já com o
total), cabendo ao controller escolher a representação final.

## Consequências

- ➕ O aplicativo continua funcionando sem nenhuma mudança de sua parte.
- ➕ O avaliador já enxerga HATEOAS na resposta padrão do Swagger (`_links` por item) e pode
  solicitar o envelope completo usando o cabeçalho `Accept` (há um exemplo com `curl` no
  README).
- ➕ Não exige biblioteca de terceiros para o que são ~100 linhas de código.
- ➖ Passa a existir uma consulta `COUNT` extra por listagem.
- ➖ O envelope completo só aparece mediante um cabeçalho específico — precisa ficar bem
  documentado.

## Alternativas descartadas

- **Envelope sempre presente.** Atende o rubric ao pé da letra, mas quebra o app.
- **Nova versão de rota (`/api/v2/…`).** Duplicaria controllers por um ganho que a
  negociação de conteúdo já entrega.
- **Biblioteca HATEOAS de terceiros.** Mais uma dependência para auditar, em troca de pouco
  código evitado.
