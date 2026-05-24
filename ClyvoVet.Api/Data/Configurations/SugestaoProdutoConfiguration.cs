using ClyvoVet.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClyvoVet.Api.Data.Configurations;

public class SugestaoProdutoConfiguration : IEntityTypeConfiguration<SugestaoProduto>
{
    public void Configure(EntityTypeBuilder<SugestaoProduto> builder)
    {
        builder.ToTable("T_CLYVO_SUGESTAO_PRODUTO");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasColumnName("ID")
            .HasColumnType("VARCHAR2(36)")
            .ValueGeneratedOnAdd();

        builder.Property(s => s.AnimalId)
            .HasColumnName("ANIMAL_ID")
            .HasColumnType("VARCHAR2(36)")
            .IsRequired();

        builder.Property(s => s.ProdutoId)
            .HasColumnName("PRODUTO_ID")
            .HasColumnType("VARCHAR2(36)")
            .IsRequired();

        builder.Property(s => s.Justificativa)
            .HasColumnName("JUSTIFICATIVA")
            .HasColumnType("VARCHAR2(1000)");

        builder.Property(s => s.DataSugestao)
            .HasColumnName("DATA_SUGESTAO")
            .HasColumnType("DATE")
            .IsRequired();

        builder.Property(s => s.Ativo)
            .HasColumnName("ATIVO")
            .HasColumnType("NUMBER(1)")
            .HasConversion<int>();

        builder.Property(s => s.CriadoEm)
            .HasColumnName("CRIADO_EM")
            .ValueGeneratedOnAdd();

        builder.HasOne(s => s.Animal)
            .WithMany()
            .HasForeignKey(s => s.AnimalId)
            .HasConstraintName("FK_SUGESTAO_ANIMAL");

        builder.HasOne(s => s.Produto)
            .WithMany(p => p.Sugestoes)
            .HasForeignKey(s => s.ProdutoId)
            .HasConstraintName("FK_SUGESTAO_PRODUTO");
    }
}
