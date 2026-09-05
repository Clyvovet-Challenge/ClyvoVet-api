using ClyvoVet.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ClyvoVet.Api.Data.Configurations;

public class SugestaoProdutoConfiguration : IEntityTypeConfiguration<SugestaoProduto>
{
    public void Configure(EntityTypeBuilder<SugestaoProduto> builder)
    {
        builder.ToTable("t_clyvo_sugestao_produto");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasColumnName("id")
            .HasColumnType("VARCHAR(36)")
            .ValueGeneratedOnAdd();

        builder.Property(s => s.AnimalId)
            .HasColumnName("animal_id")
            .HasColumnType("VARCHAR(36)")
            .IsRequired();

        builder.Property(s => s.ProdutoId)
            .HasColumnName("produto_id")
            .HasColumnType("VARCHAR(36)")
            .IsRequired();

        builder.Property(s => s.Justificativa)
            .HasColumnName("justificativa")
            .HasColumnType("VARCHAR(500)");   // DDL real: VARCHAR(500)

        // Oracle EF Core não suporta DateOnly nativamente — converter para DateTime
        var dateOnlyConverter = new ValueConverter<DateOnly, DateTime>(
            d => d.ToDateTime(TimeOnly.MinValue),
            dt => DateOnly.FromDateTime(dt));

        builder.Property(s => s.DataSugestao)
            .HasColumnName("data_sugestao")
            .HasColumnType("DATE")
            .HasConversion(dateOnlyConverter)
            .IsRequired();

        builder.Property(s => s.Ativo)
            .HasColumnName("ativo");

        builder.Property(s => s.CriadoEm)
            .HasColumnName("criado_em")
            .ValueGeneratedOnAdd();

        builder.HasOne(s => s.Animal)
            .WithMany()
            .HasForeignKey(s => s.AnimalId)
            .HasConstraintName("fk_sugestao_animal");

        builder.HasOne(s => s.Produto)
            .WithMany(p => p.Sugestoes)
            .HasForeignKey(s => s.ProdutoId)
            .HasConstraintName("fk_sugestao_produto");
    }
}
