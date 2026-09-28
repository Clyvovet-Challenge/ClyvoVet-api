-- =============================================================
-- ClyvoVet — schema/05_seed_predisposicao_dotnet.sql
-- Dados de predisposição de saúde por espécie/raça e idade,
-- usados pelo Widget de Saúde Preditiva.
--
-- PRÉ-REQUISITO: execute 04_criar_tabela_predisposicao_dotnet.sql
-- primeiro.
--
-- As condições listadas são baseadas em literatura veterinária
-- consolidada; onde há um estudo específico consultado (ex.:
-- VetCompass/RVC), a fonte é citada na coluna fonte_referencia.
--
-- Como rodar: Oracle SQL Developer → F5 (Run Script)
-- =============================================================

SET SERVEROUTPUT ON;

PROMPT ============================================================
PROMPT Semeando predisposicoes de saude (Widget de Saude Preditiva)
PROMPT ============================================================

-- ----------------------------------------------------------
-- CACHORRO (por raça)
-- ----------------------------------------------------------
INSERT INTO t_clyvo_predisposicao_saude (especie, raca, idade_minima_anos, doenca, recomendacao, fonte_referencia)
VALUES ('CACHORRO', 'Labrador', 6, 'Displasia de quadril', 'Agendar avaliacao ortopedica e considerar suplementacao articular preventiva.', 'VetCompass (RVC) - Labrador Retrievers under primary veterinary care in the UK');

INSERT INTO t_clyvo_predisposicao_saude (especie, raca, idade_minima_anos, doenca, recomendacao, fonte_referencia)
VALUES ('CACHORRO', 'Labrador', 7, 'Obesidade', 'Reavaliar dieta e nivel de atividade fisica; agendar checkup nutricional.', 'VetCompass (RVC) - Labrador Retrievers under primary veterinary care in the UK');

INSERT INTO t_clyvo_predisposicao_saude (especie, raca, idade_minima_anos, doenca, recomendacao, fonte_referencia)
VALUES ('CACHORRO', 'Dachshund', 3, 'Doenca de disco intervertebral (hernia)', 'Evitar impacto/escadas e agendar avaliacao neurologica se houver dor ou dificuldade de locomocao.', 'Conhecimento veterinario consolidado');

INSERT INTO t_clyvo_predisposicao_saude (especie, raca, idade_minima_anos, doenca, recomendacao, fonte_referencia)
VALUES ('CACHORRO', 'Bulldog Frances', 0, 'Sindrome respiratoria braquicefalica', 'Evitar exercicio intenso e calor; agendar avaliacao respiratoria com veterinario.', 'Conhecimento veterinario consolidado');

INSERT INTO t_clyvo_predisposicao_saude (especie, raca, idade_minima_anos, doenca, recomendacao, fonte_referencia)
VALUES ('CACHORRO', 'Bulldog Ingles', 0, 'Sindrome respiratoria braquicefalica', 'Evitar exercicio intenso e calor; agendar avaliacao respiratoria com veterinario.', 'Conhecimento veterinario consolidado');

INSERT INTO t_clyvo_predisposicao_saude (especie, raca, idade_minima_anos, doenca, recomendacao, fonte_referencia)
VALUES ('CACHORRO', 'Pastor Alemao', 7, 'Displasia de quadril', 'Agendar avaliacao ortopedica preventiva.', 'Conhecimento veterinario consolidado');

INSERT INTO t_clyvo_predisposicao_saude (especie, raca, idade_minima_anos, doenca, recomendacao, fonte_referencia)
VALUES ('CACHORRO', 'Pastor Alemao', 8, 'Mielopatia degenerativa', 'Monitorar fraqueza em membros posteriores; agendar avaliacao neurologica.', 'Conhecimento veterinario consolidado');

INSERT INTO t_clyvo_predisposicao_saude (especie, raca, idade_minima_anos, doenca, recomendacao, fonte_referencia)
VALUES ('CACHORRO', 'Yorkshire', 2, 'Luxacao de patela', 'Agendar avaliacao ortopedica se houver claudicacao intermitente.', 'Conhecimento veterinario consolidado');

INSERT INTO t_clyvo_predisposicao_saude (especie, raca, idade_minima_anos, doenca, recomendacao, fonte_referencia)
VALUES ('CACHORRO', 'Poodle', 4, 'Atrofia progressiva de retina', 'Agendar avaliacao oftalmologica preventiva anual.', 'Conhecimento veterinario consolidado');

