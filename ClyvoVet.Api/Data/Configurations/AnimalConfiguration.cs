using ClyvoVet.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClyvoVet.Api.Data.Configurations;

public class AnimalConfiguration : IEntityTypeConfiguration<Animal>
{
    public void Configure(EntityTypeBuilder<Animal> builder)
    {
        builder.ToTable("ANIMAIS");

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
            .HasColumnType("VARCHAR2(50)")
            .IsRequired();

        builder.Property(a => a.Raca)
            .HasColumnName("RACA")
            .HasColumnType("VARCHAR2(100)");

        builder.Property(a => a.DataNascimento)
            .HasColumnName("DATA_NASCIMENTO");

        builder.Property(a => a.Sexo)
            .HasColumnName("SEXO")
            .HasColumnType("VARCHAR2(20)");

        builder.Property(a => a.Castrado)
            .HasColumnName("CASTRADO")
            .HasColumnType("NUMBER(1)")
            .HasConversion<int>();

        builder.Property(a => a.Ativo)
            .HasColumnName("ATIVO")
            .HasColumnType("NUMBER(1)")
            .HasConversion<int>();

        builder.Property(a => a.CriadoEm)
            .HasColumnName("CRIADO_EM")
            .ValueGeneratedOnAdd();

        builder.Property(a => a.TutorId)
            .HasColumnName("TUTOR_ID")
            .HasColumnType("VARCHAR2(36)")
            .IsRequired();

        builder.HasOne(a => a.Tutor)
            .WithMany(t => t.Animais)
            .HasForeignKey(a => a.TutorId)
            .HasConstraintName("FK_ANIMAIS_TUTORES");
    }
}
