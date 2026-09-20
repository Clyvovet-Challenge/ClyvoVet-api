# ADR-001 — Clean Architecture em 4 projetos reais

**Status:** aceita · **Data:** 19/09/2026 · **Fase:** F1

## Contexto

Hoje tudo mora em **um projeto único** (`ClyvoVet.Api`), com camadas separadas apenas por
pasta (`Models`, `Services`, `Repositories`, `Controllers`…) e dois projetos de teste
aninhados dentro dele. O rubric dá 30 pts para "Clean Architecture aplicada corretamente,
com separação de camadas" e cobra cobertura mínima "das camadas de Domínio e Aplicação" —
item que só dá para medir se essas camadas forem unidades separadas de verdade, não pastas.

A favor, e já confirmado lendo o código: nem os `Services` dependem de EF, nem os
repositórios usam DTOs. Isso significa que boa parte da separação é, na prática, mover
arquivo de lugar.

## Decisão

Passar a quatro projetos dentro de `src/` (`Domain`, `Application`, `Infrastructure`, `Api`),
com os testes indo para `tests/` e as dependências sempre apontando para dentro (design §3).
As regras deixam de valer por convenção e passam a ser **checadas por teste**
(`ArquiteturaTests`, design §4).

## Consequências

- ➕ A separação passa a ser verificável — por `.csproj` e por teste — em vez de depender da
  opinião de quem avalia.
- ➕ "Cobertura de Domínio e Aplicação" vira número, não interpretação.
- ➕ `Program.cs` sai das 411 linhas atuais.
- ➖ `Dockerfile`, `.dockerignore`, o `slnx` e todo `namespace` mudam juntos, de uma vez — é o
  trecho de maior risco do plano (mitigado com passos pequenos e build/testes verdes a cada
  etapa).
- ➖ A referência entre projetos, sozinha, não impede o uso transitivo de pacotes; por isso o
  teste de arquitetura entra como parte da decisão, não como extra opcional.

## Alternativas descartadas

- **Manter um projeto só, com pastas mais rígidas.** Risco menor, mas a separação seguiria
  existindo só por convenção — quem avaliar pode concluir que não há camadas de fato.
- **Três projetos (Core, Infrastructure, Api).** Junta entidades e casos de uso num único
  lugar, e o rubric trata Domínio e Aplicação como itens distintos.
