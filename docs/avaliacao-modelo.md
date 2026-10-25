# Guia de avaliação — ClyvoVet API (.NET)

> Código no commit `{{COMMIT}}`, guia gerado em {{DATA}}.

Este guia diz **como testar** a API .NET do ClyvoVet e **onde olhar cada item do rubric**. São três níveis,
do mais rápido ao mais completo, e o primeiro não precisa de nenhuma credencial.

| Nível | O que prova | Precisa de |
|---|---|---|
| 1. Build e testes | compila, 585 testes automatizados (padrão AAA), cobertura | .NET 8 SDK |
| 2. API no ar | JWT, perfis, paginação, HATEOAS, health checks | as credenciais da seção 2 |
| 3. Mapa do rubric | em que arquivo está cada requisito | só ler |

---

## 1. Build e testes (2 minutos, sem credencial)

```bash
dotnet build src/ClyvoVet.Api/ClyvoVet.Api.csproj
dotnet test tests/ClyvoVet.Api.Tests.Unit
dotnet test tests/ClyvoVet.Api.Tests.Integration
```

**Resultado esperado:** build sem erros nem avisos; **357** testes unitários e **228** de integração passando.
Outros 8 testes, contra um MongoDB real, são pulados quando a variável `MONGO_TEST_URI` não existe.

Cobertura de linhas das camadas Domain e Application (falha abaixo de 90%; medição na entrega: 98,8%):

```bash
dotnet tool restore
bash scripts/cobertura.sh
```

Os testes de integração sobem a API inteira em memória (`WebApplicationFactory`) com um banco EF Core
InMemory, então **não precisam de MySQL, de MongoDB nem da API Java**.

---

## 2. A API no ar

| O quê | Endereço |
|---|---|
| **API .NET** (este projeto), Swagger | https://clyvovet-api.onrender.com/swagger |
| Saúde: dependências | https://clyvovet-api.onrender.com/health |
| Métricas (Prometheus) | https://clyvovet-api.onrender.com/metrics |
| **API Java** (emite o login), Swagger | https://clyvovet-backend-java.onrender.com/swagger-ui.html |

> ⏳ **Acorde a Java antes de começar.** O plano gratuito da Render desliga o serviço depois de cerca de 15 minutos
> sem uso. A primeira chamada leva **alguns minutos na Java** (Spring) e algumas dezenas de segundos na .NET. Abra o
> Swagger da Java primeiro e espere a página carregar; depois disso tudo responde rápido.

### Credenciais

| Item | Valor |
|---|---|
| `X-Api-Key` da API .NET | `{{API_KEY}}` |
| Senha das contas de demonstração | `{{SENHA_DEMO}}` |

Contas de demonstração (todas com a mesma senha):

| E-mail | Perfil | O que enxerga na .NET |
|---|---|---|
| `demo.tutor@clyvovet.com` | TUTOR | os próprios lembretes e sugestões; lê produtos e eventos |
| `demo.veterinaria@clyvovet.com` | VETERINARIO | lê e **escreve** produtos e eventos; sem tutor, recebe 403 nas rotas de tutor |
| `demo.gestor@clyvovet.com` | ADMIN_CLINICA | lê produtos e eventos |
| `demo.admin@clyvovet.com` | ADMIN | lê e **escreve** produtos e eventos |

### Passo a passo (cerca de 10 minutos)

1. **Saúde.** Abra `/health` (anônimo). Deve vir `Healthy`: a API, o MySQL e o bot do Telegram.
2. **Token.** No Swagger da Java, `POST /api/v1/auth/login` com
   `{"email": "demo.tutor@clyvovet.com", "senha": "{{SENHA_DEMO}}"}`. Copie o `accessToken` da resposta.
3. **Autorizar na .NET.** No Swagger da .NET, clique em **Authorize** e informe o `X-Api-Key` e o `Bearer`
   (só o token; o Swagger acrescenta o prefixo). As duas credenciais são exigidas.
4. **Perfis.** Com o token do **tutor**: `GET /api/v1/produtos` dá **200**, `GET /api/v1/lembretes` dá **200** (só
   os dele) e `POST /api/v1/produtos` dá **403**. Repita o login como `demo.veterinaria`: `GET /api/v1/produtos`
   dá **200** e `GET /api/v1/lembretes` dá **403** (a veterinária não tem tutor no token).
