using ClyvoVet.Api.Controllers;
using ClyvoVet.Application.DTOs.Response;
using Microsoft.AspNetCore.Routing;

namespace ClyvoVet.Api.Hateoas;

/// <summary>
/// Monta os <c>_links</c> dos DTOs de resposta. Os <c>href</c> saem do <see cref="LinkGenerator"/> a
/// partir do <b>nome da ação</b> (<c>nameof</c>) — nenhuma rota escrita à mão em string, então
/// renomear ou mover uma rota não deixa link morto: o teste que a exercita quebra, ou o código nem compila.
/// </summary>
public class GeradorDeLinks(LinkGenerator gerador, IHttpContextAccessor acessor)
{
    public void Lembrete(LembreteResponse r) => r.Links = new Dictionary<string, Link>
    {
        ["self"] = Get<LembreteController>(nameof(LembreteController.GetById), new { id = r.Id }),
        ["atualizar"] = Rota<LembreteController>("PUT", nameof(LembreteController.Update), new { id = r.Id }),
        ["excluir"] = Rota<LembreteController>("DELETE", nameof(LembreteController.Delete), new { id = r.Id }),
        ["colecao"] = Get<LembreteController>(nameof(LembreteController.GetAll)),
        ["saudePreditiva"] = Get<SaudePreditivaController>(nameof(SaudePreditivaController.GetByAnimalId), new { animalId = r.AnimalId }),
    };

    public void EventoPet(EventoPetResponse r) => r.Links = new Dictionary<string, Link>
    {
        ["self"] = Get<EventoPetController>(nameof(EventoPetController.GetById), new { id = r.Id }),
        ["atualizar"] = Rota<EventoPetController>("PUT", nameof(EventoPetController.Update), new { id = r.Id }),
        ["excluir"] = Rota<EventoPetController>("DELETE", nameof(EventoPetController.Delete), new { id = r.Id }),
        ["colecao"] = Get<EventoPetController>(nameof(EventoPetController.GetAll)),
    };

    public void Produto(ProdutoResponse r) => r.Links = new Dictionary<string, Link>
    {
        ["self"] = Get<ProdutoController>(nameof(ProdutoController.GetById), new { id = r.Id }),
        ["atualizar"] = Rota<ProdutoController>("PUT", nameof(ProdutoController.Update), new { id = r.Id }),
        ["excluir"] = Rota<ProdutoController>("DELETE", nameof(ProdutoController.Delete), new { id = r.Id }),
        ["colecao"] = Get<ProdutoController>(nameof(ProdutoController.GetAll)),
    };

    public void Sugestao(SugestaoProdutoResponse r) => r.Links = new Dictionary<string, Link>
    {
        ["self"] = Get<SugestaoProdutoController>(nameof(SugestaoProdutoController.GetById), new { id = r.Id }),
        ["atualizar"] = Rota<SugestaoProdutoController>("PUT", nameof(SugestaoProdutoController.Update), new { id = r.Id }),
        ["excluir"] = Rota<SugestaoProdutoController>("DELETE", nameof(SugestaoProdutoController.Delete), new { id = r.Id }),
        ["colecao"] = Get<SugestaoProdutoController>(nameof(SugestaoProdutoController.GetAll)),
        ["saudePreditiva"] = Get<SaudePreditivaController>(nameof(SaudePreditivaController.GetByAnimalId), new { animalId = r.AnimalId }),
    };

    public void SaudePreditiva(SaudePreditivaResponse r, string animalId) => r.Links = new Dictionary<string, Link>
    {
        ["self"] = Get<SaudePreditivaController>(nameof(SaudePreditivaController.GetByAnimalId), new { animalId }),
        ["widget"] = Get<WidgetSaudePreditivaController>(nameof(WidgetSaudePreditivaController.GetByAnimalId), new { animalId }),
        ["lembretes"] = Get<LembreteController>(nameof(LembreteController.GetAll), new { animalId }),
        ["sugestoes"] = Get<SugestaoProdutoController>(nameof(SugestaoProdutoController.GetAll), new { animalId }),
    };

    public void Widget(WidgetSaudePreditivaResponse r, string animalId) => r.Links = new Dictionary<string, Link>
    {
        ["self"] = Get<WidgetSaudePreditivaController>(nameof(WidgetSaudePreditivaController.GetByAnimalId), new { animalId }),
        ["saudePreditiva"] = Get<SaudePreditivaController>(nameof(SaudePreditivaController.GetByAnimalId), new { animalId }),
    };

    public void TelegramLink(TelegramLinkResponse r, string tutorId) => r.Links = new Dictionary<string, Link>
    {
        ["self"] = Get<TelegramController>(nameof(TelegramController.GerarLink), new { tutorId }),
        ["vinculo"] = Get<TelegramController>(nameof(TelegramController.Vinculo), new { tutorId }),
    };

    public void TelegramVinculo(TelegramVinculoResponse r, string tutorId) => r.Links = new Dictionary<string, Link>
    {
        ["self"] = Get<TelegramController>(nameof(TelegramController.Vinculo), new { tutorId }),
        ["link"] = Get<TelegramController>(nameof(TelegramController.GerarLink), new { tutorId }),
        ["desvincular"] = Rota<TelegramController>("DELETE", nameof(TelegramController.Desvincular), new { tutorId }),
    };

    private Link Get<TController>(string acao, object? valores = null) => Rota<TController>("GET", acao, valores);

    private Link Rota<TController>(string metodo, string acao, object? valores = null)
    {
        var controlador = typeof(TController).Name.Replace("Controller", string.Empty);
        var caminho = gerador.GetPathByAction(acessor.HttpContext!, acao, controlador, valores);

        return new Link(caminho ?? throw new InvalidOperationException($"Não há rota para {controlador}.{acao}."), metodo);
    }
}
