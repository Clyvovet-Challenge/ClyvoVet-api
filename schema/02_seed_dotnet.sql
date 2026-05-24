-- =============================================================
-- ClyvoVet — schema/02_seed_dotnet.sql
-- Dados de exemplo para as 4 tabelas .NET.
--
-- PRÉ-REQUISITO: execute 01_criar_tabelas_dotnet.sql primeiro.
--
-- Estratégia para animal_id (tabela Java):
--   O bloco tenta buscar um ID real de t_clyvo_animal.
--   Se a tabela não existir ou estiver vazia, usa um UUID
--   placeholder (00000000-0000-0000-0000-000000000001) e exibe
--   um aviso. Nesse caso, os endpoints de Lembrete e
--   SugestaoProduto retornarão erro 500 até que um animal
--   válido exista no banco.
--
-- Como rodar: abra no Oracle SQL Developer e pressione F5
-- =============================================================

SET SERVEROUTPUT ON;

PROMPT ============================================================
PROMPT Populando tabelas .NET com dados de exemplo...
PROMPT ============================================================

DECLARE
    -- IDs dos produtos (usados também nas sugestões)
    v_prod1_id  VARCHAR2(36);
    v_prod2_id  VARCHAR2(36);
    v_prod3_id  VARCHAR2(36);
    v_prod4_id  VARCHAR2(36);
    v_prod5_id  VARCHAR2(36);

    -- ID de animal vindo da tabela Java (ou placeholder)
    v_animal_id VARCHAR2(36);

    -- Contador de registros inseridos
    v_ok        NUMBER := 0;

