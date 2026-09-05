using ClyvoVet.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ClyvoVet.Api.Data.Configurations;

public class EventoPetConfiguration : IEntityTypeConfiguration<EventoPet>
{
    public void Configure(EntityTypeBuilder<EventoPet> builder)
    {
        builder.ToTable("t_clyvo_evento_pet");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasColumnType("VARCHAR(36)")
            .ValueGeneratedOnAdd();

        builder.Property(e => e.Titulo)
            .HasColumnName("titulo")
            .HasColumnType("VARCHAR(200)")   // DDL real: VARCHAR(200)
            .IsRequired();

        builder.Property(e => e.Descricao)
            .HasColumnName("descricao")
            .HasColumnType("VARCHAR(1000)");

        builder.Property(e => e.Tipo)
            .HasColumnName("tipo");

        builder.Property(e => e.Rua)
            .HasColumnName("rua")
            .HasColumnType("VARCHAR(300)");

        builder.Property(e => e.Numero)
            .HasColumnName("numero")
            .HasColumnType("VARCHAR(10)");   // DDL real: VARCHAR(10)

        builder.Property(e => e.Bairro)
            .HasColumnName("bairro")
            .HasColumnType("VARCHAR(150)");  // DDL real: VARCHAR(150)

        builder.Property(e => e.Cidade)
            .HasColumnName("cidade")
            .HasColumnType("VARCHAR(100)");  // DDL real: VARCHAR(100)

        builder.Property(e => e.Estado)
            .HasColumnName("estado")
            .HasColumnType("VARCHAR(50)");   // DDL real: VARCHAR(50)

        builder.Property(e => e.Cep)
            .HasColumnName("cep")
            .HasColumnType("VARCHAR(10)");   // DDL real: VARCHAR(10)

        // Oracle EF Core não suporta DateOnly nativamente — converter para DateTime
        var dateOnlyConverter = new ValueConverter<DateOnly, DateTime>(
            d => d.ToDateTime(TimeOnly.MinValue),
            dt => DateOnly.FromDateTime(dt));

        var dateOnlyNullConverter = new ValueConverter<DateOnly?, DateTime?>(
            d => d.HasValue ? d.Value.ToDateTime(TimeOnly.MinValue) : null,
            dt => dt.HasValue ? DateOnly.FromDateTime(dt.Value) : null);

        builder.Property(e => e.DataInicio)
            .HasColumnName("data_inicio")
            .HasColumnType("DATE")
            .HasConversion(dateOnlyConverter)
            .IsRequired();

        builder.Property(e => e.DataFim)
            .HasColumnName("data_fim")
            .HasColumnType("DATE")
            .HasConversion(dateOnlyNullConverter);

        builder.Property(e => e.EspecieAlvo)
            .HasColumnName("especie_alvo");

        builder.Property(e => e.Organizador)
            .HasColumnName("organizador")
            .HasColumnType("VARCHAR(200)");  // DDL real: VARCHAR(200)

        builder.Property(e => e.Gratuito)
            .HasColumnName("gratuito");

        builder.Property(e => e.LinkInscricao)
            .HasColumnName("link_inscricao")
            .HasColumnType("VARCHAR(500)");

        builder.Property(e => e.Ativo)
            .HasColumnName("ativo");

        builder.Property(e => e.CriadoEm)
            .HasColumnName("criado_em")
            .ValueGeneratedOnAdd();
    }
}
