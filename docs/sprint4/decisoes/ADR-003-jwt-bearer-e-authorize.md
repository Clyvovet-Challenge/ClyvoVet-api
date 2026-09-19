# ADR-003 — `AddJwtBearer` + `[Authorize]`, sem `FallbackPolicy`

**Status:** aceita · **Data:** 19/09/2026 · **Fase:** F2
**Revisita** uma decisão da Sprint 3 (`docs/auditoria-de-arquitetura.md` §2.1, item 1, e o
comentário no `ClyvoVet.Api.csproj` sobre `System.IdentityModel.Tokens.Jwt`).

## Contexto

O rubric exige "Autenticação e Autorização com JWT ou Identity". No estado atual, a API
**lê** o access token emitido pela API Java (`ValidadorDeTokenJwt` + `IdentidadeMiddleware`),
mas esse middleware é inerte — não rejeita nada —, e quem de fato protege as rotas é a
`X-Api-Key`. Não existe `AddAuthentication` nem `[Authorize]`, exatamente o que um avaliador
tende a procurar.

Na Sprint 3, o `AddJwtBearer` foi **deixado de fora deliberadamente**, por dois motivos:

1. ele traria consigo o modelo de `[Authorize]`/`FallbackPolicy`, e uma política global
   derrubaria `/health`, `/metrics`, `/swagger` e os webhooks — um health check quebrado
   retira a aplicação de rotação no Render;
2. também traria uma nova cadeia de `Microsoft.IdentityModel` para um grafo hoje unificado
   em 8.3.1.

## Decisão

Adotar `AddJwtBearer` e `[Authorize]` com políticas (`Autenticado`, `Equipe`), aplicadas
**por controller/ação** — **sem `FallbackPolicy`** (design §6.2). Isso resolve o primeiro
motivo sem abandonar o padrão. O segundo motivo passa a ser tratado como **risco a
verificar**, não como veto: a F2 abre com uma prova de compatibilidade, antes de qualquer
refatoração.

**Regras que permanecem válidas, sem exceção** (cada uma já custou uma rodada de
diagnóstico): a chave é `Convert.FromBase64String(segredo)`; apenas `tipo = access` é
aceito; `tutorId` nulo **nega** acesso; recurso de outro tutor responde **404**.

Rede de segurança: `Auth:ExigirToken` (padrão `true`) — quando `false`, as políticas
autorizam sem exigir token. A aplicação **continua subindo** mesmo sem `Jwt__Secret`
(registra um `Warning` no log) e passa a responder 401; ela nunca cai por isso.

## Consequências

- ➕ Passa a usar autenticação/autorização padrão do ASP.NET, algo reconhecível na avaliação.
- ➕ As rotas de infraestrutura continuam abertas (comprovado por teste: 200 sem token).
- ➖ **Quem não enviar `Authorization: Bearer` passa a receber 401.** O app já envia desde
  10/09 (`ENVIAR_BEARER_DOTNET` ligado por padrão, auditoria §2.1), mas outros clientes
  (scripts, `test_api.sh`) vão precisar de token.
- ➖ **`Jwt__Secret` precisa estar configurado no Render antes do deploy** — sem isso, toda
  rota protegida retorna 401. Isso entra no checklist de deploy da F2.
- ➖ O Widget e a Saúde Preditiva, que hoje aceitam chamadas sem `X-Api-Key`, passam a
  exigir Bearer.
- ❓ Caso o app **escreva** produto, o `Produto` POST/PUT/DELETE ficaria em `Autenticado`
  em vez de `Equipe`; isso será confirmado e registrado aqui durante a F2.

## Alternativas descartadas

- **Manter o middleware como está e apenas documentar.** Não atende ao que o avaliador
  procura; é o caminho de maior risco para os pontos deste item.
- **Emitir tokens aqui usando Identity.** Duplicaria o login já feito pela Java, deixando
  o tutor com duas contas.
