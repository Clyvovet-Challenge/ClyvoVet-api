using ClyvoVet.Domain.Enums;
using ClyvoVet.Domain.Entities;
using ClyvoVet.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace ClyvoVet.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Tutor> Tutores => Set<Tutor>();
    public DbSet<Animal> Animais => Set<Animal>();
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<Lembrete> Lembretes => Set<Lembrete>();
    public DbSet<EventoPet> EventosPet => Set<EventoPet>();
    public DbSet<SugestaoProduto> SugestoesProduto => Set<SugestaoProduto>();
    public DbSet<PredisposicaoSaude> PredisposicoesSaude => Set<PredisposicaoSaude>();
    public DbSet<TutorTelegram> TutoresTelegram => Set<TutorTelegram>();
    public DbSet<Raca> Racas => Set<Raca>();
    public DbSet<BaseDoenca> BaseDoencas => Set<BaseDoenca>();
    public DbSet<ParecerIa> PareceresIa => Set<ParecerIa>();

    // A Application e a Api não conhecem o EF: a falha de integridade (chave estrangeira
    // barrando um delete, por exemplo) sobe como RegistroEmUsoException, que o MapaDeErro
    // traduz para 409. Toda DbUpdateException — inclusive a de concorrência — era 409
    // antes desta mudança, e continua sendo.
    public override async Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
        catch (DbUpdateException excecao)
        {
            throw new RegistroEmUsoException(excecao);
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        try
        {
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }
        catch (DbUpdateException excecao)
        {
            throw new RegistroEmUsoException(excecao);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        modelBuilder.Entity<Produto>()
            .Property(p => p.Categoria)
            .HasColumnType("VARCHAR(30)")
            .HasConversion(
                v => v.ToString().ToUpper(),
                v => (CategoriaEnum)Enum.Parse(typeof(CategoriaEnum), v, true));

        modelBuilder.Entity<Produto>()
            .Property(p => p.EspecieIndicada)
            .HasColumnType("VARCHAR(30)")
            .HasConversion(
                v => v.ToString().ToUpper(),
                v => (EspecieEnum)Enum.Parse(typeof(EspecieEnum), v, true));

        modelBuilder.Entity<Produto>()
            .Property(p => p.PorteIndicado)
            .HasColumnType("VARCHAR(30)")
            .HasConversion(
                v => v.ToString().ToUpper(),
                v => (PorteEnum)Enum.Parse(typeof(PorteEnum), v, true));

        modelBuilder.Entity<Lembrete>()
            .Property(l => l.Tipo)
            .HasColumnType("VARCHAR(30)")
            .HasConversion(
                v => v.ToString().ToUpper(),
                v => (TipoLembreteEnum)Enum.Parse(typeof(TipoLembreteEnum), v, true));

        modelBuilder.Entity<Lembrete>()
            .Property(l => l.Status)
            .HasColumnType("VARCHAR(30)")
            .HasConversion(
                v => v.ToString().ToUpper(),
                v => (StatusLembreteEnum)Enum.Parse(typeof(StatusLembreteEnum), v, true));

        modelBuilder.Entity<EventoPet>()
            .Property(e => e.Tipo)
            .HasColumnType("VARCHAR(30)")
            .HasConversion(
                v => v.ToString().ToUpper(),
                v => (TipoEventoPetEnum)Enum.Parse(typeof(TipoEventoPetEnum), v, true));

        modelBuilder.Entity<EventoPet>()
            .Property(e => e.EspecieAlvo)
            .HasColumnType("VARCHAR(30)")
            .HasConversion(
                v => v.ToString().ToUpper(),
                v => (EspecieEnum)Enum.Parse(typeof(EspecieEnum), v, true));

        modelBuilder.Entity<PredisposicaoSaude>()
            .Property(p => p.Especie)
            .HasColumnType("VARCHAR(30)")
            .HasConversion(
                v => v.ToString().ToUpper(),
                v => (EspecieEnum)Enum.Parse(typeof(EspecieEnum), v, true));
    }
}
