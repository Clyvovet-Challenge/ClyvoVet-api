CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `ProductVersion` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
) CHARACTER SET=utf8mb4;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260927054703_Inicial') THEN

    ALTER DATABASE CHARACTER SET utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260927054703_Inicial') THEN

    CREATE TABLE `t_clyvo_evento_pet` (
        `id` VARCHAR(36) CHARACTER SET utf8mb4 NOT NULL,
        `titulo` VARCHAR(200) CHARACTER SET utf8mb4 NOT NULL,
        `descricao` VARCHAR(1000) CHARACTER SET utf8mb4 NULL,
        `tipo` VARCHAR(30) CHARACTER SET utf8mb4 NOT NULL,
        `rua` VARCHAR(300) CHARACTER SET utf8mb4 NULL,
        `numero` VARCHAR(10) CHARACTER SET utf8mb4 NULL,
        `bairro` VARCHAR(150) CHARACTER SET utf8mb4 NULL,
        `cidade` VARCHAR(100) CHARACTER SET utf8mb4 NULL,
        `estado` VARCHAR(10) CHARACTER SET utf8mb4 NULL,
        `cep` VARCHAR(10) CHARACTER SET utf8mb4 NULL,
        `data_inicio` DATE NOT NULL,
        `data_fim` DATE NULL,
        `especie_alvo` VARCHAR(30) CHARACTER SET utf8mb4 NOT NULL,
        `organizador` VARCHAR(200) CHARACTER SET utf8mb4 NULL,
        `gratuito` tinyint(1) NOT NULL,
        `link_inscricao` VARCHAR(500) CHARACTER SET utf8mb4 NULL,
        `ativo` tinyint(1) NOT NULL,
        `criado_em` DATETIME NOT NULL,
        CONSTRAINT `PK_t_clyvo_evento_pet` PRIMARY KEY (`id`),
        CONSTRAINT `chk_evento_pet_ativo` CHECK (ativo IN (0,1)),
        CONSTRAINT `chk_evento_pet_gratuito` CHECK (gratuito IN (0,1))
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260927054703_Inicial') THEN

    CREATE TABLE `t_clyvo_lembrete` (
        `id` VARCHAR(36) CHARACTER SET utf8mb4 NOT NULL,
        `animal_id` VARCHAR(36) CHARACTER SET utf8mb4 NOT NULL,
        `titulo` VARCHAR(200) CHARACTER SET utf8mb4 NOT NULL,
        `descricao` VARCHAR(1000) CHARACTER SET utf8mb4 NULL,
        `tipo` VARCHAR(30) CHARACTER SET utf8mb4 NOT NULL,
        `agendado_em` DATETIME NOT NULL,
        `recorrente` tinyint(1) NOT NULL,
        `intervalo_dias` int NULL,
        `repetir_ate` DATETIME NULL,
        `status` VARCHAR(30) CHARACTER SET utf8mb4 NOT NULL,
        `criado_em` DATETIME NOT NULL,
        CONSTRAINT `PK_t_clyvo_lembrete` PRIMARY KEY (`id`),
        CONSTRAINT `chk_lembrete_intervalo` CHECK (intervalo_dias IS NULL OR intervalo_dias > 0),
        CONSTRAINT `chk_lembrete_recorrente` CHECK (recorrente IN (0,1)),
        CONSTRAINT `fk_lembrete_animal` FOREIGN KEY (`animal_id`) REFERENCES `t_clyvo_animal` (`id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260927054703_Inicial') THEN

    CREATE TABLE `t_clyvo_parecer_ia` (
        `id` VARCHAR(36) CHARACTER SET utf8mb4 NOT NULL,
        `animal_id` VARCHAR(36) CHARACTER SET utf8mb4 NOT NULL,
        `origem` VARCHAR(20) CHARACTER SET utf8mb4 NOT NULL,
        `modelo` VARCHAR(120) CHARACTER SET utf8mb4 NULL,
        `conteudo` TEXT CHARACTER SET utf8mb4 NOT NULL,
        `gerado_em` DATETIME NOT NULL,
        `valido_ate` DATETIME NOT NULL,
        CONSTRAINT `PK_t_clyvo_parecer_ia` PRIMARY KEY (`id`),
        CONSTRAINT `ck_parecer_ia_origem` CHECK (origem IN ('IA','REGRAS')),
        CONSTRAINT `fk_parecer_ia_animal` FOREIGN KEY (`animal_id`) REFERENCES `t_clyvo_animal` (`id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260927054703_Inicial') THEN

    CREATE TABLE `t_clyvo_predisposicao_saude` (
        `id` VARCHAR(36) CHARACTER SET utf8mb4 NOT NULL,
        `especie` VARCHAR(30) CHARACTER SET utf8mb4 NOT NULL,
        `raca` VARCHAR(100) CHARACTER SET utf8mb4 NULL,
        `idade_minima_anos` NUMERIC(4,1) NULL,
        `doenca` VARCHAR(200) CHARACTER SET utf8mb4 NOT NULL,
        `recomendacao` VARCHAR(1000) CHARACTER SET utf8mb4 NOT NULL,
        `fonte_referencia` VARCHAR(300) CHARACTER SET utf8mb4 NULL,
        `criado_em` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP(),
        CONSTRAINT `PK_t_clyvo_predisposicao_saude` PRIMARY KEY (`id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260927054703_Inicial') THEN

    CREATE TABLE `t_clyvo_produto` (
        `id` VARCHAR(36) CHARACTER SET utf8mb4 NOT NULL,
        `nome` VARCHAR(200) CHARACTER SET utf8mb4 NOT NULL,
        `descricao` VARCHAR(1000) CHARACTER SET utf8mb4 NULL,
        `categoria` VARCHAR(30) CHARACTER SET utf8mb4 NOT NULL,
        `preco` NUMERIC(10,2) NULL,
        `especie_indicada` VARCHAR(30) CHARACTER SET utf8mb4 NOT NULL,
        `porte_indicado` VARCHAR(30) CHARACTER SET utf8mb4 NOT NULL,
        `ativo` tinyint(1) NOT NULL,
        `criado_em` DATETIME NOT NULL,
        CONSTRAINT `PK_t_clyvo_produto` PRIMARY KEY (`id`),
        CONSTRAINT `chk_produto_ativo` CHECK (ativo IN (0,1)),
        CONSTRAINT `chk_produto_porte` CHECK (porte_indicado IN ('PEQUENO', 'MEDIO', 'GRANDE', 'TODOS'))
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260927054703_Inicial') THEN

    CREATE TABLE `t_clyvo_tutor_telegram` (
        `id` VARCHAR(36) CHARACTER SET utf8mb4 NOT NULL,
        `tutor_id` VARCHAR(36) CHARACTER SET utf8mb4 NOT NULL,
        `chat_id` bigint NOT NULL,
        `criado_em` DATETIME NOT NULL,
        CONSTRAINT `PK_t_clyvo_tutor_telegram` PRIMARY KEY (`id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260927054703_Inicial') THEN

    CREATE TABLE `t_clyvo_sugestao_produto` (
        `id` VARCHAR(36) CHARACTER SET utf8mb4 NOT NULL,
        `animal_id` VARCHAR(36) CHARACTER SET utf8mb4 NOT NULL,
        `produto_id` VARCHAR(36) CHARACTER SET utf8mb4 NOT NULL,
        `justificativa` VARCHAR(500) CHARACTER SET utf8mb4 NULL,
        `data_sugestao` DATE NOT NULL,
        `ativo` tinyint(1) NOT NULL,
        `criado_em` DATETIME NOT NULL,
        CONSTRAINT `PK_t_clyvo_sugestao_produto` PRIMARY KEY (`id`),
        CONSTRAINT `chk_sugestao_ativo` CHECK (ativo IN (0,1)),
        CONSTRAINT `fk_sugestao_animal` FOREIGN KEY (`animal_id`) REFERENCES `t_clyvo_animal` (`id`) ON DELETE CASCADE,
        CONSTRAINT `fk_sugestao_produto` FOREIGN KEY (`produto_id`) REFERENCES `t_clyvo_produto` (`id`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260927054703_Inicial') THEN

    CREATE INDEX `idx_lembrete_animal` ON `t_clyvo_lembrete` (`animal_id`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260927054703_Inicial') THEN

    CREATE INDEX `idx_lembrete_varredura` ON `t_clyvo_lembrete` (`status`, `agendado_em`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260927054703_Inicial') THEN

    CREATE UNIQUE INDEX `uk_parecer_ia_animal` ON `t_clyvo_parecer_ia` (`animal_id`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260927054703_Inicial') THEN

    CREATE INDEX `idx_produto_especie_porte` ON `t_clyvo_produto` (`especie_indicada`, `porte_indicado`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260927054703_Inicial') THEN

    CREATE INDEX `idx_sugestao_animal` ON `t_clyvo_sugestao_produto` (`animal_id`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260927054703_Inicial') THEN

    CREATE INDEX `idx_sugestao_produto` ON `t_clyvo_sugestao_produto` (`produto_id`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260927054703_Inicial') THEN

    CREATE UNIQUE INDEX `uk_tutor_telegram_tutor` ON `t_clyvo_tutor_telegram` (`tutor_id`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260927054703_Inicial') THEN

    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260927054703_Inicial', '8.0.11');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260927191105_ChatIdUnico') THEN


                    DELETE antigo FROM t_clyvo_tutor_telegram antigo
                    JOIN t_clyvo_tutor_telegram recente
                      ON antigo.chat_id = recente.chat_id
                     AND (antigo.criado_em < recente.criado_em
                          OR (antigo.criado_em = recente.criado_em AND antigo.id < recente.id));

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260927191105_ChatIdUnico') THEN


                    SET @existe_uk_chat := (SELECT COUNT(*) FROM information_schema.statistics
                        WHERE table_schema = DATABASE()
                          AND table_name = 't_clyvo_tutor_telegram'
                          AND index_name = 'uk_tutor_telegram_chat');
                    SET @ddl_uk_chat := IF(@existe_uk_chat = 0,
                        'CREATE UNIQUE INDEX uk_tutor_telegram_chat ON t_clyvo_tutor_telegram (chat_id)',
                        'DO 0');
                    PREPARE criar_uk_chat FROM @ddl_uk_chat;
                    EXECUTE criar_uk_chat;
                    DEALLOCATE PREPARE criar_uk_chat;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260927191105_ChatIdUnico') THEN

    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260927191105_ChatIdUnico', '8.0.11');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

