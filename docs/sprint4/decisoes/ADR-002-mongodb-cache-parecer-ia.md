# ADR-002 — MongoDB guarda o cache do parecer de IA

**Status:** aceita · **Data:** 19/09/2026 · **Fase:** F4

## Contexto

O rubric pede "MongoDB integrado para operações NoSQL" e não diz para quê. O domínio já tem
um candidato natural, o `ParecerIa` (`t_clyvo_parecer_ia`):

- é um **cache com endereço**: um parecer por animal, com validade (`ValidoAte`);
- o conteúdo (`ParecerConteudo`) é um **documento aninhado** que hoje viaja como JSON dentro
  de uma coluna de texto;
- já vive atrás de uma interface (`IParecerIaRepository`: `GetByAnimalIdAsync`,
  `SalvarAsync`), e só o `SaudePreditivaService` a usa.

## Decisão

Implementar `ParecerIaMongoRepository : IParecerIaRepository` (design §7.2): coleção
`pareceres_ia`, `_id = animalId`, conteúdo como subdocumento BSON, **índice TTL** em
`validoAte`. Interface, entidade e serviço **não mudam**.

O Mongo é escolhido **por configuração** (`Mongo:ConnectionString`); sem ela, continua o
repositório EF/MySQL atual. Falha do Mongo vira *cache miss*, nunca erro para o usuário.

## Consequências

- ➕ Sem migração de dados: é um cache regenerável.
- ➕ O motivo de usar NoSQL é real (documento aninhado, expiração nativa), e defensável na
  avaliação oral.
- ➕ Produção não corre risco: sem Mongo configurado, nada muda.
- ➖ Novo componente para operar; ligar em produção exige um Mongo hospedado (pendência
  **P3**, fora do escopo do código).
- ➖ Testes contra Mongo real precisam de um servidor; ficam condicionados a
  `MONGO_TEST_URI` e o mapeamento é testado sem servidor.
- ➖ A tabela `t_clyvo_parecer_ia` (Flyway V15 da Java) fica sem uso com o Mongo ligado.

## Alternativas descartadas

- **Histórico de notificações do Telegram.** Não mexe em nada existente, mas cria uma
  feature que o domínio não pediu só para cumprir um requisito.
- **Os dois.** Dobra trabalho e testes numa sprint já cheia, sem ganho proporcional.
