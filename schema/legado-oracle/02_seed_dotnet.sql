-- =============================================================
-- ClyvoVet — schema/02_seed_dotnet.sql
-- Dados de exemplo para todas as tabelas necessárias à API .NET.
--
-- PRÉ-REQUISITO: execute 01_criar_tabelas_dotnet.sql primeiro.
--
-- BLOCOS:
--   Bloco 1 — Produtos + Eventos Pet (sem FK, sempre commita)
--   Bloco 2 — Tutor + Animal + Lembretes + Sugestões
--              Tenta encontrar animal existente.
--              Se t_clyvo_animal estiver vazia, insere um tutor
--              e um animal de seed para que TODOS os endpoints
--              da API possam ser testados sem depender de dados
--              externos.
--
-- Como rodar: Oracle SQL Developer → F5 (Run Script)
-- =============================================================

SET SERVEROUTPUT ON;

-- =============================================================
PROMPT ============================================================
PROMPT BLOCO 1 — Produtos e Eventos Pet (sem FK externa)
PROMPT ============================================================
-- =============================================================

DECLARE
    v_prod1_id  VARCHAR2(36);
    v_prod2_id  VARCHAR2(36);
    v_prod3_id  VARCHAR2(36);
    v_prod4_id  VARCHAR2(36);
    v_prod5_id  VARCHAR2(36);
    v_ok        NUMBER := 0;

