-- ============================================================
-- Baseline das migrations do EF no banco COMPARTILHADO com a API Java
--
-- PARA QUE SERVE
-- No banco de produção (e em qualquer banco montado pelo Flyway da Java), as
-- tabelas desta API já existem: o Flyway as cria desde a V8. Este arquivo só
-- registra em __EFMigrationsHistory que a migration Inicial já está aplicada.
-- Depois dele, o migrations-idempotente.sql não recria nada nesse banco.
--
-- ORDEM OBRIGATÓRIA: Flyway da Java primeiro, este arquivo depois.
-- Num banco novo, qualquer tabela criada antes do primeiro boot da Java deixa o
-- schema "não vazio" sem flyway_schema_history, e o Flyway recusa a V1: a API
-- Java não sobe. Pelo mesmo motivo, NUNCA rode o migrations-idempotente.sql num
-- banco compartilhado sem este baseline: ele tentaria criar tabelas que já
-- existem e pararia no primeiro CREATE TABLE (sem apagar nada).
--
-- Pode rodar mais de uma vez: CREATE TABLE IF NOT EXISTS e INSERT IGNORE.
-- A tabela de histórico é a mesma que o EF cria (copiada do script gerado).
-- ============================================================

CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `ProductVersion` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
) CHARACTER SET=utf8mb4;

INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260927054703_Inicial', '8.0.11');
