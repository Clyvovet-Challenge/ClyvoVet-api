using ClyvoVet.Domain.Entities;

namespace ClyvoVet.Api.Repositories.Interfaces;

public interface IBaseDoencaRepository
{
    /// <param name="especieCodigo">Vocabulário do catálogo: CAO, GATO, ROEDOR, AVE, REPTIL.</param>
    Task<IReadOnlyList<BaseDoenca>> GetByEspecieAsync(string especieCodigo);
}
