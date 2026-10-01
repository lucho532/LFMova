# Build de LFMova.Api. El contexto de build debe ser la carpeta
# backend/ del repositorio (ver docker-compose.yml), porque el proyecto
# depende de los demás proyectos de la solución.

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY LFMova.sln .
COPY Directory.Build.props .
COPY src/LFMova.Api/LFMova.Api.csproj src/LFMova.Api/
COPY src/LFMova.Application/LFMova.Application.csproj src/LFMova.Application/
COPY src/LFMova.Domain/LFMova.Domain.csproj src/LFMova.Domain/
COPY src/LFMova.Infrastructure/LFMova.Infrastructure.csproj src/LFMova.Infrastructure/
RUN dotnet restore src/LFMova.Api/LFMova.Api.csproj

COPY src/ src/
RUN dotnet publish src/LFMova.Api/LFMova.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "LFMova.Api.dll"]
