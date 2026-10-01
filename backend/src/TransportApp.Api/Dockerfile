# Build de TransportApp.Api. El contexto de build debe ser la carpeta
# backend/ del repositorio (ver docker-compose.yml), porque el proyecto
# depende de los demás proyectos de la solución.

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY TransportApp.sln .
COPY Directory.Build.props .
COPY src/TransportApp.Api/TransportApp.Api.csproj src/TransportApp.Api/
COPY src/TransportApp.Application/TransportApp.Application.csproj src/TransportApp.Application/
COPY src/TransportApp.Domain/TransportApp.Domain.csproj src/TransportApp.Domain/
COPY src/TransportApp.Infrastructure/TransportApp.Infrastructure.csproj src/TransportApp.Infrastructure/
RUN dotnet restore src/TransportApp.Api/TransportApp.Api.csproj

COPY src/ src/
RUN dotnet publish src/TransportApp.Api/TransportApp.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "TransportApp.Api.dll"]
