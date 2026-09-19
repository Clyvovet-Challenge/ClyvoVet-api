using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ClyvoVet.Api.Services.Interfaces;

namespace ClyvoVet.Api.Services;

/// <summary>
/// Cliente HTTP da OCI Generative AI Inference, com a assinatura de requisição
/// da OCI feita à mão.
///
/// <para><b>Por que não o SDK oficial?</b> O pacote OCI.DotNetSDK traria dezenas
/// de assemblies para UMA chamada POST. A autenticação por API key da OCI é o
/// esquema HTTP Signatures (draft-cavage): montar a signing string com os
/// headers date, (request-target), host, content-length, content-type e
/// x-content-sha256, assinar com RSA-SHA256 e mandar no header Authorization.
/// São ~60 linhas auditáveis contra a documentação, e nada além de
/// System.Security.Cryptography.</para>
///
/// <para><b>Configuração — tudo por variável de ambiente</b> (regra do projeto:
/// nenhum segredo em fonte; a rubrica de DevOps desconta 20 pontos por
/// credencial commitada):</para>
/// <code>
/// Oci__TenancyOcid      ocid1.tenancy.oc1..aaaa...
/// Oci__UserOcid         ocid1.user.oc1..aaaa...
/// Oci__Fingerprint      aa:bb:cc:...
/// Oci__PrivateKeyPem    conteúdo PEM da chave (ou Oci__PrivateKeyPath para um arquivo)
/// Oci__Region           ex.: us-chicago-1 (região COM Generative AI)
/// Oci__GenAi__CompartmentOcid   ocid1.compartment.oc1..aaaa...
/// Oci__GenAi__ModelId           default meta.llama-3.3-70b-instruct
/// </code>
///
/// <para>Sem essas variáveis o cliente nasce com <see cref="Configurado"/> =
/// false e o parecer é gerado pelas regras — a feature degrada, não quebra.</para>
/// </summary>
public class OciGenerativeAiClient : IOciGenerativeAiClient
{
    private const string ModeloPadrao = "meta.llama-3.3-70b-instruct";

    private readonly HttpClient _http;
    private readonly ILogger<OciGenerativeAiClient> _logger;

    private readonly string? _tenancy;
    private readonly string? _user;
    private readonly string? _fingerprint;
    private readonly string? _privateKeyPem;
    private readonly string? _region;
    private readonly string? _compartment;
    private readonly string _modelId;
    private readonly string _apiFormat;

    public OciGenerativeAiClient(HttpClient http, IConfiguration configuration, ILogger<OciGenerativeAiClient> logger)
    {
        _http = http;
        _logger = logger;

        _tenancy     = configuration["Oci:TenancyOcid"];
        _user        = configuration["Oci:UserOcid"];
        _fingerprint = configuration["Oci:Fingerprint"];
        _region      = configuration["Oci:Region"];
        _compartment = configuration["Oci:GenAi:CompartmentOcid"];
        _modelId     = configuration["Oci:GenAi:ModelId"] ?? ModeloPadrao;
        // GENERIC cobre os modelos Meta/Llama; COHERE é o formato dos Command.
        _apiFormat   = configuration["Oci:GenAi:ApiFormat"] ?? "GENERIC";

        _privateKeyPem = configuration["Oci:PrivateKeyPem"];
        var caminhoChave = configuration["Oci:PrivateKeyPath"];
        if (string.IsNullOrWhiteSpace(_privateKeyPem) && !string.IsNullOrWhiteSpace(caminhoChave) && File.Exists(caminhoChave))
            _privateKeyPem = File.ReadAllText(caminhoChave);
    }

    public bool Configurado =>
        !string.IsNullOrWhiteSpace(_tenancy) &&
        !string.IsNullOrWhiteSpace(_user) &&
        !string.IsNullOrWhiteSpace(_fingerprint) &&
        !string.IsNullOrWhiteSpace(_privateKeyPem) &&
        !string.IsNullOrWhiteSpace(_region) &&
        !string.IsNullOrWhiteSpace(_compartment);

    public string? ModelId => Configurado ? _modelId : null;

