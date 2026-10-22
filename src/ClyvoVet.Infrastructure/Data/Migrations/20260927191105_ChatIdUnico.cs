using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClyvoVet.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChatIdUnico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Antes do índice, e não depois: num banco que já tenha dois tutores no
            // mesmo chat, o CREATE UNIQUE INDEX falharia e a migration pararia no meio.
            // Fica a linha mais recente de cada chat_id (empate: o maior id).
            migrationBuilder.Sql(@"
                DELETE antigo FROM t_clyvo_tutor_telegram antigo
                JOIN t_clyvo_tutor_telegram recente
                  ON antigo.chat_id = recente.chat_id
                 AND (antigo.criado_em < recente.criado_em
                      OR (antigo.criado_em = recente.criado_em AND antigo.id < recente.id));");

            // Com guarda: a tabela é do Flyway da Java, que pode ganhar o mesmo índice numa
            // V21. O Flyway roda antes deste script, e o MySQL não tem CREATE INDEX IF NOT
            // EXISTS; sem a checagem, o deploy pararia em "Duplicate key name". PREPARE, e
            // não IF, porque IF solto só vale dentro de procedure (fora do script idempotente,
            // num "dotnet ef database update", daria erro de sintaxe).
            migrationBuilder.Sql(@"
                SET @existe_uk_chat := (SELECT COUNT(*) FROM information_schema.statistics
                    WHERE table_schema = DATABASE()
                      AND table_name = 't_clyvo_tutor_telegram'
                      AND index_name = 'uk_tutor_telegram_chat');
                SET @ddl_uk_chat := IF(@existe_uk_chat = 0,
                    'CREATE UNIQUE INDEX uk_tutor_telegram_chat ON t_clyvo_tutor_telegram (chat_id)',
                    'DO 0');
                PREPARE criar_uk_chat FROM @ddl_uk_chat;
                EXECUTE criar_uk_chat;
                DEALLOCATE PREPARE criar_uk_chat;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uk_tutor_telegram_chat",
                table: "t_clyvo_tutor_telegram");
        }
    }
}
