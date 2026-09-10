using ClyvoVet.Api.Exceptions;
using ClyvoVet.Api.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ClyvoVet.Api.Errors;

/// <summary>
/// De exceção para resposta HTTP.
///
/// <para>
/// Vive fora do <c>UseExceptionHandler</c> porque dentro dele era um lambda, e
/// lambda não se testa. E esta é justamente a decisão que precisa de teste: o
/// caso do 409 só acontece com chave estrangeira de verdade, e os testes de
/// integração rodam em EF Core InMemory, que não aplica FK. A regra ficaria
/// coberta apenas por um teste que passa pelo motivo errado.
/// </para>
/// </summary>
public static class MapaDeErro
{
    /// <summary>O status que cada exceção merece.</summary>
    public static int Status(Exception? excecao) => excecao switch
    {
        NotFoundException => StatusCodes.Status404NotFound,
        BadRequestException => StatusCodes.Status400BadRequest,
        // O recorte esta ligado e a requisicao nao identifica um tutor.
        // 403 e nao 401: a X-Api-Key foi aceita, o que falta e IDENTIDADE.
        SemTutorNoTokenException => StatusCodes.Status403Forbidden,
        // Chave estrangeira barrando: o registro esta em uso por outro. E regra
        // de negocio funcionando, nao falha -- a API Java responde 409 na mesma
        // situacao, e o app ja sabe ler esse status.
        DbUpdateException => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status500InternalServerError
    };

    /// <summary>A mensagem que vai no corpo.</summary>
    public static string Mensagem(Exception? excecao) => excecao switch
    {
        NotFoundException e => e.Message,
        BadRequestException e => e.Message,
        SemTutorNoTokenException e => e.Message,
        // A mensagem do EF cita tabela e constraint -- detalhe de banco que nao
        // ajuda quem le a tela e que expoe o schema.
        DbUpdateException => "Registro em uso por outro cadastro.",
        _ => "Erro interno no servidor."
    };
}
