using ClyvoVet.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClyvoVet.Api.Data.Configurations;

public class RacaConfiguration : IEntityTypeConfiguration<Raca>
{
    public void Configure(EntityTypeBuilder<Raca> builder)
    {
        // Tabela da API Java (V14 do Flyway de lá) — aqui é só leitura, como
        // t_clyvo_animal e t_clyvo_tutor.
        builder.ToTable("t_clyvo_raca");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasColumnName("id")
            .HasColumnType("VARCHAR(36)");

        builder.Property(r => r.Especie)
            .HasColumnName("especie")
            .HasColumnType("VARCHAR(20)");

        builder.Property(r => r.Nome)
            .HasColumnName("nome")
            .HasColumnType("VARCHAR(100)");

        builder.Property(r => r.Chave)
            .HasColumnName("chave")
            .HasColumnType("VARCHAR(60)");

        builder.Property(r => r.PorteTipico)
            .HasColumnName("porte_tipico")
            .HasColumnType("VARCHAR(20)");

        // Sem HasColumnType, como em Animal.Castrado: a coluna real é INT
        // (NumericBooleanConverter do lado Java); afirmar TINYINT aqui seria
        // escrever o tipo errado no código.
        builder.Property(r => r.Ativo)
            .HasColumnName("ativo");
    }
}
