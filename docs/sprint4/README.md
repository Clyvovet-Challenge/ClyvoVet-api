# Sprint 4 — Advanced Business Development with .NET

Consolidação de tudo das Sprints 1–3 num projeto completo, funcional e integrado.
Rubric de 100 pontos. **Este diretório é a fonte única de verdade do trabalho da Sprint.**

## Ordem de leitura

1. [`00-requisitos.md`](00-requisitos.md) — o rubric, literal do enunciado.
2. [`01-gap-analysis.md`](01-gap-analysis.md) — o que já atende e o que falta, com evidência.
3. [`02-design.md`](02-design.md) — o design aprovado (arquitetura, API, persistência).
4. [`03-plano.md`](03-plano.md) — as fases, na ordem em que serão feitas.
5. [`decisoes/`](decisoes/) — por que cada decisão foi tomada (ADRs).

## Estado

| Fase | Tema | Estado |
|---|---|---|
| F0 | Organização (`CLAUDE.md`, docs, ambiente) | ⏳ escrita, falta commit |
| F1 | Clean Architecture em 4 projetos | ⬜ |
| F2 | Exceções globais + JWT/`[Authorize]` | ⬜ |
| F3 | Ordenação, paginação com total e HATEOAS | ⬜ |
| F4 | MongoDB (cache do parecer de IA) | ⬜ |
| F5 | Observabilidade, testes e qualidade de código | ⬜ |
| F6 | README final, diagrama, Swagger exportado e ensaio geral | ⬜ |
| F7 | Migrations EF | ⛔ condicional — ADR-005 aguarda o professor |

Atualize esta tabela no mesmo commit que fecha cada fase.

## Como trabalhar aqui

- Uma fase por vez. Cada fase termina com build e testes verdes e **um commit por mudança
  lógica**, só depois do "sim" do dono do repositório.
- O plano-mestre (`03-plano.md`) dá as tarefas e os critérios de pronto de todas as fases.
  O **passo a passo detalhado** de cada fase (com código) é escrito quando a fase começa,
  em `docs/sprint4/fases/FN-*.md`: as fases seguintes dependem da forma que o código
  ganha nas anteriores, e detalhar tudo agora seria escrever suposição.
