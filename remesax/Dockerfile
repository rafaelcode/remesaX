# Multi-stage Dockerfile para RemesaX API
# Stage 1: build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar archivos de proyecto primero para aprovechar cache de Docker
COPY ["src/RemesaX.Api/RemesaX.Api.csproj", "RemesaX.Api/"]
COPY ["src/RemesaX.Core/RemesaX.Core.csproj", "RemesaX.Core/"]
COPY ["src/RemesaX.Infrastructure/RemesaX.Infrastructure.csproj", "RemesaX.Infrastructure/"]
RUN dotnet restore "RemesaX.Api/RemesaX.Api.csproj"

# Copiar el resto del código y publicar
COPY src/ .
WORKDIR "/src/RemesaX.Api"
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "RemesaX.Api.dll"]
