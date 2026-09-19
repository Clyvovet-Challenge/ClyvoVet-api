# ADR-002 — MongoDB guarda o cache do parecer de IA

**Status:** aceita · **Data:** 19/09/2026 · **Fase:** F4

## Contexto

O rubric exige "MongoDB integrado para operações NoSQL", sem especificar para qual
finalidade. O domínio já oferece um candidato natural: o `ParecerIa` (`t_clyvo_parecer_ia`).

- funciona como **cache endereçável**: um parecer por animal, com validade (`ValidoAte`);
- seu conteúdo (`ParecerConteudo`) é um **documento aninhado**, hoje transportado como JSON
  dentro de uma coluna de texto;
- já é acessado por trás de uma interface (`IParecerIaRepository`: `GetByAnimalIdAsync`,
  `SalvarAsync`), usada apenas pelo `SaudePreditivaService`.

## Decisão

Implementar `ParecerIaMongoRepository : IParecerIaRepository` (design §7.2): coleção
`pareceres_ia`, `_id = animalId`, conteúdo em subdocumento BSON e **índice TTL** sobre
`validoAte`. A interface, a entidade e o serviço **permanecem inalterados**.

A escolha do Mongo acontece **por configuração** (`Mongo:ConnectionString`); na ausência
dela, o repositório EF/MySQL atual segue em uso. Uma falha no Mongo se traduz em *cache
miss*, nunca em erro exposto ao usuário.

## Consequências

- ➕ Não há migração de dados: trata-se de um cache regenerável.
- ➕ A justificativa para o NoSQL é concreta (documento aninhado, expiração nativa) e
  sustentável numa avaliação oral.
- ➕ A produção não corre risco: sem Mongo configurado, nada se altera.
- ➖ Surge um componente novo a operar; colocá-lo em produção exige um Mongo hospedado
  (pendência **P3**, fora do escopo do código).
- ➖ Testes contra um Mongo real dependem de um servidor disponível, condicionados a
  `MONGO_TEST_URI`; o mapeamento em si é testado sem servidor.
- ➖ Com o Mongo em uso, a tabela `t_clyvo_parecer_ia` (Flyway V15 da Java) deixa de ser
  utilizada.

## Alternativas descartadas

- **Histórico de notificações do Telegram.** Preserva tudo o que já existe, mas cria uma
  funcionalidade que o domínio não pediu, só para satisfazer um requisito.
- **Fazer os dois.** Duplicaria trabalho e testes numa sprint já carregada, sem ganho
  proporcional.
