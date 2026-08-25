using System.Net;
using System.Net.Http.Json;
using ClyvoVet.Api.Data;
using ClyvoVet.Api.DTOs.Response;
using ClyvoVet.Api.Enums;
using ClyvoVet.Api.Models;
using Microsoft.Extensions.DependencyInjection;

namespace ClyvoVet.Api.Tests.Integration;

// Não usa a IntegrationTestCollection compartilhada porque precisa semear um Animal
// com raça e idade específicas (a fixture padrão só cria o Animal "Rex" genérico,
// sem raça/idade, usado pelos outros testes de Lembrete/SugestaoProduto).
public class WidgetSaudePreditivaEndpointsTests
{
    [Fact]
    public async Task GetByAnimalId_LabradorSeteAnos_RetornaPredisposicoesESugereConsulta()
    {
        // Arrange
        using var factory = new IntegrationTestFixture();
        var client = factory.CreateClient();

        string animalId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var tutor = new Tutor { Id = Guid.NewGuid().ToString(), Nome = "Tutor Teste Widget", Cpf = "22222222222", CriadoEm = DateTime.UtcNow };
            var animal = new Animal
            {
                Id = Guid.NewGuid().ToString(),
                Nome = "Thor",
                Especie = "CACHORRO",
                Raca = "Labrador Retriever",
                DataNascimento = DateTime.UtcNow.AddYears(-7),
                TutorId = tutor.Id,
                CriadoEm = DateTime.UtcNow
            };
            db.Tutores.Add(tutor);
            db.Animais.Add(animal);
            db.PredisposicoesSaude.Add(new PredisposicaoSaude
            {
                Id = Guid.NewGuid().ToString(),
                Especie = EspecieEnum.Cachorro,
                Raca = "Labrador",
                IdadeMinimaAnos = 6,
                Doenca = "Displasia de quadril",
                Recomendacao = "Agendar avaliacao ortopedica",
                CriadoEm = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            animalId = animal.Id;
        }

        // Act
        var response = await client.GetAsync($"/api/v1/widget-saude-preditiva/{animalId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<WidgetSaudePreditivaResponse>();
        Assert.NotNull(result);
        Assert.Equal("Thor", result!.NomeAnimal);
        Assert.True(result.SugerirAgendamentoConsulta);
        Assert.Single(result.Predisposicoes);
        Assert.Equal("Displasia de quadril", result.Predisposicoes[0].Doenca);
    }

    [Fact]
    public async Task GetByAnimalId_IdInexistente_RetornaNotFound()
    {
        // Arrange
        using var factory = new IntegrationTestFixture();
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/widget-saude-preditiva/id-que-nao-existe");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
