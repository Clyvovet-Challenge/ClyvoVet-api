using ClyvoVet.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClyvoVet.Api.Data.Configurations;

public class ConsultaConfiguration : IEntityTypeConfiguration<Consulta>
{
    public void Configure(EntityTypeBuilder<Consulta> builder)
    {
        builder.ToTable("t_clyvo_consulta");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("id")
            .HasColumnType("VARCHAR(36)")
            .ValueGeneratedOnAdd();

        builder.Property(c => c.DataHora)
            .HasColumnName("data_hora")
            .IsRequired();

        builder.Property(c => c.Status)
            .HasColumnName("status")
            .HasColumnType("VARCHAR(30)")
            .IsRequired();

        builder.Property(c => c.Motivo)
            .HasColumnName("motivo")
            .HasColumnType("VARCHAR(500)");

        builder.Property(c => c.Observacoes)
            .HasColumnName("observacoes")
            .HasColumnType("VARCHAR(2000)");

        builder.Property(c => c.Valor)
            .HasColumnName("valor")
            .HasColumnType("NUMERIC(10,2)");

        builder.Property(c => c.CriadoEm)
            .HasColumnName("criado_em")
            .ValueGeneratedOnAdd();

        builder.Property(c => c.AnimalId)
            .HasColumnName("animal_id")
            .HasColumnType("VARCHAR(36)")
            .IsRequired();

        builder.Property(c => c.VeterinarioId)
            .HasColumnName("veterinario_id")
            .HasColumnType("VARCHAR(36)")
            .IsRequired();

        builder.HasOne(c => c.Animal)
            .WithMany(a => a.Consultas)
            .HasForeignKey(c => c.AnimalId)
            .HasConstraintName("fk_consulta_animal");

        builder.HasOne(c => c.Veterinario)
            .WithMany(v => v.Consultas)
            .HasForeignKey(c => c.VeterinarioId)
            .HasConstraintName("fk_consulta_veterinario");
    }
}
