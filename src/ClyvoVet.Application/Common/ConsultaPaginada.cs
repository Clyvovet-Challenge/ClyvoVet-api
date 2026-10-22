namespace ClyvoVet.Application.Common;

/// <summary>
/// O que o cliente pediu para uma listagem: qual página, de que tamanho e em que ordem. Substitui
/// os parâmetros soltos (<c>page</c>, <c>pageSize</c>…) na assinatura dos serviços e repositórios,
/// que já passavam de seis. Sem <c>OrdenarPor</c>, vale a ordem de hoje de cada repositório.
/// </summary>
public record ConsultaPaginada(
    int Page,
    int PageSize,
    string? OrdenarPor = null,
    DirecaoOrdenacao Direcao = DirecaoOrdenacao.Asc);
