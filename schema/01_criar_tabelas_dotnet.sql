-- ============================================================
-- CLYVO VET -- ORACLE DATABASE
-- Arquivo 01: DDL -- Criação das Tabelas
-- ============================================================

-- ------------------------------------------------------------
-- LIMPEZA PRÉVIA
-- Remove tudo sem erro, mesmo que os objetos não existam.
-- Ordem inversa das FKs para evitar conflitos.
-- ------------------------------------------------------------
BEGIN EXECUTE IMMEDIATE 'DROP TABLE t_clyvo_lembrete         CASCADE CONSTRAINTS'; EXCEPTION WHEN OTHERS THEN NULL; END;
/
BEGIN EXECUTE IMMEDIATE 'DROP TABLE t_clyvo_sugestao_produto  CASCADE CONSTRAINTS'; EXCEPTION WHEN OTHERS THEN NULL; END;
/
BEGIN EXECUTE IMMEDIATE 'DROP TABLE t_clyvo_evento_pet        CASCADE CONSTRAINTS'; EXCEPTION WHEN OTHERS THEN NULL; END;
/
BEGIN EXECUTE IMMEDIATE 'DROP TABLE t_clyvo_produto           CASCADE CONSTRAINTS'; EXCEPTION WHEN OTHERS THEN NULL; END;
/
BEGIN EXECUTE IMMEDIATE 'DROP TABLE t_clyvo_pagamento         CASCADE CONSTRAINTS'; EXCEPTION WHEN OTHERS THEN NULL; END;
/
BEGIN EXECUTE IMMEDIATE 'DROP TABLE t_clyvo_evento_clinico    CASCADE CONSTRAINTS'; EXCEPTION WHEN OTHERS THEN NULL; END;
/
BEGIN EXECUTE IMMEDIATE 'DROP TABLE t_clyvo_veterinario       CASCADE CONSTRAINTS'; EXCEPTION WHEN OTHERS THEN NULL; END;
/
BEGIN EXECUTE IMMEDIATE 'DROP TABLE t_clyvo_animal            CASCADE CONSTRAINTS'; EXCEPTION WHEN OTHERS THEN NULL; END;
/
BEGIN EXECUTE IMMEDIATE 'DROP TABLE t_clyvo_clinica           CASCADE CONSTRAINTS'; EXCEPTION WHEN OTHERS THEN NULL; END;
/
BEGIN EXECUTE IMMEDIATE 'DROP TABLE t_clyvo_tutor             CASCADE CONSTRAINTS'; EXCEPTION WHEN OTHERS THEN NULL; END;
/
BEGIN EXECUTE IMMEDIATE 'DROP TABLE t_clyvo_log_erros         CASCADE CONSTRAINTS'; EXCEPTION WHEN OTHERS THEN NULL; END;
/
BEGIN EXECUTE IMMEDIATE 'DROP SEQUENCE seq_log_erros'; EXCEPTION WHEN OTHERS THEN NULL; END;
/

