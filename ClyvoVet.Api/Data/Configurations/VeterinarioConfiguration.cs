using ClyvoVet.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClyvoVet.Api.Data.Configurations;

public class VeterinarioConfiguration : IEntityTypeConfiguration<Veterinario>
{
    public void Configure(EntityTypeBuilder<Veterinario> builder)
    {
        builder.ToTable("T_CLYVO_VETERINARIO");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Id)
            .HasColumnName("ID")
            .HasColumnType("VARCHAR2(36)")
            .ValueGeneratedOnAdd();

        builder.Property(v => v.Nome)
            .HasColumnName("NOME")
            .HasColumnType("VARCHAR2(200)")
            .IsRequired();

        builder.Property(v => v.Crmv)
            .HasColumnName("CRMV")
            .HasColumnType("VARCHAR2(20)")
            .IsRequired();

        builder.Property(v => v.Email)
            .HasColumnName("EMAIL")
            .HasColumnType("VARCHAR2(200)");

        builder.Property(v => v.Especialidade)
            .HasColumnName("ESPECIALIDADE")
            .HasColumnType("VARCHAR2(100)");

        builder.Property(v => v.Ativo)
            .HasColumnName("ATIVO")
            .HasColumnType("NUMBER(1)")
            .HasConversion<int>();

        builder.Property(v => v.CriadoEm)
            .HasColumnName("CRIADO_EM")
            .ValueGeneratedOnAdd();
    }
}