BEGIN
    -- ----------------------------------------------------------
    -- t_clyvo_produto — 5 produtos (categoria e especie em maiúsculo
    -- conforme conversão do EF Core: v => v.ToString().ToUpper())
    -- ----------------------------------------------------------
    DBMS_OUTPUT.PUT_LINE('--- Inserindo produtos ---');

    v_prod1_id := fn_uuid();
    INSERT INTO t_clyvo_produto (id, nome, descricao, categoria, preco, especie_indicada, ativo)
    VALUES (v_prod1_id,
            'Racao Golden Adulto 15kg',
            'Racao premium para caes adultos de medio e grande porte, rica em proteinas e omega-3.',
            'RACAO', 189.90, 'CACHORRO', 1);
    v_ok := v_ok + 1;

    v_prod2_id := fn_uuid();
    INSERT INTO t_clyvo_produto (id, nome, descricao, categoria, preco, especie_indicada, ativo)
    VALUES (v_prod2_id,
            'Racao Whiskas Adulto Frango 3kg',
            'Racao completa para gatos adultos com sabor frango e vitaminas essenciais.',
            'RACAO', 42.50, 'GATO', 1);
    v_ok := v_ok + 1;

    v_prod3_id := fn_uuid();
    INSERT INTO t_clyvo_produto (id, nome, descricao, categoria, preco, especie_indicada, ativo)
    VALUES (v_prod3_id,
            'Frontline Plus Antipulgas 10-20kg',
            'Antiparasitario topico de amplo espectro contra pulgas, carrapatos e piolhos.',
            'MEDICAMENTO', 68.00, 'CACHORRO', 1);
    v_ok := v_ok + 1;

    v_prod4_id := fn_uuid();
    INSERT INTO t_clyvo_produto (id, nome, descricao, categoria, preco, especie_indicada, ativo)
    VALUES (v_prod4_id,
            'Coleira Seresto Antipulgas 8 Meses',
            'Coleira de longa duracao com protecao contra pulgas e carrapatos por ate 8 meses.',
            'ACESSORIO', 159.90, 'TODOS', 1);
    v_ok := v_ok + 1;

    v_prod5_id := fn_uuid();
    INSERT INTO t_clyvo_produto (id, nome, descricao, categoria, preco, especie_indicada, ativo)
    VALUES (v_prod5_id,
            'Consulta Veterinaria Clinica Geral',
            'Consulta veterinaria presencial para avaliacao de saude, vacinacao e orientacao.',
            'SERVICO', 120.00, 'TODOS', 1);
    v_ok := v_ok + 1;

    DBMS_OUTPUT.PUT_LINE('[OK] ' || v_ok || ' produtos inseridos.');
    v_ok := 0;

    -- ----------------------------------------------------------
    -- t_clyvo_evento_pet — 4 eventos futuros
    -- Colunas alinhadas ao DDL real: titulo VARCHAR2(200),
    -- numero VARCHAR2(10), bairro VARCHAR2(150), etc.
    -- ----------------------------------------------------------
    DBMS_OUTPUT.PUT_LINE('--- Inserindo eventos pet ---');

    INSERT INTO t_clyvo_evento_pet (
        id, titulo, descricao, tipo,
        rua, numero, bairro, cidade, estado, cep,
        data_inicio, data_fim, especie_alvo,
        organizador, gratuito, link_inscricao, ativo
    ) VALUES (
        fn_uuid(),
        'Feira de Adocao Amigo Fiel',
        'Feira de adocao responsavel com caes e gatos. Microchipagem gratuita.',
        'FEIRA',
        'Av. Paulista', '1578', 'Bela Vista', 'Sao Paulo', 'SP', '01310-200',
        TO_DATE('2026-06-14','YYYY-MM-DD'), TO_DATE('2026-06-15','YYYY-MM-DD'),
        'TODOS', 'ONG Amigo Fiel', 1,
        'https://amigofiel.org.br/feira-junho-2026', 1
    );
    v_ok := v_ok + 1;

    INSERT INTO t_clyvo_evento_pet (
        id, titulo, descricao, tipo,
        rua, numero, bairro, cidade, estado, cep,
        data_inicio, data_fim, especie_alvo,
        organizador, gratuito, link_inscricao, ativo
    ) VALUES (
        fn_uuid(),
        'Vacinacao Antirabica Gratuita 2026',
        'Campanha municipal. Traga o cartao de vacinacao do animal.',
        'VACINACAO',
        'Rua das Flores', '300', 'Centro', 'Campinas', 'SP', '13010-050',
        TO_DATE('2026-07-05','YYYY-MM-DD'), TO_DATE('2026-07-05','YYYY-MM-DD'),
        'TODOS', 'Prefeitura de Campinas', 1, NULL, 1
    );
    v_ok := v_ok + 1;

    INSERT INTO t_clyvo_evento_pet (
        id, titulo, descricao, tipo,
        rua, numero, bairro, cidade, estado, cep,
        data_inicio, data_fim, especie_alvo,
        organizador, gratuito, link_inscricao, ativo
    ) VALUES (
        fn_uuid(),
        'Workshop: Primeiros Socorros para Pets',
        'Tecnicas de primeiros socorros com veterinarios. Vagas limitadas.',
        'WORKSHOP',
        'Rua Voluntarios', '82', 'Botafogo', 'Rio de Janeiro', 'RJ', '22270-010',
        TO_DATE('2026-08-23','YYYY-MM-DD'), TO_DATE('2026-08-23','YYYY-MM-DD'),
        'TODOS', 'Clinica VetCare RJ', 0,
        'https://vetcarerj.com.br/workshop-2026', 1
    );
    v_ok := v_ok + 1;

    INSERT INTO t_clyvo_evento_pet (
        id, titulo, descricao, tipo,
        rua, numero, bairro, cidade, estado, cep,
        data_inicio, data_fim, especie_alvo,
        organizador, gratuito, link_inscricao, ativo
    ) VALUES (
        fn_uuid(),
        'Castracao Solidaria Agosto 2026',
        'Mutirao com preco social para tutores de baixa renda. Agendamento obrigatorio.',
        'CASTRACAO',
        'Av. Brasil', '2000', 'Penha', 'Sao Paulo', 'SP', '03614-000',
        TO_DATE('2026-08-01','YYYY-MM-DD'), TO_DATE('2026-08-31','YYYY-MM-DD'),
        'TODOS', 'Instituto Pet Solidario', 0,
        'https://petssolidario.org.br/castracao-2026', 1
    );
    v_ok := v_ok + 1;

    DBMS_OUTPUT.PUT_LINE('[OK] ' || v_ok || ' eventos inseridos.');

    COMMIT;
    DBMS_OUTPUT.PUT_LINE('[COMMIT] Bloco 1 salvo.');

EXCEPTION
    WHEN OTHERS THEN
        ROLLBACK;
        DBMS_OUTPUT.PUT_LINE('[ERRO] Bloco 1: ' || SQLERRM);
        DBMS_OUTPUT.PUT_LINE('Verifique se 01_criar_tabelas_dotnet.sql foi executado.');
        RAISE;
END;
/

-- =============================================================
PROMPT ============================================================
PROMPT BLOCO 2 — Tutor, Animal, Lembretes e Sugestoes
PROMPT ============================================================
-- Este bloco é autossuficiente:
--   1. Busca um animal existente em t_clyvo_animal
--   2. Se não encontrar, cria um tutor + animal de seed
--   3. Insere lembretes e sugestoes vinculados ao animal
-- =============================================================

