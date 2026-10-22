using ClyvoVet.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClyvoVet.Infrastructure.Data.Configurations;

public class LembreteConfiguration : IEntityTypeConfiguration<Lembrete>
{
    public void Configure(EntityTypeBuilder<Lembrete> builder)
    {
        builder.ToTable("t_clyvo_lembrete", t =>
        {
            t.HasCheckConstraint("chk_lembrete_recorrente", "recorrente IN (0,1)");
            t.HasCheckConstraint("chk_lembrete_intervalo", "intervalo_dias IS NULL OR intervalo_dias > 0");
        });

        builder.HasIndex(l => l.AnimalId).HasDatabaseName("idx_lembrete_animal");
        // A varredura do LembreteNotificationService filtra por status e data (V18 da Java).
        builder.HasIndex(l => new { l.Status, l.AgendadoEm }).HasDatabaseName("idx_lembrete_varredura");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id)
            .HasColumnName("id")
            .HasColumnType("VARCHAR(36)")
            .ValueGeneratedNever();

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
            .HasColumnType("DATETIME")
            .IsRequired();

        builder.Property(l => l.Recorrente)
            .HasColumnName("recorrente");

        builder.Property(l => l.IntervaloDias)
            .HasColumnName("intervalo_dias");

        builder.Property(l => l.RepetirAte)
            .HasColumnName("repetir_ate")
            .HasColumnType("DATETIME");

        builder.Property(l => l.Status)
            .HasColumnName("status");

        builder.Property(l => l.CriadoEm)
            .HasColumnName("criado_em")
            .HasColumnType("DATETIME");

        builder.HasOne(l => l.Animal)
            .WithMany()
            .HasForeignKey(l => l.AnimalId)
            .HasConstraintName("fk_lembrete_animal");
    }
}
