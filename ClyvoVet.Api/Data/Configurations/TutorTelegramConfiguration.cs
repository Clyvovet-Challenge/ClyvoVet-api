using ClyvoVet.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClyvoVet.Api.Data.Configurations;

public class TutorTelegramConfiguration : IEntityTypeConfiguration<TutorTelegram>
{
    public void Configure(EntityTypeBuilder<TutorTelegram> builder)
    {
        builder.ToTable("T_CLYVO_TUTOR_TELEGRAM");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasColumnName("ID")
            .HasColumnType("VARCHAR2(36)")
            .ValueGeneratedOnAdd();

        builder.Property(t => t.TutorId)
            .HasColumnName("TUTOR_ID")
            .HasColumnType("VARCHAR2(36)")
            .IsRequired();

        builder.HasIndex(t => t.TutorId).IsUnique();

        builder.Property(t => t.ChatId)
            .HasColumnName("CHAT_ID")
            .IsRequired();

        builder.Property(t => t.CriadoEm)
            .HasColumnName("CRIADO_EM")
            .ValueGeneratedOnAdd();
    }
}