DECLARE
    v_tutor_id  VARCHAR2(36);
    v_animal_id VARCHAR2(36);
    v_prod1_id  VARCHAR2(36);   -- racao cachorro
    v_prod3_id  VARCHAR2(36);   -- medicamento
    v_ok        NUMBER := 0;

BEGIN
    -- ----------------------------------------------------------
    -- Passo 1: localiza ou cria animal de seed
    -- ----------------------------------------------------------
    DBMS_OUTPUT.PUT_LINE('--- Resolvendo animal_id ---');

    BEGIN
        SELECT id INTO v_animal_id
        FROM   t_clyvo_animal
        WHERE  ROWNUM = 1;
        DBMS_OUTPUT.PUT_LINE('[OK] Animal existente: ' || v_animal_id);

    EXCEPTION
        WHEN NO_DATA_FOUND THEN
            DBMS_OUTPUT.PUT_LINE('[INFO] t_clyvo_animal vazia. Criando tutor e animal de seed...');

            -- Tutor de seed (CPF fixo para reexecucao segura)
            BEGIN
                SELECT id INTO v_tutor_id
                FROM   t_clyvo_tutor
                WHERE  cpf = '00000000000'
                AND    ROWNUM = 1;
                DBMS_OUTPUT.PUT_LINE('[OK] Tutor de seed ja existe: ' || v_tutor_id);
            EXCEPTION
                WHEN NO_DATA_FOUND THEN
                    v_tutor_id := fn_uuid();
                    INSERT INTO t_clyvo_tutor (
                        id, nome, cpf, email, telefone,
                        rua, numero, bairro, cidade, estado, cep
                    ) VALUES (
                        v_tutor_id,
                        'Tutor Seed ClyvoVet', '00000000000',
                        'seed@clyvovet.com', '11999990000',
                        'Rua Seed', '1', 'Centro', 'Sao Paulo', 'SP', '01310-100'
                    );
                    DBMS_OUTPUT.PUT_LINE('[OK] Tutor de seed criado: ' || v_tutor_id);
            END;

            -- Animal de seed
            v_animal_id := fn_uuid();
            INSERT INTO t_clyvo_animal (
                id, nome, especie, raca, genero,
                porte, castrado, tutor_id
            ) VALUES (
                v_animal_id,
                'Rex (Seed)', 'Cachorro', 'Labrador', 'MACHO',
                'MEDIO', 1, v_tutor_id
            );
            DBMS_OUTPUT.PUT_LINE('[OK] Animal de seed criado: ' || v_animal_id);

        WHEN OTHERS THEN
            DBMS_OUTPUT.PUT_LINE('[AVISO] Nao foi possivel acessar t_clyvo_animal: ' || SQLERRM);
            DBMS_OUTPUT.PUT_LINE('        Execute 01_criar_tabelas_dotnet.sql primeiro.');
            RETURN;
    END;

    -- ----------------------------------------------------------
    -- Passo 2: pega IDs de produtos para vincular nas sugestoes
    -- ----------------------------------------------------------
    BEGIN
        SELECT id INTO v_prod1_id FROM t_clyvo_produto
        WHERE  categoria = 'RACAO' AND especie_indicada = 'CACHORRO' AND ROWNUM = 1;
    EXCEPTION
        WHEN OTHERS THEN
            SELECT id INTO v_prod1_id FROM t_clyvo_produto WHERE ROWNUM = 1;
    END;

    BEGIN
        SELECT id INTO v_prod3_id FROM t_clyvo_produto
        WHERE  categoria = 'MEDICAMENTO' AND ROWNUM = 1;
    EXCEPTION
        WHEN OTHERS THEN
            v_prod3_id := v_prod1_id;
    END;

    -- ----------------------------------------------------------
    -- Passo 3: t_clyvo_lembrete — 3 lembretes
    -- status sempre PENDENTE (regra de negocio da API)
    -- agendado_em deve ser futuro (validado pelo LembreteService)
    -- ----------------------------------------------------------
    DBMS_OUTPUT.PUT_LINE('--- Inserindo lembretes ---');

    INSERT INTO t_clyvo_lembrete (
        id, animal_id, titulo, descricao,
        tipo, agendado_em, recorrente, status
    ) VALUES (
        fn_uuid(), v_animal_id,
        'Vacina V10 Reforco anual',
        'Aplicar vacina polivalente V10 para protecao contra doencas caninas.',
        'VACINA',
        TO_TIMESTAMP('2026-06-20 09:00:00','YYYY-MM-DD HH24:MI:SS'),
        0, 'PENDENTE'
    );
    v_ok := v_ok + 1;

    INSERT INTO t_clyvo_lembrete (
        id, animal_id, titulo, descricao,
        tipo, agendado_em, recorrente, status
    ) VALUES (
        fn_uuid(), v_animal_id,
        'Vermifugacao trimestral',
        'Administrar vermifugo conforme orientacao veterinaria.',
        'MEDICAMENTO',
        TO_TIMESTAMP('2026-07-01 08:00:00','YYYY-MM-DD HH24:MI:SS'),
        1, 'PENDENTE'
    );
    v_ok := v_ok + 1;

    INSERT INTO t_clyvo_lembrete (
        id, animal_id, titulo, descricao,
        tipo, agendado_em, recorrente, status
    ) VALUES (
        fn_uuid(), v_animal_id,
        'Retorno Dermatologia',
        'Avaliacao de dermatite atopica. Levar exames anteriores.',
        'CONSULTA',
        TO_TIMESTAMP('2026-06-10 14:30:00','YYYY-MM-DD HH24:MI:SS'),
        0, 'PENDENTE'
    );
    v_ok := v_ok + 1;

    DBMS_OUTPUT.PUT_LINE('[OK] ' || v_ok || ' lembretes inseridos.');
    v_ok := 0;

    -- ----------------------------------------------------------
    -- Passo 4: t_clyvo_sugestao_produto — 3 sugestoes
    -- justificativa limitada a 500 chars (DDL real)
    -- ----------------------------------------------------------
    DBMS_OUTPUT.PUT_LINE('--- Inserindo sugestoes de produto ---');

    INSERT INTO t_clyvo_sugestao_produto (
        id, animal_id, produto_id, justificativa, data_sugestao, ativo
    ) VALUES (
        fn_uuid(), v_animal_id, v_prod3_id,
        'Animal com infestacao de pulgas. Uso mensal de antipulgas topico recomendado.',
        SYSDATE, 1
    );
    v_ok := v_ok + 1;

    INSERT INTO t_clyvo_sugestao_produto (
        id, animal_id, produto_id, justificativa, data_sugestao, ativo
    ) VALUES (
        fn_uuid(), v_animal_id, v_prod1_id,
        'Troca para racao premium por sensibilidade alimentar diagnosticada.',
        SYSDATE, 1
    );
    v_ok := v_ok + 1;

    INSERT INTO t_clyvo_sugestao_produto (
        id, animal_id, produto_id, justificativa, data_sugestao, ativo
    ) VALUES (
        fn_uuid(), v_animal_id, v_prod1_id,
        'Check-up anual com hemograma e urinanalise recomendado pelo veterinario.',
        SYSDATE, 1
    );
    v_ok := v_ok + 1;

    DBMS_OUTPUT.PUT_LINE('[OK] ' || v_ok || ' sugestoes inseridas.');

    COMMIT;
    DBMS_OUTPUT.PUT_LINE('[COMMIT] Bloco 2 salvo.');

EXCEPTION
    WHEN OTHERS THEN
        ROLLBACK;
        DBMS_OUTPUT.PUT_LINE('[ERRO] Bloco 2: ' || SQLERRM);
        DBMS_OUTPUT.PUT_LINE('Dados do Bloco 1 (Produtos/Eventos) foram preservados.');
        RAISE;
END;
/

-- Resumo final
PROMPT
PROMPT Contagem de registros por tabela:
SELECT 'T_CLYVO_TUTOR'              AS tabela, COUNT(*) AS total FROM t_clyvo_tutor
UNION ALL
SELECT 'T_CLYVO_ANIMAL',                       COUNT(*)          FROM t_clyvo_animal
UNION ALL
SELECT 'T_CLYVO_PRODUTO',                      COUNT(*)          FROM t_clyvo_produto
UNION ALL
SELECT 'T_CLYVO_EVENTO_PET',                   COUNT(*)          FROM t_clyvo_evento_pet
UNION ALL
SELECT 'T_CLYVO_LEMBRETE',                      COUNT(*)          FROM t_clyvo_lembrete
UNION ALL
SELECT 'T_CLYVO_SUGESTAO_PRODUTO',              COUNT(*)          FROM t_clyvo_sugestao_produto
ORDER BY 1;
