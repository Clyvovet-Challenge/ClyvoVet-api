using ClyvoVet.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClyvoVet.Api.Data.Configurations;

public class VeterinarioConfiguration : IEntityTypeConfiguration<Veterinario>
{
    public void Configure(EntityTypeBuilder<Veterinario> builder)
    {
        builder.ToTable("t_clyvo_veterinario");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Id)
            .HasColumnName("id")
            .HasColumnType("VARCHAR(36)")
            .ValueGeneratedOnAdd();

        builder.Property(v => v.Nome)
            .HasColumnName("nome")
            .HasColumnType("VARCHAR(150)")   // DDL real: VARCHAR(150)
            .IsRequired();

        builder.Property(v => v.Crmv)
            .HasColumnName("crmv")
            .HasColumnType("VARCHAR(30)");

        builder.Property(v => v.Email)
            .HasColumnName("email")
            .HasColumnType("VARCHAR(200)");

        builder.Property(v => v.Especialidade)
            .HasColumnName("especialidade")
            .HasColumnType("VARCHAR(100)");

        builder.Property(v => v.CriadoEm)
            .HasColumnName("criado_em")
            .ValueGeneratedOnAdd();
    }
}
