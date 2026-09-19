using ClyvoVet.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClyvoVet.Api.Data.Configurations;

public class TutorTelegramConfiguration : IEntityTypeConfiguration<TutorTelegram>
{
    public void Configure(EntityTypeBuilder<TutorTelegram> builder)
    {
        builder.ToTable("t_clyvo_tutor_telegram");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasColumnName("id")
            .HasColumnType("VARCHAR(36)")
            .ValueGeneratedNever();

        builder.Property(t => t.TutorId)
            .HasColumnName("tutor_id")
            .HasColumnType("VARCHAR(36)")
            .IsRequired();

        builder.HasIndex(t => t.TutorId).IsUnique();

        builder.Property(t => t.ChatId)
            .HasColumnName("chat_id")
            .IsRequired();

        builder.Property(t => t.CriadoEm)
            .HasColumnName("criado_em");
    }
}
