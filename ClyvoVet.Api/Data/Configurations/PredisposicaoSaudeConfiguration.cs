using ClyvoVet.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClyvoVet.Api.Data.Configurations;

public class PredisposicaoSaudeConfiguration : IEntityTypeConfiguration<PredisposicaoSaude>
{
    public void Configure(EntityTypeBuilder<PredisposicaoSaude> builder)
    {
        builder.ToTable("t_clyvo_predisposicao_saude");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id")
            .HasColumnType("VARCHAR(36)")
            .ValueGeneratedOnAdd();

        builder.Property(p => p.Especie)
            .HasColumnName("especie");

        builder.Property(p => p.Raca)
            .HasColumnName("raca")
            .HasColumnType("VARCHAR(100)");

        builder.Property(p => p.IdadeMinimaAnos)
            .HasColumnName("idade_minima_anos")
            .HasColumnType("NUMERIC(4,1)");

        builder.Property(p => p.Doenca)
            .HasColumnName("doenca")
            .HasColumnType("VARCHAR(200)")
            .IsRequired();

        builder.Property(p => p.Recomendacao)
            .HasColumnName("recomendacao")
            .HasColumnType("VARCHAR(1000)")
            .IsRequired();

        builder.Property(p => p.FonteReferencia)
            .HasColumnName("fonte_referencia")
            .HasColumnType("VARCHAR(300)");

        builder.Property(p => p.CriadoEm)
            .HasColumnName("criado_em")
            .ValueGeneratedOnAdd();
    }
}
