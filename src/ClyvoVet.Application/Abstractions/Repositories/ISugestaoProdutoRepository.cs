using ClyvoVet.Application.Common;
using ClyvoVet.Domain.Entities;

namespace ClyvoVet.Application.Abstractions.Repositories;

public interface ISugestaoProdutoRepository
{
    /// <param name="tutorId">
    /// Quando informado, devolve apenas sugestoes de animais deste tutor. Nulo
    /// significa sem recorte.
    /// </param>
    Task<PaginaDeResultados<SugestaoProduto>> GetAllAsync(ConsultaPaginada consulta, string? animalId, string? tutorId = null, bool? ativo = null);
    Task<SugestaoProduto?> GetByIdAsync(string id);
    Task<SugestaoProduto> CreateAsync(SugestaoProduto sugestao);
    Task<SugestaoProduto?> UpdateAsync(string id, SugestaoProduto sugestao);
    Task<bool> DeleteAsync(string id);
}
