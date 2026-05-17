using ClyvoVet.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClyvoVet.Api.Data.Configurations;

public class LembreteConfiguration : IEntityTypeConfiguration<Lembrete>
{
    public void Configure(EntityTypeBuilder<Lembrete> builder)
    {
        builder.ToTable("LEMBRETES");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id)
            .HasColumnName("ID")
            .HasColumnType("VARCHAR2(36)")
            .ValueGeneratedOnAdd();

        builder.Property(l => l.AnimalId)
            .HasColumnName("ANIMAL_ID")
            .HasColumnType("VARCHAR2(36)")
            .IsRequired();

        builder.Property(l => l.Titulo)
            .HasColumnName("TITULO")
            .HasColumnType("VARCHAR2(200)")
            .IsRequired();

        builder.Property(l => l.Descricao)
            .HasColumnName("DESCRICAO")
            .HasColumnType("VARCHAR2(1000)");

        builder.Property(l => l.Tipo)
            .HasColumnName("TIPO");

        builder.Property(l => l.AgendadoEm)
            .HasColumnName("AGENDADO_EM")
            .IsRequired();

        builder.Property(l => l.Recorrente)
            .HasColumnName("RECORRENTE")
            .HasColumnType("NUMBER(1)")
            .HasConversion<int>();

        builder.Property(l => l.Status)
            .HasColumnName("STATUS");

        builder.Property(l => l.CriadoEm)
            .HasColumnName("CRIADO_EM")
            .ValueGeneratedOnAdd();

        builder.HasOne(l => l.Animal)
            .WithMany()
            .HasForeignKey(l => l.AnimalId)
            .HasConstraintName("FK_LEMBRETES_ANIMAIS");
    }
}
