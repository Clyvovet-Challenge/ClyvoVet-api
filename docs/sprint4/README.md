# Sprint 4 — Advanced Business Development with .NET

Reúne tudo o que foi construído nas Sprints 1–3 num projeto único, completo e funcional.
Rubric de 100 pontos. **Este diretório é a fonte única de verdade sobre o trabalho da Sprint.**

## Ordem de leitura

1. [`00-requisitos.md`](00-requisitos.md) — o rubric, transcrito literalmente do enunciado.
2. [`01-gap-analysis.md`](01-gap-analysis.md) — o que já é atendido e o que falta, com evidências.
3. [`02-design.md`](02-design.md) — o design já aprovado (arquitetura, API, persistência).
4. [`03-plano.md`](03-plano.md) — as fases, na sequência em que serão executadas.
5. [`decisoes/`](decisoes/) — o porquê de cada decisão tomada (ADRs).

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
| F7 | Migrations EF (MySQL, só `t_clyvo_*`) | ⬜ liberada — o professor aceitou o MySQL (ADR-005) |

Mantenha esta tabela atualizada no mesmo commit que encerra cada fase.

## Como trabalhar aqui

- Uma fase de cada vez. Cada uma só termina com build e testes verdes, seguida de **um
  commit por mudança lógica** — e apenas depois do "sim" do dono do repositório.
- O plano-mestre (`03-plano.md`) já define as tarefas e os critérios de conclusão de todas
  as fases. O **passo a passo detalhado** de cada uma (com código) é escrito só quando ela
  começa, em `docs/sprint4/fases/FN-*.md`: como as fases seguintes dependem da forma que o
  código toma nas anteriores, detalhar tudo agora seria apenas presunção.
