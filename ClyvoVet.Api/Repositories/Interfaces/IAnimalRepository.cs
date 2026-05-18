using ClyvoVet.Api.Models;

namespace ClyvoVet.Api.Repositories.Interfaces;

public interface IAnimalRepository
{
    Task<IEnumerable<Animal>> GetAllAsync(int page, int pageSize, string? tutorId);
    Task<Animal?> GetByIdAsync(string id);
    Task<Animal> CreateAsync(Animal animal);
    Task<Animal?> UpdateAsync(string id, Animal animal);
    Task<bool> DeleteAsync(string id);
}
