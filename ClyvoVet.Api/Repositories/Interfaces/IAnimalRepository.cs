using ClyvoVet.Api.Models;

namespace ClyvoVet.Api.Repositories.Interfaces;

public interface IAnimalRepository
{
    Task<Animal?> GetByIdAsync(string id);
}
