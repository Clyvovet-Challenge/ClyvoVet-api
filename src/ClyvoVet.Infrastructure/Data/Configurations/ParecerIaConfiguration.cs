using ClyvoVet.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClyvoVet.Infrastructure.Data.Configurations;

public class ParecerIaConfiguration : IEntityTypeConfiguration<ParecerIa>
{
    public void Configure(EntityTypeBuilder<ParecerIa> builder)
    {
        builder.ToTable("t_clyvo_parecer_ia",
            t => t.HasCheckConstraint("ck_parecer_ia_origem", "origem IN ('IA','REGRAS')"));

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id")
            .HasColumnType("VARCHAR(36)");

        builder.Property(p => p.AnimalId)
            .HasColumnName("animal_id")
            .HasColumnType("VARCHAR(36)");

        // UNIQUE no banco (uk_parecer_ia_animal). Declarar o índice aqui faz o
        // provider InMemory dos testes de integração aplicar a MESMA regra —
        // é o que impede a suíte de aceitar dois pareceres por animal.
        builder.HasIndex(p => p.AnimalId).IsUnique().HasDatabaseName("uk_parecer_ia_animal");

        // Sem navegação: o parecer só precisa da FK no banco (V15, cascade desde a V20),
        // nenhuma consulta daqui carrega o animal a partir dele.
        builder.HasOne<Animal>()
            .WithMany()
            .HasForeignKey(p => p.AnimalId)
            .HasConstraintName("fk_parecer_ia_animal")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(p => p.Origem)
            .HasColumnName("origem")
            .HasColumnType("VARCHAR(20)");

        builder.Property(p => p.Modelo)
            .HasColumnName("modelo")
            .HasColumnType("VARCHAR(120)");

        builder.Property(p => p.Conteudo)
            .HasColumnName("conteudo")
            .HasColumnType("TEXT");

        builder.Property(p => p.GeradoEm)
            .HasColumnName("gerado_em")
            .HasColumnType("DATETIME");

        builder.Property(p => p.ValidoAte)
            .HasColumnName("valido_ate")
            .HasColumnType("DATETIME");
    }
}
