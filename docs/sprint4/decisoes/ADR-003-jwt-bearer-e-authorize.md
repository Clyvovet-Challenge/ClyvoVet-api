# ADR-003 — `AddJwtBearer` + `[Authorize]`, sem `FallbackPolicy`

**Status:** aceita · **Data:** 19/09/2026 · **Fase:** F2
**Revisa** uma decisão da Sprint 3 (`docs/auditoria-de-arquitetura.md` §2.1, item 1, e o
comentário no `ClyvoVet.Api.csproj` sobre `System.IdentityModel.Tokens.Jwt`).

## Contexto

O rubric pede "Autenticação e Autorização com JWT ou Identity". Hoje a API **lê** o access
token da API Java (`ValidadorDeTokenJwt` + `IdentidadeMiddleware`), mas o middleware é
inerte (não rejeita nada) e quem protege as rotas é a `X-Api-Key`. Não há `AddAuthentication`
nem `[Authorize]`, que é justamente o que um avaliador procura.

Na Sprint 3 o `AddJwtBearer` foi **evitado de propósito**, por dois motivos:

1. traria o modelo de `[Authorize]`/`FallbackPolicy`, e uma política global derrubaria
   `/health`, `/metrics`, `/swagger` e os webhooks — health check quebrado tira a app de
   rotação no Render;
2. traria uma cadeia nova de `Microsoft.IdentityModel` para um grafo unificado em 8.3.1.

## Decisão

Usar `AddJwtBearer` e `[Authorize]` com políticas (`Autenticado`, `Equipe`), aplicadas
**por controller/ação** — **sem `FallbackPolicy`** (design §6.2). Isso resolve o motivo 1
sem abrir mão do padrão. O motivo 2 vira um **risco a verificar**, não um veto: a F2 começa
com uma prova de compatibilidade antes de qualquer refatoração.

**O que continua valendo, sem exceção** (cada item já custou uma rodada de diagnóstico):
chave = `Convert.FromBase64String(segredo)`; só `tipo = access`; `tutorId` nulo **nega**;
recurso de outro tutor responde **404**.

Rede de segurança: `Auth:ExigirToken` (padrão `true`) — em `false`, as políticas autorizam
sem token. A aplicação **sobe** sem `Jwt__Secret` (loga `Warning`) e responde 401; nunca cai.

## Consequências

- ➕ Autenticação/autorização padrão do ASP.NET, reconhecível na avaliação.
- ➕ As rotas de infraestrutura seguem abertas (testadas: 200 sem token).
- ➖ **Quem não mandar `Authorization: Bearer` passa a receber 401.** O app já manda desde
  10/09 (`ENVIAR_BEARER_DOTNET` ligado por padrão, auditoria §2.1), mas outros clientes
  (scripts, `test_api.sh`) precisam de token.
- ➖ **`Jwt__Secret` precisa estar no Render antes do deploy** — sem ele, toda rota
  protegida dá 401. Vai para o checklist de deploy da F2.
- ➖ Widget e Saúde Preditiva, que hoje aceitam chamada sem `X-Api-Key`, passam a exigir Bearer.
- ❓ Se o app **escreve** produto, `Produto` POST/PUT/DELETE fica em `Autenticado` em vez de
  `Equipe`; a F2 confirma e registra aqui.

## Alternativas descartadas

- **Manter o middleware e só documentar.** Não cumpre o que o avaliador procura; é o
  caminho de maior risco para os pontos do item.
- **Emitir tokens aqui com Identity.** Duplica o login da Java; o tutor teria duas contas.
