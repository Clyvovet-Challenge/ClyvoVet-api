-- ============================================================
-- CLYVO VET -- ORACLE DATABASE
-- Arquivo 04: DDL -- Tabela de Predisposições de Saúde
-- (suporte ao Widget de Saúde Preditiva -- domínio API .NET)
-- ============================================================

BEGIN EXECUTE IMMEDIATE 'DROP TABLE t_clyvo_predisposicao_saude CASCADE CONSTRAINTS'; EXCEPTION WHEN OTHERS THEN NULL; END;
/

-- ============================================================
-- PREDISPOSICAO_SAUDE  (domínio API .NET)
-- Tabela de referência: doenças/condições mais comuns por
-- espécie (e opcionalmente raça) e a partir de que idade elas
-- costumam se manifestar. Não tem FK com Animal -- é um
-- catálogo consultado pelo Service, não vinculado a um
-- animal específico.
-- ============================================================
CREATE TABLE t_clyvo_predisposicao_saude (
    id                 VARCHAR2(36)   NOT NULL,
    especie            VARCHAR2(20)   NOT NULL,
    raca               VARCHAR2(100),
    idade_minima_anos  NUMBER(4,1),
    doenca             VARCHAR2(200)  NOT NULL,
    recomendacao       VARCHAR2(1000) NOT NULL,
    fonte_referencia   VARCHAR2(300),
    criado_em          TIMESTAMP      DEFAULT SYSTIMESTAMP,
    CONSTRAINT pk_predisposicao_saude    PRIMARY KEY (id),
    CONSTRAINT chk_predisposicao_especie CHECK (especie IN ('CACHORRO','GATO','PASSARO','REPTIL','ROEDOR','BOVINO','EQUINO'))
);

CREATE OR REPLACE TRIGGER trg_predisposicao_saude_id
BEFORE INSERT ON t_clyvo_predisposicao_saude
FOR EACH ROW
BEGIN
    IF :NEW.id IS NULL THEN
        :NEW.id := fn_uuid();
    END IF;
END;
/
