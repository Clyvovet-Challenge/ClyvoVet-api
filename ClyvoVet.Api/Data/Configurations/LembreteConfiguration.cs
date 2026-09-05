using ClyvoVet.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClyvoVet.Api.Data.Configurations;

public class LembreteConfiguration : IEntityTypeConfiguration<Lembrete>
{
    public void Configure(EntityTypeBuilder<Lembrete> builder)
    {
        builder.ToTable("t_clyvo_lembrete");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id)
            .HasColumnName("id")
            .HasColumnType("VARCHAR(36)")
            .ValueGeneratedOnAdd();

        builder.Property(l => l.AnimalId)
            .HasColumnName("animal_id")
            .HasColumnType("VARCHAR(36)")
            .IsRequired();

        builder.Property(l => l.Titulo)
            .HasColumnName("titulo")
            .HasColumnType("VARCHAR(200)")
            .IsRequired();

        builder.Property(l => l.Descricao)
            .HasColumnName("descricao")
            .HasColumnType("VARCHAR(1000)");

        builder.Property(l => l.Tipo)
            .HasColumnName("tipo");

        builder.Property(l => l.AgendadoEm)
            .HasColumnName("agendado_em")
            .IsRequired();

        builder.Property(l => l.Recorrente)
            .HasColumnName("recorrente");

        builder.Property(l => l.Status)
            .HasColumnName("status");

        builder.Property(l => l.CriadoEm)
            .HasColumnName("criado_em")
            .ValueGeneratedOnAdd();

        builder.HasOne(l => l.Animal)
            .WithMany()
            .HasForeignKey(l => l.AnimalId)
            .HasConstraintName("fk_lembrete_animal");
    }
}
