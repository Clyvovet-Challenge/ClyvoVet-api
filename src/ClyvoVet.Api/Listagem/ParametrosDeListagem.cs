using ClyvoVet.Application.Common;
using ClyvoVet.Domain.Exceptions;

namespace ClyvoVet.Api.Listagem;

/// <summary>
/// Valida e monta a <see cref="ConsultaPaginada"/> dos quatro controllers de listagem. A validação
/// de <c>page</c> e <c>pageSize</c> estava copiada em cada um; as mensagens são as de sempre, então
/// o 400 e o campo <c>error</c> que o app lê não mudam.
/// </summary>
public static class ParametrosDeListagem
{
    public static ConsultaPaginada Montar(int page, int pageSize, string? ordenarPor, string? direcao)
    {
        if (page < 1)
            throw new BadRequestException("O parâmetro 'page' deve ser maior que zero.");
        if (pageSize < 1 || pageSize > 100)
            throw new BadRequestException("O parâmetro 'pageSize' deve estar entre 1 e 100.");

        // "direcao sem ordenarPor é ignorada" (design §6.3): nem chega a ser validada.
        if (string.IsNullOrWhiteSpace(ordenarPor))
            return new ConsultaPaginada(page, pageSize);

        return new ConsultaPaginada(page, pageSize, ordenarPor.Trim(), LerDirecao(direcao));
    }

    private static DirecaoOrdenacao LerDirecao(string? direcao) => direcao?.Trim().ToLowerInvariant() switch
    {
        null or "" or "asc" => DirecaoOrdenacao.Asc,
        "desc" => DirecaoOrdenacao.Desc,
        _ => throw new BadRequestException($"Direção '{direcao}' inválida. Use 'asc' ou 'desc'."),
    };
}
