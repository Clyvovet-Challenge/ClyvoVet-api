-- script_bd.sql - banco compartilhado da Sprint de DevOps (MySQL na Azure)
--
-- Esse banco é o mesmo usado pela API Java do time (Tutor, Animal, Clinica,
-- etc.) e pela nossa API .NET (Produto, Sugestao de Produto, Lembrete,
-- Evento Pet). Rodar esse script cria as duas partes.
--
-- A PARTE 1 é uma cópia das migrations Flyway reais do time de Java
-- (src/main/resources/db/migration/mysql/V1 a V7 do repo clyvovet-backend-java),
-- concatenadas aqui numa tacada só em vez de rodar o Flyway. Se o schema
-- dele mudar, essa parte precisa ser atualizada a partir do repo dele.
--
-- A PARTE 2 é só nossa: t_clyvo_produto e t_clyvo_sugestao_produto sao as
-- tabelas do CRUD dessa entrega (relacionadas por produto_id). As outras
-- (t_clyvo_lembrete, t_clyvo_evento_pet, t_clyvo_predisposicao_saude,
-- t_clyvo_tutor_telegram) sao features que ja existiam antes dessa Sprint.
--
-- animal_id/tutor_id nas nossas tabelas apontam pras tabelas da PARTE 1.

-- ============================================================
-- LIMPEZA (ordem inversa as dependencias)
-- ============================================================

DROP TABLE IF EXISTS t_clyvo_sugestao_produto;
DROP TABLE IF EXISTS t_clyvo_lembrete;
DROP TABLE IF EXISTS t_clyvo_evento_pet;
DROP TABLE IF EXISTS t_clyvo_predisposicao_saude;
DROP TABLE IF EXISTS t_clyvo_tutor_telegram;
DROP TABLE IF EXISTS t_clyvo_produto;

DROP TABLE IF EXISTS acesso_historico;
DROP TABLE IF EXISTS autorizacao_acesso;
DROP TABLE IF EXISTS alerta_clinico;
DROP TABLE IF EXISTS bloqueio;
DROP TABLE IF EXISTS disponibilidade_veterinario;
DROP TABLE IF EXISTS pagamento;
DROP TABLE IF EXISTS evento_clinico;
DROP TABLE IF EXISTS servico;
DROP TABLE IF EXISTS usuario;
DROP TABLE IF EXISTS veterinario;
DROP TABLE IF EXISTS animal;
DROP TABLE IF EXISTS clinica;
DROP TABLE IF EXISTS tutor;

-- ============================================================
-- PARTE 1 — schema da API Java (tutor, animal, clinica, etc.)
-- Copiado de db/migration/mysql/V1, V3, V5, V6, V7 do repo dela.
-- (V4 é so um UPDATE/ALTER em cima do V1, ja aplicado direto abaixo;
-- V2 e o seed, que entra la no final desta parte.)
-- ============================================================