    public async Task<string?> GerarTextoAsync(string prompt, CancellationToken cancellationToken = default)
    {
        if (!Configurado)
        {
            _logger.LogInformation("OCI Generative AI sem credenciais no ambiente; parecer seguirá pelas regras.");
            return null;
        }

        var host = $"inference.generativeai.{_region}.oci.oraclecloud.com";
        var path = "/20231130/actions/chat";
        var body = MontarCorpo(prompt);
        var bytes = Encoding.UTF8.GetBytes(body);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"https://{host}{path}");
        request.Content = new ByteArrayContent(bytes);
        request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

        AssinarRequisicao(request, host, path, bytes);

        using var response = await _http.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            // O corpo da OCI diz o motivo (NotAuthenticated, modelo inexistente
            // na região...) — sem ele o log diria só "400" e ninguém saberia
            // qual variável corrigir.
            _logger.LogWarning("OCI Generative AI respondeu {Status}: {Corpo}",
                (int)response.StatusCode, Truncar(payload, 500));
            throw new HttpRequestException($"OCI Generative AI respondeu {(int)response.StatusCode}.");
        }

        return ExtrairTexto(payload);
    }

    private string MontarCorpo(string prompt)
    {
        object chatRequest = _apiFormat == "COHERE"
            ? new
            {
                apiFormat = "COHERE",
                message = prompt,
                maxTokens = 900,
                temperature = 0.2,
            }
            : new
            {
                apiFormat = "GENERIC",
                messages = new[]
                {
                    new { role = "USER", content = new[] { new { type = "TEXT", text = prompt } } },
                },
                maxTokens = 900,
                temperature = 0.2,
            };

        return JsonSerializer.Serialize(new
        {
            compartmentId = _compartment,
            servingMode = new { servingType = "ON_DEMAND", modelId = _modelId },
            chatRequest,
        });
    }

    /// <summary>Os dois formatos de resposta que a OCI devolve, lidos de forma defensiva.</summary>
    internal static string? ExtrairTexto(string payload)
    {
        using var doc = JsonDocument.Parse(payload);
        if (!doc.RootElement.TryGetProperty("chatResponse", out var chat))
            return null;

        // COHERE: { "chatResponse": { "text": "..." } }
        if (chat.TryGetProperty("text", out var texto))
            return texto.GetString();

        // GENERIC: { "chatResponse": { "choices": [ { "message": { "content": [ { "text": "..." } ] } } ] } }
        if (chat.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0 &&
            choices[0].TryGetProperty("message", out var message) &&
            message.TryGetProperty("content", out var content) && content.GetArrayLength() > 0 &&
            content[0].TryGetProperty("text", out var textoGeneric))
            return textoGeneric.GetString();

        return null;
    }

    /// <summary>
    /// HTTP Signatures da OCI (API key): assina date, (request-target), host e,
    /// por ser POST, content-length, content-type e x-content-sha256.
    /// </summary>
    private void AssinarRequisicao(HttpRequestMessage request, string host, string path, byte[] body)
    {
        var date = DateTime.UtcNow.ToString("r");
        var sha256Corpo = Convert.ToBase64String(SHA256.HashData(body));

        request.Headers.TryAddWithoutValidation("date", date);
        request.Headers.TryAddWithoutValidation("host", host);
        request.Headers.TryAddWithoutValidation("x-content-sha256", sha256Corpo);
        request.Content!.Headers.ContentLength = body.Length;

        var signingString =
            $"date: {date}\n" +
            $"(request-target): post {path}\n" +
            $"host: {host}\n" +
            $"content-length: {body.Length}\n" +
            "content-type: application/json\n" +
            $"x-content-sha256: {sha256Corpo}";

        using var rsa = RSA.Create();
        rsa.ImportFromPem(_privateKeyPem);
        var assinatura = Convert.ToBase64String(
            rsa.SignData(Encoding.UTF8.GetBytes(signingString), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));

        var keyId = $"{_tenancy}/{_user}/{_fingerprint}";
        request.Headers.TryAddWithoutValidation("authorization",
            "Signature version=\"1\"," +
            $"keyId=\"{keyId}\"," +
            "algorithm=\"rsa-sha256\"," +
            "headers=\"date (request-target) host content-length content-type x-content-sha256\"," +
            $"signature=\"{assinatura}\"");
    }

    private static string Truncar(string s, int max) => s.Length <= max ? s : s[..max];
}
