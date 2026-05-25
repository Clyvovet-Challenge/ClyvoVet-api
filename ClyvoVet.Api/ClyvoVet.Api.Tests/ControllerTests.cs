using Moq;
using Microsoft.AspNetCore.Mvc;
using ClyvoVet.Api.Controllers;
using ClyvoVet.Api.Services.Interfaces;
using ClyvoVet.Api.DTOs.Response;
using ClyvoVet.Api.Enums;

namespace ClyvoVet.Api.Tests;

public class ControllerTests
{
    [Fact]
    public async Task EventoPetController_GetAll_ReturnsOk()
    {
        var mockService = new Mock<IEventoPetService>();
        mockService.Setup(s => s.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<TipoEventoPetEnum?>(), It.IsAny<EspecieEnum?>()))
                   .ReturnsAsync(new List<EventoPetResponse>());
        var controller = new EventoPetController(mockService.Object);

        var result = await controller.GetAll();

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task LembreteController_GetAll_ReturnsOk()
    {
        var mockService = new Mock<ILembreteService>();
        mockService.Setup(s => s.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<StatusLembreteEnum?>(), It.IsAny<TipoLembreteEnum?>()))
                   .ReturnsAsync(new List<LembreteResponse>());
        var controller = new LembreteController(mockService.Object);

        var result = await controller.GetAll();

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ProdutoController_GetAll_ReturnsOk()
    {
        var mockService = new Mock<IProdutoService>();
        mockService.Setup(s => s.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CategoriaEnum?>(), It.IsAny<EspecieEnum?>()))
                   .ReturnsAsync(new List<ProdutoResponse>());
        var controller = new ProdutoController(mockService.Object);

        var result = await controller.GetAll();

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task SugestaoProdutoController_GetAll_ReturnsOk()
    {
        var mockService = new Mock<ISugestaoProdutoService>();
        mockService.Setup(s => s.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
                   .ReturnsAsync(new List<SugestaoProdutoResponse>());
        var controller = new SugestaoProdutoController(mockService.Object);

        var result = await controller.GetAll();

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task EventoPetController_GetById_ReturnsOk()
    {
        var mockService = new Mock<IEventoPetService>();
        mockService.Setup(s => s.GetByIdAsync(It.IsAny<string>()))
                   .ReturnsAsync(new EventoPetResponse());
        var controller = new EventoPetController(mockService.Object);

        var result = await controller.GetById("uuid");

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task LembreteController_GetById_ReturnsOk()
    {
        var mockService = new Mock<ILembreteService>();
        mockService.Setup(s => s.GetByIdAsync(It.IsAny<string>()))
                   .ReturnsAsync(new LembreteResponse());
        var controller = new LembreteController(mockService.Object);

        var result = await controller.GetById("uuid");

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ProdutoController_GetById_ReturnsOk()
    {
        var mockService = new Mock<IProdutoService>();
        mockService.Setup(s => s.GetByIdAsync(It.IsAny<string>()))
                   .ReturnsAsync(new ProdutoResponse());
        var controller = new ProdutoController(mockService.Object);

        var result = await controller.GetById("uuid");

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task SugestaoProdutoController_GetById_ReturnsOk()
    {
        var mockService = new Mock<ISugestaoProdutoService>();
        mockService.Setup(s => s.GetByIdAsync(It.IsAny<string>()))
                   .ReturnsAsync(new SugestaoProdutoResponse());
        var controller = new SugestaoProdutoController(mockService.Object);

        var result = await controller.GetById("uuid");

        Assert.IsType<OkObjectResult>(result);
    }
}
