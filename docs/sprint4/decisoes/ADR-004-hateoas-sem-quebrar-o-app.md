# ADR-004 — HATEOAS sem quebrar o app

**Status:** aceita · **Data:** 19/09/2026 · **Fase:** F3

## Contexto

O rubric pede "HATEOAS implementado nos endpoints de consulta". A forma óbvia é embrulhar
cada listagem num envelope `{ itens, _links }`. **Mas as listagens desta API devolvem um
array JSON puro** (`IEnumerable<T>`, ex.: `ILembreteService.GetAllAsync`), e o app móvel — que
é outro repositório e não muda junto — lê esse array. Um envelope quebraria o app.

Além disso, sem o **total** de itens não dá para montar o link `last`; hoje nenhum serviço
devolve o total.

## Decisão

Três camadas, todas compatíveis (design §6.4):

1. **Por item, sempre ligado e aditivo:** `_links` dentro de cada objeto, indexado pela
   relação (`self`, `atualizar`, `excluir`, `colecao`, …). Acrescentar uma propriedade não
   quebra quem lê só os campos que conhece.
2. **Por coleção, sem mudar o corpo:** o array permanece; a navegação vai em `Link`
   (RFC 8288: `first`/`prev`/`next`/`last`) e `X-Total-Count`.
3. **Envelope opcional por negociação de conteúdo:**
   `Accept: application/vnd.clyvovet.hateoas+json` devolve `{ itens, page, pageSize, total,
   _links }`. Quem não pedir, não recebe.

Os `href` vêm do `LinkGenerator` pelo nome da ação; nenhuma rota é escrita à mão.
Os serviços passam a devolver `PaginaDeResultados<T>` (com total); o controller escolhe a
representação.

## Consequências

- ➕ O app continua funcionando sem nenhuma alteração.
- ➕ O avaliador vê HATEOAS já na resposta padrão do Swagger (`_links` por item) e pode
  pedir o envelope completo com o `Accept` (exemplo `curl` no README).
- ➕ Sem dependência de biblioteca de terceiros para ~100 linhas de código.
- ➖ Uma consulta `COUNT` a mais por listagem.
- ➖ O envelope completo só aparece com um cabeçalho — precisa estar bem documentado.

## Alternativas descartadas

- **Envelope sempre.** Cumpre o rubric ao pé da letra, mas quebra o app.
- **Nova versão da rota (`/api/v2/…`).** Duplica controllers para um ganho que a negociação
  de conteúdo já dá.
- **Biblioteca HATEOAS de terceiros.** Uma dependência a mais (e a auditar) para pouco código.
