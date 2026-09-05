using ClyvoVet.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClyvoVet.Api.Data.Configurations;

public class AnimalConfiguration : IEntityTypeConfiguration<Animal>
{
    public void Configure(EntityTypeBuilder<Animal> builder)
    {
        // Tabela pertence à API Java (schema em db/migration/mysql do repo dela) — aqui é
        // só leitura via FK/Include, nunca escrita.
        builder.ToTable("animal");

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

        builder.Property(a => a.Castrado)
            .HasColumnName("castrado");

        builder.Property(a => a.TutorId)
            .HasColumnName("tutor_id")
            .HasColumnType("VARCHAR(36)");

        builder.HasOne(a => a.Tutor)
            .WithMany(t => t.Animais)
            .HasForeignKey(a => a.TutorId)
            .HasConstraintName("fk_animal_tutor");
    }
}
