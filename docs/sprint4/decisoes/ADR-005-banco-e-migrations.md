# ADR-005 — Banco de dados e migrations do EF Core

**Status:** ⏳ **aguardando o professor** · **Data:** 19/09/2026 · **Fase:** F7 (condicional)

## Contexto

O rubric diz: *"Entity Framework Core com migrações aplicadas (**Oracle ou SQL Server**)"*.
O estado atual conflita com isso em dois pontos:

- o provider é **MySQL** (`Pomelo.EntityFrameworkCore.MySql 8.0.2`), num banco **compartilhado
  com a API Java**, que é dona do schema (Flyway);
- **não há migrations**, por decisão da auditoria de 06/09 (§6): criá-las introduziria uma
  terceira fonte de verdade para o mesmo schema (Flyway da Java, `schema/script_bd.sql` e o
  EF), o problema que a V8 da Java acabara de resolver.

O rubric não divide os 20 pts do bloco por item; "EF Core com migrações" é um de três itens.
Não sabemos, sem perguntar, se o MySQL é aceito.

## Decisão

**Adiar.** Nenhuma linha de código depende desta resposta: as fases F1–F6 seguem normalmente
e a F7 fica reservada. Até a resposta, **não criar migrations** (regra registrada no
`CLAUDE.md`).

### Pergunta para o professor (texto pronto)

> Professor, o projeto .NET do nosso grupo usa **EF Core com MySQL**, num banco compartilhado
> com a API Java do mesmo produto (a Java é dona do schema, via Flyway). O critério da Sprint 4
> fala em *"EF Core com migrações aplicadas (Oracle ou SQL Server)"*.
> **(1)** MySQL é aceito para esse item, desde que haja migrations do EF? 
> **(2)** Se não, o senhor exige Oracle ou SQL Server? Nesse caso, o schema das tabelas da
> parte .NET precisaria ficar num banco próprio, separado do da API Java.

### O que fazer conforme a resposta

| Resposta | O que acontece | Esforço |
|---|---|---|
| **MySQL aceito** | **F7-A.** Uma migration *baseline* apenas das tabelas `t_clyvo_*`; `animal` e `tutor` (pertencentes à Java) ficam **fora** das migrations (`ExcludeFromMigrations`); um script idempotente (`dotnet ef migrations script --idempotent`) vira entregável, comprovado ao ser aplicado num banco **vazio**. No banco compartilhado, a migration inicial é registrada como *já aplicada*, já que as tabelas já existem via Flyway. Um teste passa a detectar qualquer divergência entre modelo e migrations. | Pequeno |
| **SQL Server ou Oracle exigido** | **F7-B — deixa de ser fase e vira redesenho.** O banco compartilhado com a Java deixa de fazer sentido: `animal`/`tutor` não estariam mais no mesmo banco. Seria preciso decidir como o .NET passaria a obter esses dados (réplica, consumo via HTTP da Java…). **Volta para o brainstorming, com spec própria, antes de qualquer linha de código.** | Maior que todas as outras fases somadas |

Uma observação para o caso de resposta "Oracle": a auditoria já registra que a API Java
mantém migrations tanto para MySQL **quanto** para Oracle (`db/migration/mysql` e
`db/migration/oracle`), então o schema das `t_clyvo_*` já existe em Oracle do lado da Java.

## Consequências

- ➕ Nenhuma fase fica bloqueada à espera da resposta.
- ➕ Evita conduzir a F7 na direção errada (uma migration "de graça" no MySQL pode virar
  trabalho descartado se a exigência acabar sendo outra).
- ➖ Enquanto o professor não responde, o item "EF Core com migrações" permanece 🔴 no
  gap-analysis, mantendo o risco de desconto parcial nesse item.

## Alternativas descartadas

- **Criar migrations no MySQL desde já, por precaução.** Traz de volta o problema das três
  fontes de verdade e pode não valer nada caso a exigência acabe sendo Oracle/SQL Server.
- **Trocar para SQL Server agora.** Quebraria o banco compartilhado com a Java e o deploy,
  sem confirmação de que a troca é sequer necessária.
