using ClyvoVet.Api.Data;
using ClyvoVet.Api.Repositories;
using ClyvoVet.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseOracle(builder.Configuration.GetConnectionString("OracleConnection")));

builder.Services.AddScoped<IProdutoRepository, ProdutoRepository>();
builder.Services.AddScoped<ISugestaoProdutoRepository, SugestaoProdutoRepository>();
builder.Services.AddScoped<ILembreteRepository, LembreteRepository>();
builder.Services.AddScoped<IEventoPetRepository, EventoPetRepository>();
builder.Services.AddScoped<IAnimalRepository, AnimalRepository>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
