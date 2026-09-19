namespace ClyvoVet.Api.Services.Interfaces;

/// <summary>
/// Cliente da OCI Generative AI (Inference, endpoint de chat). A abstração
/// existe por dois motivos: os testes trocam a OCI por um fake, e o serviço de
/// saúde preditiva decide pelo <see cref="Configurado"/> se tenta a IA ou vai
/// direto ao fallback determinístico — sem try/catch de configuração espalhado.
/// </summary>
public interface IOciGenerativeAiClient
{
    /// <summary>
    /// Verdadeiro quando todas as credenciais OCI estão no ambiente. Falso não
    /// é erro: é o modo "sem IA", em que o parecer sai das regras.
    /// </summary>
    bool Configurado { get; }

    /// <summary>O modelo configurado (ex.: 'meta.llama-3.3-70b-instruct'), para registrar no parecer.</summary>
    string? ModelId { get; }

    /// <summary>
    /// Envia o prompt e devolve o texto da resposta, ou null quando o cliente
    /// não está configurado. Falhas de rede/HTTP sobem como exceção — quem
    /// chama decide o fallback.
    /// </summary>
    Task<string?> GerarTextoAsync(string prompt, CancellationToken cancellationToken = default);
}
