using ClyvoVet.Domain.Entities;

namespace ClyvoVet.Application.Abstractions.Repositories;

public interface IAnimalRepository
{
    Task<Animal?> GetByIdAsync(string id);
    Task<IEnumerable<Animal>> GetByTutorIdAsync(string tutorId);
}
