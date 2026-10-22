using ClyvoVet.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClyvoVet.Infrastructure.Data.Configurations;

public class ProdutoConfiguration : IEntityTypeConfiguration<Produto>
{
    public void Configure(EntityTypeBuilder<Produto> builder)
    {
        // CHECKs e índice repetem os do Flyway (V8 e V16 da Java): o script das
        // migrations tem de montar o mesmo banco que a produção tem.
        builder.ToTable("t_clyvo_produto", t =>
        {
            t.HasCheckConstraint("chk_produto_ativo", "ativo IN (0,1)");
            t.HasCheckConstraint("chk_produto_porte", "porte_indicado IN ('PEQUENO', 'MEDIO', 'GRANDE', 'TODOS')");
        });

        builder.HasIndex(p => new { p.EspecieIndicada, p.PorteIndicado })
            .HasDatabaseName("idx_produto_especie_porte");

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

        builder.Property(p => p.PorteIndicado)
            .HasColumnName("porte_indicado");
        builder.Property(p => p.Ativo)
            .HasColumnName("ativo");

        builder.Property(p => p.CriadoEm)
            .HasColumnName("criado_em")
            .HasColumnType("DATETIME");
    }
}
