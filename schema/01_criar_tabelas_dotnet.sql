-- =============================================================
-- ClyvoVet — schema/01_criar_tabelas_dotnet.sql
-- DDL completo das 4 tabelas gerenciadas pela API .NET (C#)
--
-- PRÉ-REQUISITO: execute o script da API Java primeiro para que
-- a tabela t_clyvo_animal exista antes das constraints de FK.
--
-- Como rodar: abra no Oracle SQL Developer e pressione F5
-- =============================================================

SET SERVEROUTPUT ON;

PROMPT ============================================================
PROMPT [1/5] Criando funcao fn_uuid...
PROMPT ============================================================

-- Gera UUID v4-compatível no formato 8-4-4-4-12 usando SYS_GUID()
CREATE OR REPLACE FUNCTION fn_uuid RETURN VARCHAR2 IS
BEGIN
    RETURN LOWER(
        REGEXP_REPLACE(
            RAWTOHEX(SYS_GUID()),
            '([A-F0-9]{8})([A-F0-9]{4})([A-F0-9]{4})([A-F0-9]{4})([A-F0-9]{12})',
            '\1-\2-\3-\4-\5'
        )
    );
END fn_uuid;
/

PROMPT [OK] fn_uuid criada.

-- =============================================================
PROMPT ============================================================
PROMPT [2/5] Removendo tabelas antigas (se existirem)...
PROMPT ============================================================
-- =============================================================
-- Drops na ordem inversa das FKs para evitar violações

BEGIN
    EXECUTE IMMEDIATE 'DROP TABLE t_clyvo_sugestao_produto CASCADE CONSTRAINTS';
    DBMS_OUTPUT.PUT_LINE('[OK] t_clyvo_sugestao_produto removida.');
EXCEPTION
    WHEN OTHERS THEN
        DBMS_OUTPUT.PUT_LINE('[SKIP] t_clyvo_sugestao_produto nao existia.');
END;
/

BEGIN
    EXECUTE IMMEDIATE 'DROP TABLE t_clyvo_lembrete CASCADE CONSTRAINTS';
    DBMS_OUTPUT.PUT_LINE('[OK] t_clyvo_lembrete removida.');
EXCEPTION
    WHEN OTHERS THEN
        DBMS_OUTPUT.PUT_LINE('[SKIP] t_clyvo_lembrete nao existia.');
END;
/

BEGIN
    EXECUTE IMMEDIATE 'DROP TABLE t_clyvo_evento_pet CASCADE CONSTRAINTS';
    DBMS_OUTPUT.PUT_LINE('[OK] t_clyvo_evento_pet removida.');
EXCEPTION
    WHEN OTHERS THEN
        DBMS_OUTPUT.PUT_LINE('[SKIP] t_clyvo_evento_pet nao existia.');
END;
/

BEGIN
    EXECUTE IMMEDIATE 'DROP TABLE t_clyvo_produto CASCADE CONSTRAINTS';
    DBMS_OUTPUT.PUT_LINE('[OK] t_clyvo_produto removida.');
EXCEPTION
    WHEN OTHERS THEN
        DBMS_OUTPUT.PUT_LINE('[SKIP] t_clyvo_produto nao existia.');
END;
/

-- =============================================================
PROMPT ============================================================
PROMPT [3/5] Criando tabelas...
PROMPT ============================================================
-- =============================================================

-- -------------------------------------------------------------
-- 1. t_clyvo_produto
--    Catálogo de produtos e serviços para pets.
--    Sem dependências de outras tabelas .NET ou Java.
-- -------------------------------------------------------------
CREATE TABLE t_clyvo_produto (
    id               VARCHAR2(36)    NOT NULL,
    nome             VARCHAR2(200)   NOT NULL,
    descricao        VARCHAR2(1000),
    categoria        VARCHAR2(30),                        -- RACAO | MEDICAMENTO | ACESSORIO | SERVICO | OUTRO
    preco            NUMBER(10, 2),
    especie_indicada VARCHAR2(30),                        -- CACHORRO | GATO | PASSARO | REPTIL | ROEDOR | TODOS | OUTRO
    ativo            NUMBER(1)       DEFAULT 1  NOT NULL, -- 1=ativo, 0=inativo
    criado_em        TIMESTAMP       DEFAULT SYSTIMESTAMP NOT NULL,
    --
    CONSTRAINT pk_produto          PRIMARY KEY (id),
    CONSTRAINT ck_produto_ativo    CHECK (ativo IN (0, 1)),
    CONSTRAINT ck_produto_preco    CHECK (preco IS NULL OR preco >= 0),
    CONSTRAINT ck_produto_categ    CHECK (categoria IS NULL OR categoria IN
                                         ('RACAO','MEDICAMENTO','ACESSORIO','SERVICO','OUTRO')),
    CONSTRAINT ck_produto_especie  CHECK (especie_indicada IS NULL OR especie_indicada IN
                                         ('CACHORRO','GATO','PASSARO','REPTIL','ROEDOR','TODOS','OUTRO'))
);

COMMENT ON TABLE  t_clyvo_produto               IS 'Catalogo de produtos e servicos para pets — API .NET';
COMMENT ON COLUMN t_clyvo_produto.id            IS 'UUID gerado pela API .NET (Guid.NewGuid)';
COMMENT ON COLUMN t_clyvo_produto.categoria     IS 'RACAO | MEDICAMENTO | ACESSORIO | SERVICO | OUTRO';
COMMENT ON COLUMN t_clyvo_produto.especie_indicada IS 'CACHORRO | GATO | PASSARO | REPTIL | ROEDOR | TODOS | OUTRO';
COMMENT ON COLUMN t_clyvo_produto.ativo         IS '1 = ativo, 0 = inativo (soft-delete)';

-- -------------------------------------------------------------
-- 2. t_clyvo_evento_pet
--    Eventos públicos para pets (feiras, vacinações, etc.).
--    Sem dependências de FK — pode ser criada antes das Java.
-- -------------------------------------------------------------
CREATE TABLE t_clyvo_evento_pet (
    id             VARCHAR2(36)    NOT NULL,
    titulo         VARCHAR2(300)   NOT NULL,
    descricao      VARCHAR2(2000),
    tipo           VARCHAR2(30),                         -- VACINACAO | FEIRA | CASTRACAO | WORKSHOP | OUTRO
    rua            VARCHAR2(300),
    numero         VARCHAR2(20),
    bairro         VARCHAR2(200),
    cidade         VARCHAR2(200),
    estado         VARCHAR2(2),
    cep            VARCHAR2(9),
    data_inicio    DATE            NOT NULL,
    data_fim       DATE,
    especie_alvo   VARCHAR2(30),                         -- mesmos valores de EspecieEnum
    organizador    VARCHAR2(300),
    gratuito       NUMBER(1)       DEFAULT 1  NOT NULL,  -- 1=gratuito, 0=pago
    link_inscricao VARCHAR2(500),
    ativo          NUMBER(1)       DEFAULT 1  NOT NULL,
    criado_em      TIMESTAMP       DEFAULT SYSTIMESTAMP NOT NULL,
    --
    CONSTRAINT pk_evento_pet        PRIMARY KEY (id),
    CONSTRAINT ck_evento_gratuito   CHECK (gratuito IN (0, 1)),
    CONSTRAINT ck_evento_ativo      CHECK (ativo IN (0, 1)),
    CONSTRAINT ck_evento_datas      CHECK (data_fim IS NULL OR data_fim >= data_inicio),
    CONSTRAINT ck_evento_tipo       CHECK (tipo IS NULL OR tipo IN
                                          ('VACINACAO','FEIRA','CASTRACAO','WORKSHOP','OUTRO')),
    CONSTRAINT ck_evento_especie    CHECK (especie_alvo IS NULL OR especie_alvo IN
                                          ('CACHORRO','GATO','PASSARO','REPTIL','ROEDOR','TODOS','OUTRO'))
);

COMMENT ON TABLE  t_clyvo_evento_pet              IS 'Eventos publicos para pets — API .NET';
COMMENT ON COLUMN t_clyvo_evento_pet.tipo         IS 'VACINACAO | FEIRA | CASTRACAO | WORKSHOP | OUTRO';
COMMENT ON COLUMN t_clyvo_evento_pet.especie_alvo IS 'Especie-alvo do evento (mesmos valores de EspecieEnum)';
COMMENT ON COLUMN t_clyvo_evento_pet.gratuito     IS '1 = gratuito, 0 = pago';

-- -------------------------------------------------------------
-- 3. t_clyvo_lembrete
--    Lembretes de cuidados vinculados a um animal.
--    FK para t_clyvo_animal (tabela criada pela API Java).
-- -------------------------------------------------------------
CREATE TABLE t_clyvo_lembrete (
    id          VARCHAR2(36)    NOT NULL,
    animal_id   VARCHAR2(36)    NOT NULL,                -- FK → t_clyvo_animal.id (API Java)
    titulo      VARCHAR2(200)   NOT NULL,
    descricao   VARCHAR2(1000),
    tipo        VARCHAR2(30),                            -- VACINA | MEDICAMENTO | CONSULTA | HIGIENE | OUTRO
    agendado_em TIMESTAMP       NOT NULL,
    recorrente  NUMBER(1)       DEFAULT 0  NOT NULL,     -- 1=recorrente, 0=pontual
    status      VARCHAR2(30)    DEFAULT 'PENDENTE',      -- PENDENTE | ENVIADO | CANCELADO
    criado_em   TIMESTAMP       DEFAULT SYSTIMESTAMP NOT NULL,
    --
    CONSTRAINT pk_lembrete          PRIMARY KEY (id),
    CONSTRAINT ck_lembrete_recorr   CHECK (recorrente IN (0, 1)),
    CONSTRAINT ck_lembrete_status   CHECK (status IS NULL OR status IN
                                          ('PENDENTE','ENVIADO','CANCELADO')),
    CONSTRAINT ck_lembrete_tipo     CHECK (tipo IS NULL OR tipo IN
                                          ('VACINA','MEDICAMENTO','CONSULTA','HIGIENE','OUTRO'))
);

COMMENT ON TABLE  t_clyvo_lembrete             IS 'Lembretes de cuidados por animal — API .NET';
COMMENT ON COLUMN t_clyvo_lembrete.animal_id   IS 'FK para t_clyvo_animal.id (criada pela API Java)';
COMMENT ON COLUMN t_clyvo_lembrete.tipo        IS 'VACINA | MEDICAMENTO | CONSULTA | HIGIENE | OUTRO';
COMMENT ON COLUMN t_clyvo_lembrete.status      IS 'PENDENTE | ENVIADO | CANCELADO — sempre PENDENTE na criacao';
COMMENT ON COLUMN t_clyvo_lembrete.recorrente  IS '1 = recorrente, 0 = pontual';

-- -------------------------------------------------------------
-- 4. t_clyvo_sugestao_produto
--    Sugestão de produto para um animal específico.
--    FK dupla: t_clyvo_animal (Java) + t_clyvo_produto (.NET).
-- -------------------------------------------------------------
CREATE TABLE t_clyvo_sugestao_produto (
    id            VARCHAR2(36)    NOT NULL,
    animal_id     VARCHAR2(36)    NOT NULL,              -- FK → t_clyvo_animal.id (API Java)
    produto_id    VARCHAR2(36)    NOT NULL,              -- FK → t_clyvo_produto.id (API .NET)
    justificativa VARCHAR2(1000),
    data_sugestao DATE            NOT NULL,
    ativo         NUMBER(1)       DEFAULT 1  NOT NULL,
    criado_em     TIMESTAMP       DEFAULT SYSTIMESTAMP NOT NULL,
    --
    CONSTRAINT pk_sugestao_produto  PRIMARY KEY (id),
    CONSTRAINT ck_sugestao_ativo    CHECK (ativo IN (0, 1)),
    CONSTRAINT fk_sugestao_produto  FOREIGN KEY (produto_id)
                                    REFERENCES t_clyvo_produto(id)
);

COMMENT ON TABLE  t_clyvo_sugestao_produto              IS 'Sugestao de produto para um animal — API .NET';
COMMENT ON COLUMN t_clyvo_sugestao_produto.animal_id    IS 'FK para t_clyvo_animal.id (criada pela API Java)';
COMMENT ON COLUMN t_clyvo_sugestao_produto.produto_id   IS 'FK para t_clyvo_produto.id (criada por este script)';

-- =============================================================
PROMPT ============================================================
PROMPT [4/5] Adicionando FK para t_clyvo_animal (tabela Java)...
PROMPT ============================================================
-- =============================================================
-- As FKs para t_clyvo_animal são adicionadas após a criação das
-- tabelas .NET. Se a tabela Java ainda não existir, o bloco
-- registra um aviso e continua — as tabelas .NET funcionam
-- normalmente, mas os inserts com animal_id serão rejeitados
-- em tempo de execução.

BEGIN
    EXECUTE IMMEDIATE '
        ALTER TABLE t_clyvo_lembrete
        ADD CONSTRAINT fk_lembrete_animal
            FOREIGN KEY (animal_id) REFERENCES t_clyvo_animal(id)';
    DBMS_OUTPUT.PUT_LINE('[OK] FK fk_lembrete_animal criada.');
EXCEPTION
    WHEN OTHERS THEN
        DBMS_OUTPUT.PUT_LINE('[AVISO] Nao foi possivel criar fk_lembrete_animal.');
        DBMS_OUTPUT.PUT_LINE('        Certifique-se de que t_clyvo_animal existe (script da API Java).');
        DBMS_OUTPUT.PUT_LINE('        Erro: ' || SQLERRM);
END;
/

BEGIN
    EXECUTE IMMEDIATE '
        ALTER TABLE t_clyvo_sugestao_produto
        ADD CONSTRAINT fk_sugestao_animal
            FOREIGN KEY (animal_id) REFERENCES t_clyvo_animal(id)';
    DBMS_OUTPUT.PUT_LINE('[OK] FK fk_sugestao_animal criada.');
EXCEPTION
    WHEN OTHERS THEN
        DBMS_OUTPUT.PUT_LINE('[AVISO] Nao foi possivel criar fk_sugestao_animal.');
        DBMS_OUTPUT.PUT_LINE('        Certifique-se de que t_clyvo_animal existe (script da API Java).');
        DBMS_OUTPUT.PUT_LINE('        Erro: ' || SQLERRM);
END;
/

-- =============================================================
PROMPT ============================================================
PROMPT [5/5] Criando triggers de auto-UUID...
PROMPT ============================================================
-- =============================================================
-- Os triggers preenchem id e criado_em caso a inserção venha
-- de fora da API (ex.: script de seed rodando direto no SQL Developer).
-- Quando a API .NET insere um registro, ela já traz id e criado_em
-- preenchidos — o trigger respeita isso com a cláusula IF ... IS NULL.

CREATE OR REPLACE TRIGGER trg_produto_bi
BEFORE INSERT ON t_clyvo_produto
FOR EACH ROW
BEGIN
    IF :NEW.id IS NULL THEN
        :NEW.id := fn_uuid();
    END IF;
    IF :NEW.criado_em IS NULL THEN
        :NEW.criado_em := SYSTIMESTAMP;
    END IF;
END trg_produto_bi;
/

CREATE OR REPLACE TRIGGER trg_evento_pet_bi
BEFORE INSERT ON t_clyvo_evento_pet
FOR EACH ROW
BEGIN
    IF :NEW.id IS NULL THEN
        :NEW.id := fn_uuid();
    END IF;
    IF :NEW.criado_em IS NULL THEN
        :NEW.criado_em := SYSTIMESTAMP;
    END IF;
END trg_evento_pet_bi;
/

CREATE OR REPLACE TRIGGER trg_lembrete_bi
BEFORE INSERT ON t_clyvo_lembrete
FOR EACH ROW
BEGIN
    IF :NEW.id IS NULL THEN
        :NEW.id := fn_uuid();
    END IF;
    IF :NEW.status IS NULL THEN
        :NEW.status := 'PENDENTE';
    END IF;
    IF :NEW.criado_em IS NULL THEN
        :NEW.criado_em := SYSTIMESTAMP;
    END IF;
END trg_lembrete_bi;
/

CREATE OR REPLACE TRIGGER trg_sugestao_produto_bi
BEFORE INSERT ON t_clyvo_sugestao_produto
FOR EACH ROW
BEGIN
    IF :NEW.id IS NULL THEN
        :NEW.id := fn_uuid();
    END IF;
    IF :NEW.criado_em IS NULL THEN
        :NEW.criado_em := SYSTIMESTAMP;
    END IF;
END trg_sugestao_produto_bi;
/

COMMIT;

PROMPT ============================================================
PROMPT Tabelas e triggers criados com sucesso!
PROMPT
PROMPT Proximos passos:
PROMPT   1. Execute schema/02_seed_dotnet.sql para popular o banco
PROMPT   2. Rode a API: dotnet run (na pasta ClyvoVet.Api)
PROMPT   3. Acesse o Swagger: http://localhost:5191/swagger
PROMPT ============================================================