INSERT INTO t_clyvo_predisposicao_saude (especie, raca, idade_minima_anos, doenca, recomendacao, fonte_referencia)
VALUES ('CACHORRO', 'Golden Retriever', 8, 'Predisposicao a neoplasias (linfoma, hemangiossarcoma)', 'Agendar checkup geral com exames de rotina a partir dessa idade.', 'Conhecimento veterinario consolidado');

INSERT INTO t_clyvo_predisposicao_saude (especie, raca, idade_minima_anos, doenca, recomendacao, fonte_referencia)
VALUES ('CACHORRO', 'Rottweiler', 5, 'Displasia de cotovelo', 'Agendar avaliacao ortopedica se houver claudicacao.', 'Conhecimento veterinario consolidado');

INSERT INTO t_clyvo_predisposicao_saude (especie, raca, idade_minima_anos, doenca, recomendacao, fonte_referencia)
VALUES ('CACHORRO', 'Chihuahua', 5, 'Colapso de traqueia', 'Evitar coleira (preferir peitoral) e agendar avaliacao respiratoria se houver tosse seca persistente.', 'Conhecimento veterinario consolidado');

INSERT INTO t_clyvo_predisposicao_saude (especie, raca, idade_minima_anos, doenca, recomendacao, fonte_referencia)
VALUES ('CACHORRO', 'Cavalier King Charles Spaniel', 4, 'Doenca valvar mitral', 'Agendar avaliacao cardiologica preventiva (ausculta/ecocardiograma).', 'VetCompass (RVC) - Disorders in Cavalier King Charles Spaniels attending primary-care practices in England');

INSERT INTO t_clyvo_predisposicao_saude (especie, raca, idade_minima_anos, doenca, recomendacao, fonte_referencia)
VALUES ('CACHORRO', 'Pug', 0, 'Sindrome respiratoria braquicefalica e dermatite de dobras cutaneas', 'Higienizar dobras de pele regularmente e evitar exercicio intenso em dias quentes.', 'VetCompass (RVC) - Health of Pug dogs in the UK: disorder predispositions and protections');

-- ----------------------------------------------------------
-- GATO (por raça)
-- ----------------------------------------------------------
INSERT INTO t_clyvo_predisposicao_saude (especie, raca, idade_minima_anos, doenca, recomendacao, fonte_referencia)
VALUES ('GATO', 'Persa', 3, 'Doenca renal policistica (PKD)', 'Agendar ultrassonografia renal preventiva.', 'Conhecimento veterinario consolidado');

INSERT INTO t_clyvo_predisposicao_saude (especie, raca, idade_minima_anos, doenca, recomendacao, fonte_referencia)
VALUES ('GATO', 'Persa', 0, 'Sindrome respiratoria braquicefalica', 'Monitorar respiracao ruidosa; evitar calor excessivo.', 'Conhecimento veterinario consolidado');

INSERT INTO t_clyvo_predisposicao_saude (especie, raca, idade_minima_anos, doenca, recomendacao, fonte_referencia)
VALUES ('GATO', 'Siames', 2, 'Cardiomiopatia hipertrofica', 'Agendar avaliacao cardiologica preventiva.', 'Conhecimento veterinario consolidado');

INSERT INTO t_clyvo_predisposicao_saude (especie, raca, idade_minima_anos, doenca, recomendacao, fonte_referencia)
VALUES ('GATO', 'Maine Coon', 2, 'Cardiomiopatia hipertrofica', 'Agendar avaliacao cardiologica preventiva (raca com predisposicao genetica conhecida).', 'Conhecimento veterinario consolidado');

INSERT INTO t_clyvo_predisposicao_saude (especie, raca, idade_minima_anos, doenca, recomendacao, fonte_referencia)
VALUES ('GATO', 'SRD', 7, 'Obesidade e diabetes mellitus', 'Reavaliar dieta e agendar exame de glicemia preventivo.', 'Conhecimento veterinario consolidado');

-- ----------------------------------------------------------
-- PASSARO (geral, sem raca especifica)
-- ----------------------------------------------------------
INSERT INTO t_clyvo_predisposicao_saude (especie, raca, idade_minima_anos, doenca, recomendacao, fonte_referencia)
VALUES ('PASSARO', 'Calopsita', 0, 'Deficiencia de calcio', 'Revisar dieta (suplementacao de calcio e exposicao a luz UV adequada).', 'Conhecimento veterinario consolidado');

