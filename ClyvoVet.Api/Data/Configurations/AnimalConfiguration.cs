using ClyvoVet.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClyvoVet.Api.Data.Configurations;

public class AnimalConfiguration : IEntityTypeConfiguration<Animal>
{
    public void Configure(EntityTypeBuilder<Animal> builder)
    {
        builder.ToTable("T_CLYVO_ANIMAL");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasColumnName("ID")
            .HasColumnType("VARCHAR2(36)")
            .ValueGeneratedOnAdd();

        builder.Property(a => a.Nome)
            .HasColumnName("NOME")
            .HasColumnType("VARCHAR2(100)")
            .IsRequired();

        builder.Property(a => a.Especie)
            .HasColumnName("ESPECIE")
            .HasColumnType("VARCHAR2(50)");

        builder.Property(a => a.Raca)
            .HasColumnName("RACA")
            .HasColumnType("VARCHAR2(100)");

        builder.Property(a => a.DataNascimento)
            .HasColumnName("DATA_NASCIMENTO");

        // DDL real: coluna é GENERO (não SEXO) — CHECK: MACHO, FEMEA, DESCONHECIDO
        builder.Property(a => a.Sexo)
            .HasColumnName("GENERO")
            .HasColumnType("VARCHAR2(10)");

        builder.Property(a => a.Castrado)
            .HasColumnName("CASTRADO")
            .HasColumnType("NUMBER(1)")
            .HasConversion<int>();

        builder.Property(a => a.CriadoEm)
            .HasColumnName("CRIADO_EM")
            .ValueGeneratedOnAdd();

        builder.Property(a => a.TutorId)
            .HasColumnName("TUTOR_ID")
            .HasColumnType("VARCHAR2(36)");

        builder.HasOne(a => a.Tutor)
            .WithMany(t => t.Animais)
            .HasForeignKey(a => a.TutorId)
            .HasConstraintName("FK_ANIMAL_TUTOR");
    }
}
