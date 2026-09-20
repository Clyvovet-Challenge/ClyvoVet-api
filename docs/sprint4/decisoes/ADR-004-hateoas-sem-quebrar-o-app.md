# ADR-004 — HATEOAS sem quebrar o app

**Status:** aceita · **Data:** 19/09/2026 · **Fase:** F3

## Contexto

O rubric pede "HATEOAS implementado nos endpoints de consulta". A saída óbvia seria embrulhar
cada listagem num envelope `{ itens, _links }`. **O problema é que as listagens desta API
devolvem array JSON puro** (`IEnumerable<T>`, ex.: `ILembreteService.GetAllAsync`), e é
exatamente esse array que o app móvel — repositório separado, que não acompanha essa mudança
— consome. Um envelope quebraria o aplicativo.

Existe ainda um segundo obstáculo: sem o **total** de itens não dá para montar o link `last`,
e hoje nenhum serviço devolve esse total.

## Decisão

Usar três camadas compatíveis entre si (design §6.4):

1. **Por item, sempre presente e aditiva:** `_links` dentro de cada objeto, indexado pela
   relação (`self`, `atualizar`, `excluir`, `colecao`, …). Somar uma propriedade não afeta
   quem só lê os campos já conhecidos.
2. **Por coleção, preservando o corpo:** o array segue igual; a navegação passa para o
   cabeçalho `Link` (RFC 8288: `first`/`prev`/`next`/`last`) e para `X-Total-Count`.
3. **Envelope opcional via negociação de conteúdo:**
   `Accept: application/vnd.clyvovet.hateoas+json` devolve `{ itens, page, pageSize, total,
   _links }`. Quem não pedir isso explicitamente não recebe nada diferente.

O `LinkGenerator` gera os `href` a partir do nome da ação — nenhuma rota fica escrita à mão.
Os serviços passam a devolver `PaginaDeResultados<T>` (já com o total), e cabe ao controller
decidir a representação final.

## Consequências

- ➕ O aplicativo segue funcionando sem precisar mudar nada.
- ➕ Quem avalia já vê HATEOAS na resposta padrão do Swagger (`_links` por item) e pode pedir
  o envelope completo com o cabeçalho `Accept` (há exemplo com `curl` no README).
- ➕ Não precisa de biblioteca de terceiros para algo em torno de 100 linhas de código.
- ➖ Cada listagem passa a rodar uma consulta `COUNT` a mais.
- ➖ O envelope completo só aparece com um cabeçalho específico — isso precisa estar bem
  documentado.

## Alternativas descartadas

- **Envelope sempre presente.** Cumpre o rubric ao pé da letra, mas quebra o app.
- **Nova versão de rota (`/api/v2/…`).** Duplicaria controllers por um ganho que a
  negociação de conteúdo já cobre.
- **Biblioteca HATEOAS de terceiros.** Mais uma dependência para auditar, em troca de pouco
  código economizado.