INSERT INTO t_clyvo_predisposicao_saude (especie, raca, idade_minima_anos, doenca, recomendacao, fonte_referencia)
VALUES ('PASSARO', 'Periquito', 0, 'Deficiencia de calcio', 'Revisar dieta (suplementacao de calcio e exposicao a luz UV adequada).', 'Conhecimento veterinario consolidado');

INSERT INTO t_clyvo_predisposicao_saude (especie, raca, idade_minima_anos, doenca, recomendacao, fonte_referencia)
VALUES ('PASSARO', 'Papagaio', 0, 'Doenca respiratoria por ma ventilacao (aspergilose)', 'Melhorar ventilacao do ambiente e agendar avaliacao respiratoria se houver espirros/secrecao.', 'Conhecimento veterinario consolidado');

INSERT INTO t_clyvo_predisposicao_saude (especie, raca, idade_minima_anos, doenca, recomendacao, fonte_referencia)
VALUES ('PASSARO', 'Calopsita', 5, 'Tumores (lipoma, tumor renal)', 'Agendar checkup geral a partir dessa idade.', 'Conhecimento veterinario consolidado');

-- ----------------------------------------------------------
-- REPTIL (geral)
-- ----------------------------------------------------------
INSERT INTO t_clyvo_predisposicao_saude (especie, raca, idade_minima_anos, doenca, recomendacao, fonte_referencia)
VALUES ('REPTIL', 'Tartaruga', 0, 'Doenca ossea metabolica (deficit de UV/calcio)', 'Revisar exposicao a luz UVB e suplementacao de calcio.', 'Conhecimento veterinario consolidado');

INSERT INTO t_clyvo_predisposicao_saude (especie, raca, idade_minima_anos, doenca, recomendacao, fonte_referencia)
VALUES ('REPTIL', 'Iguana', 0, 'Doenca ossea metabolica (deficit de UV/calcio)', 'Revisar exposicao a luz UVB e suplementacao de calcio.', 'Conhecimento veterinario consolidado');

INSERT INTO t_clyvo_predisposicao_saude (especie, raca, idade_minima_anos, doenca, recomendacao, fonte_referencia)
VALUES ('REPTIL', 'Jabuti', 0, 'Infeccao respiratoria por temperatura inadequada', 'Revisar temperatura e umidade do terrario; agendar avaliacao se houver secrecao nasal.', 'Conhecimento veterinario consolidado');

-- ----------------------------------------------------------
-- ROEDOR (geral)
-- ----------------------------------------------------------
INSERT INTO t_clyvo_predisposicao_saude (especie, raca, idade_minima_anos, doenca, recomendacao, fonte_referencia)
VALUES ('ROEDOR', 'Coelho', 0, 'Estase gastrointestinal', 'Revisar dieta rica em fibras (feno) e agendar avaliacao se houver reducao de apetite.', 'Conhecimento veterinario consolidado');

INSERT INTO t_clyvo_predisposicao_saude (especie, raca, idade_minima_anos, doenca, recomendacao, fonte_referencia)
VALUES ('ROEDOR', 'Coelho', 0, 'Ma oclusao dentaria', 'Agendar avaliacao odontologica se houver dificuldade para se alimentar.', 'Conhecimento veterinario consolidado');

INSERT INTO t_clyvo_predisposicao_saude (especie, raca, idade_minima_anos, doenca, recomendacao, fonte_referencia)
VALUES ('ROEDOR', 'Hamster', 1.5, 'Tumor adrenal', 'Agendar checkup geral a partir dessa idade.', 'Conhecimento veterinario consolidado');

INSERT INTO t_clyvo_predisposicao_saude (especie, raca, idade_minima_anos, doenca, recomendacao, fonte_referencia)
VALUES ('ROEDOR', 'Porquinho-da-india', 0, 'Ma oclusao dentaria', 'Agendar avaliacao odontologica se houver dificuldade para se alimentar.', 'Conhecimento veterinario consolidado');

INSERT INTO t_clyvo_predisposicao_saude (especie, raca, idade_minima_anos, doenca, recomendacao, fonte_referencia)
VALUES ('ROEDOR', 'Chinchila', 0, 'Golpe de calor (sensibilidade termica)', 'Manter ambiente fresco e ventilado, evitar exposicao a temperaturas acima de 25 graus.', 'Conhecimento veterinario consolidado');

