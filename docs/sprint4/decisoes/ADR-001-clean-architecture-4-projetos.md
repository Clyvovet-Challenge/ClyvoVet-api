# ADR-001 — Clean Architecture em 4 projetos reais

**Status:** aceita · **Data:** 19/09/2026 · **Fase:** F1

## Contexto

Atualmente existe **um único projeto** (`ClyvoVet.Api`), organizado em pastas por camada
(`Models`, `Services`, `Repositories`, `Controllers`…), com dois projetos de teste aninhados
dentro dele. O rubric atribui 30 pts a "Clean Architecture aplicada corretamente, com
separação de camadas" e exige cobertura mínima "das camadas de Domínio e Aplicação" —
algo que só pode ser medido se essas camadas existirem como unidades de fato separadas.

Um ponto a favor, confirmado no código: os `Services` **não** dependem de EF e os
repositórios **não** usam DTOs. Boa parte da separação, portanto, se resume a mover arquivos.

## Decisão

Adotar quatro projetos dentro de `src/` (`Domain`, `Application`, `Infrastructure`, `Api`) e
mover os testes para `tests/`, com dependências apontando sempre para dentro (design §3).
As regras passam a ser **verificadas por teste** (`ArquiteturaTests`, design §4), e não
apenas seguidas por convenção.

## Consequências

- ➕ A separação vira algo verificável, por `.csproj` e por teste — não uma questão de
  opinião do avaliador.
- ➕ "Cobertura de Domínio e Aplicação" deixa de ser interpretação e passa a ser um número.
- ➕ `Program.cs` deixa de ter 411 linhas.
- ➖ `Dockerfile`, `.dockerignore`, `slnx` e todos os `namespace` mudam de uma só vez; é a
  fase de maior risco (mitigada com passos pequenos e build/testes verdes a cada um).
- ➖ A referência entre projetos não bloqueia o uso transitivo de pacotes — por isso o teste
  de arquitetura é parte da decisão, e não um extra opcional.

## Alternativas descartadas

- **Um projeto só, com pastas reforçadas.** Reduz o risco, mas a separação continuaria
  existindo apenas por convenção; o avaliador poderia concluir que não há camadas reais.
- **Três projetos (Core, Infrastructure, Api).** Mistura entidades e casos de uso num só
  lugar, e o rubric cita Domínio e Aplicação como itens separados.
