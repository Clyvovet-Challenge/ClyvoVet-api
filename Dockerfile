FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ClyvoVet.Api/ClyvoVet.Api.csproj ClyvoVet.Api/
RUN dotnet restore ClyvoVet.Api/ClyvoVet.Api.csproj

COPY ClyvoVet.Api/ ClyvoVet.Api/
WORKDIR /src/ClyvoVet.Api
RUN dotnet publish ClyvoVet.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# O Render (e a maioria dos PaaS) injeta a porta via variável de ambiente PORT em
# tempo de execução — só sabemos o valor real quando o container sobe, por isso o
# ASPNETCORE_URLS é montado no CMD (shell form) em vez de um ENV fixo no build.
ENV ASPNETCORE_ENVIRONMENT=Production

# "exec" substitui o processo do shell pelo dotnet, em vez de rodar como filho —
# necessário pra o container repassar corretamente o SIGTERM de shutdown (senão o
# Render mata o container na força depois do timeout, sem dar chance de encerrar limpo).
CMD ["/bin/sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} exec dotnet ClyvoVet.Api.dll"]