BEGIN
    -- ==========================================================
    -- [1] t_clyvo_produto — 5 produtos variados
    --     Não depende de nenhuma outra tabela.
    -- ==========================================================
    DBMS_OUTPUT.PUT_LINE('');
    DBMS_OUTPUT.PUT_LINE('--- Inserindo produtos ---');

    v_prod1_id := fn_uuid();
    INSERT INTO t_clyvo_produto (id, nome, descricao, categoria, preco, especie_indicada, ativo)
    VALUES (
        v_prod1_id,
        'Racao Golden Adulto 15kg',
        'Racao premium para caes adultos de medio e grande porte, rica em proteinas e omega-3.',
        'RACAO',
        189.90,
        'CACHORRO',
        1
    );
    v_ok := v_ok + 1;

    v_prod2_id := fn_uuid();
    INSERT INTO t_clyvo_produto (id, nome, descricao, categoria, preco, especie_indicada, ativo)
    VALUES (
        v_prod2_id,
        'Racao Whiskas Adulto Frango 3kg',
        'Racao completa para gatos adultos com sabor frango e vitaminas essenciais.',
        'RACAO',
        42.50,
        'GATO',
        1
    );
    v_ok := v_ok + 1;

    v_prod3_id := fn_uuid();
    INSERT INTO t_clyvo_produto (id, nome, descricao, categoria, preco, especie_indicada, ativo)
    VALUES (
        v_prod3_id,
        'Frontline Plus Antipulgas Caes 10-20kg',
        'Antiparasitario topico de amplo espectro contra pulgas, carrapatos e piolhos.',
        'MEDICAMENTO',
        68.00,
        'CACHORRO',
        1
    );
    v_ok := v_ok + 1;

    v_prod4_id := fn_uuid();
    INSERT INTO t_clyvo_produto (id, nome, descricao, categoria, preco, especie_indicada, ativo)
    VALUES (
        v_prod4_id,
        'Coleira Antipulgas Seresto 8 Meses',
        'Coleira de longa duracao com protecao contra pulgas e carrapatos por ate 8 meses.',
        'ACESSORIO',
        159.90,
        'TODOS',
        1
    );
    v_ok := v_ok + 1;

    v_prod5_id := fn_uuid();
    INSERT INTO t_clyvo_produto (id, nome, descricao, categoria, preco, especie_indicada, ativo)
    VALUES (
        v_prod5_id,
        'Consulta Veterinaria Clinica Geral',
        'Consulta veterinaria presencial para avaliacao geral de saude, vacinacao e orientacao.',
        'SERVICO',
        120.00,
        'TODOS',
        1
    );
    v_ok := v_ok + 1;

    DBMS_OUTPUT.PUT_LINE('[OK] ' || v_ok || ' produtos inseridos.');

    -- ==========================================================
    -- [2] t_clyvo_evento_pet — 4 eventos futuros
    --     Não depende de nenhuma outra tabela.
    -- ==========================================================
    v_ok := 0;
    DBMS_OUTPUT.PUT_LINE('');
    DBMS_OUTPUT.PUT_LINE('--- Inserindo eventos pet ---');

    INSERT INTO t_clyvo_evento_pet (
        id, titulo, descricao, tipo,
        rua, numero, bairro, cidade, estado, cep,
        data_inicio, data_fim,
        especie_alvo, organizador, gratuito, link_inscricao, ativo
    ) VALUES (
        fn_uuid(),
        'Feira de Adocao Amigo Fiel',
        'Feira de adocao responsavel com caes e gatos para adocao, microchipagem gratuita e orientacao sobre guarda responsavel.',
        'FEIRA',
        'Av. Paulista', '1578', 'Bela Vista', 'Sao Paulo', 'SP', '01310-200',
        TO_DATE('2026-06-14', 'YYYY-MM-DD'),
        TO_DATE('2026-06-15', 'YYYY-MM-DD'),
        'TODOS', 'ONG Amigo Fiel', 1,
        'https://amigofiel.org.br/feira-junho-2026',
        1
    );
    v_ok := v_ok + 1;

    INSERT INTO t_clyvo_evento_pet (
        id, titulo, descricao, tipo,
        rua, numero, bairro, cidade, estado, cep,
        data_inicio, data_fim,
        especie_alvo, organizador, gratuito, link_inscricao, ativo
    ) VALUES (
        fn_uuid(),
        'Vacinacao Antirabica Gratuita 2026',
        'Campanha municipal de vacinacao antirabica para caes e gatos. Traga o cartao de vacinacao do animal.',
        'VACINACAO',
        'Rua das Flores', '300', 'Centro', 'Campinas', 'SP', '13010-050',
        TO_DATE('2026-07-05', 'YYYY-MM-DD'),
        TO_DATE('2026-07-05', 'YYYY-MM-DD'),
        'TODOS', 'Prefeitura de Campinas', 1,
        NULL,
        1
    );
    v_ok := v_ok + 1;

    INSERT INTO t_clyvo_evento_pet (
        id, titulo, descricao, tipo,
        rua, numero, bairro, cidade, estado, cep,
        data_inicio, data_fim,
        especie_alvo, organizador, gratuito, link_inscricao, ativo
    ) VALUES (
        fn_uuid(),
        'Workshop: Primeiros Socorros para Pets',
        'Aprenda tecnicas de primeiros socorros para caes e gatos com veterinarios especializados. Vagas limitadas.',
        'WORKSHOP',
        'Rua Voluntarios da Patria', '82', 'Botafogo', 'Rio de Janeiro', 'RJ', '22270-010',
        TO_DATE('2026-08-23', 'YYYY-MM-DD'),
        TO_DATE('2026-08-23', 'YYYY-MM-DD'),
        'TODOS', 'Clinica VetCare RJ', 0,
        'https://vetcarerj.com.br/workshop-primeiros-socorros',
        1
    );
    v_ok := v_ok + 1;

    INSERT INTO t_clyvo_evento_pet (
        id, titulo, descricao, tipo,
        rua, numero, bairro, cidade, estado, cep,
        data_inicio, data_fim,
        especie_alvo, organizador, gratuito, link_inscricao, ativo
    ) VALUES (
        fn_uuid(),
        'Castracao Solidaria — Agosto 2026',
        'Mutirao de castracao com preco social para caes e gatos de tutores de baixa renda. Agendamento obrigatorio.',
        'CASTRACAO',
        'Av. Brasil', '2000', 'Penha', 'Sao Paulo', 'SP', '03614-000',
        TO_DATE('2026-08-01', 'YYYY-MM-DD'),
        TO_DATE('2026-08-31', 'YYYY-MM-DD'),
        'TODOS', 'Instituto Pet Solidario', 0,
        'https://petssolidario.org.br/castracao-2026',
        1
    );
    v_ok := v_ok + 1;

    DBMS_OUTPUT.PUT_LINE('[OK] ' || v_ok || ' eventos inseridos.');

    -- ==========================================================
    -- [3] Busca animal_id real da tabela Java (t_clyvo_animal).
    --     Se não existir ou estiver vazia, usa um placeholder.
    -- ==========================================================
    DBMS_OUTPUT.PUT_LINE('');
    DBMS_OUTPUT.PUT_LINE('--- Resolvendo animal_id ---');

    BEGIN
        SELECT id
        INTO   v_animal_id
        FROM   t_clyvo_animal
        WHERE  ROWNUM = 1;

        DBMS_OUTPUT.PUT_LINE('[OK] animal_id real encontrado: ' || v_animal_id);

    EXCEPTION
        WHEN NO_DATA_FOUND THEN
            v_animal_id := '00000000-0000-0000-0000-000000000001';
            DBMS_OUTPUT.PUT_LINE('');
            DBMS_OUTPUT.PUT_LINE('[AVISO] t_clyvo_animal esta vazia.');
            DBMS_OUTPUT.PUT_LINE('        Usando animal_id placeholder: ' || v_animal_id);
            DBMS_OUTPUT.PUT_LINE('        Endpoints de Lembrete e SugestaoProduto falharao com');
            DBMS_OUTPUT.PUT_LINE('        violacao de FK ate que um animal real seja inserido.');

        WHEN OTHERS THEN
            v_animal_id := '00000000-0000-0000-0000-000000000001';
            DBMS_OUTPUT.PUT_LINE('');
            DBMS_OUTPUT.PUT_LINE('[AVISO] t_clyvo_animal nao encontrada (API Java nao foi executada?).');
            DBMS_OUTPUT.PUT_LINE('        Usando animal_id placeholder: ' || v_animal_id);
            DBMS_OUTPUT.PUT_LINE('        Execute o script da API Java antes de testar Lembrete e Sugestao.');
    END;

    -- ==========================================================
    -- [4] t_clyvo_lembrete — 3 lembretes de cuidados
    --     Depende de animal_id (Java). Status sempre PENDENTE.
    -- ==========================================================
    v_ok := 0;
    DBMS_OUTPUT.PUT_LINE('');
    DBMS_OUTPUT.PUT_LINE('--- Inserindo lembretes ---');

    INSERT INTO t_clyvo_lembrete (
        id, animal_id, titulo, descricao,
        tipo, agendado_em, recorrente, status
    ) VALUES (
        fn_uuid(),
        v_animal_id,
        'Vacina V10 — Reforco anual',
        'Aplicar vacina polivalente V10 para protecao contra as principais doencas caninas.',
        'VACINA',
        TO_TIMESTAMP('2026-06-20 09:00:00', 'YYYY-MM-DD HH24:MI:SS'),
        0,
        'PENDENTE'
    );
    v_ok := v_ok + 1;

    INSERT INTO t_clyvo_lembrete (
        id, animal_id, titulo, descricao,
        tipo, agendado_em, recorrente, status
    ) VALUES (
        fn_uuid(),
        v_animal_id,
        'Vermifugacao trimestral',
        'Administrar vermifugo (Drontal Plus) conforme orientacao veterinaria. Peso atual deve ser verificado.',
        'MEDICAMENTO',
        TO_TIMESTAMP('2026-07-01 08:00:00', 'YYYY-MM-DD HH24:MI:SS'),
        1,
        'PENDENTE'
    );
    v_ok := v_ok + 1;

    INSERT INTO t_clyvo_lembrete (
        id, animal_id, titulo, descricao,
        tipo, agendado_em, recorrente, status
    ) VALUES (
        fn_uuid(),
        v_animal_id,
        'Consulta de retorno — Dermatologia',
        'Retorno ao Dr. Silva para avaliacao da dermatite atopica. Levar exames anteriores.',
        'CONSULTA',
        TO_TIMESTAMP('2026-06-10 14:30:00', 'YYYY-MM-DD HH24:MI:SS'),
        0,
        'PENDENTE'
    );
    v_ok := v_ok + 1;

    DBMS_OUTPUT.PUT_LINE('[OK] ' || v_ok || ' lembretes inseridos (animal_id: ' || v_animal_id || ').');

    -- ==========================================================
    -- [5] t_clyvo_sugestao_produto — 3 sugestões
    --     Depende de animal_id (Java) e produto_id (.NET).
    -- ==========================================================
    v_ok := 0;
    DBMS_OUTPUT.PUT_LINE('');
    DBMS_OUTPUT.PUT_LINE('--- Inserindo sugestoes de produto ---');

    INSERT INTO t_clyvo_sugestao_produto (
        id, animal_id, produto_id,
        justificativa, data_sugestao, ativo
    ) VALUES (
        fn_uuid(),
        v_animal_id,
        v_prod3_id,       -- Frontline Plus Antipulgas
        'Animal apresentou infestacao de pulgas na ultima consulta. Recomendado uso mensal de antipulgas topico.',
        SYSDATE,
        1
    );
    v_ok := v_ok + 1;

    INSERT INTO t_clyvo_sugestao_produto (
        id, animal_id, produto_id,
        justificativa, data_sugestao, ativo
    ) VALUES (
        fn_uuid(),
        v_animal_id,
        v_prod1_id,       -- Racao Golden Adulto
        'Veterinario indicou troca para racao premium devido a sensibilidade alimentar diagnosticada.',
        SYSDATE,
        1
    );
    v_ok := v_ok + 1;

    INSERT INTO t_clyvo_sugestao_produto (
        id, animal_id, produto_id,
        justificativa, data_sugestao, ativo
    ) VALUES (
        fn_uuid(),
        v_animal_id,
        v_prod5_id,       -- Consulta Veterinaria Clinica Geral
        'Sugerido retorno para check-up anual completo com hemograma e urinanalise.',
        SYSDATE,
        1
    );
    v_ok := v_ok + 1;

    DBMS_OUTPUT.PUT_LINE('[OK] ' || v_ok || ' sugestoes inseridas (animal_id: ' || v_animal_id || ').');

    -- ==========================================================
    COMMIT;
    -- ==========================================================

    DBMS_OUTPUT.PUT_LINE('');
    DBMS_OUTPUT.PUT_LINE('============================================================');
    DBMS_OUTPUT.PUT_LINE('Seed concluido com sucesso!');
    DBMS_OUTPUT.PUT_LINE('');
    DBMS_OUTPUT.PUT_LINE('Produtos e Eventos Pet: funcionam 100% (sem FK Java).');
    IF v_animal_id = '00000000-0000-0000-0000-000000000001' THEN
        DBMS_OUTPUT.PUT_LINE('Lembretes / Sugestoes: usam animal_id PLACEHOLDER.');
        DBMS_OUTPUT.PUT_LINE('  => Execute o script Java e repita o seed para FKs reais.');
    ELSE
        DBMS_OUTPUT.PUT_LINE('Lembretes / Sugestoes: animal_id real (' || v_animal_id || ').');
    END IF;
    DBMS_OUTPUT.PUT_LINE('============================================================');

EXCEPTION
    WHEN OTHERS THEN
        ROLLBACK;
        DBMS_OUTPUT.PUT_LINE('');
        DBMS_OUTPUT.PUT_LINE('[ERRO] ' || SQLERRM);
        DBMS_OUTPUT.PUT_LINE('Nenhum dado foi inserido (ROLLBACK aplicado).');
        DBMS_OUTPUT.PUT_LINE('Verifique se 01_criar_tabelas_dotnet.sql foi executado antes.');
        RAISE;
END;
/

-- Resumo final: contagem dos registros inseridos
PROMPT
PROMPT Contagem de registros por tabela:
SELECT 'T_CLYVO_PRODUTO'          AS tabela, COUNT(*) AS registros FROM t_clyvo_produto
UNION ALL
SELECT 'T_CLYVO_EVENTO_PET',                 COUNT(*)              FROM t_clyvo_evento_pet
UNION ALL
SELECT 'T_CLYVO_LEMBRETE',                   COUNT(*)              FROM t_clyvo_lembrete
UNION ALL
SELECT 'T_CLYVO_SUGESTAO_PRODUTO',           COUNT(*)              FROM t_clyvo_sugestao_produto
ORDER BY 1;
