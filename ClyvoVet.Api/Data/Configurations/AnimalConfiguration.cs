using ClyvoVet.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClyvoVet.Api.Data.Configurations;

public class AnimalConfiguration : IEntityTypeConfiguration<Animal>
{
    public void Configure(EntityTypeBuilder<Animal> builder)
    {
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

        // DDL real: coluna é GENERO (não SEXO) — CHECK: MACHO, FEMEA, DESCONHECIDO
        builder.Property(a => a.Sexo)
            .HasColumnName("genero")
            .HasColumnType("VARCHAR(10)");

        builder.Property(a => a.Castrado)
            .HasColumnName("castrado");

        builder.Property(a => a.CriadoEm)
            .HasColumnName("criado_em")
            .ValueGeneratedOnAdd();

        builder.Property(a => a.TutorId)
            .HasColumnName("tutor_id")
            .HasColumnType("VARCHAR(36)");

        builder.HasOne(a => a.Tutor)
            .WithMany(t => t.Animais)
            .HasForeignKey(a => a.TutorId)
            .HasConstraintName("fk_animal_tutor");
    }
}
