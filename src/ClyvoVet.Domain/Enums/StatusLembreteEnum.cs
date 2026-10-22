namespace ClyvoVet.Domain.Enums;

public enum StatusLembreteEnum
{
    Pendente,
    Enviado,
    // Esta API nunca grava Cancelado: o POST força Pendente e o PUT ignora o status do corpo.
    // O valor fica porque o CHECK de t_clyvo_lembrete e o app (filtro e cor do card) já o
    // conhecem; tirá-lo faria ?status=Cancelado virar 400 e uma linha CANCELADO no banco
    // estourar na leitura (Enum.Parse no AppDbContext).
    Cancelado
}