-- ------------------------------------------------------------
-- FUNÇÃO: fn_uuid
-- Gera UUID no formato 8-4-4-4-12 (compatível com Java/C#)
-- ------------------------------------------------------------
CREATE OR REPLACE FUNCTION fn_uuid RETURN VARCHAR2 IS
BEGIN
    RETURN LOWER(REGEXP_REPLACE(
        RAWTOHEX(SYS_GUID()),
        '([A-F0-9]{8})([A-F0-9]{4})([A-F0-9]{4})([A-F0-9]{4})([A-F0-9]{12})',
        '\1-\2-\3-\4-\5'
    ));
END fn_uuid;
/

-- ------------------------------------------------------------
-- SEQUENCE: somente para t_clyvo_log_erros (ID numérico de sistema)
-- ------------------------------------------------------------
CREATE SEQUENCE seq_log_erros START WITH 1 INCREMENT BY 1 NOCACHE NOCYCLE;

-- ============================================================
-- LOG DE ERROS  (tabela de sistema)
-- ============================================================
CREATE TABLE t_clyvo_log_erros (
    id              NUMBER        DEFAULT seq_log_erros.NEXTVAL PRIMARY KEY,
    nome_procedure  VARCHAR2(100) NOT NULL,
    usuario         VARCHAR2(100) DEFAULT USER,
    data_erro       TIMESTAMP     DEFAULT SYSTIMESTAMP,
    codigo_erro     NUMBER,
    mensagem_erro   VARCHAR2(4000)
);

-- ============================================================
-- 1. TUTOR
-- ============================================================
CREATE TABLE t_clyvo_tutor (
    id              VARCHAR2(36)  NOT NULL,
    cpf             VARCHAR2(11),
    nome            VARCHAR2(150) NOT NULL,
    data_nascimento DATE,
    genero          VARCHAR2(10),
    email           VARCHAR2(200),
    telefone        VARCHAR2(20),
    rua             VARCHAR2(300),
    numero          VARCHAR2(10),
    bairro          VARCHAR2(150),
    cidade          VARCHAR2(100),
    estado          VARCHAR2(50),
    cep             VARCHAR2(10),
    criado_em       TIMESTAMP     DEFAULT SYSTIMESTAMP,
    CONSTRAINT pk_tutor         PRIMARY KEY (id),
    CONSTRAINT uk_tutor_cpf     UNIQUE (cpf),
    CONSTRAINT uk_tutor_email   UNIQUE (email),
    CONSTRAINT chk_tutor_genero CHECK (genero IN ('MASCULINO','FEMININO','OUTRO'))
);


CREATE OR REPLACE TRIGGER trg_tutor_id
BEFORE INSERT ON t_clyvo_tutor
FOR EACH ROW
BEGIN
    IF :NEW.id IS NULL THEN
        :NEW.id := fn_uuid();
    END IF;
END;
/

-- ============================================================
-- 2. ANIMAL
-- ============================================================
CREATE TABLE t_clyvo_animal (
    id              VARCHAR2(36)  NOT NULL,
    nome            VARCHAR2(100) NOT NULL,
    raca            VARCHAR2(100),
    especie         VARCHAR2(50),
    porte           VARCHAR2(10),
    cor             VARCHAR2(80),
    genero          VARCHAR2(10),
    data_nascimento DATE,
    observacoes     VARCHAR2(1000),
    peso            NUMBER(5,2),
    castrado        NUMBER(1)     DEFAULT 0,
    microchip       VARCHAR2(50),
    foto_url        VARCHAR2(500),
    qr_code         VARCHAR2(100),
    tutor_id        VARCHAR2(36),
    criado_em       TIMESTAMP     DEFAULT SYSTIMESTAMP,
    CONSTRAINT pk_animal             PRIMARY KEY (id),
    CONSTRAINT fk_animal_tutor       FOREIGN KEY (tutor_id) REFERENCES t_clyvo_tutor(id),
    CONSTRAINT chk_animal_castrado   CHECK (castrado IN (0,1)),
    CONSTRAINT uk_animal_microchip   UNIQUE (microchip),
    CONSTRAINT uk_animal_qr_code     UNIQUE (qr_code),
    CONSTRAINT chk_animal_porte      CHECK (porte   IN ('PEQUENO','MEDIO','GRANDE')),
    CONSTRAINT chk_animal_genero     CHECK (genero  IN ('MACHO','FEMEA','DESCONHECIDO'))
);

CREATE OR REPLACE TRIGGER trg_animal_id
BEFORE INSERT ON t_clyvo_animal
FOR EACH ROW
BEGIN
    IF :NEW.id IS NULL THEN
        :NEW.id := fn_uuid();
    END IF;
END;
/

-- ============================================================
-- 3. CLINICA
-- ============================================================
CREATE TABLE t_clyvo_clinica (
    id        VARCHAR2(36)  NOT NULL,
    nome      VARCHAR2(200) NOT NULL,
    cnpj      VARCHAR2(14),
    telefone  VARCHAR2(20),
    email     VARCHAR2(200),
    rua       VARCHAR2(300),
    numero    VARCHAR2(10),
    bairro    VARCHAR2(150),
    cidade    VARCHAR2(100),
    estado    VARCHAR2(50),
    cep       VARCHAR2(10),
    criado_em TIMESTAMP     DEFAULT SYSTIMESTAMP,
    CONSTRAINT pk_clinica      PRIMARY KEY (id),
    CONSTRAINT uk_clinica_cnpj UNIQUE (cnpj)
);

CREATE OR REPLACE TRIGGER trg_clinica_id
BEFORE INSERT ON t_clyvo_clinica
FOR EACH ROW
BEGIN
    IF :NEW.id IS NULL THEN
        :NEW.id := fn_uuid();
    END IF;
END;
/

-- ============================================================
-- 4. VETERINARIO
-- ============================================================
CREATE TABLE t_clyvo_veterinario (
    id              VARCHAR2(36)  NOT NULL,
    cpf             VARCHAR2(11),
    nome            VARCHAR2(150) NOT NULL,
    data_nascimento DATE,
    genero          VARCHAR2(10),
    email           VARCHAR2(200),
    telefone        VARCHAR2(20),
    especialidade   VARCHAR2(100),
    crmv            VARCHAR2(30),
    rua             VARCHAR2(300),
    numero          VARCHAR2(10),
    bairro          VARCHAR2(150),
    cidade          VARCHAR2(100),
    estado          VARCHAR2(50),
    cep             VARCHAR2(10),
    clinica_id      VARCHAR2(36),
    criado_em       TIMESTAMP     DEFAULT SYSTIMESTAMP,
    CONSTRAINT pk_veterinario         PRIMARY KEY (id),
    CONSTRAINT fk_veterinario_clinica FOREIGN KEY (clinica_id) REFERENCES t_clyvo_clinica(id),
    CONSTRAINT uk_vet_cpf             UNIQUE (cpf),
    CONSTRAINT uk_vet_crmv            UNIQUE (crmv),
    CONSTRAINT chk_vet_genero         CHECK (genero IN ('MASCULINO','FEMININO','OUTRO'))
);

CREATE OR REPLACE TRIGGER trg_veterinario_id
BEFORE INSERT ON t_clyvo_veterinario
FOR EACH ROW
BEGIN
    IF :NEW.id IS NULL THEN
        :NEW.id := fn_uuid();
    END IF;
END;
/

-- ============================================================
-- 5. EVENTO_CLINICO
-- ============================================================
CREATE TABLE t_clyvo_evento_clinico (
    id              VARCHAR2(36)  NOT NULL,
    data_evento     DATE,
    hora_evento     VARCHAR2(5),
    descricao       VARCHAR2(1000),
    tipo_evento     VARCHAR2(20),
    veterinario_id  VARCHAR2(36),
    animal_id       VARCHAR2(36),
    clinica_id      VARCHAR2(36),
    criado_em       TIMESTAMP     DEFAULT SYSTIMESTAMP,
    CONSTRAINT pk_evento_clinico     PRIMARY KEY (id),
    CONSTRAINT fk_evento_veterinario FOREIGN KEY (veterinario_id) REFERENCES t_clyvo_veterinario(id),
    CONSTRAINT fk_evento_animal      FOREIGN KEY (animal_id)      REFERENCES t_clyvo_animal(id),
    CONSTRAINT fk_evento_clinica     FOREIGN KEY (clinica_id)     REFERENCES t_clyvo_clinica(id),
    CONSTRAINT chk_evento_tipo       CHECK (tipo_evento IN ('CONSULTA','RETORNO','VACINA','EXAME','CIRURGIA','OUTRO'))
);

CREATE OR REPLACE TRIGGER trg_evento_clinico_id
BEFORE INSERT ON t_clyvo_evento_clinico
FOR EACH ROW
BEGIN
    IF :NEW.id IS NULL THEN
        :NEW.id := fn_uuid();
    END IF;
END;
/

-- ============================================================
-- 6. PAGAMENTO
-- PIX e BOLETO mantidos sem tradução (sistemas brasileiros)
-- ============================================================
CREATE TABLE t_clyvo_pagamento (
    id                VARCHAR2(36)  NOT NULL,
    metodo_pagamento  VARCHAR2(10),
    valor             NUMBER(10,2),
    data_pagamento    DATE,
    descricao         VARCHAR2(500),
    notas             VARCHAR2(1000),
    status_pagamento  VARCHAR2(15),
    evento_id         VARCHAR2(36),
    criado_em         TIMESTAMP     DEFAULT SYSTIMESTAMP,
    CONSTRAINT pk_pagamento          PRIMARY KEY (id),
    CONSTRAINT fk_pagamento_evento   FOREIGN KEY (evento_id) REFERENCES t_clyvo_evento_clinico(id),
    CONSTRAINT chk_pagamento_metodo  CHECK (metodo_pagamento IN ('PIX','CARTAO','DINHEIRO','BOLETO')),
    CONSTRAINT chk_pagamento_status  CHECK (status_pagamento IN ('PENDENTE','PAGO','CANCELADO','REEMBOLSADO')),
    CONSTRAINT chk_pagamento_valor   CHECK (valor > 0)
);

CREATE OR REPLACE TRIGGER trg_pagamento_id
BEFORE INSERT ON t_clyvo_pagamento
FOR EACH ROW
BEGIN
    IF :NEW.id IS NULL THEN
        :NEW.id := fn_uuid();
    END IF;
END;
/

-- ============================================================
-- 7. PRODUTO  (domínio API .Net)
-- ============================================================
CREATE TABLE t_clyvo_produto (
    id               VARCHAR2(36)  NOT NULL,
    nome             VARCHAR2(200) NOT NULL,
    descricao        VARCHAR2(1000),
    categoria        VARCHAR2(20),
    preco            NUMBER(10,2),
    especie_indicada VARCHAR2(20),
    ativo            NUMBER(1)     DEFAULT 1,
    criado_em        TIMESTAMP     DEFAULT SYSTIMESTAMP,
    CONSTRAINT pk_produto             PRIMARY KEY (id),
    CONSTRAINT chk_produto_categoria  CHECK (categoria         IN ('RACAO','MEDICAMENTO','ACESSORIO','SERVICO','OUTRO')),
    CONSTRAINT chk_produto_especie    CHECK (especie_indicada  IN ('CACHORRO','GATO','PASSARO','REPTIL','ROEDOR','TODOS','OUTRO')),
    CONSTRAINT chk_produto_ativo      CHECK (ativo             IN (0,1))
);

CREATE OR REPLACE TRIGGER trg_produto_id
BEFORE INSERT ON t_clyvo_produto
FOR EACH ROW
BEGIN
    IF :NEW.id IS NULL THEN
        :NEW.id := fn_uuid();
    END IF;
END;
/

-- ============================================================
-- 8. SUGESTAO_PRODUTO  (domínio API .Net)
-- animal_id vem do front-end; FK valida existência no banco
-- ============================================================
CREATE TABLE t_clyvo_sugestao_produto (
    id              VARCHAR2(36)  NOT NULL,
    animal_id       VARCHAR2(36)  NOT NULL,
    produto_id      VARCHAR2(36)  NOT NULL,
    justificativa   VARCHAR2(500),
    data_sugestao   DATE          DEFAULT SYSDATE,
    ativo           NUMBER(1)     DEFAULT 1,
    criado_em       TIMESTAMP     DEFAULT SYSTIMESTAMP,
    CONSTRAINT pk_sugestao_produto  PRIMARY KEY (id),
    CONSTRAINT fk_sugestao_animal   FOREIGN KEY (animal_id)  REFERENCES t_clyvo_animal(id),
    CONSTRAINT fk_sugestao_produto  FOREIGN KEY (produto_id) REFERENCES t_clyvo_produto(id),
    CONSTRAINT chk_sugestao_ativo   CHECK (ativo IN (0,1))
);

CREATE OR REPLACE TRIGGER trg_sugestao_produto_id
BEFORE INSERT ON t_clyvo_sugestao_produto
FOR EACH ROW
BEGIN
    IF :NEW.id IS NULL THEN
        :NEW.id := fn_uuid();
    END IF;
END;
/

-- ============================================================
-- 9. LEMBRETE  (domínio API .Net)
-- ============================================================
CREATE TABLE t_clyvo_lembrete (
    id          VARCHAR2(36)  NOT NULL,
    animal_id   VARCHAR2(36)  NOT NULL,
    titulo      VARCHAR2(200) NOT NULL,
    descricao   VARCHAR2(1000),
    tipo        VARCHAR2(20),
    agendado_em TIMESTAMP     NOT NULL,
    recorrente  NUMBER(1)     DEFAULT 0,
    status      VARCHAR2(20)  DEFAULT 'PENDENTE',
    criado_em   TIMESTAMP     DEFAULT SYSTIMESTAMP,
    CONSTRAINT pk_lembrete             PRIMARY KEY (id),
    CONSTRAINT fk_lembrete_animal      FOREIGN KEY (animal_id) REFERENCES t_clyvo_animal(id),
    CONSTRAINT chk_lembrete_tipo       CHECK (tipo       IN ('VACINA','MEDICAMENTO','CONSULTA','HIGIENE','OUTRO')),
    CONSTRAINT chk_lembrete_recorrente CHECK (recorrente IN (0,1)),
    CONSTRAINT chk_lembrete_status     CHECK (status     IN ('PENDENTE','ENVIADO','CANCELADO'))
);

CREATE OR REPLACE TRIGGER trg_lembrete_id
BEFORE INSERT ON t_clyvo_lembrete
FOR EACH ROW
BEGIN
    IF :NEW.id IS NULL THEN
        :NEW.id := fn_uuid();
    END IF;
END;
/

-- ============================================================
-- 10. EVENTO_PET  (domínio API .Net)
-- ============================================================
CREATE TABLE t_clyvo_evento_pet (
    id              VARCHAR2(36)  NOT NULL,
    titulo          VARCHAR2(200) NOT NULL,
    descricao       VARCHAR2(1000),
    tipo            VARCHAR2(20),
    rua             VARCHAR2(300),
    numero          VARCHAR2(10),
    bairro          VARCHAR2(150),
    cidade          VARCHAR2(100),
    estado          VARCHAR2(50),
    cep             VARCHAR2(10),
    data_inicio     DATE          NOT NULL,
    data_fim        DATE,
    especie_alvo    VARCHAR2(20)  DEFAULT 'TODOS',
    organizador     VARCHAR2(200),
    gratuito        NUMBER(1)     DEFAULT 1,
    link_inscricao  VARCHAR2(500),
    ativo           NUMBER(1)     DEFAULT 1,
    criado_em       TIMESTAMP     DEFAULT SYSTIMESTAMP,
    CONSTRAINT pk_evento_pet               PRIMARY KEY (id),
    CONSTRAINT chk_evento_pet_tipo         CHECK (tipo         IN ('VACINACAO','FEIRA','CASTRACAO','WORKSHOP','OUTRO')),
    CONSTRAINT chk_evento_pet_especie_alvo CHECK (especie_alvo IN ('CACHORRO','GATO','PASSARO','REPTIL','ROEDOR','TODOS','OUTRO')),
    CONSTRAINT chk_evento_pet_gratuito     CHECK (gratuito     IN (0,1)),
    CONSTRAINT chk_evento_pet_ativo        CHECK (ativo        IN (0,1))
);

CREATE OR REPLACE TRIGGER trg_evento_pet_id
BEFORE INSERT ON t_clyvo_evento_pet
FOR EACH ROW
BEGIN
    IF :NEW.id IS NULL THEN
        :NEW.id := fn_uuid();
    END IF;
END;
/
