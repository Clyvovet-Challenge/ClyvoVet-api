-- =============================================================
-- ClyvoVet — schema/02_seed_dotnet.sql
-- Dados de exemplo para as 4 tabelas .NET.
--
-- PRÉ-REQUISITO: execute 01_criar_tabelas_dotnet.sql primeiro.
--
-- Estratégia de isolamento:
--   BLOCO 1 — Produtos e Eventos Pet
--     Sem dependência de FK externa. Sempre executa e commita.
--
--   BLOCO 2 — Lembretes e Sugestões de Produto
--     Dependem de t_clyvo_animal (e indiretamente t_clyvo_tutor).
--     Só executa se existir um animal_id REAL na tabela Java.
--     Se a tabela não existir ou estiver vazia, pula com aviso
--     — os dados do Bloco 1 já foram commitados e não são perdidos.
--
-- Como rodar: abra no Oracle SQL Developer e pressione F5
-- =============================================================

SET SERVEROUTPUT ON;

-- =============================================================
PROMPT ============================================================
PROMPT BLOCO 1 — Produtos e Eventos Pet (sem FK Java)
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
    -- t_clyvo_produto — 5 produtos variados
    -- ----------------------------------------------------------
    DBMS_OUTPUT.PUT_LINE('');
    DBMS_OUTPUT.PUT_LINE('--- Inserindo produtos ---');

    v_prod1_id := fn_uuid();
    INSERT INTO t_clyvo_produto (id, nome, descricao, categoria, preco, especie_indicada, ativo)
    VALUES (
        v_prod1_id,
        'Racao Golden Adulto 15kg',
        'Racao premium para caes adultos de medio e grande porte, rica em proteinas e omega-3.',
        'RACAO', 189.90, 'CACHORRO', 1
    );
    v_ok := v_ok + 1;

    v_prod2_id := fn_uuid();
    INSERT INTO t_clyvo_produto (id, nome, descricao, categoria, preco, especie_indicada, ativo)
    VALUES (
        v_prod2_id,
        'Racao Whiskas Adulto Frango 3kg',
        'Racao completa para gatos adultos com sabor frango e vitaminas essenciais.',
        'RACAO', 42.50, 'GATO', 1
    );
    v_ok := v_ok + 1;

    v_prod3_id := fn_uuid();
    INSERT INTO t_clyvo_produto (id, nome, descricao, categoria, preco, especie_indicada, ativo)
    VALUES (
        v_prod3_id,
        'Frontline Plus Antipulgas 10-20kg',
        'Antiparasitario topico de amplo espectro contra pulgas, carrapatos e piolhos.',
        'MEDICAMENTO', 68.00, 'CACHORRO', 1
    );
    v_ok := v_ok + 1;

    v_prod4_id := fn_uuid();
    INSERT INTO t_clyvo_produto (id, nome, descricao, categoria, preco, especie_indicada, ativo)
    VALUES (
        v_prod4_id,
        'Coleira Seresto Antipulgas 8 Meses',
        'Coleira de longa duracao com protecao contra pulgas e carrapatos por ate 8 meses.',
        'ACESSORIO', 159.90, 'TODOS', 1
    );
    v_ok := v_ok + 1;

    v_prod5_id := fn_uuid();
    INSERT INTO t_clyvo_produto (id, nome, descricao, categoria, preco, especie_indicada, ativo)
    VALUES (
        v_prod5_id,
        'Consulta Veterinaria Clinica Geral',
        'Consulta veterinaria presencial para avaliacao de saude, vacinacao e orientacao.',
        'SERVICO', 120.00, 'TODOS', 1
    );
    v_ok := v_ok + 1;

    DBMS_OUTPUT.PUT_LINE('[OK] ' || v_ok || ' produtos inseridos.');
    v_ok := 0;

    -- ----------------------------------------------------------
    -- t_clyvo_evento_pet — 4 eventos futuros em cidades distintas
    -- ----------------------------------------------------------
    DBMS_OUTPUT.PUT_LINE('');
    DBMS_OUTPUT.PUT_LINE('--- Inserindo eventos pet ---');

    INSERT INTO t_clyvo_evento_pet (
        id, titulo, descricao, tipo,
        rua, numero, bairro, cidade, estado, cep,
        data_inicio, data_fim, especie_alvo,
        organizador, gratuito, link_inscricao, ativo
    ) VALUES (
        fn_uuid(),
        'Feira de Adocao Amigo Fiel',
        'Feira de adocao responsavel com caes e gatos. Microchipagem gratuita e orientacao sobre guarda responsavel.',
        'FEIRA',
        'Av. Paulista', '1578', 'Bela Vista', 'Sao Paulo', 'SP', '01310-200',
        TO_DATE('2026-06-14', 'YYYY-MM-DD'), TO_DATE('2026-06-15', 'YYYY-MM-DD'),
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
        'Campanha municipal de vacinacao antirabica. Traga o cartao de vacinacao do animal.',
        'VACINACAO',
        'Rua das Flores', '300', 'Centro', 'Campinas', 'SP', '13010-050',
        TO_DATE('2026-07-05', 'YYYY-MM-DD'), TO_DATE('2026-07-05', 'YYYY-MM-DD'),
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
        'Aprenda tecnicas de primeiros socorros para caes e gatos com veterinarios especializados. Vagas limitadas.',
        'WORKSHOP',
        'Rua Voluntarios da Patria', '82', 'Botafogo', 'Rio de Janeiro', 'RJ', '22270-010',
        TO_DATE('2026-08-23', 'YYYY-MM-DD'), TO_DATE('2026-08-23', 'YYYY-MM-DD'),
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
        'Castracao Solidaria — Agosto 2026',
        'Mutirao de castracao com preco social para tutores de baixa renda. Agendamento obrigatorio.',
        'CASTRACAO',
        'Av. Brasil', '2000', 'Penha', 'Sao Paulo', 'SP', '03614-000',
        TO_DATE('2026-08-01', 'YYYY-MM-DD'), TO_DATE('2026-08-31', 'YYYY-MM-DD'),
        'TODOS', 'Instituto Pet Solidario', 0,
        'https://petssolidario.org.br/castracao-2026', 1
    );
    v_ok := v_ok + 1;

    DBMS_OUTPUT.PUT_LINE('[OK] ' || v_ok || ' eventos inseridos.');

    -- Commita produtos e eventos independente do que acontecer depois
    COMMIT;
    DBMS_OUTPUT.PUT_LINE('[COMMIT] Produtos e Eventos Pet salvos.');