5. **Paginação, ordenação e HATEOAS.** `GET /api/v1/produtos?page=1&pageSize=2&ordenarPor=preco&direcao=desc`
   devolve um array JSON, e o total e a navegação vêm nos cabeçalhos `X-Total-Count` e `Link`. Com o cabeçalho
   `Accept: application/vnd.clyvovet.hateoas+json` a resposta vira um envelope com `_links`.
6. **Lembretes de ponta a ponta (como tutor).** Pegue um `id` de animal em `GET /api/v1/animais`, no Swagger da
   Java (com o mesmo token). Na .NET: `POST /api/v1/lembretes` (corpo e regras em *Lembretes*, no README; o
   `agendadoEm` precisa ser futuro, em horário de Brasília e sem fuso), depois `GET`, `PUT` e `DELETE` (204).
   O `animalId` de outro tutor responde **404**, de propósito.
7. **Saúde preditiva.** `GET /api/v1/saude-preditiva/{animalId}`: no ambiente publicado o parecer sai por
   **regras** (não há provedor de IA configurado lá) e é guardado em cache.

> O roteiro de 54 passos em [`docs/guia-de-testes-manuais.md`](guia-de-testes-manuais.md) foi escrito para o banco
> local. No ambiente publicado, pegue os ids pela própria API (passo 6) em vez de consultar o banco.

---

## 3. Onde olhar cada item do rubric

| Item (pts) | Onde está |
|---|---|
| **Arquitetura e código (30)** | Clean Architecture em 4 projetos em `src/`; as regras de dependência são **testadas** em `tests/ClyvoVet.Api.Tests.Unit/ArquiteturaTests.cs`. SOLID e Clean Code: README, seção *Princípios aplicados*. Injeção de dependência: `src/ClyvoVet.Infrastructure/InfrastructureServiceExtensions.cs` e `src/ClyvoVet.Api/Program.cs`. Exceções globais: `src/ClyvoVet.Api/Errors/TratadorGlobalDeExcecoes.cs` e `MapaDeErro.cs`. |
| **API RESTful (20)** | Swagger no ar (`/swagger`) e exportado em `docs/swagger/openapi-v1.json`. Paginação, ordenação e filtros: README, seção *Paginação, ordenação e navegação (HATEOAS)*. HATEOAS: `src/ClyvoVet.Api/Hateoas/GeradorDeLinks.cs`. JWT e autorização por perfil: `src/ClyvoVet.Api/Extensions/AutenticacaoJwtExtensions.cs`. |
| **Persistência (20)** | EF Core com migrations: `src/ClyvoVet.Infrastructure/Data/Migrations/` e `schema/ef/` (README, seção 3.3). MongoDB: `src/ClyvoVet.Infrastructure/Mongo/` e README, seção *NoSQL*. Repositórios concretos: `src/ClyvoVet.Infrastructure/Repositories/`. O banco é MySQL, com a autorização registrada no README (*Duas APIs, um banco*). |
| **Monitoramento e testes (20)** | Health checks em `/health`, `/health/live` e `/health/ready`; log estruturado em JSON (Serilog) e métricas em `/metrics` (OpenTelemetry), descritos no README em *Monitoramento e Observabilidade*. Testes xUnit no padrão AAA em `tests/`; cobertura em `scripts/cobertura.sh`. |
| **Documentação (10)** | `README.md` (visão geral, arquitetura com diagramas, endpoints, instalação, testes e integrantes) e `docs/swagger/openapi-v1.json`. |

---

## 4. Limites do ambiente publicado

- **Cold start:** a Render grátis dorme após ~15 minutos sem requisições; a primeira chamada espera a instância subir.
- **Telegram e lembretes:** o bot (`@clyvovet_notificacoes_bot`) e a varredura de lembretes rodam dentro do processo da
  API. Com o serviço dormindo, nenhum dos dois funciona; qualquer chamada o acorda.
- **MongoDB:** não está ligado no ambiente publicado; o cache do parecer de IA fica no MySQL. O repositório do MongoDB
  é exercitado pelos testes (`MONGO_TEST_URI`) e descrito no README.
- **Banco:** o período de teste do Aiven é de 30 dias a partir de 18/10/2026; depois dele o banco é desligado e as duas APIs
  ficam sem dados. Os testes do nível 1 continuam funcionando, porque não dependem dele.
- **Dados:** são todos de demonstração.
