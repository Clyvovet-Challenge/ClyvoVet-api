using ClyvoVet.Domain.Entities;

namespace ClyvoVet.Api.Repositories.Interfaces;

public interface IAnimalRepository
{
    Task<Animal?> GetByIdAsync(string id);
    Task<IEnumerable<Animal>> GetByTutorIdAsync(string tutorId);
}
