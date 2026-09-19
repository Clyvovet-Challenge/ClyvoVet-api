using ClyvoVet.Api.Data;
using ClyvoVet.Api.Models;
using ClyvoVet.Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ClyvoVet.Api.Tests.Integration;

public class AnimalRepositoryTests
{
    private static AppDbContext CriarContexto()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"AnimalRepoTestDb-{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetByTutorIdAsync_TutorComAnimais_RetornaOrdenadoPorNome()
    {
        // Arrange
        using var context = CriarContexto();
        var tutor = new Tutor { Id = Guid.NewGuid().ToString(), Nome = "Tutor Teste", Cpf = "00000000000" };
        context.Tutores.Add(tutor);
        context.Animais.Add(new Animal { Id = Guid.NewGuid().ToString(), Nome = "Zeus", Especie = "CACHORRO", TutorId = tutor.Id, Tutor = tutor });
        context.Animais.Add(new Animal { Id = Guid.NewGuid().ToString(), Nome = "Amora", Especie = "GATO", TutorId = tutor.Id, Tutor = tutor });
        await context.SaveChangesAsync();
        var repository = new AnimalRepository(context);

        // Act
        var result = (await repository.GetByTutorIdAsync(tutor.Id)).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Amora", result[0].Nome);
        Assert.Equal("Zeus", result[1].Nome);
    }

    [Fact]
    public async Task GetByTutorIdAsync_AnimalDeOutroTutor_NaoRetornaOAnimal()
    {
        // Arrange
        using var context = CriarContexto();
        var tutor = new Tutor { Id = Guid.NewGuid().ToString(), Nome = "Tutor Teste", Cpf = "00000000000" };
        context.Tutores.Add(tutor);
        context.Animais.Add(new Animal { Id = Guid.NewGuid().ToString(), Nome = "Rex", Especie = "CACHORRO", TutorId = tutor.Id, Tutor = tutor });
        await context.SaveChangesAsync();
        var repository = new AnimalRepository(context);

        // Act
        var result = await repository.GetByTutorIdAsync(Guid.NewGuid().ToString());

        // Assert
        Assert.Empty(result);
    }
}
