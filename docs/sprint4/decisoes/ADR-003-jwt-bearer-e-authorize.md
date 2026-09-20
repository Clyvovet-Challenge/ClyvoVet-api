# ADR-003 — `AddJwtBearer` + `[Authorize]`, sem `FallbackPolicy`

**Status:** aceita · **Data:** 19/09/2026 · **Fase:** F2
**Revisita** uma decisão da Sprint 3 (`docs/auditoria-de-arquitetura.md` §2.1, item 1, e o
comentário no `ClyvoVet.Api.csproj` sobre `System.IdentityModel.Tokens.Jwt`).

## Contexto

O rubric pede "Autenticação e Autorização com JWT ou Identity". Hoje a API **lê** o access
token emitido pela API Java (`ValidadorDeTokenJwt` + `IdentidadeMiddleware`), só que esse
middleware não rejeita nada — é inerte —, e a proteção de rota de fato fica por conta da
`X-Api-Key`. Não há `AddAuthentication` nem `[Authorize]`, que é justamente o que costuma
chamar atenção de quem avalia.

Na Sprint 3, deixamos o `AddJwtBearer` **fora de propósito**, por dois motivos:

1. ele puxa junto o modelo de `[Authorize]`/`FallbackPolicy`, e uma política global
   derrubaria `/health`, `/metrics`, `/swagger` e os webhooks — um health check quebrado tira
   a aplicação de rotação no Render;
2. também puxaria uma cadeia nova de `Microsoft.IdentityModel` para um grafo hoje unificado
   em 8.3.1.

## Decisão

Passar a usar `AddJwtBearer` e `[Authorize]` com políticas (`Autenticado`, `Equipe`),
aplicadas **por controller/ação** — **sem `FallbackPolicy`** (design §6.2). Isso resolve o
primeiro motivo sem largar o padrão. O segundo vira **risco a verificar**, não veto: a F2
começa com uma prova de compatibilidade, antes de qualquer refatoração.

**Regras que continuam valendo, sem exceção** (cada uma já custou uma rodada de
diagnóstico): a chave vem de `Convert.FromBase64String(segredo)`; só `tipo = access` é
aceito; `tutorId` nulo **nega** acesso; recurso de outro tutor responde **404**.

Rede de segurança: `Auth:ExigirToken` (padrão `true`) — em `false`, as políticas autorizam
sem cobrar token. Sem `Jwt__Secret`, a aplicação **continua no ar** (registra um `Warning` no
log) e passa a responder 401; ela não cai por causa disso.

## Consequências

- ➕ Passa a usar autenticação/autorização padrão do ASP.NET, item reconhecível na avaliação.
- ➕ As rotas de infraestrutura seguem abertas (comprovado por teste: 200 sem token).
- ➖ **Quem não mandar `Authorization: Bearer` passa a levar 401.** O app já manda desde
  10/09 (`ENVIAR_BEARER_DOTNET` ligado por padrão, auditoria §2.1), mas outros clientes
  (scripts, `test_api.sh`) vão precisar de token.
- ➖ **`Jwt__Secret` precisa estar configurado no Render antes do deploy** — sem isso, toda
  rota protegida devolve 401. Isso entra no checklist de deploy da F2.
- ➖ Widget e Saúde Preditiva, que hoje aceitam chamada sem `X-Api-Key`, passam a exigir
  Bearer.
- ❓ Se o app **escrever** produto, o `Produto` POST/PUT/DELETE ficaria em `Autenticado` em
  vez de `Equipe`; isso será confirmado e registrado aqui durante a F2.

## Alternativas descartadas

- **Deixar o middleware como está e só documentar.** Não atende ao que o avaliador procura;
  é o caminho de maior risco para os pontos deste item.
- **Emitir tokens aqui via Identity.** Duplicaria o login já feito pela Java, deixando o
  tutor com duas contas.