-- ----------------------------------------------------------
-- BOVINO (por raça e geral)
-- ----------------------------------------------------------
INSERT INTO t_clyvo_predisposicao_saude (especie, raca, idade_minima_anos, doenca, recomendacao, fonte_referencia)
VALUES ('BOVINO', 'Holandesa', 0, 'Deslocamento de abomaso', 'Monitorar animais no pos-parto imediato; agendar avaliacao veterinaria se houver reducao brusca de apetite.', 'Conhecimento veterinario consolidado');

INSERT INTO t_clyvo_predisposicao_saude (especie, raca, idade_minima_anos, doenca, recomendacao, fonte_referencia)
VALUES ('BOVINO', 'Holandesa', 0, 'Cetose', 'Monitorar animais em inicio de lactacao; ajustar manejo nutricional.', 'Conhecimento veterinario consolidado');

INSERT INTO t_clyvo_predisposicao_saude (especie, raca, idade_minima_anos, doenca, recomendacao, fonte_referencia)
VALUES ('BOVINO', 'Holandesa', 5, 'Febre do leite (hipocalcemia)', 'Monitorar animais mais velhos ao redor do parto; considerar suplementacao preventiva de calcio.', 'Conhecimento veterinario consolidado');

INSERT INTO t_clyvo_predisposicao_saude (especie, raca, idade_minima_anos, doenca, recomendacao, fonte_referencia)
VALUES ('BOVINO', 'Nelore', 0, 'Verminose gastrointestinal', 'Manter protocolo de vermifugacao em animais jovens.', 'Conhecimento veterinario consolidado');

INSERT INTO t_clyvo_predisposicao_saude (especie, raca, idade_minima_anos, doenca, recomendacao, fonte_referencia)
VALUES ('BOVINO', NULL, 0, 'Mastite', 'Reforcar higiene da ordenha em femeas em lactacao.', 'Conhecimento veterinario consolidado');

INSERT INTO t_clyvo_predisposicao_saude (especie, raca, idade_minima_anos, doenca, recomendacao, fonte_referencia)
VALUES ('BOVINO', NULL, 0, 'Laminite', 'Revisar dieta com excesso de graos; agendar avaliacao podal.', 'Conhecimento veterinario consolidado');

-- ----------------------------------------------------------
-- EQUINO (por raça e geral)
-- ----------------------------------------------------------
INSERT INTO t_clyvo_predisposicao_saude (especie, raca, idade_minima_anos, doenca, recomendacao, fonte_referencia)
VALUES ('EQUINO', 'Mangalarga Marchador', 0, 'Colica', 'Manter rotina de alimentacao regular e acesso continuo a agua; agendar avaliacao imediata em caso de sinais de dor abdominal.', 'Conhecimento veterinario consolidado');

INSERT INTO t_clyvo_predisposicao_saude (especie, raca, idade_minima_anos, doenca, recomendacao, fonte_referencia)
VALUES ('EQUINO', 'Quarto de Milha', 0, 'Laminite', 'Revisar dieta rica em graos/pastagem e controlar peso corporal.', 'Conhecimento veterinario consolidado');

INSERT INTO t_clyvo_predisposicao_saude (especie, raca, idade_minima_anos, doenca, recomendacao, fonte_referencia)
VALUES ('EQUINO', NULL, 15, 'Sindrome de Cushing equino (PPID)', 'Agendar avaliacao hormonal preventiva em cavalos idosos.', 'Conhecimento veterinario consolidado');

INSERT INTO t_clyvo_predisposicao_saude (especie, raca, idade_minima_anos, doenca, recomendacao, fonte_referencia)
VALUES ('EQUINO', NULL, 15, 'Osteoartrite / doenca articular degenerativa', 'Agendar avaliacao ortopedica se houver claudicacao ou rigidez.', 'Conhecimento veterinario consolidado');

INSERT INTO t_clyvo_predisposicao_saude (especie, raca, idade_minima_anos, doenca, recomendacao, fonte_referencia)
VALUES ('EQUINO', NULL, 0, 'RAO / obstrucao recorrente das vias aereas ("asma equina")', 'Revisar qualidade do feno/estabulo (poeira e mofo) e ventilacao do ambiente.', 'Conhecimento veterinario consolidado');

COMMIT;

PROMPT ============================================================
PROMPT Resumo por especie:
PROMPT ============================================================

SELECT especie, COUNT(*) AS total
FROM   t_clyvo_predisposicao_saude
GROUP BY especie
ORDER BY especie;

PROMPT ============================================================
PROMPT Concluido.
PROMPT ============================================================
