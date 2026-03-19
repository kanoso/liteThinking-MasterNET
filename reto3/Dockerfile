# Etapa 1: build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/OrderSystem.Api/OrderSystem.Api.csproj", "src/OrderSystem.Api/"]
COPY ["src/OrderSystem.Application/OrderSystem.Application.csproj", "src/OrderSystem.Application/"]
COPY ["src/OrderSystem.Domain/OrderSystem.Domain.csproj", "src/OrderSystem.Domain/"]
COPY ["src/OrderSystem.Infrastructure/OrderSystem.Infrastructure.csproj", "src/OrderSystem.Infrastructure/"]
RUN dotnet restore "src/OrderSystem.Api/OrderSystem.Api.csproj"

COPY . .
WORKDIR "/src/src/OrderSystem.Api"
RUN dotnet publish "OrderSystem.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Etapa 2: runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "OrderSystem.Api.dll"]
