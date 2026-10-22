FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia src/ inteiro, e não só o csproj da Api: ela referencia Application e
# Infrastructure, e uma lista de csproj para manter à mão é o tipo de coisa que
# ninguém lembra de atualizar quando entra o próximo projeto.
COPY src/ src/
RUN dotnet restore src/ClyvoVet.Api/ClyvoVet.Api.csproj

WORKDIR /src/src/ClyvoVet.Api
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
