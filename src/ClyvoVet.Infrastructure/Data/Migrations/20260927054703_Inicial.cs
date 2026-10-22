using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClyvoVet.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "t_clyvo_evento_pet",
                columns: table => new
                {
                    id = table.Column<string>(type: "VARCHAR(36)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    titulo = table.Column<string>(type: "VARCHAR(200)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    descricao = table.Column<string>(type: "VARCHAR(1000)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tipo = table.Column<string>(type: "VARCHAR(30)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    rua = table.Column<string>(type: "VARCHAR(300)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    numero = table.Column<string>(type: "VARCHAR(10)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    bairro = table.Column<string>(type: "VARCHAR(150)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    cidade = table.Column<string>(type: "VARCHAR(100)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    estado = table.Column<string>(type: "VARCHAR(10)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    cep = table.Column<string>(type: "VARCHAR(10)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    data_inicio = table.Column<DateTime>(type: "DATE", nullable: false),
                    data_fim = table.Column<DateTime>(type: "DATE", nullable: true),
                    especie_alvo = table.Column<string>(type: "VARCHAR(30)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    organizador = table.Column<string>(type: "VARCHAR(200)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    gratuito = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    link_inscricao = table.Column<string>(type: "VARCHAR(500)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ativo = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    criado_em = table.Column<DateTime>(type: "DATETIME", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_clyvo_evento_pet", x => x.id);
                    table.CheckConstraint("chk_evento_pet_ativo", "ativo IN (0,1)");
                    table.CheckConstraint("chk_evento_pet_gratuito", "gratuito IN (0,1)");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "t_clyvo_lembrete",
                columns: table => new
                {
                    id = table.Column<string>(type: "VARCHAR(36)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    animal_id = table.Column<string>(type: "VARCHAR(36)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    titulo = table.Column<string>(type: "VARCHAR(200)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    descricao = table.Column<string>(type: "VARCHAR(1000)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tipo = table.Column<string>(type: "VARCHAR(30)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    agendado_em = table.Column<DateTime>(type: "DATETIME", nullable: false),
                    recorrente = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    intervalo_dias = table.Column<int>(type: "int", nullable: true),
                    repetir_ate = table.Column<DateTime>(type: "DATETIME", nullable: true),
                    status = table.Column<string>(type: "VARCHAR(30)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    criado_em = table.Column<DateTime>(type: "DATETIME", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_clyvo_lembrete", x => x.id);
                    table.CheckConstraint("chk_lembrete_intervalo", "intervalo_dias IS NULL OR intervalo_dias > 0");
                    table.CheckConstraint("chk_lembrete_recorrente", "recorrente IN (0,1)");
                    table.ForeignKey(
                        name: "fk_lembrete_animal",
                        column: x => x.animal_id,
                        principalTable: "t_clyvo_animal",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "t_clyvo_parecer_ia",
                columns: table => new
                {
                    id = table.Column<string>(type: "VARCHAR(36)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    animal_id = table.Column<string>(type: "VARCHAR(36)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    origem = table.Column<string>(type: "VARCHAR(20)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modelo = table.Column<string>(type: "VARCHAR(120)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    conteudo = table.Column<string>(type: "TEXT", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    gerado_em = table.Column<DateTime>(type: "DATETIME", nullable: false),
                    valido_ate = table.Column<DateTime>(type: "DATETIME", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_clyvo_parecer_ia", x => x.id);
                    table.CheckConstraint("ck_parecer_ia_origem", "origem IN ('IA','REGRAS')");
                    table.ForeignKey(
                        name: "fk_parecer_ia_animal",
                        column: x => x.animal_id,
                        principalTable: "t_clyvo_animal",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "t_clyvo_predisposicao_saude",
                columns: table => new
                {
                    id = table.Column<string>(type: "VARCHAR(36)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    especie = table.Column<string>(type: "VARCHAR(30)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    raca = table.Column<string>(type: "VARCHAR(100)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    idade_minima_anos = table.Column<decimal>(type: "NUMERIC(4,1)", nullable: true),
                    doenca = table.Column<string>(type: "VARCHAR(200)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    recomendacao = table.Column<string>(type: "VARCHAR(1000)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    fonte_referencia = table.Column<string>(type: "VARCHAR(300)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    criado_em = table.Column<DateTime>(type: "DATETIME", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_clyvo_predisposicao_saude", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "t_clyvo_produto",
                columns: table => new
                {
                    id = table.Column<string>(type: "VARCHAR(36)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    nome = table.Column<string>(type: "VARCHAR(200)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    descricao = table.Column<string>(type: "VARCHAR(1000)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    categoria = table.Column<string>(type: "VARCHAR(30)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    preco = table.Column<decimal>(type: "NUMERIC(10,2)", nullable: true),
                    especie_indicada = table.Column<string>(type: "VARCHAR(30)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    porte_indicado = table.Column<string>(type: "VARCHAR(30)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ativo = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    criado_em = table.Column<DateTime>(type: "DATETIME", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_clyvo_produto", x => x.id);
                    table.CheckConstraint("chk_produto_ativo", "ativo IN (0,1)");
                    table.CheckConstraint("chk_produto_porte", "porte_indicado IN ('PEQUENO', 'MEDIO', 'GRANDE', 'TODOS')");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "t_clyvo_tutor_telegram",
                columns: table => new
                {
                    id = table.Column<string>(type: "VARCHAR(36)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tutor_id = table.Column<string>(type: "VARCHAR(36)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    chat_id = table.Column<long>(type: "bigint", nullable: false),
                    criado_em = table.Column<DateTime>(type: "DATETIME", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_clyvo_tutor_telegram", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "t_clyvo_sugestao_produto",
                columns: table => new
                {
                    id = table.Column<string>(type: "VARCHAR(36)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    animal_id = table.Column<string>(type: "VARCHAR(36)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    produto_id = table.Column<string>(type: "VARCHAR(36)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    justificativa = table.Column<string>(type: "VARCHAR(500)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    data_sugestao = table.Column<DateTime>(type: "DATE", nullable: false),
                    ativo = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    criado_em = table.Column<DateTime>(type: "DATETIME", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_clyvo_sugestao_produto", x => x.id);
                    table.CheckConstraint("chk_sugestao_ativo", "ativo IN (0,1)");
                    table.ForeignKey(
                        name: "fk_sugestao_animal",
                        column: x => x.animal_id,
                        principalTable: "t_clyvo_animal",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_sugestao_produto",
                        column: x => x.produto_id,
                        principalTable: "t_clyvo_produto",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "idx_lembrete_animal",
                table: "t_clyvo_lembrete",
                column: "animal_id");

            migrationBuilder.CreateIndex(
                name: "idx_lembrete_varredura",
                table: "t_clyvo_lembrete",
                columns: new[] { "status", "agendado_em" });

            migrationBuilder.CreateIndex(
                name: "uk_parecer_ia_animal",
                table: "t_clyvo_parecer_ia",
                column: "animal_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_produto_especie_porte",
                table: "t_clyvo_produto",
                columns: new[] { "especie_indicada", "porte_indicado" });

            migrationBuilder.CreateIndex(
                name: "idx_sugestao_animal",
                table: "t_clyvo_sugestao_produto",
                column: "animal_id");

            migrationBuilder.CreateIndex(
                name: "idx_sugestao_produto",
                table: "t_clyvo_sugestao_produto",
                column: "produto_id");

            migrationBuilder.CreateIndex(
                name: "uk_tutor_telegram_tutor",
                table: "t_clyvo_tutor_telegram",
                column: "tutor_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "t_clyvo_evento_pet");

            migrationBuilder.DropTable(
                name: "t_clyvo_lembrete");

            migrationBuilder.DropTable(
                name: "t_clyvo_parecer_ia");

            migrationBuilder.DropTable(
                name: "t_clyvo_predisposicao_saude");

            migrationBuilder.DropTable(
                name: "t_clyvo_sugestao_produto");

            migrationBuilder.DropTable(
                name: "t_clyvo_tutor_telegram");

            migrationBuilder.DropTable(
                name: "t_clyvo_produto");
        }
    }
}
