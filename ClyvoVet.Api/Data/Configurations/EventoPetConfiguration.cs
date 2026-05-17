using ClyvoVet.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClyvoVet.Api.Data.Configurations;

public class EventoPetConfiguration : IEntityTypeConfiguration<EventoPet>
{
    public void Configure(EntityTypeBuilder<EventoPet> builder)
    {
        builder.ToTable("EVENTOS_PET");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("ID")
            .HasColumnType("VARCHAR2(36)")
            .ValueGeneratedOnAdd();

        builder.Property(e => e.Titulo)
            .HasColumnName("TITULO")
            .HasColumnType("VARCHAR2(300)")
            .IsRequired();

        builder.Property(e => e.Descricao)
            .HasColumnName("DESCRICAO")
            .HasColumnType("VARCHAR2(2000)");

        builder.Property(e => e.Tipo)
            .HasColumnName("TIPO");

        builder.Property(e => e.Rua)
            .HasColumnName("RUA")
            .HasColumnType("VARCHAR2(300)");

        builder.Property(e => e.Numero)
            .HasColumnName("NUMERO")
            .HasColumnType("VARCHAR2(20)");

        builder.Property(e => e.Bairro)
            .HasColumnName("BAIRRO")
            .HasColumnType("VARCHAR2(200)");

        builder.Property(e => e.Cidade)
            .HasColumnName("CIDADE")
            .HasColumnType("VARCHAR2(200)");

        builder.Property(e => e.Estado)
            .HasColumnName("ESTADO")
            .HasColumnType("VARCHAR2(2)");

        builder.Property(e => e.Cep)
            .HasColumnName("CEP")
            .HasColumnType("VARCHAR2(9)");

        builder.Property(e => e.DataInicio)
            .HasColumnName("DATA_INICIO")
            .HasColumnType("DATE")
            .IsRequired();

        builder.Property(e => e.DataFim)
            .HasColumnName("DATA_FIM")
            .HasColumnType("DATE");

        builder.Property(e => e.EspecieAlvo)
            .HasColumnName("ESPECIE_ALVO");

        builder.Property(e => e.Organizador)
            .HasColumnName("ORGANIZADOR")
            .HasColumnType("VARCHAR2(300)");

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
