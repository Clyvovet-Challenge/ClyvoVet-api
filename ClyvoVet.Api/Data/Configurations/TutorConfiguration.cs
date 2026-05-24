using ClyvoVet.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClyvoVet.Api.Data.Configurations;

public class TutorConfiguration : IEntityTypeConfiguration<Tutor>
{
    public void Configure(EntityTypeBuilder<Tutor> builder)
    {
        builder.ToTable("T_CLYVO_TUTOR");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasColumnName("ID")
            .HasColumnType("VARCHAR2(36)")
            .ValueGeneratedOnAdd();

        builder.Property(t => t.Nome)
            .HasColumnName("NOME")
            .HasColumnType("VARCHAR2(150)")   // DDL real: VARCHAR2(150)
            .IsRequired();

        builder.Property(t => t.Cpf)
            .HasColumnName("CPF")
            .HasColumnType("VARCHAR2(11)");   // DDL real: VARCHAR2(11), sem NOT NULL

        builder.Property(t => t.Email)
            .HasColumnName("EMAIL")
            .HasColumnType("VARCHAR2(200)");

        builder.Property(t => t.Telefone)
            .HasColumnName("TELEFONE")
            .HasColumnType("VARCHAR2(20)");

        // DDL real: t_clyvo_tutor não tem coluna ENDERECO.
        // O endereço é armazenado em colunas separadas (rua, numero, bairro,
        // cidade, estado, cep) que não são mapeadas nesta API.
        builder.Ignore(t => t.Endereco);

        // DDL real: coluna ATIVO não existe em t_clyvo_tutor
        builder.Ignore(t => t.Ativo);

        builder.Property(t => t.CriadoEm)
            .HasColumnName("CRIADO_EM")
            .ValueGeneratedOnAdd();
    }
}
