# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first for better layer caching.
COPY ["src/Logistics.Api/Logistics.Api.csproj", "src/Logistics.Api/"]
COPY ["src/Logistics.Application/Logistics.Application.csproj", "src/Logistics.Application/"]
COPY ["src/Logistics.Infrastructure/Logistics.Infrastructure.csproj", "src/Logistics.Infrastructure/"]
COPY ["src/Logistics.Domain/Logistics.Domain.csproj", "src/Logistics.Domain/"]
COPY ["Directory.Build.props", "./"]
RUN dotnet restore "src/Logistics.Api/Logistics.Api.csproj"

COPY . .
RUN dotnet publish "src/Logistics.Api/Logistics.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Logistics.Api.dll"]
