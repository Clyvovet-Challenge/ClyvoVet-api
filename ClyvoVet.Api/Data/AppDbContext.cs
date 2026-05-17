using ClyvoVet.Api.Enums;
using ClyvoVet.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ClyvoVet.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Tutor> Tutores => Set<Tutor>();
    public DbSet<Animal> Animais => Set<Animal>();
    public DbSet<Veterinario> Veterinarios => Set<Veterinario>();
    public DbSet<Consulta> Consultas => Set<Consulta>();
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<Lembrete> Lembretes => Set<Lembrete>();
    public DbSet<EventoPet> EventosPet => Set<EventoPet>();
    public DbSet<SugestaoProduto> SugestoesProduto => Set<SugestaoProduto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        modelBuilder.Entity<Produto>()
            .Property(p => p.Categoria)
            .HasColumnType("VARCHAR2(30)")
            .HasConversion(
                v => v.ToString().ToUpper(),
                v => (CategoriaEnum)Enum.Parse(typeof(CategoriaEnum), v, true));

        modelBuilder.Entity<Produto>()
            .Property(p => p.EspecieIndicada)
            .HasColumnType("VARCHAR2(30)")
            .HasConversion(
                v => v.ToString().ToUpper(),
                v => (EspecieEnum)Enum.Parse(typeof(EspecieEnum), v, true));

        modelBuilder.Entity<Lembrete>()
            .Property(l => l.Tipo)
            .HasColumnType("VARCHAR2(30)")
            .HasConversion(
                v => v.ToString().ToUpper(),
                v => (TipoLembreteEnum)Enum.Parse(typeof(TipoLembreteEnum), v, true));

        modelBuilder.Entity<Lembrete>()
            .Property(l => l.Status)
            .HasColumnType("VARCHAR2(30)")
            .HasConversion(
                v => v.ToString().ToUpper(),
                v => (StatusLembreteEnum)Enum.Parse(typeof(StatusLembreteEnum), v, true));

        modelBuilder.Entity<EventoPet>()
            .Property(e => e.Tipo)
            .HasColumnType("VARCHAR2(30)")
            .HasConversion(
                v => v.ToString().ToUpper(),
                v => (TipoEventoPetEnum)Enum.Parse(typeof(TipoEventoPetEnum), v, true));

        modelBuilder.Entity<EventoPet>()
            .Property(e => e.EspecieAlvo)
            .HasColumnType("VARCHAR2(30)")
            .HasConversion(
                v => v.ToString().ToUpper(),
                v => (EspecieEnum)Enum.Parse(typeof(EspecieEnum), v, true));
    }
}
