-- script_bd.sql - banco pra entrega de DevOps (Postgres na Azure)
--
-- esse banco é separado do Oracle que uso na disciplina de .NET, é só pra
-- essa entrega mesmo (opção 2: App Service + banco PaaS)
--
-- as tabelas do CRUD são t_clyvo_produto e t_clyvo_sugestao_produto (uma FK
-- pra outra). tutor e animal ficam só de apoio porque o EF Core faz um
-- Include no Animal toda vez que busca uma Sugestao, e Animal exige Tutor

DROP TABLE IF EXISTS t_clyvo_sugestao_produto CASCADE;
DROP TABLE IF EXISTS t_clyvo_produto           CASCADE;
DROP TABLE IF EXISTS t_clyvo_animal            CASCADE;
DROP TABLE IF EXISTS t_clyvo_tutor             CASCADE;

-- tutor (dono do animal, só de apoio)
CREATE TABLE t_clyvo_tutor (
    id          VARCHAR(36)  PRIMARY KEY DEFAULT gen_random_uuid()::text,
    nome        VARCHAR(150) NOT NULL,
    cpf         VARCHAR(11)  UNIQUE,
    email       VARCHAR(200) UNIQUE,
    telefone    VARCHAR(20),
    criado_em   TIMESTAMP    NOT NULL DEFAULT now()
);

COMMENT ON TABLE  t_clyvo_tutor            IS 'Tutor (dono) do animal — tabela de apoio mínima, necessária pela FK de t_clyvo_animal nesta entrega.';
COMMENT ON COLUMN t_clyvo_tutor.id         IS 'Identificador único (UUID gerado pelo PostgreSQL — equivalente ao trigger fn_clyvo_uuid() do Oracle).';
COMMENT ON COLUMN t_clyvo_tutor.nome       IS 'Nome completo do tutor.';
COMMENT ON COLUMN t_clyvo_tutor.cpf        IS 'CPF do tutor, sem máscara (somente dígitos), opcional e único.';
COMMENT ON COLUMN t_clyvo_tutor.email      IS 'E-mail de contato do tutor, único.';
COMMENT ON COLUMN t_clyvo_tutor.telefone   IS 'Telefone de contato do tutor (com DDD).';
COMMENT ON COLUMN t_clyvo_tutor.criado_em  IS 'Data/hora de criação do registro, preenchida automaticamente pelo banco.';

-- animal (também de apoio, usado no JOIN e na FK da sugestao)
CREATE TABLE t_clyvo_animal (
    id               VARCHAR(36)  PRIMARY KEY DEFAULT gen_random_uuid()::text,
    nome             VARCHAR(100) NOT NULL,
    especie          VARCHAR(50),
    raca             VARCHAR(100),
    data_nascimento  DATE,
    genero           VARCHAR(10)  CHECK (genero IN ('MACHO', 'FEMEA', 'DESCONHECIDO')),
    castrado         BOOLEAN      NOT NULL DEFAULT false,
    tutor_id         VARCHAR(36)  NOT NULL REFERENCES t_clyvo_tutor(id),
    criado_em        TIMESTAMP    NOT NULL DEFAULT now()
);

COMMENT ON TABLE  t_clyvo_animal                  IS 'Animal do tutor — tabela de apoio mínima, necessária pelo JOIN automático do EF Core (Include) em toda consulta de Sugestão de Produto.';
COMMENT ON COLUMN t_clyvo_animal.id               IS 'Identificador único (UUID gerado pelo PostgreSQL).';
COMMENT ON COLUMN t_clyvo_animal.especie           IS 'Espécie do animal (ex.: Cachorro, Gato).';
COMMENT ON COLUMN t_clyvo_animal.genero            IS 'Gênero do animal: MACHO, FEMEA ou DESCONHECIDO.';
COMMENT ON COLUMN t_clyvo_animal.castrado          IS 'Indica se o animal é castrado.';
COMMENT ON COLUMN t_clyvo_animal.tutor_id          IS 'FK para t_clyvo_tutor — dono do animal.';

