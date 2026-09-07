# Documentação técnica

O [README da raiz](../README.md) cobre o uso da API: endpoints, execução local e o
passo a passo do deploy na Azure. Esta pasta guarda o que não cabe lá.

| Documento | Conteúdo |
|---|---|
| [plano-de-entrega-sprint3.md](plano-de-entrega-sprint3.md) | **O plano da entrega de 12/09.** Por que esta API já atende ~90 dos 100 pontos da disciplina dela, o que falta, e o papel dela no deploy |
| [auditoria-de-arquitetura.md](auditoria-de-arquitetura.md) | Auditoria de 06/09/2026. Por que o banco é compartilhado com a API Java, o que esta API é dona, e a lista do que ela precisa corrigir — com arquivo e linha para cada achado |
| [arquitetura-azure.svg](arquitetura-azure.svg) | Diagrama de implantação exigido pela disciplina de DevOps |

---

## Onde estão as outras peças

Este é um dos três repositórios do ClyvoVet. As specs se referenciam entre si:

| Repositório | Documento |
|---|---|
| `clyvovet-backend-java` | `docs/11-auditoria-de-arquitetura.md` — o recorte da API Java |
| `2tdspw-challenge-clyvovet-challenge` | `spec/12-auditoria-de-arquitetura.md` — o recorte do app móvel |

O relatório completo, com as 23 seções, a comparação das arquiteturas A/B/C, a
matriz de riscos e o checklist, está em
[claude.ai/code/artifact/79da68b6-2d7d-41b1-82c2-aac5fc68652a](https://claude.ai/code/artifact/79da68b6-2d7d-41b1-82c2-aac5fc68652a).

---

## O schema é definido pela API Java

Ponto que vale repetir fora da auditoria, porque muda o dia a dia: desde a
migration `V8__tabelas_dotnet.sql` do repositório Java, **as seis tabelas
`t_clyvo_*` são criadas pelo Flyway de lá**, não por SQL avulso daqui.

Esta API continua dona do conteúdo delas. O que mudou é onde a definição vive — e
o motivo foi concreto: no deploy só o Flyway roda, então tabela que não estivesse
nele simplesmente não existiria na nuvem.

Consequência prática: **para acrescentar coluna ou tabela, a alteração vai numa
migration do repositório Java**, não no `schema/script_bd.sql`.
