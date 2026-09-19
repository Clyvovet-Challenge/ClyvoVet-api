# ADR-001 — Clean Architecture em 4 projetos reais

**Status:** aceita · **Data:** 19/09/2026 · **Fase:** F1

## Contexto

Hoje há **um projeto** (`ClyvoVet.Api`) com pastas por camada (`Models`, `Services`,
`Repositories`, `Controllers`…) e dois projetos de teste aninhados dentro dele. O rubric dá
30 pts a "Clean Architecture aplicada corretamente, com separação de camadas" e pede
cobertura mínima "das camadas de Domínio e Aplicação" — o que só é mensurável se essas
camadas existirem como unidades separadas.

Um fato favorável, verificado no código: os `Services` **não** dependem de EF, e os
repositórios **não** usam DTOs. A separação é, em sua maior parte, mover arquivos.

## Decisão

Quatro projetos em `src/` (`Domain`, `Application`, `Infrastructure`, `Api`) e os testes em
`tests/`, com dependências só para dentro (design §3). As regras são **verificadas por
teste** (`ArquiteturaTests`, design §4), não só por convenção.

## Consequências

- ➕ A separação é verificável: por `.csproj` e por teste, não por opinião do avaliador.
- ➕ "Cobertura de Domínio e Aplicação" passa a ser um número, não uma interpretação.
- ➕ `Program.cs` deixa de ter 411 linhas.
- ➖ `Dockerfile`, `.dockerignore`, `slnx` e todos os `namespace` mudam de uma vez; é a
  fase de maior risco (mitigada: passos pequenos, build e testes verdes em cada um).
- ➖ Referência de projeto não impede o uso transitivo de pacotes — por isso o teste de
  arquitetura é parte da decisão, não um extra.

## Alternativas descartadas

- **Um projeto, pastas reforçadas.** Menos risco, mas a separação só existe por convenção;
  o avaliador pode concluir que não há camadas.
- **Três projetos (Core, Infrastructure, Api).** Mistura entidades e casos de uso, e o
  rubric cita Domínio e Aplicação separadamente.
