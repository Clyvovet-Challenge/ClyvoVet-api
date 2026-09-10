using ClyvoVet.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClyvoVet.Api.Data.Configurations;

public class BaseDoencaConfiguration : IEntityTypeConfiguration<BaseDoenca>
{
    public void Configure(EntityTypeBuilder<BaseDoenca> builder)
    {
        // Schema criado pela V15 do Flyway da API Java (que é quem provisiona
        // as tabelas do domínio .NET desde a V8). O seed também mora lá.
        builder.ToTable("t_clyvo_base_doencas");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Id)
            .HasColumnName("id")
            .HasColumnType("VARCHAR(36)");

        builder.Property(b => b.Especie)
            .HasColumnName("especie")
            .HasColumnType("VARCHAR(20)");

        builder.Property(b => b.RacaTexto)
            .HasColumnName("raca_texto")
            .HasColumnType("VARCHAR(100)");

        builder.Property(b => b.RacaChave)
            .HasColumnName("raca_chave")
            .HasColumnType("VARCHAR(60)");

        builder.Property(b => b.DoencaCodigo)
            .HasColumnName("doenca_codigo")
            .HasColumnType("VARCHAR(60)");

        builder.Property(b => b.DoencaNome)
            .HasColumnName("doenca_nome")
            .HasColumnType("VARCHAR(200)");

        builder.Property(b => b.Categoria)
            .HasColumnName("categoria")
            .HasColumnType("VARCHAR(60)");

        builder.Property(b => b.Casos)
            .HasColumnName("casos");

        builder.Property(b => b.Controles)
            .HasColumnName("controles");

        builder.Property(b => b.Fonte)
            .HasColumnName("fonte")
            .HasColumnType("VARCHAR(300)");

        builder.Property(b => b.Doi)
            .HasColumnName("doi")
            .HasColumnType("VARCHAR(100)");

        builder.Property(b => b.CriadoEm)
            .HasColumnName("criado_em")
            .ValueGeneratedOnAdd();
    }
}
