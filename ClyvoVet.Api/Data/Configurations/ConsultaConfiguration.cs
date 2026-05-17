using ClyvoVet.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClyvoVet.Api.Data.Configurations;

public class ConsultaConfiguration : IEntityTypeConfiguration<Consulta>
{
    public void Configure(EntityTypeBuilder<Consulta> builder)
    {
        builder.ToTable("CONSULTAS");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("ID")
            .HasColumnType("VARCHAR2(36)")
            .ValueGeneratedOnAdd();

        builder.Property(c => c.DataHora)
            .HasColumnName("DATA_HORA")
            .IsRequired();

        builder.Property(c => c.Status)
            .HasColumnName("STATUS")
            .HasColumnType("VARCHAR2(30)")
            .IsRequired();

        builder.Property(c => c.Motivo)
            .HasColumnName("MOTIVO")
            .HasColumnType("VARCHAR2(500)");

        builder.Property(c => c.Observacoes)
            .HasColumnName("OBSERVACOES")
            .HasColumnType("VARCHAR2(2000)");

        builder.Property(c => c.Valor)
            .HasColumnName("VALOR")
            .HasColumnType("NUMBER(10,2)");

        builder.Property(c => c.CriadoEm)
            .HasColumnName("CRIADO_EM")
            .ValueGeneratedOnAdd();

        builder.Property(c => c.AnimalId)
            .HasColumnName("ANIMAL_ID")
            .HasColumnType("VARCHAR2(36)")
            .IsRequired();

        builder.Property(c => c.VeterinarioId)
            .HasColumnName("VETERINARIO_ID")
            .HasColumnType("VARCHAR2(36)")
            .IsRequired();

        builder.HasOne(c => c.Animal)
            .WithMany(a => a.Consultas)
            .HasForeignKey(c => c.AnimalId)
            .HasConstraintName("FK_CONSULTAS_ANIMAIS");

        builder.HasOne(c => c.Veterinario)
            .WithMany(v => v.Consultas)
            .HasForeignKey(c => c.VeterinarioId)
            .HasConstraintName("FK_CONSULTAS_VETS");
    }
}
