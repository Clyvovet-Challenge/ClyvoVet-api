using ClyvoVet.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClyvoVet.Api.Data.Configurations;

public class PredisposicaoSaudeConfiguration : IEntityTypeConfiguration<PredisposicaoSaude>
{
    public void Configure(EntityTypeBuilder<PredisposicaoSaude> builder)
    {
        builder.ToTable("T_CLYVO_PREDISPOSICAO_SAUDE");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("ID")
            .HasColumnType("VARCHAR2(36)")
            .ValueGeneratedOnAdd();

        builder.Property(p => p.Especie)
            .HasColumnName("ESPECIE");

        builder.Property(p => p.Raca)
            .HasColumnName("RACA")
            .HasColumnType("VARCHAR2(100)");

        builder.Property(p => p.IdadeMinimaAnos)
            .HasColumnName("IDADE_MINIMA_ANOS")
            .HasColumnType("NUMBER(4,1)");

        builder.Property(p => p.Doenca)
            .HasColumnName("DOENCA")
            .HasColumnType("VARCHAR2(200)")
            .IsRequired();

        builder.Property(p => p.Recomendacao)
            .HasColumnName("RECOMENDACAO")
            .HasColumnType("VARCHAR2(1000)")
            .IsRequired();

        builder.Property(p => p.FonteReferencia)
            .HasColumnName("FONTE_REFERENCIA")
            .HasColumnType("VARCHAR2(300)");

        builder.Property(p => p.CriadoEm)
            .HasColumnName("CRIADO_EM")
            .ValueGeneratedOnAdd();
    }
}
