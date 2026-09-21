# ADR-003 — `AddJwtBearer` + `[Authorize]`, sem `FallbackPolicy`

**Status:** implementada · **Data:** 20/09/2026 (aceita em 19/09/2026) · **Fase:** F2
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
- ✅ **D1 respondida:** o app **não escreve** produto. Ele só faz `GET` em `/produtos` (e só
  escreve `/lembretes` na API .NET), então `Produto` POST/PUT/DELETE ficou em `Equipe` (ADMIN
  ou VETERINARIO), como no design §6.2. Conferido no app em 20/09/2026 (`main`, commit `42b36ae`).
- ➖ **`ADMIN_CLINICA` fica fora de `Equipe`, de propósito.** A API Java tem quatro perfis e o
  design (§6.1) listava três; `Equipe` reconhece só `ADMIN` e `VETERINARIO`, então o gestor de
  uma clínica recebe 403 ao escrever produto (ler continua liberado). Decisão do dono em
  20/09/2026: o catálogo de produtos é **global**, e quem administra uma clínica não deve
  alterar o que todas as clínicas veem. Reavaliar se o catálogo virar por clínica ou se o app
  ganhar uma tela de cadastro de produto.

## Alternativas descartadas

- **Deixar o middleware como está e só documentar.** Não atende ao que o avaliador procura;
  é o caminho de maior risco para os pontos deste item.
- **Emitir tokens aqui via Identity.** Duplicaria o login já feito pela Java, deixando o
  tutor com duas contas.

## Resultado (F2)

- **Prova de compatibilidade (T1).** O `JwtBearer 8.0.11` convive com o `IdentityModel` já
  presente. Grafo resolvido: `Protocols` e `Protocols.OpenIdConnect` em **7.1.2**;
  `Tokens`, `JsonWebTokens`, `Logging`, `Abstractions` e `System.IdentityModel.Tokens.Jwt` em
  **8.3.1**; sem `NU1605`. A referência direta ao `System.IdentityModel.Tokens.Jwt 8.3.1`
  **fica**: é ela que mantém o resto do grafo em 8.3.1.
- **Prova de execução.** Compilar não prova que duas versões convivem. A prova foi o
  `AutenticacaoJwtTests` (21 casos): token válido passa, e os inválidos (refresh, expirado,
  outra chave, chave derivada de UTF-8, emissor ou público errado, malformado, sem `sub`)
  respondem 401 — sem `TypeLoadException` nem `MissingMethodException`.
- **`tipo = access` vive na autenticação (`OnTokenValidated`), não na política.** As rotas do
  Telegram não têm `[Authorize]`, mas o `EscopoDoTutor` lê a identidade delas: com a regra só
  na política, um refresh token viraria identidade ali.
- **Sem `Jwt__Secret` a aplicação sobe e responde 401.** No lugar da chave ausente entra uma
  aleatória de 32 bytes que ninguém conhece, então nenhum token confere; ausente registra
  `Warning`, e base64 inválido ou curto registra `Error`.
- **401 e 403 do `JwtBearer` saem sem corpo**, como o 401 da `X-Api-Key` já saía. O `error`
  não é escrito nelas.
- **Sem `FallbackPolicy`, provado por teste:** `/health/live`, `/health/ready`, `/metrics`,
  `/swagger/v1/swagger.json` e `/swagger/index.html` respondem 200 sem token.
- **`Content-Type` do erro mudou** de `application/json` para `application/problem+json`. O
  corpo mantém `error` (e `referencia` só em falha de servidor) e ganha `type`, `title`,
  `status` e `traceId`. Quem só aceita `text/html` continua recebendo o corpo, por um
  *fallback* do tratador.
- **Achado — a `referencia` nunca chegava ao app.** Numa falha 500 real, nem `referencia` nem
  o header `X-Correlation-Id` saíam: o `ExceptionHandlerMiddleware` limpa os headers da
  resposta antes de chamar o tratador, que lia o id justamente do header. O defeito já existia
  antes da F2 e os testes de unidade não o viam, porque montavam o contexto à mão. Corrigido
  guardando o id em `HttpContext.Items` (commit `f09b620`), com testes pelo pipeline real.
- **Achado — o `/health` completo chama a Bot API do Telegram de verdade.** Um teste que o
  executava deixava a suíte refém da rede (até ~100 s). Ele passou a checar só que a rota não
  tem `[Authorize]` (commit `b556b97`).
