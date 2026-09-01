-- ============================================================
-- CLYVO VET -- ORACLE DATABASE
-- Arquivo 06: DDL -- Tabela de vínculo Tutor <-> Telegram
-- (suporte ao envio de notificações via bot -- domínio API .NET)
-- ============================================================

BEGIN EXECUTE IMMEDIATE 'DROP TABLE t_clyvo_tutor_telegram CASCADE CONSTRAINTS'; EXCEPTION WHEN OTHERS THEN NULL; END;
/

-- ============================================================
-- TUTOR_TELEGRAM  (domínio API .NET)
-- Vincula um tutor (t_clyvo_tutor, tabela Java) ao chat_id que
-- o Telegram atribui quando ele inicia conversa com o bot via
-- deep link (t.me/<bot>?start=<tutor_id>). Não referencia
-- t_clyvo_tutor com FK -- mesma decisão de design já usada em
-- t_clyvo_lembrete/t_clyvo_sugestao_produto (tutor_id validado
-- via API, não via constraint de banco).
-- ============================================================
CREATE TABLE t_clyvo_tutor_telegram (
    id         VARCHAR2(36)  NOT NULL,
    tutor_id   VARCHAR2(36)  NOT NULL,
    chat_id    NUMBER(19)    NOT NULL,
    criado_em  TIMESTAMP     DEFAULT SYSTIMESTAMP,
    CONSTRAINT pk_clyvo_tutor_telegram        PRIMARY KEY (id),
    CONSTRAINT uq_clyvo_tutor_telegram_tutor  UNIQUE (tutor_id)
);

CREATE OR REPLACE TRIGGER trg_clyvo_tutor_telegram_id
BEFORE INSERT ON t_clyvo_tutor_telegram
FOR EACH ROW
BEGIN
    IF :NEW.id IS NULL THEN
        :NEW.id := fn_clyvo_uuid();
    END IF;
END;
/
