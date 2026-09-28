# Documentação técnica

O [README da raiz](../README.md) trata do uso da API: arquitetura, endpoints, execução local
e testes. Aqui fica o que não tem lugar lá, incluindo o deploy na Azure da Sprint 3.

| Documento | Conteúdo |
|---|---|
| [plano-de-entrega-sprint3.md](plano-de-entrega-sprint3.md) | **O plano da entrega de 12/09.** Por que esta API já atende ~90 dos 100 pontos da disciplina dela, o que ainda falta, e o papel dela no deploy |
| [auditoria-de-arquitetura.md](auditoria-de-arquitetura.md) | Auditoria de 06/09/2026 (registro histórico; o estado atual está no README). Por que o banco é compartilhado com a API Java, o que é de responsabilidade desta API, e a lista do que precisa ser corrigido — com arquivo e linha para cada achado |
| [arquitetura-azure.svg](arquitetura-azure.svg) | Diagrama de implantação exigido pela disciplina de DevOps |
| [deploy-azure.md](deploy-azure.md) | Passo a passo do deploy na Azure (App Service + MySQL gerenciado) e endereços de produção |
| [guia-de-testes-manuais.md](guia-de-testes-manuais.md) | Os 54 testes manuais pelo Swagger |
| [ensaio-geral.md](ensaio-geral.md) | Ensaio geral da Sprint 4: a API contra MySQL + MongoDB reais, 79 checagens ponta a ponta, o que ficou de fora e o que o ensaio encontrou |
| [swagger/openapi-v1.json](swagger/openapi-v1.json) | Documento OpenAPI exportado da API (`scripts/exportar-swagger.sh`) |

---

## Onde estão as outras peças

Este repositório é um dos três do ClyvoVet, e as specs se referenciam mutuamente:

| Repositório | Documento |
|---|---|
| `clyvovet-backend-java` | `docs/11-auditoria-de-arquitetura.md` — o recorte da API Java |
| `2tdspw-challenge-clyvovet-challenge` | `spec/12-auditoria-de-arquitetura.md` — o recorte do app móvel |

O relatório completo — as 23 seções, a comparação entre as arquiteturas A/B/C, a matriz de
riscos e o checklist — está disponível em
[claude.ai/code/artifact/79da68b6-2d7d-41b1-82c2-aac5fc68652a](https://claude.ai/code/artifact/79da68b6-2d7d-41b1-82c2-aac5fc68652a).

---

## O schema é definido pela API Java

Vale repetir este ponto fora da auditoria, porque ele afeta o dia a dia: desde a migration
`V8__tabelas_dotnet.sql` do repositório Java, **as seis tabelas `t_clyvo_*` passaram a ser
criadas pelo Flyway de lá**, e não por SQL avulso deste repositório.

Esta API segue dona do conteúdo dessas tabelas — o que mudou foi apenas onde a definição
delas vive. O motivo é concreto: no deploy, só o Flyway é executado, então qualquer tabela
ausente dele simplesmente não existiria na nuvem.

Na prática: **acrescentar uma coluna ou tabela exige uma migration no repositório Java**,
não uma alteração em `schema/script_bd.sql`.
