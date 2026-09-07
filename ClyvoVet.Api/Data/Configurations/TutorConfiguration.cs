using ClyvoVet.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClyvoVet.Api.Data.Configurations;

public class TutorConfiguration : IEntityTypeConfiguration<Tutor>
{
    public void Configure(EntityTypeBuilder<Tutor> builder)
    {
        // Tabela pertence à API Java (schema em db/migration/mysql do repo dela) — aqui é
        // só leitura via FK/Include, nunca escrita. O prefixo t_clyvo_ veio da V9 de lá:
        // renomear a tabela no Java sem trocar este nome quebra a primeira consulta, e o
        // EF não avisa no boot porque não valida schema.
        builder.ToTable("t_clyvo_tutor");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasColumnName("id")
            .HasColumnType("VARCHAR(36)")
            .ValueGeneratedOnAdd();

        builder.Property(t => t.Nome)
            .HasColumnName("nome")
            .HasColumnType("VARCHAR(150)")
            .IsRequired();

        builder.Property(t => t.Cpf)
            .HasColumnName("cpf")
            .HasColumnType("VARCHAR(11)");

        builder.Property(t => t.Email)
            .HasColumnName("email")
            .HasColumnType("VARCHAR(200)");

        builder.Property(t => t.Telefone)
            .HasColumnName("telefone")
            .HasColumnType("VARCHAR(20)");
    }
}
