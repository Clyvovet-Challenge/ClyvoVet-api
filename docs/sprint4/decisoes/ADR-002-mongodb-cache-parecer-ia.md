# ADR-002 — MongoDB guarda o cache do parecer de IA

**Status:** aceita · **Data:** 19/09/2026 · **Fase:** F4

## Contexto

O rubric pede "MongoDB integrado para operações NoSQL" sem dizer para qual finalidade. O
domínio já tem um candidato natural: o `ParecerIa` (`t_clyvo_parecer_ia`).

- ele se comporta como **cache endereçável** — um parecer por animal, com validade
  (`ValidoAte`);
- o conteúdo (`ParecerConteudo`) é um **documento aninhado**, hoje guardado como JSON dentro
  de uma coluna de texto;
- o acesso já passa por uma interface (`IParecerIaRepository`: `GetByAnimalIdAsync`,
  `SalvarAsync`), usada só pelo `SaudePreditivaService`.

## Decisão

Criar `ParecerIaMongoRepository : IParecerIaRepository` (design §7.2): coleção
`pareceres_ia`, `_id = animalId`, conteúdo em subdocumento BSON e **índice TTL** sobre
`validoAte`. Interface, entidade e serviço **não mudam**.

O Mongo entra **por configuração** (`Mongo:ConnectionString`); se ela não existir, continua
valendo o repositório EF/MySQL de hoje. Uma falha do Mongo vira *cache miss* — nunca um erro
que chegue ao usuário.

## Consequências

- ➕ Não existe migração de dados a fazer: é um cache que se regenera sozinho.
- ➕ A justificativa do NoSQL é concreta — documento aninhado, expiração nativa — e se
  sustenta numa arguição oral.
- ➕ Produção não corre risco: sem Mongo configurado, nada muda.
- ➖ Aparece um componente novo para operar; colocá-lo em produção exige um Mongo hospedado
  (pendência **P3**, fora do escopo do código).
- ➖ Teste contra Mongo real depende de servidor disponível, atrás de `MONGO_TEST_URI`; o
  mapeamento em si é testável sem servidor.
- ➖ Uma vez o Mongo em uso, a tabela `t_clyvo_parecer_ia` (Flyway V15 da Java) fica sem uso.

## Alternativas descartadas

- **Histórico de notificações do Telegram.** Mantém tudo que já existe, mas cria uma
  funcionalidade que o domínio não pediu, só para cumprir requisito.
- **Fazer os dois.** Dobraria trabalho e testes numa sprint já cheia, sem ganho proporcional.
