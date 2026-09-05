using ClyvoVet.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClyvoVet.Api.Data.Configurations;

public class TutorConfiguration : IEntityTypeConfiguration<Tutor>
{
    public void Configure(EntityTypeBuilder<Tutor> builder)
    {
        builder.ToTable("t_clyvo_tutor");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasColumnName("id")
            .HasColumnType("VARCHAR(36)")
            .ValueGeneratedOnAdd();

        builder.Property(t => t.Nome)
            .HasColumnName("nome")
            .HasColumnType("VARCHAR(150)")   // DDL real: VARCHAR(150)
            .IsRequired();

        builder.Property(t => t.Cpf)
            .HasColumnName("cpf")
            .HasColumnType("VARCHAR(11)");   // DDL real: VARCHAR(11), sem NOT NULL

        builder.Property(t => t.Email)
            .HasColumnName("email")
            .HasColumnType("VARCHAR(200)");

        builder.Property(t => t.Telefone)
            .HasColumnName("telefone")
            .HasColumnType("VARCHAR(20)");

        builder.Property(t => t.CriadoEm)
            .HasColumnName("criado_em")
            .ValueGeneratedOnAdd();
    }
}
