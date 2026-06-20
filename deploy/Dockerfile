# R2-ACC-CAP6: multi-stage build for the LocalAIFactory web app (CPU; GPU is optional and external via Ollama).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore LocalAIFactory.sln
RUN dotnet publish src/LocalAIFactory.Web/LocalAIFactory.Web.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
# The app migrates + seeds + installs the base knowledge pack on startup; it needs a reachable MSSQL only.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "LocalAIFactory.Web.dll"]
