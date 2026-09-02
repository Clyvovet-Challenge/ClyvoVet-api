using ClyvoVet.Api.Data;
using ClyvoVet.Api.Enums;
using ClyvoVet.Api.Models;
using ClyvoVet.Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ClyvoVet.Api.Tests.Integration;

public class LembreteRepositoryTests
{
    private static AppDbContext CriarContexto()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"LembreteRepoTestDb-{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }

    private static (Tutor tutor, Animal animal) CriarTutorEAnimal(AppDbContext context)
    {
        var tutor = new Tutor { Id = Guid.NewGuid().ToString(), Nome = "Tutor Teste", Cpf = "00000000000", CriadoEm = DateTime.UtcNow };
        var animal = new Animal { Id = Guid.NewGuid().ToString(), Nome = "Rex", Especie = "CACHORRO", TutorId = tutor.Id, Tutor = tutor, CriadoEm = DateTime.UtcNow };
        context.Tutores.Add(tutor);
        context.Animais.Add(animal);
        return (tutor, animal);
    }

    [Fact]
    public async Task GetPendentesVencendoAsync_LembretePendenteDentroDaJanela_RetornaOLembrete()
    {
        // Arrange
        using var context = CriarContexto();
        var (_, animal) = CriarTutorEAnimal(context);
        context.Lembretes.Add(new Lembrete
        {
            Id = Guid.NewGuid().ToString(),
            AnimalId = animal.Id,
            Animal = animal,
            Titulo = "Vacina",
            Tipo = TipoLembreteEnum.Vacina,
            AgendadoEm = DateTime.UtcNow.AddMinutes(30),
            Status = StatusLembreteEnum.Pendente,
            CriadoEm = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
        var repository = new LembreteRepository(context);

        // Act
        var result = await repository.GetPendentesVencendoAsync(DateTime.UtcNow.AddHours(1));

        // Assert
        Assert.Single(result);
    }

    [Fact]
    public async Task GetPendentesVencendoAsync_LembreteJaEnviado_NaoRetornaOLembrete()
    {
        // Arrange
        using var context = CriarContexto();
        var (_, animal) = CriarTutorEAnimal(context);
        context.Lembretes.Add(new Lembrete
        {
            Id = Guid.NewGuid().ToString(),
            AnimalId = animal.Id,
            Animal = animal,
            Titulo = "Vacina",
            Tipo = TipoLembreteEnum.Vacina,
            AgendadoEm = DateTime.UtcNow.AddMinutes(30),
            Status = StatusLembreteEnum.Enviado,
            CriadoEm = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
        var repository = new LembreteRepository(context);

        // Act
        var result = await repository.GetPendentesVencendoAsync(DateTime.UtcNow.AddHours(1));

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPendentesVencendoAsync_LembreteForaDaJanela_NaoRetornaOLembrete()
    {
        // Arrange
        using var context = CriarContexto();
        var (_, animal) = CriarTutorEAnimal(context);
        context.Lembretes.Add(new Lembrete
        {
            Id = Guid.NewGuid().ToString(),
            AnimalId = animal.Id,
            Animal = animal,
            Titulo = "Vacina",
            Tipo = TipoLembreteEnum.Vacina,
            AgendadoEm = DateTime.UtcNow.AddDays(5),
            Status = StatusLembreteEnum.Pendente,
            CriadoEm = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
        var repository = new LembreteRepository(context);

        // Act
        var result = await repository.GetPendentesVencendoAsync(DateTime.UtcNow.AddHours(1));

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPendentesByTutorIdAsync_TutorComLembretePendente_RetornaOLembrete()
    {
        // Arrange
        using var context = CriarContexto();
        var (tutor, animal) = CriarTutorEAnimal(context);
        context.Lembretes.Add(new Lembrete
        {
            Id = Guid.NewGuid().ToString(),
            AnimalId = animal.Id,
            Animal = animal,
            Titulo = "Vacina",
            Tipo = TipoLembreteEnum.Vacina,
            AgendadoEm = DateTime.UtcNow.AddDays(2),
            Status = StatusLembreteEnum.Pendente,
            CriadoEm = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
        var repository = new LembreteRepository(context);

        // Act
        var result = await repository.GetPendentesByTutorIdAsync(tutor.Id);

        // Assert
        Assert.Single(result);
    }

    [Fact]
    public async Task GetPendentesByTutorIdAsync_LembreteJaEnviado_NaoRetornaOLembrete()
    {
        // Arrange
        using var context = CriarContexto();
        var (tutor, animal) = CriarTutorEAnimal(context);
        context.Lembretes.Add(new Lembrete
        {
            Id = Guid.NewGuid().ToString(),
            AnimalId = animal.Id,
            Animal = animal,
            Titulo = "Vacina",
            Tipo = TipoLembreteEnum.Vacina,
            AgendadoEm = DateTime.UtcNow.AddDays(2),
            Status = StatusLembreteEnum.Enviado,
            CriadoEm = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
        var repository = new LembreteRepository(context);

        // Act
        var result = await repository.GetPendentesByTutorIdAsync(tutor.Id);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPendentesByTutorIdAsync_LembreteDeOutroTutor_NaoRetornaOLembrete()
    {
        // Arrange
        using var context = CriarContexto();
        var (_, animal) = CriarTutorEAnimal(context);
        context.Lembretes.Add(new Lembrete
        {
            Id = Guid.NewGuid().ToString(),
            AnimalId = animal.Id,
            Animal = animal,
            Titulo = "Vacina",
            Tipo = TipoLembreteEnum.Vacina,
            AgendadoEm = DateTime.UtcNow.AddDays(2),
            Status = StatusLembreteEnum.Pendente,
            CriadoEm = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
        var repository = new LembreteRepository(context);

        // Act
        var result = await repository.GetPendentesByTutorIdAsync(Guid.NewGuid().ToString());

        // Assert
        Assert.Empty(result);
    }
}