CREATE TABLE tutor (
    id              VARCHAR(36)  PRIMARY KEY,
    cpf             VARCHAR(11),
    nome            VARCHAR(150) NOT NULL,
    data_nascimento DATE,
    genero          VARCHAR(10),
    email           VARCHAR(200),
    telefone        VARCHAR(20),
    rua             VARCHAR(300),
    numero          VARCHAR(10),
    complemento     VARCHAR(100),
    bairro          VARCHAR(150),
    cidade          VARCHAR(100),
    estado          VARCHAR(50),
    cep             VARCHAR(10),
    CONSTRAINT uk_tutor_cpf     UNIQUE (cpf),
    CONSTRAINT uk_tutor_email   UNIQUE (email),
    CONSTRAINT chk_tutor_genero CHECK (genero IN ('MASCULINO','FEMININO','OUTRO'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE clinica (
    id          VARCHAR(36)  PRIMARY KEY,
    nome        VARCHAR(200) NOT NULL,
    cnpj        VARCHAR(14),
    telefone    VARCHAR(20),
    email       VARCHAR(200),
    rua         VARCHAR(300),
    numero      VARCHAR(10),
    complemento VARCHAR(100),
    bairro      VARCHAR(150),
    cidade      VARCHAR(100),
    estado      VARCHAR(50),
    cep         VARCHAR(10),
    CONSTRAINT uk_clinica_cnpj UNIQUE (cnpj)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE animal (
    id               VARCHAR(36)  PRIMARY KEY,
    nome             VARCHAR(100) NOT NULL,
    raca             VARCHAR(100),
    especie          VARCHAR(50),
    porte            VARCHAR(20),
    cor              VARCHAR(80),
    genero           VARCHAR(10),
    data_nascimento  DATE,
    observacoes      VARCHAR(1000),
    tutor_id         VARCHAR(36),
    CONSTRAINT fk_animal_tutor   FOREIGN KEY (tutor_id) REFERENCES tutor(id),
    CONSTRAINT chk_animal_porte  CHECK (porte  IN ('PEQUENO','MEDIO','GRANDE')),
    CONSTRAINT chk_animal_genero CHECK (genero IN ('MACHO','FEMEA','DESCONHECIDO'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE veterinario (
    id               VARCHAR(36)  PRIMARY KEY,
    cpf              VARCHAR(11),
    nome             VARCHAR(150) NOT NULL,
    data_nascimento  DATE,
    genero           VARCHAR(10),
    email            VARCHAR(200),
    telefone         VARCHAR(20),
    especialidade    VARCHAR(100),
    crmv             VARCHAR(30),
    rua              VARCHAR(300),
    numero           VARCHAR(10),
    complemento      VARCHAR(100),
    bairro           VARCHAR(150),
    cidade           VARCHAR(100),
    estado           VARCHAR(50),
    cep              VARCHAR(10),
    clinica_id       VARCHAR(36),
    CONSTRAINT fk_vet_clinica  FOREIGN KEY (clinica_id) REFERENCES clinica(id),
    CONSTRAINT uk_vet_cpf      UNIQUE (cpf),
    CONSTRAINT uk_vet_crmv     UNIQUE (crmv),
    CONSTRAINT chk_vet_genero  CHECK (genero IN ('MASCULINO','FEMININO','OUTRO'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE evento_clinico (
    id              VARCHAR(36)  PRIMARY KEY,
    data_evento     DATE,
    hora_evento     VARCHAR(5),
    descricao       VARCHAR(1000),
    tipo_evento     VARCHAR(20),
    veterinario_id  VARCHAR(36),
    animal_id       VARCHAR(36),
    clinica_id      VARCHAR(36),
    status_evento   VARCHAR(20) NOT NULL DEFAULT 'REALIZADO',
    data_retorno_previsto DATE,
    evento_origem_id VARCHAR(36),
    peso_kg         DECIMAL(6,3),
    servico_id      VARCHAR(36),
    desfecho        VARCHAR(20),
    motivo_cancelamento VARCHAR(500),
    CONSTRAINT fk_evento_vet     FOREIGN KEY (veterinario_id) REFERENCES veterinario(id),
    CONSTRAINT fk_evento_animal  FOREIGN KEY (animal_id)      REFERENCES animal(id),
    CONSTRAINT fk_evento_clinica FOREIGN KEY (clinica_id)     REFERENCES clinica(id),
    CONSTRAINT fk_evento_origem  FOREIGN KEY (evento_origem_id) REFERENCES evento_clinico(id),
    CONSTRAINT chk_evento_tipo   CHECK (tipo_evento IN ('CONSULTA','RETORNO','VACINA','EXAME','CIRURGIA','OUTRO')),
    CONSTRAINT chk_evento_status CHECK (status_evento IN ('AGENDADO','REALIZADO','FALTOU','CANCELADO')),
    CONSTRAINT chk_evento_peso   CHECK (peso_kg IS NULL OR peso_kg > 0),
    CONSTRAINT chk_evento_origem_propria CHECK (evento_origem_id IS NULL OR evento_origem_id <> id),
    CONSTRAINT chk_evento_desfecho CHECK (desfecho IS NULL OR desfecho IN ('MELHORA','ESTAVEL','PIORA','OBITO','INDEFINIDO'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE INDEX idx_evento_vet_data    ON evento_clinico (veterinario_id, data_evento);
CREATE INDEX idx_evento_animal_data ON evento_clinico (animal_id, data_evento);
CREATE INDEX idx_evento_retorno     ON evento_clinico (data_retorno_previsto);

-- status_pagamento ja entra com REEMBOLSADO (V4 aplicada direto, nao tem ESTORNADO pra corrigir)
CREATE TABLE pagamento (
    id                VARCHAR(36)  PRIMARY KEY,
    metodo_pagamento  VARCHAR(10),
    valor             DECIMAL(10,2),
    data_pagamento    DATE,
    descricao         VARCHAR(500),
    notas             VARCHAR(1000),
    status_pagamento  VARCHAR(15),
    evento_id         VARCHAR(36),
    CONSTRAINT fk_pagamento_evento  FOREIGN KEY (evento_id) REFERENCES evento_clinico(id),
    CONSTRAINT chk_forma_pagamento  CHECK (metodo_pagamento IN ('PIX','CARTAO','DINHEIRO','BOLETO')),
    CONSTRAINT chk_status_pagamento CHECK (status_pagamento IN ('PENDENTE','PAGO','CANCELADO','REEMBOLSADO')),
    CONSTRAINT chk_pagamento_valor  CHECK (valor > 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE usuario (
    id                VARCHAR(36)  PRIMARY KEY,
    email             VARCHAR(200) NOT NULL,
    senha             VARCHAR(100) NOT NULL,
    perfil            VARCHAR(20)  NOT NULL,
    ativo             TINYINT      DEFAULT 1 NOT NULL,
    tentativas_falhas INT          DEFAULT 0 NOT NULL,
    bloqueado_ate     DATETIME,
    tutor_id          VARCHAR(36),
    veterinario_id    VARCHAR(36),
    CONSTRAINT uk_usuario_email    UNIQUE (email),
    CONSTRAINT fk_usuario_tutor    FOREIGN KEY (tutor_id)       REFERENCES tutor(id),
    CONSTRAINT fk_usuario_vet      FOREIGN KEY (veterinario_id) REFERENCES veterinario(id),
    CONSTRAINT chk_usuario_perfil  CHECK (perfil IN ('TUTOR','VETERINARIO','ADMIN')),
    CONSTRAINT chk_usuario_ativo   CHECK (ativo IN (0,1))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE INDEX idx_usuario_email ON usuario (email);

CREATE TABLE servico (
    id               VARCHAR(36)   PRIMARY KEY,
    clinica_id       VARCHAR(36)   NOT NULL,
    nome             VARCHAR(100)  NOT NULL,
    tipo_evento      VARCHAR(20)   NOT NULL,
    preco            DECIMAL(10,2) NOT NULL,
    duracao_minutos  INT           NOT NULL,
    ativo            TINYINT       NOT NULL DEFAULT 1,
    CONSTRAINT fk_servico_clinica  FOREIGN KEY (clinica_id) REFERENCES clinica(id),
    CONSTRAINT chk_servico_tipo    CHECK (tipo_evento IN ('CONSULTA','RETORNO','VACINA','EXAME','CIRURGIA','OUTRO')),
    CONSTRAINT chk_servico_preco   CHECK (preco >= 0),
    CONSTRAINT chk_servico_duracao CHECK (duracao_minutos BETWEEN 5 AND 480),
    CONSTRAINT chk_servico_ativo   CHECK (ativo IN (0,1)),
    CONSTRAINT uk_servico_clinica_nome UNIQUE (clinica_id, nome)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE INDEX idx_servico_clinica ON servico (clinica_id, ativo);

CREATE TABLE disponibilidade_veterinario (
    id               VARCHAR(36) PRIMARY KEY,
    veterinario_id   VARCHAR(36) NOT NULL,
    dia_semana       VARCHAR(10) NOT NULL,
    hora_inicio      VARCHAR(5)  NOT NULL,
    hora_fim         VARCHAR(5)  NOT NULL,
    vigencia_inicio  DATE        NOT NULL,
    vigencia_fim     DATE,
    CONSTRAINT fk_disp_veterinario FOREIGN KEY (veterinario_id) REFERENCES veterinario(id),
    CONSTRAINT chk_disp_dia        CHECK (dia_semana IN
        ('SEGUNDA','TERCA','QUARTA','QUINTA','SEXTA','SABADO','DOMINGO')),
    CONSTRAINT chk_disp_horas      CHECK (hora_fim > hora_inicio),
    CONSTRAINT chk_disp_vigencia   CHECK (vigencia_fim IS NULL OR vigencia_fim >= vigencia_inicio)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE INDEX idx_disp_vet_dia ON disponibilidade_veterinario (veterinario_id, dia_semana);

CREATE TABLE bloqueio (
    id              VARCHAR(36)  PRIMARY KEY,
    veterinario_id  VARCHAR(36)  NOT NULL,
    data_inicio     DATE         NOT NULL,
    data_fim        DATE         NOT NULL,
    hora_inicio     VARCHAR(5),
    hora_fim        VARCHAR(5),
    motivo          VARCHAR(200) NOT NULL,
    CONSTRAINT fk_bloqueio_veterinario FOREIGN KEY (veterinario_id) REFERENCES veterinario(id),
    CONSTRAINT chk_bloqueio_datas CHECK (data_fim >= data_inicio),
    CONSTRAINT chk_bloqueio_horas CHECK (
        (hora_inicio IS NULL AND hora_fim IS NULL)
     OR (hora_inicio IS NOT NULL AND hora_fim IS NOT NULL AND hora_fim > hora_inicio))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE INDEX idx_bloqueio_vet_data ON bloqueio (veterinario_id, data_inicio, data_fim);

CREATE TABLE alerta_clinico (
    id            VARCHAR(36)  PRIMARY KEY,
    animal_id     VARCHAR(36)  NOT NULL,
    tipo          VARCHAR(20)  NOT NULL,
    descricao     VARCHAR(500) NOT NULL,
    origem        VARCHAR(15)  NOT NULL,
    registrado_em DATE         NOT NULL DEFAULT (CURRENT_DATE),
    ativo         TINYINT      NOT NULL DEFAULT 1,
    CONSTRAINT fk_alerta_animal FOREIGN KEY (animal_id)
        REFERENCES animal(id) ON DELETE CASCADE,
    CONSTRAINT chk_alerta_tipo  CHECK (tipo IN
        ('ALERGIA','CONDICAO_CRONICA','MEDICACAO_CONTINUA','CRITICO')),
    CONSTRAINT chk_alerta_origem CHECK (origem IN ('TUTOR','VETERINARIO')),
    CONSTRAINT chk_alerta_ativo  CHECK (ativo IN (0,1))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE INDEX idx_alerta_animal ON alerta_clinico (animal_id, ativo);

ALTER TABLE animal ADD COLUMN microchip VARCHAR(15);
ALTER TABLE animal ADD COLUMN castrado TINYINT;
ALTER TABLE animal ADD CONSTRAINT uk_animal_microchip UNIQUE (microchip);
ALTER TABLE animal ADD CONSTRAINT chk_animal_castrado CHECK (castrado IS NULL OR castrado IN (0,1));
ALTER TABLE animal ADD COLUMN resumo_seguranca_ativo TINYINT NOT NULL DEFAULT 1;
ALTER TABLE animal ADD CONSTRAINT chk_animal_resumo CHECK (resumo_seguranca_ativo IN (0,1));

ALTER TABLE evento_clinico ADD CONSTRAINT fk_evento_servico
    FOREIGN KEY (servico_id) REFERENCES servico(id);

CREATE TABLE autorizacao_acesso (
    id               VARCHAR(36) PRIMARY KEY,
    animal_id        VARCHAR(36) NOT NULL,
    clinica_id       VARCHAR(36) NOT NULL,
    status           VARCHAR(15) NOT NULL,
    concedida_em     DATE        NOT NULL DEFAULT (CURRENT_DATE),
    valido_ate       DATE        NOT NULL,
    revogada_em      DATE,
    origem_evento_id VARCHAR(36),
    CONSTRAINT fk_autorizacao_animal  FOREIGN KEY (animal_id)
        REFERENCES animal(id) ON DELETE CASCADE,
    CONSTRAINT fk_autorizacao_clinica FOREIGN KEY (clinica_id) REFERENCES clinica(id),
    CONSTRAINT fk_autorizacao_evento  FOREIGN KEY (origem_evento_id)
        REFERENCES evento_clinico(id) ON DELETE SET NULL,
    CONSTRAINT chk_autorizacao_status CHECK (status IN ('VIGENTE','REVOGADA','EXPIRADA')),
    CONSTRAINT chk_autorizacao_datas  CHECK (valido_ate >= concedida_em),
    CONSTRAINT chk_autorizacao_revogacao CHECK (
        (status = 'REVOGADA' AND revogada_em IS NOT NULL)
     OR (status <> 'REVOGADA' AND revogada_em IS NULL)),
    CONSTRAINT uk_autorizacao_animal_clinica UNIQUE (animal_id, clinica_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE INDEX idx_autorizacao_animal ON autorizacao_acesso (animal_id, status);

CREATE TABLE acesso_historico (
    id          VARCHAR(36) PRIMARY KEY,
    animal_id   VARCHAR(36) NOT NULL,
    usuario_id  VARCHAR(36) NOT NULL,
    clinica_id  VARCHAR(36),
    dia         DATE        NOT NULL,
    nivel       TINYINT     NOT NULL,
    vezes       INT         NOT NULL DEFAULT 1,
    emergencial TINYINT     NOT NULL DEFAULT 0,
    motivo      VARCHAR(500),
    CONSTRAINT fk_acesso_animal  FOREIGN KEY (animal_id)
        REFERENCES animal(id) ON DELETE CASCADE,
    CONSTRAINT fk_acesso_usuario FOREIGN KEY (usuario_id) REFERENCES usuario(id),
    CONSTRAINT fk_acesso_clinica FOREIGN KEY (clinica_id) REFERENCES clinica(id),
    CONSTRAINT chk_acesso_nivel  CHECK (nivel IN (1,2)),
    CONSTRAINT chk_acesso_emerg  CHECK (emergencial IN (0,1)),
    CONSTRAINT chk_acesso_motivo CHECK (emergencial = 0 OR motivo IS NOT NULL),
    CONSTRAINT uk_acesso_dia UNIQUE (animal_id, usuario_id, dia, emergencial)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE INDEX idx_acesso_animal ON acesso_historico (animal_id, dia);
CREATE INDEX idx_acesso_usuario ON acesso_historico (usuario_id, dia);

-- ---------- Seed (V2 do repo Java) ----------

INSERT INTO clinica (id, nome, cnpj, telefone, email, rua, numero, bairro, cidade, estado, cep) VALUES
('11111111-1111-1111-1111-000000000001', 'VetCare Prime', '12345678000191', '1131000001', 'contato@vetcareprime.com.br', 'Av. Paulista', '1000', 'Bela Vista', 'Sao Paulo', 'SP', '01310100'),
('11111111-1111-1111-1111-000000000002', 'PetMed Centro', '23456789000102', '1131000002', 'contato@petmed.com.br', 'R. Augusta', '420', 'Consolacao', 'Sao Paulo', 'SP', '01304000'),
('11111111-1111-1111-1111-000000000003', 'AnimalSaude SP', '34567890000113', '1131000003', 'contato@animalsaude.com.br', 'R. Oscar Freire', '88', 'Jardins', 'Sao Paulo', 'SP', '01426001'),
('11111111-1111-1111-1111-000000000004', 'CliniPet Jardins', '45678901000124', '1131000004', 'contato@clinipet.com.br', 'Al. Santos', '200', 'Jardim Paulista', 'Sao Paulo', 'SP', '01419001'),
('11111111-1111-1111-1111-000000000005', 'Hospital Vet Ipiranga', '56789012000135', '1131000005', 'contato@hvipiranga.com.br', 'Av. Nazare', '1500', 'Ipiranga', 'Sao Paulo', 'SP', '04262001');

INSERT INTO tutor (id, nome, cpf, telefone, data_nascimento, genero, rua, numero, bairro, cidade, estado, cep, email) VALUES
('22222222-2222-2222-2222-000000000001', 'Lucas M. Santos', '11100011100', '11980000001', '1990-05-10', 'MASCULINO', 'R. Haddock Lobo', '595', 'Cerqueira Cesar', 'Sao Paulo', 'SP', '01414002', 'lucas.santos@email.com'),
('22222222-2222-2222-2222-000000000002', 'Maria Oliveira', '22200022200', '11970000002', '1985-08-22', 'FEMININO', 'R. Estados Unidos', '1000', 'Jardins', 'Sao Paulo', 'SP', '01427002', 'maria.oliveira@email.com'),
('22222222-2222-2222-2222-000000000003', 'Carlos Eduardo Lima', '33300033300', '11960000003', '1978-02-14', 'MASCULINO', 'R. Vergueiro', '2200', 'Vila Mariana', 'Sao Paulo', 'SP', '04101000', 'carlos.lima@email.com'),
('22222222-2222-2222-2222-000000000004', 'Ana Paula Ribeiro', '44400044400', '11950000004', '1995-11-30', 'FEMININO', 'Av. Ibirapuera', '300', 'Moema', 'Sao Paulo', 'SP', '04029000', 'ana.ribeiro@email.com'),
('22222222-2222-2222-2222-000000000005', 'Fernanda Souza', '55500055500', '11940000005', '1992-07-05', 'FEMININO', 'R. Domingos de Morais', '900', 'Vila Mariana', 'Sao Paulo', 'SP', '04010100', 'fernanda.souza@email.com');

INSERT INTO veterinario (id, nome, crmv, especialidade, email, cpf, telefone, genero, data_nascimento, clinica_id, rua, numero, bairro, cidade, estado, cep) VALUES
('33333333-3333-3333-3333-000000000001', 'Camila Ferreira', 'CRMV-SP 14320', 'Clinica Geral', 'camila.ferreira@vetcare.com.br', '11122233344', '11990010001', 'FEMININO', '1985-03-15', '11111111-1111-1111-1111-000000000001', 'Av. Paulista', '1500', 'Bela Vista', 'Sao Paulo', 'SP', '01310200'),
('33333333-3333-3333-3333-000000000002', 'Rafael Matos', 'CRMV-SP 18741', 'Cardiologia', 'rafael.matos@petmed.com.br', '22233344455', '11990010002', 'MASCULINO', '1980-07-22', '11111111-1111-1111-1111-000000000002', 'R. Augusta', '500', 'Consolacao', 'Sao Paulo', 'SP', '01305000'),
('33333333-3333-3333-3333-000000000003', 'Andre Costa', 'CRMV-SP 9812', 'Ortopedia', 'andre.costa@animalsaude.com.br', '33344455566', '11990010003', 'MASCULINO', '1978-11-05', '11111111-1111-1111-1111-000000000003', 'R. Oscar Freire', '90', 'Jardins', 'Sao Paulo', 'SP', '01426002'),
('33333333-3333-3333-3333-000000000004', 'Livia Rocha', 'CRMV-SP 16540', 'Dermatologia', 'livia.rocha@clinipet.com.br', '44455566677', '11990010004', 'FEMININO', '1990-09-18', '11111111-1111-1111-1111-000000000004', 'Al. Santos', '300', 'Jardim Paulista', 'Sao Paulo', 'SP', '01419002'),
('33333333-3333-3333-3333-000000000005', 'Tomas Oliveira', 'CRMV-SP 11204', 'Clinica Geral', 'tomas.oliveira@vetcare.com.br', '55566677788', '11990010005', 'MASCULINO', '1982-01-30', '11111111-1111-1111-1111-000000000001', 'Av. Paulista', '1200', 'Bela Vista', 'Sao Paulo', 'SP', '01310300'),
('33333333-3333-3333-3333-000000000006', 'Beatriz Lima', 'CRMV-SP 20333', 'Oncologia', 'beatriz.lima@petmed.com.br', '66677788899', '11990010006', 'FEMININO', '1992-06-14', '11111111-1111-1111-1111-000000000002', 'R. Augusta', '600', 'Consolacao', 'Sao Paulo', 'SP', '01305100'),
('33333333-3333-3333-3333-000000000007', 'Felipe Souza', 'CRMV-SP 25101', 'Nutricao Animal', 'felipe.souza@animalsaude.com.br', '77788899900', '11990010007', 'MASCULINO', '1995-04-09', '11111111-1111-1111-1111-000000000003', 'R. Oscar Freire', '100', 'Jardins', 'Sao Paulo', 'SP', '01426003');

INSERT INTO animal (id, nome, especie, raca, porte, cor, genero, data_nascimento, observacoes, tutor_id) VALUES
('44444444-4444-4444-4444-000000000001', 'Bolinha', 'CAO', 'Golden Retriever', 'GRANDE', 'Dourado', 'MACHO', '2022-03-12', 'Cachorro brincalhao e afetivo', '22222222-2222-2222-2222-000000000001'),
('44444444-4444-4444-4444-000000000002', 'Mimi', 'GATO', 'Siames', 'PEQUENO', 'Bege e marrom', 'FEMEA', '2021-07-05', 'Gata independente', '22222222-2222-2222-2222-000000000002'),
('44444444-4444-4444-4444-000000000003', 'Rex', 'CAO', 'Pastor Alemao', 'GRANDE', 'Preto e marrom', 'MACHO', '2020-01-18', 'Cao de guarda, obediente', '22222222-2222-2222-2222-000000000002'),
('44444444-4444-4444-4444-000000000004', 'Nina', 'GATO', 'Persa', 'PEQUENO', 'Branco', 'FEMEA', '2023-04-02', 'Precisa de escovacao frequente', '22222222-2222-2222-2222-000000000003'),
('44444444-4444-4444-4444-000000000005', 'Thor', 'CAO', 'Bulldog Frances', 'MEDIO', 'Cinza', 'MACHO', '2021-10-25', 'Historico de dermatite', '22222222-2222-2222-2222-000000000004'),
('44444444-4444-4444-4444-000000000006', 'Luna', 'CAO', 'Border Collie', 'MEDIO', 'Preto e branco', 'FEMEA', '2022-09-08', 'Muito ativa, precisa de exercicio diario', '22222222-2222-2222-2222-000000000005');

INSERT INTO evento_clinico (id, data_evento, hora_evento, tipo_evento, descricao, veterinario_id, animal_id, clinica_id) VALUES
('55555555-5555-5555-5555-000000000001', '2024-01-10', '09:00', 'CONSULTA', 'Check-up anual de rotina', '33333333-3333-3333-3333-000000000001', '44444444-4444-4444-4444-000000000001', '11111111-1111-1111-1111-000000000001'),
('55555555-5555-5555-5555-000000000002', '2024-02-15', '10:00', 'VACINA', 'V10 - Vacina polivalente anual', '33333333-3333-3333-3333-000000000001', '44444444-4444-4444-4444-000000000001', '11111111-1111-1111-1111-000000000001'),
('55555555-5555-5555-5555-000000000003', '2024-03-20', '14:00', 'EXAME', 'Hemograma completo e bioquimica', '33333333-3333-3333-3333-000000000005', '44444444-4444-4444-4444-000000000001', '11111111-1111-1111-1111-000000000001'),
('55555555-5555-5555-5555-000000000004', '2024-06-05', '11:00', 'RETORNO', 'Retorno pos-exame, resultados normais', '33333333-3333-3333-3333-000000000001', '44444444-4444-4444-4444-000000000001', '11111111-1111-1111-1111-000000000001'),
('55555555-5555-5555-5555-000000000005', '2024-09-10', '09:30', 'VACINA', 'Antirabica anual', '33333333-3333-3333-3333-000000000005', '44444444-4444-4444-4444-000000000001', '11111111-1111-1111-1111-000000000001'),
('55555555-5555-5555-5555-000000000006', '2026-12-15', '10:00', 'CONSULTA', 'Check-up e vermifugacao', '33333333-3333-3333-3333-000000000001', '44444444-4444-4444-4444-000000000001', '11111111-1111-1111-1111-000000000001'),
('55555555-5555-5555-5555-000000000007', '2024-02-20', '15:00', 'CONSULTA', 'Consulta de rotina', '33333333-3333-3333-3333-000000000004', '44444444-4444-4444-4444-000000000002', '11111111-1111-1111-1111-000000000004'),
('55555555-5555-5555-5555-000000000008', '2024-04-15', '16:00', 'VACINA', 'Vacina triplice felina', '33333333-3333-3333-3333-000000000004', '44444444-4444-4444-4444-000000000002', '11111111-1111-1111-1111-000000000004'),
('55555555-5555-5555-5555-000000000009', '2026-12-22', '14:00', 'EXAME', 'Exame de urina e sangue', '33333333-3333-3333-3333-000000000002', '44444444-4444-4444-4444-000000000002', '11111111-1111-1111-1111-000000000002'),
('55555555-5555-5555-5555-000000000010', '2024-03-08', '08:00', 'CIRURGIA', 'Cirurgia de castracao', '33333333-3333-3333-3333-000000000003', '44444444-4444-4444-4444-000000000003', '11111111-1111-1111-1111-000000000003'),
('55555555-5555-5555-5555-000000000011', '2024-03-25', '09:00', 'RETORNO', 'Retorno pos-cirurgico', '33333333-3333-3333-3333-000000000003', '44444444-4444-4444-4444-000000000003', '11111111-1111-1111-1111-000000000003');

INSERT INTO pagamento (id, metodo_pagamento, valor, status_pagamento, data_pagamento, descricao, evento_id) VALUES
('66666666-6666-6666-6666-000000000001', 'PIX', 150.00, 'PAGO', '2024-01-10', 'Consulta de rotina', '55555555-5555-5555-5555-000000000001'),
('66666666-6666-6666-6666-000000000002', 'CARTAO', 80.00, 'PAGO', '2024-02-15', 'Vacina V10', '55555555-5555-5555-5555-000000000002'),
('66666666-6666-6666-6666-000000000003', 'DINHEIRO', 200.00, 'PAGO', '2024-03-20', 'Hemograma e bioquimica', '55555555-5555-5555-5555-000000000003'),
('66666666-6666-6666-6666-000000000004', 'PIX', 120.00, 'PENDENTE', NULL, 'Retorno Bolinha', '55555555-5555-5555-5555-000000000004'),
('66666666-6666-6666-6666-000000000005', 'CARTAO', 100.00, 'PAGO', '2024-02-20', 'Consulta Mimi', '55555555-5555-5555-5555-000000000007'),
('66666666-6666-6666-6666-000000000006', 'PIX', 90.00, 'PENDENTE', NULL, 'Vacina felina Mimi', '55555555-5555-5555-5555-000000000008'),
('66666666-6666-6666-6666-000000000007', 'BOLETO', 800.00, 'PAGO', '2024-03-08', 'Cirurgia castracao Rex', '55555555-5555-5555-5555-000000000010'),
('66666666-6666-6666-6666-000000000008', 'PIX', 150.00, 'CANCELADO', NULL, 'Retorno cancelado', '55555555-5555-5555-5555-000000000011');

-- ============================================================
-- PARTE 2 — nossas tabelas (API .NET)
-- t_clyvo_produto e t_clyvo_sugestao_produto sao o CRUD desta entrega.
-- ============================================================

CREATE TABLE t_clyvo_produto (
    id                VARCHAR(36)   NOT NULL PRIMARY KEY COMMENT 'Identificador unico (UUID gerado pela API .NET)',
    nome              VARCHAR(200)  NOT NULL COMMENT 'Nome comercial do produto/servico',
    descricao         VARCHAR(1000) COMMENT 'Descricao detalhada do produto/servico',
    categoria         VARCHAR(30)   COMMENT 'RACAO, MEDICAMENTO, ACESSORIO, SERVICO ou OUTRO',
    preco             DECIMAL(10,2) COMMENT 'Preco unitario em reais (BRL)',
    especie_indicada  VARCHAR(30)   COMMENT 'Especie para a qual o produto e indicado',
    ativo             TINYINT       NOT NULL DEFAULT 1 COMMENT 'Se o produto esta disponivel pra venda/sugestao',
    criado_em         DATETIME      NOT NULL COMMENT 'Data/hora de criacao, gerada pela API .NET'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
  COMMENT='Catalogo de produtos e servicos veterinarios oferecidos - tabela CORE do CRUD desta entrega';

CREATE TABLE t_clyvo_sugestao_produto (
    id             VARCHAR(36)  NOT NULL PRIMARY KEY COMMENT 'Identificador unico (UUID gerado pela API .NET)',
    animal_id      VARCHAR(36)  NOT NULL COMMENT 'FK para animal - animal ao qual a sugestao se refere',
    produto_id     VARCHAR(36)  NOT NULL COMMENT 'FK para t_clyvo_produto - produto sugerido',
    justificativa  VARCHAR(500) COMMENT 'Motivo/justificativa da sugestao',
    data_sugestao  DATE         NOT NULL COMMENT 'Data em que a sugestao foi gerada',
    ativo          TINYINT      NOT NULL DEFAULT 1 COMMENT 'Se a sugestao ainda esta vigente',
    criado_em      DATETIME     NOT NULL COMMENT 'Data/hora de criacao, gerada pela API .NET',
    CONSTRAINT fk_sugestao_animal  FOREIGN KEY (animal_id)  REFERENCES animal(id),
    CONSTRAINT fk_sugestao_produto FOREIGN KEY (produto_id) REFERENCES t_clyvo_produto(id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
  COMMENT='Sugestao de produto gerada para um animal, relacionada a Produto por FK - tabela CORE do CRUD desta entrega';

CREATE INDEX idx_sugestao_produto_animal_id  ON t_clyvo_sugestao_produto(animal_id);
CREATE INDEX idx_sugestao_produto_produto_id ON t_clyvo_sugestao_produto(produto_id);

CREATE TABLE t_clyvo_lembrete (
    id            VARCHAR(36)   NOT NULL PRIMARY KEY,
    animal_id     VARCHAR(36)   NOT NULL,
    titulo        VARCHAR(200)  NOT NULL,
    descricao     VARCHAR(1000),
    tipo          VARCHAR(30)   NOT NULL,
    agendado_em   DATETIME      NOT NULL,
    recorrente    TINYINT       NOT NULL DEFAULT 0,
    status        VARCHAR(30)   NOT NULL,
    criado_em     DATETIME      NOT NULL,
    CONSTRAINT fk_lembrete_animal FOREIGN KEY (animal_id) REFERENCES animal(id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
  COMMENT='Lembretes de cuidados vinculados a um animal';

CREATE TABLE t_clyvo_evento_pet (
    id                VARCHAR(36)   NOT NULL PRIMARY KEY,
    titulo            VARCHAR(200)  NOT NULL,
    descricao         VARCHAR(1000),
    tipo              VARCHAR(30)   NOT NULL,
    rua               VARCHAR(300),
    numero            VARCHAR(10),
    bairro            VARCHAR(150),
    cidade            VARCHAR(100),
    estado            VARCHAR(10),
    cep               VARCHAR(10),
    data_inicio       DATE          NOT NULL,
    data_fim          DATE,
    especie_alvo      VARCHAR(30)   NOT NULL,
    organizador       VARCHAR(200),
    gratuito          TINYINT       NOT NULL DEFAULT 1,
    link_inscricao    VARCHAR(500),
    ativo             TINYINT       NOT NULL DEFAULT 1,
    criado_em         DATETIME      NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
  COMMENT='Eventos publicos para pets (feiras, vacinacao, workshops)';

CREATE TABLE t_clyvo_predisposicao_saude (
    id                VARCHAR(36)   NOT NULL PRIMARY KEY,
    especie           VARCHAR(30)   NOT NULL,
    raca              VARCHAR(100),
    idade_minima_anos DECIMAL(4,1),
    doenca            VARCHAR(200)  NOT NULL,
    recomendacao      VARCHAR(1000) NOT NULL,
    fonte_referencia  VARCHAR(300)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
  COMMENT='Catalogo de predisposicoes de saude por especie/raca/idade, usado pelo Widget de Saude Preditiva';

CREATE TABLE t_clyvo_tutor_telegram (
    id          VARCHAR(36) NOT NULL PRIMARY KEY,
    tutor_id    VARCHAR(36) NOT NULL,
    chat_id     BIGINT      NOT NULL,
    criado_em   DATETIME    NOT NULL,
    CONSTRAINT uk_tutor_telegram_tutor_id UNIQUE (tutor_id)
    -- sem FK pra tutor de proposito: tutor_id e validado via API, nao via constraint de banco
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
  COMMENT='Vinculo entre um tutor e seu chatId no bot do Telegram';

-- ---------- Seed nosso — massa de dados significativa (>= 2 linhas nas tabelas CORE) ----------

INSERT INTO t_clyvo_produto (id, nome, descricao, categoria, preco, especie_indicada, ativo, criado_em) VALUES
(UUID(), 'Ração Golden Fórmula Adulto 15kg',       'Ração premium para cães adultos de médio/grande porte.',        'RACAO',       189.90, 'CACHORRO', 1, NOW()),
(UUID(), 'Ração Whiskas Sachê Carne 85g',           'Ração úmida completa para gatos adultos.',                       'RACAO',        4.50,  'GATO',      1, NOW()),
(UUID(), 'Frontline Plus Antipulgas e Carrapatos',  'Antiparasitário tópico de amplo espectro, aplicação mensal.',    'MEDICAMENTO', 68.00,  'CACHORRO',  1, NOW()),
(UUID(), 'Consulta de Rotina Veterinária',          'Check-up clínico geral com veterinário credenciado.',            'SERVICO',     150.00, 'TODOS',     1, NOW()),
(UUID(), 'Coleira Antipulgas Seresto',              'Proteção contínua contra pulgas e carrapatos por até 8 meses.',  'ACESSORIO',   120.00, 'GATO',      1, NOW());

INSERT INTO t_clyvo_sugestao_produto (id, animal_id, produto_id, justificativa, data_sugestao, ativo, criado_em)
SELECT UUID(), '44444444-4444-4444-4444-000000000001', p.id, 'Ração indicada para o porte e a fase de vida do animal.', CURDATE(), 1, NOW()
FROM t_clyvo_produto p WHERE p.nome = 'Ração Golden Fórmula Adulto 15kg';

INSERT INTO t_clyvo_sugestao_produto (id, animal_id, produto_id, justificativa, data_sugestao, ativo, criado_em)
SELECT UUID(), '44444444-4444-4444-4444-000000000001', p.id, 'Antipulgas recomendado conforme sazonalidade e histórico de consultas.', CURDATE(), 1, NOW()
FROM t_clyvo_produto p WHERE p.nome = 'Frontline Plus Antipulgas e Carrapatos';

INSERT INTO t_clyvo_sugestao_produto (id, animal_id, produto_id, justificativa, data_sugestao, ativo, criado_em)
SELECT UUID(), '44444444-4444-4444-4444-000000000002', p.id, 'Sachê úmido indicado para hidratação e palatabilidade em gatos adultos.', CURDATE(), 1, NOW()
FROM t_clyvo_produto p WHERE p.nome = 'Ração Whiskas Sachê Carne 85g';

-- ---------- Conferência — contagem de linhas por tabela ----------

SELECT 'tutor'                     AS tabela, COUNT(*) AS total FROM tutor
UNION ALL SELECT 'animal',                    COUNT(*) FROM animal
UNION ALL SELECT 't_clyvo_produto',           COUNT(*) FROM t_clyvo_produto
UNION ALL SELECT 't_clyvo_sugestao_produto',  COUNT(*) FROM t_clyvo_sugestao_produto;
