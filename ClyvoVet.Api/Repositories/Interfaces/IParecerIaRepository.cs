using ClyvoVet.Api.Models;

namespace ClyvoVet.Api.Repositories.Interfaces;

public interface IParecerIaRepository
{
    Task<ParecerIa?> GetByAnimalIdAsync(string animalId);

    /// <summary>
    /// Grava o parecer do animal, substituindo o anterior se houver — a tabela
    /// tem UNIQUE(animal_id) de propósito: é cache, não histórico.
    /// </summary>
    Task SalvarAsync(ParecerIa parecer);
}
