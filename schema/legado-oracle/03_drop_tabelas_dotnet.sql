-- =============================================================
-- ClyvoVet — schema/03_drop_tabelas_dotnet.sql
-- Remove APENAS as 6 tabelas gerenciadas pela API .NET (C#).
-- As tabelas da API Java (t_clyvo_animal, t_clyvo_tutor,
-- t_clyvo_veterinario, t_clyvo_clinica, t_clyvo_evento_clinico,
-- t_clyvo_consulta) NÃO são tocadas por este script.
-- =============================================================
-- Pré-requisito: conectado ao schema correto no Oracle SQL Developer
-- Execução: abra este arquivo e pressione F5 (Run Script)
-- =============================================================

SET SERVEROUTPUT ON;

PROMPT ============================================================
PROMPT Removendo tabelas da API .NET (ClyvoVet C#)...
PROMPT ============================================================

-- -------------------------------------------------------------
-- 1. T_CLYVO_SUGESTAO_PRODUTO
--    Removida primeiro pois possui FKs para t_clyvo_produto e
--    t_clyvo_animal (tabela Java).
-- -------------------------------------------------------------
BEGIN
    EXECUTE IMMEDIATE 'DROP TABLE T_CLYVO_SUGESTAO_PRODUTO CASCADE CONSTRAINTS PURGE';
    DBMS_OUTPUT.PUT_LINE('[OK] T_CLYVO_SUGESTAO_PRODUTO removida.');
EXCEPTION
    WHEN OTHERS THEN
        IF SQLCODE = -942 THEN
            DBMS_OUTPUT.PUT_LINE('[SKIP] T_CLYVO_SUGESTAO_PRODUTO nao encontrada, ignorando.');
        ELSE
            RAISE;
        END IF;
END;
/

-- -------------------------------------------------------------
-- 2. T_CLYVO_LEMBRETE
--    Possui FK para t_clyvo_animal (tabela Java).
-- -------------------------------------------------------------
BEGIN
    EXECUTE IMMEDIATE 'DROP TABLE T_CLYVO_LEMBRETE CASCADE CONSTRAINTS PURGE';
    DBMS_OUTPUT.PUT_LINE('[OK] T_CLYVO_LEMBRETE removida.');
EXCEPTION
    WHEN OTHERS THEN
        IF SQLCODE = -942 THEN
            DBMS_OUTPUT.PUT_LINE('[SKIP] T_CLYVO_LEMBRETE nao encontrada, ignorando.');
        ELSE
            RAISE;
        END IF;
END;
/

-- -------------------------------------------------------------
-- 3. T_CLYVO_PRODUTO
--    Sem FKs para tabelas externas. Sugestoes ja foram removidas.
-- -------------------------------------------------------------
BEGIN
    EXECUTE IMMEDIATE 'DROP TABLE T_CLYVO_PRODUTO CASCADE CONSTRAINTS PURGE';
    DBMS_OUTPUT.PUT_LINE('[OK] T_CLYVO_PRODUTO removida.');
EXCEPTION
    WHEN OTHERS THEN
        IF SQLCODE = -942 THEN
            DBMS_OUTPUT.PUT_LINE('[SKIP] T_CLYVO_PRODUTO nao encontrada, ignorando.');
        ELSE
            RAISE;
        END IF;
END;
/

-- -------------------------------------------------------------
-- 4. T_CLYVO_EVENTO_PET
--    Sem FKs para outras tabelas.
-- -------------------------------------------------------------
BEGIN
    EXECUTE IMMEDIATE 'DROP TABLE T_CLYVO_EVENTO_PET CASCADE CONSTRAINTS PURGE';
    DBMS_OUTPUT.PUT_LINE('[OK] T_CLYVO_EVENTO_PET removida.');
EXCEPTION
    WHEN OTHERS THEN
        IF SQLCODE = -942 THEN
            DBMS_OUTPUT.PUT_LINE('[SKIP] T_CLYVO_EVENTO_PET nao encontrada, ignorando.');
        ELSE
            RAISE;
        END IF;
END;
/

-- -------------------------------------------------------------
-- 5. T_CLYVO_PREDISPOSICAO_SAUDE
--    Sem FKs — tabela de catalogo (Widget de Saude Preditiva).
-- -------------------------------------------------------------
BEGIN
    EXECUTE IMMEDIATE 'DROP TABLE T_CLYVO_PREDISPOSICAO_SAUDE CASCADE CONSTRAINTS PURGE';
    DBMS_OUTPUT.PUT_LINE('[OK] T_CLYVO_PREDISPOSICAO_SAUDE removida.');
EXCEPTION
    WHEN OTHERS THEN
        IF SQLCODE = -942 THEN
            DBMS_OUTPUT.PUT_LINE('[SKIP] T_CLYVO_PREDISPOSICAO_SAUDE nao encontrada, ignorando.');
        ELSE
            RAISE;
        END IF;
END;
/

-- -------------------------------------------------------------
-- 6. T_CLYVO_TUTOR_TELEGRAM
--    Sem FK — vínculo tutor_id (validado via API) -> chat_id.
-- -------------------------------------------------------------
BEGIN
    EXECUTE IMMEDIATE 'DROP TABLE T_CLYVO_TUTOR_TELEGRAM CASCADE CONSTRAINTS PURGE';
    DBMS_OUTPUT.PUT_LINE('[OK] T_CLYVO_TUTOR_TELEGRAM removida.');
EXCEPTION
    WHEN OTHERS THEN
        IF SQLCODE = -942 THEN
            DBMS_OUTPUT.PUT_LINE('[SKIP] T_CLYVO_TUTOR_TELEGRAM nao encontrada, ignorando.');
        ELSE
            RAISE;
        END IF;
END;
/

-- -------------------------------------------------------------
-- Verificação final: confirma que as tabelas foram removidas
-- -------------------------------------------------------------
PROMPT ============================================================
PROMPT Verificando remocao...
PROMPT ============================================================

SELECT table_name
FROM   user_tables
WHERE  table_name IN (
           'T_CLYVO_PRODUTO',
           'T_CLYVO_SUGESTAO_PRODUTO',
           'T_CLYVO_LEMBRETE',
           'T_CLYVO_EVENTO_PET',
           'T_CLYVO_PREDISPOSICAO_SAUDE',
           'T_CLYVO_TUTOR_TELEGRAM'
       )
ORDER BY table_name;

-- Se a query acima retornar 0 linhas, todas as tabelas foram removidas.

PROMPT ============================================================
PROMPT Concluido. Tabelas Java preservadas.
PROMPT ============================================================
