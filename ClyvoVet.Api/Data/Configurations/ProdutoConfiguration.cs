using ClyvoVet.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClyvoVet.Api.Data.Configurations;

public class ProdutoConfiguration : IEntityTypeConfiguration<Produto>
{
    public void Configure(EntityTypeBuilder<Produto> builder)
    {
        builder.ToTable("PRODUTOS");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("ID")
            .HasColumnType("VARCHAR2(36)")
            .ValueGeneratedOnAdd();

        builder.Property(p => p.Nome)
            .HasColumnName("NOME")
            .HasColumnType("VARCHAR2(200)")
            .IsRequired();

        builder.Property(p => p.Descricao)
            .HasColumnName("DESCRICAO")
            .HasColumnType("VARCHAR2(1000)");

        builder.Property(p => p.Categoria)
            .HasColumnName("CATEGORIA");

        builder.Property(p => p.Preco)
            .HasColumnName("PRECO")
            .HasColumnType("NUMBER(10,2)");

        builder.Property(p => p.EspecieIndicada)
            .HasColumnName("ESPECIE_INDICADA");

        builder.Property(p => p.Ativo)
            .HasColumnName("ATIVO")
            .HasColumnType("NUMBER(1)")
            .HasConversion<int>();

        builder.Property(p => p.CriadoEm)
            .HasColumnName("CRIADO_EM")
            .ValueGeneratedOnAdd();
    }
}