-- produto - essa é uma das tabelas do CRUD
CREATE TABLE t_clyvo_produto (
    id                VARCHAR(36)  PRIMARY KEY DEFAULT gen_random_uuid()::text,
    nome              VARCHAR(200) NOT NULL,
    descricao         VARCHAR(1000),
    categoria         VARCHAR(30)  CHECK (categoria IN ('RACAO', 'MEDICAMENTO', 'ACESSORIO', 'SERVICO', 'OUTRO')),
    preco             NUMERIC(10,2),
    especie_indicada  VARCHAR(30)  CHECK (especie_indicada IN ('CACHORRO', 'GATO', 'PASSARO', 'REPTIL', 'ROEDOR', 'TODOS', 'OUTRO')),
    ativo             BOOLEAN      NOT NULL DEFAULT true,
    criado_em         TIMESTAMP    NOT NULL DEFAULT now()
);

COMMENT ON TABLE  t_clyvo_produto                    IS 'Catálogo de produtos e serviços veterinários oferecidos — tabela CORE do CRUD desta entrega.';
COMMENT ON COLUMN t_clyvo_produto.id                 IS 'Identificador único (UUID gerado pelo PostgreSQL — equivalente ao trigger fn_clyvo_uuid() do Oracle).';
COMMENT ON COLUMN t_clyvo_produto.nome               IS 'Nome comercial do produto/serviço.';
COMMENT ON COLUMN t_clyvo_produto.descricao          IS 'Descrição detalhada do produto/serviço.';
COMMENT ON COLUMN t_clyvo_produto.categoria          IS 'Categoria do produto: RACAO, MEDICAMENTO, ACESSORIO, SERVICO ou OUTRO.';
COMMENT ON COLUMN t_clyvo_produto.preco              IS 'Preço unitário em reais (BRL).';
COMMENT ON COLUMN t_clyvo_produto.especie_indicada   IS 'Espécie para a qual o produto é indicado.';
COMMENT ON COLUMN t_clyvo_produto.ativo              IS 'Indica se o produto está atualmente disponível para venda/sugestão.';
COMMENT ON COLUMN t_clyvo_produto.criado_em          IS 'Data/hora de criação do registro, preenchida automaticamente pelo banco.';

-- sugestao de produto - a outra tabela do CRUD, com FK pra produto
CREATE TABLE t_clyvo_sugestao_produto (
    id             VARCHAR(36) PRIMARY KEY DEFAULT gen_random_uuid()::text,
    animal_id      VARCHAR(36) NOT NULL REFERENCES t_clyvo_animal(id),
    produto_id     VARCHAR(36) NOT NULL REFERENCES t_clyvo_produto(id),
    justificativa  VARCHAR(500),
    data_sugestao  DATE        NOT NULL DEFAULT CURRENT_DATE,
    ativo          BOOLEAN     NOT NULL DEFAULT true,
    criado_em      TIMESTAMP   NOT NULL DEFAULT now()
);

COMMENT ON TABLE  t_clyvo_sugestao_produto                   IS 'Sugestão de produto gerada para um animal específico, relacionada a Produto por FK — tabela CORE do CRUD desta entrega.';
COMMENT ON COLUMN t_clyvo_sugestao_produto.id                IS 'Identificador único (UUID gerado pelo PostgreSQL).';
COMMENT ON COLUMN t_clyvo_sugestao_produto.animal_id         IS 'FK para t_clyvo_animal — animal ao qual a sugestão se refere.';
COMMENT ON COLUMN t_clyvo_sugestao_produto.produto_id        IS 'FK para t_clyvo_produto — produto sugerido.';
COMMENT ON COLUMN t_clyvo_sugestao_produto.justificativa     IS 'Motivo/justificativa da sugestão.';
COMMENT ON COLUMN t_clyvo_sugestao_produto.data_sugestao     IS 'Data em que a sugestão foi gerada.';
COMMENT ON COLUMN t_clyvo_sugestao_produto.ativo             IS 'Indica se a sugestão ainda está vigente.';
COMMENT ON COLUMN t_clyvo_sugestao_produto.criado_em         IS 'Data/hora de criação do registro, preenchida automaticamente pelo banco.';