EXCEPTION
    WHEN OTHERS THEN
        ROLLBACK;
        DBMS_OUTPUT.PUT_LINE('[ERRO] ' || SQLERRM);
        DBMS_OUTPUT.PUT_LINE('Verifique se 01_criar_tabelas_dotnet.sql foi executado.');
        RAISE;
END;
/

-- =============================================================
PROMPT ============================================================
PROMPT BLOCO 2 — Lembretes e Sugestoes de Produto (requerem animal_id)
PROMPT ============================================================
-- =============================================================
-- Este bloco só insere dados se encontrar um animal_id REAL em
-- t_clyvo_animal. Se a tabela não existir ou estiver vazia,
-- exibe um aviso e encerra sem erros — os dados do Bloco 1
-- já foram commitados e estão seguros.

DECLARE
    v_animal_id VARCHAR2(36);
    v_prod1_id  VARCHAR2(36);
    v_prod3_id  VARCHAR2(36);
    v_ok        NUMBER := 0;

BEGIN
    -- ----------------------------------------------------------
    -- Tenta obter um animal_id real da tabela Java
    -- ----------------------------------------------------------
    BEGIN
        SELECT id INTO v_animal_id
        FROM   t_clyvo_animal
        WHERE  ROWNUM = 1;

        DBMS_OUTPUT.PUT_LINE('');
        DBMS_OUTPUT.PUT_LINE('[OK] animal_id encontrado: ' || v_animal_id);

    EXCEPTION
        WHEN NO_DATA_FOUND THEN
            DBMS_OUTPUT.PUT_LINE('');
            DBMS_OUTPUT.PUT_LINE('[AVISO] t_clyvo_animal esta vazia.');
            DBMS_OUTPUT.PUT_LINE('        Lembretes e Sugestoes NAO foram inseridos.');
            DBMS_OUTPUT.PUT_LINE('        Execute o script da API Java, insira um animal,');
            DBMS_OUTPUT.PUT_LINE('        e reexecute apenas este bloco (ou o seed completo).');
            RETURN; -- encerra o bloco sem erro

        WHEN OTHERS THEN
            DBMS_OUTPUT.PUT_LINE('');
            DBMS_OUTPUT.PUT_LINE('[AVISO] t_clyvo_animal nao encontrada (API Java ainda nao foi executada).');
            DBMS_OUTPUT.PUT_LINE('        Lembretes e Sugestoes NAO foram inseridos.');
            DBMS_OUTPUT.PUT_LINE('        Tabelas necessarias: t_clyvo_animal, t_clyvo_tutor.');
            RETURN; -- encerra o bloco sem erro
    END;

    -- ----------------------------------------------------------
    -- Busca IDs de produtos existentes para vincular nas sugestoes
    -- ----------------------------------------------------------
    BEGIN
        SELECT id INTO v_prod1_id
        FROM   t_clyvo_produto
        WHERE  categoria = 'RACAO' AND especie_indicada = 'CACHORRO'
        AND    ROWNUM = 1;
    EXCEPTION
        WHEN OTHERS THEN
            SELECT id INTO v_prod1_id FROM t_clyvo_produto WHERE ROWNUM = 1;
    END;

    BEGIN
        SELECT id INTO v_prod3_id
        FROM   t_clyvo_produto
        WHERE  categoria = 'MEDICAMENTO'
        AND    ROWNUM = 1;
    EXCEPTION
        WHEN OTHERS THEN
            v_prod3_id := v_prod1_id;
    END;

    -- ----------------------------------------------------------
    -- t_clyvo_lembrete — 3 lembretes (status sempre PENDENTE)
    -- ----------------------------------------------------------
    DBMS_OUTPUT.PUT_LINE('');
    DBMS_OUTPUT.PUT_LINE('--- Inserindo lembretes ---');

    INSERT INTO t_clyvo_lembrete (
        id, animal_id, titulo, descricao,
        tipo, agendado_em, recorrente, status
    ) VALUES (
        fn_uuid(), v_animal_id,
        'Vacina V10 — Reforco anual',
        'Aplicar vacina polivalente V10 para protecao contra as principais doencas caninas.',
        'VACINA',
        TO_TIMESTAMP('2026-06-20 09:00:00', 'YYYY-MM-DD HH24:MI:SS'),
        0, 'PENDENTE'
    );
    v_ok := v_ok + 1;

    INSERT INTO t_clyvo_lembrete (
        id, animal_id, titulo, descricao,
        tipo, agendado_em, recorrente, status
    ) VALUES (
        fn_uuid(), v_animal_id,
        'Vermifugacao trimestral',
        'Administrar vermifugo conforme orientacao veterinaria. Verificar peso atual do animal.',
        'MEDICAMENTO',
        TO_TIMESTAMP('2026-07-01 08:00:00', 'YYYY-MM-DD HH24:MI:SS'),
        1, 'PENDENTE'
    );
    v_ok := v_ok + 1;

    INSERT INTO t_clyvo_lembrete (
        id, animal_id, titulo, descricao,
        tipo, agendado_em, recorrente, status
    ) VALUES (
        fn_uuid(), v_animal_id,
        'Consulta de retorno — Dermatologia',
        'Retorno ao veterinario para avaliacao de dermatite atopica. Levar exames anteriores.',
        'CONSULTA',
        TO_TIMESTAMP('2026-06-10 14:30:00', 'YYYY-MM-DD HH24:MI:SS'),
        0, 'PENDENTE'
    );
    v_ok := v_ok + 1;

    DBMS_OUTPUT.PUT_LINE('[OK] ' || v_ok || ' lembretes inseridos.');
    v_ok := 0;

    -- ----------------------------------------------------------
    -- t_clyvo_sugestao_produto — 3 sugestões
    -- ----------------------------------------------------------
    DBMS_OUTPUT.PUT_LINE('');
    DBMS_OUTPUT.PUT_LINE('--- Inserindo sugestoes de produto ---');

    INSERT INTO t_clyvo_sugestao_produto (
        id, animal_id, produto_id,
        justificativa, data_sugestao, ativo
    ) VALUES (
        fn_uuid(), v_animal_id, v_prod3_id,
        'Animal apresentou infestacao de pulgas. Recomendado uso mensal de antipulgas topico.',
        SYSDATE, 1
    );
    v_ok := v_ok + 1;

    INSERT INTO t_clyvo_sugestao_produto (
        id, animal_id, produto_id,
        justificativa, data_sugestao, ativo
    ) VALUES (
        fn_uuid(), v_animal_id, v_prod1_id,
        'Veterinario indicou troca para racao premium por sensibilidade alimentar diagnosticada.',
        SYSDATE, 1
    );
    v_ok := v_ok + 1;

    INSERT INTO t_clyvo_sugestao_produto (
        id, animal_id, produto_id,
        justificativa, data_sugestao, ativo
    ) VALUES (
        fn_uuid(), v_animal_id, v_prod1_id,
        'Sugerido retorno para check-up anual completo com hemograma e urinanalise.',
        SYSDATE, 1
    );
    v_ok := v_ok + 1;

    DBMS_OUTPUT.PUT_LINE('[OK] ' || v_ok || ' sugestoes inseridas.');

    COMMIT;
    DBMS_OUTPUT.PUT_LINE('[COMMIT] Lembretes e Sugestoes salvos.');

EXCEPTION
    WHEN OTHERS THEN
        ROLLBACK;
        DBMS_OUTPUT.PUT_LINE('[ERRO] ' || SQLERRM);
        DBMS_OUTPUT.PUT_LINE('Os dados do Bloco 1 (Produtos/Eventos) foram preservados.');
        RAISE;
END;
/

-- Resumo final
PROMPT
PROMPT Contagem de registros por tabela:
SELECT 'T_CLYVO_PRODUTO'            AS tabela, COUNT(*) AS total FROM t_clyvo_produto
UNION ALL
SELECT 'T_CLYVO_EVENTO_PET',                   COUNT(*)          FROM t_clyvo_evento_pet
UNION ALL
SELECT 'T_CLYVO_LEMBRETE',                      COUNT(*)          FROM t_clyvo_lembrete
UNION ALL
SELECT 'T_CLYVO_SUGESTAO_PRODUTO',              COUNT(*)          FROM t_clyvo_sugestao_produto
ORDER BY 1;
