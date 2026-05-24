using ClyvoVet.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ClyvoVet.Api.Data.Configurations;

public class EventoPetConfiguration : IEntityTypeConfiguration<EventoPet>
{
    public void Configure(EntityTypeBuilder<EventoPet> builder)
    {
        builder.ToTable("T_CLYVO_EVENTO_PET");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("ID")
            .HasColumnType("VARCHAR2(36)")
            .ValueGeneratedOnAdd();

        builder.Property(e => e.Titulo)
            .HasColumnName("TITULO")
            .HasColumnType("VARCHAR2(200)")   // DDL real: VARCHAR2(200)
            .IsRequired();

        builder.Property(e => e.Descricao)
            .HasColumnName("DESCRICAO")
            .HasColumnType("VARCHAR2(1000)");

        builder.Property(e => e.Tipo)
            .HasColumnName("TIPO");

        builder.Property(e => e.Rua)
            .HasColumnName("RUA")
            .HasColumnType("VARCHAR2(300)");

        builder.Property(e => e.Numero)
            .HasColumnName("NUMERO")
            .HasColumnType("VARCHAR2(10)");   // DDL real: VARCHAR2(10)

        builder.Property(e => e.Bairro)
            .HasColumnName("BAIRRO")
            .HasColumnType("VARCHAR2(150)");  // DDL real: VARCHAR2(150)

        builder.Property(e => e.Cidade)
            .HasColumnName("CIDADE")
            .HasColumnType("VARCHAR2(100)");  // DDL real: VARCHAR2(100)

        builder.Property(e => e.Estado)
            .HasColumnName("ESTADO")
            .HasColumnType("VARCHAR2(50)");   // DDL real: VARCHAR2(50)

        builder.Property(e => e.Cep)
            .HasColumnName("CEP")
            .HasColumnType("VARCHAR2(10)");   // DDL real: VARCHAR2(10)

        // Oracle EF Core não suporta DateOnly nativamente — converter para DateTime
        var dateOnlyConverter = new ValueConverter<DateOnly, DateTime>(
            d => d.ToDateTime(TimeOnly.MinValue),
            dt => DateOnly.FromDateTime(dt));

        var dateOnlyNullConverter = new ValueConverter<DateOnly?, DateTime?>(
            d => d.HasValue ? d.Value.ToDateTime(TimeOnly.MinValue) : null,
            dt => dt.HasValue ? DateOnly.FromDateTime(dt.Value) : null);

        builder.Property(e => e.DataInicio)
            .HasColumnName("DATA_INICIO")
            .HasColumnType("DATE")
            .HasConversion(dateOnlyConverter)
            .IsRequired();

        builder.Property(e => e.DataFim)
            .HasColumnName("DATA_FIM")
            .HasColumnType("DATE")
            .HasConversion(dateOnlyNullConverter);

        builder.Property(e => e.EspecieAlvo)
            .HasColumnName("ESPECIE_ALVO");

        builder.Property(e => e.Organizador)
            .HasColumnName("ORGANIZADOR")
            .HasColumnType("VARCHAR2(200)");  // DDL real: VARCHAR2(200)

        builder.Property(e => e.Gratuito)
            .HasColumnName("GRATUITO")
            .HasColumnType("NUMBER(1)")
            .HasConversion<int>();

        builder.Property(e => e.LinkInscricao)
            .HasColumnName("LINK_INSCRICAO")
            .HasColumnType("VARCHAR2(500)");

        builder.Property(e => e.Ativo)
            .HasColumnName("ATIVO")
            .HasColumnType("NUMBER(1)")
            .HasConversion<int>();

        builder.Property(e => e.CriadoEm)
            .HasColumnName("CRIADO_EM")
            .ValueGeneratedOnAdd();
    }
}
