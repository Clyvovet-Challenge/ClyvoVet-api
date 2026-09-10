using ClyvoVet.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClyvoVet.Api.Data.Configurations;

public class AnimalConfiguration : IEntityTypeConfiguration<Animal>
{
    public void Configure(EntityTypeBuilder<Animal> builder)
    {
        // Tabela pertence à API Java (schema em db/migration/mysql do repo dela) — aqui é
        // só leitura via FK/Include, nunca escrita. O prefixo t_clyvo_ veio da V9 de lá:
        // renomear a tabela no Java sem trocar este nome quebra a primeira consulta, e o
        // EF não avisa no boot porque não valida schema.
        builder.ToTable("t_clyvo_animal");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasColumnName("id")
            .HasColumnType("VARCHAR(36)")
            .ValueGeneratedOnAdd();

        builder.Property(a => a.Nome)
            .HasColumnName("nome")
            .HasColumnType("VARCHAR(100)")
            .IsRequired();

        builder.Property(a => a.Especie)
            .HasColumnName("especie")
            .HasColumnType("VARCHAR(50)");

        builder.Property(a => a.Raca)
            .HasColumnName("raca")
            .HasColumnType("VARCHAR(100)");

        builder.Property(a => a.DataNascimento)
            .HasColumnName("data_nascimento");

        builder.Property(a => a.Sexo)
            .HasColumnName("genero")
            .HasColumnType("VARCHAR(10)");

        // SEM HasColumnType, DE PROPOSITO
        // Declarava TINYINT(1), mas a coluna real e INT: ela pertence a API Java,
        // que a escreve via NumericBooleanConverter -- o converter entrega Integer
        // ao JDBC, e o ddl-auto=validate de la reprova TINYINT contra INTEGER.
        // Nao quebrava leitura (o MySQL converte), mas era um tipo errado escrito
        // no codigo, e o proximo a gerar DDL a partir daqui recriaria a coluna
        // como TINYINT e derrubaria o boot da outra API. Sem a anotacao o Pomelo
        // infere bool <-> a coluna existente e ninguem afirma o tipo errado.
        builder.Property(a => a.Castrado)
            .HasColumnName("castrado");

        builder.Property(a => a.TutorId)
            .HasColumnName("tutor_id")
            .HasColumnType("VARCHAR(36)");

        builder.HasOne(a => a.Tutor)
            .WithMany(t => t.Animais)
            .HasForeignKey(a => a.TutorId)
            .HasConstraintName("fk_animal_tutor");

        builder.Property(a => a.RacaId)
            .HasColumnName("raca_id")
            .HasColumnType("VARCHAR(36)");

        builder.HasOne(a => a.RacaCatalogo)
            .WithMany()
            .HasForeignKey(a => a.RacaId)
            .HasConstraintName("fk_animal_raca");
    }
}