CREATE INDEX idx_sugestao_produto_animal_id  ON t_clyvo_sugestao_produto(animal_id);
CREATE INDEX idx_sugestao_produto_produto_id ON t_clyvo_sugestao_produto(produto_id);

-- seed com dados de verdade (mais de 2 linhas nas tabelas do CRUD)

INSERT INTO t_clyvo_tutor (nome, cpf, email, telefone)
VALUES ('Mariana Costa Ferreira', '52998877665', 'mariana.ferreira@email.com', '11987654321');

INSERT INTO t_clyvo_animal (nome, especie, raca, data_nascimento, genero, castrado, tutor_id)
SELECT 'Rex', 'Cachorro', 'Labrador Retriever', DATE '2021-03-15', 'MACHO', true, id
FROM t_clyvo_tutor WHERE cpf = '52998877665';

INSERT INTO t_clyvo_animal (nome, especie, raca, data_nascimento, genero, castrado, tutor_id)
SELECT 'Mimi', 'Gato', 'Siamês', DATE '2022-07-02', 'FEMEA', true, id
FROM t_clyvo_tutor WHERE cpf = '52998877665';

INSERT INTO t_clyvo_produto (nome, descricao, categoria, preco, especie_indicada, ativo) VALUES
('Ração Golden Fórmula Adulto 15kg',        'Ração premium para cães adultos de médio/grande porte.',        'RACAO',       189.90, 'CACHORRO', true),
('Ração Whiskas Sachê Carne 85g',           'Ração úmida completa para gatos adultos.',                       'RACAO',        4.50,  'GATO',      true),
('Frontline Plus Antipulgas e Carrapatos',  'Antiparasitário tópico de amplo espectro, aplicação mensal.',    'MEDICAMENTO', 68.00,  'CACHORRO',  true),
('Consulta de Rotina Veterinária',          'Check-up clínico geral com veterinário credenciado.',            'SERVICO',     150.00, 'TODOS',     true),
('Coleira Antipulgas Seresto',              'Proteção contínua contra pulgas e carrapatos por até 8 meses.',  'ACESSORIO',   120.00, 'GATO',      true);

INSERT INTO t_clyvo_sugestao_produto (animal_id, produto_id, justificativa, data_sugestao)
SELECT a.id, p.id, 'Ração indicada para o porte e a fase de vida do animal.', CURRENT_DATE
FROM t_clyvo_animal a, t_clyvo_produto p
WHERE a.nome = 'Rex' AND p.nome = 'Ração Golden Fórmula Adulto 15kg';

INSERT INTO t_clyvo_sugestao_produto (animal_id, produto_id, justificativa, data_sugestao)
SELECT a.id, p.id, 'Antipulgas recomendado conforme sazonalidade e histórico de consultas.', CURRENT_DATE
FROM t_clyvo_animal a, t_clyvo_produto p
WHERE a.nome = 'Rex' AND p.nome = 'Frontline Plus Antipulgas e Carrapatos';

INSERT INTO t_clyvo_sugestao_produto (animal_id, produto_id, justificativa, data_sugestao)
SELECT a.id, p.id, 'Sachê úmido indicado para hidratação e palatabilidade em gatos adultos.', CURRENT_DATE
FROM t_clyvo_animal a, t_clyvo_produto p
WHERE a.nome = 'Mimi' AND p.nome = 'Ração Whiskas Sachê Carne 85g';

-- só pra conferir quantas linhas ficaram em cada tabela
SELECT 't_clyvo_tutor'             AS tabela, COUNT(*) AS total FROM t_clyvo_tutor
UNION ALL
SELECT 't_clyvo_animal',                     COUNT(*)          FROM t_clyvo_animal
UNION ALL
SELECT 't_clyvo_produto',                    COUNT(*)          FROM t_clyvo_produto
UNION ALL
SELECT 't_clyvo_sugestao_produto',           COUNT(*)          FROM t_clyvo_sugestao_produto;
