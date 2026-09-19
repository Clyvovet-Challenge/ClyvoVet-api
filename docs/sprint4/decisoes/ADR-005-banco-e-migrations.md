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
| **MySQL aceito** | **F7-A.** Migration *baseline* só das tabelas `t_clyvo_*`; `animal` e `tutor` (da Java) ficam **fora** das migrations (`ExcludeFromMigrations`); script idempotente (`dotnet ef migrations script --idempotent`) como entregável; provar aplicando em banco **vazio**. No banco compartilhado a migration inicial é registrada como *já aplicada*, porque as tabelas existem por Flyway. Um teste detecta divergência entre modelo e migrations. | Pequeno |
| **SQL Server ou Oracle exigido** | **F7-B — vira redesenho, não fase.** O banco compartilhado com a Java deixa de servir: `animal`/`tutor` não estariam mais no mesmo banco. É preciso decidir como o .NET obtém esses dados (réplica, consumo HTTP da Java…). **Volta ao brainstorming, com spec própria, antes de qualquer código.** | Maior que todas as outras fases somadas |

Observação para a resposta "Oracle": a auditoria registra que a API Java já mantém
migrations para MySQL **e** Oracle (`db/migration/mysql` e `db/migration/oracle`), então o
schema das `t_clyvo_*` já existe em Oracle do lado Java.

## Consequências

- ➕ Nenhuma fase fica travada esperando.
- ➕ Evita executar F7 na direção errada (uma migration no MySQL "de graça" pode ser
  trabalho jogado fora se a exigência for outra).
- ➖ Enquanto o professor não responder, o item "EF Core com migrações" segue 🔴 no
  gap-analysis, e o risco de desconto parcial nesse item existe.

## Alternativas descartadas

- **Criar migrations no MySQL agora, por precaução.** Custa o problema das três fontes de
  verdade e pode não valer nada se a exigência for Oracle/SQL Server.
- **Trocar para SQL Server agora.** Quebra o banco compartilhado com a Java e o deploy, sem
  confirmar que a troca é necessária.
