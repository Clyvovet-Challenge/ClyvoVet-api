using ClyvoVet.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClyvoVet.Api.Data.Configurations;

public class ProdutoConfiguration : IEntityTypeConfiguration<Produto>
{
    public void Configure(EntityTypeBuilder<Produto> builder)
    {
        builder.ToTable("t_clyvo_produto");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id")
            .HasColumnType("VARCHAR(36)")
            .ValueGeneratedNever();

        builder.Property(p => p.Nome)
            .HasColumnName("nome")
            .HasColumnType("VARCHAR(200)")
            .IsRequired();

        builder.Property(p => p.Descricao)
            .HasColumnName("descricao")
            .HasColumnType("VARCHAR(1000)");

        builder.Property(p => p.Categoria)
            .HasColumnName("categoria");

        builder.Property(p => p.Preco)
            .HasColumnName("preco")
            .HasColumnType("NUMERIC(10,2)");

        builder.Property(p => p.EspecieIndicada)
            .HasColumnName("especie_indicada");

        builder.Property(p => p.Ativo)
            .HasColumnName("ativo");

        builder.Property(p => p.CriadoEm)
            .HasColumnName("criado_em");
    }
}
