FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Orders.Api/Orders.Api.csproj", "Orders.Api/"]
COPY ["src/Orders.Application/Orders.Application.csproj", "Orders.Application/"]
COPY ["src/Orders.Domain/Orders.Domain.csproj", "Orders.Domain/"]
COPY ["src/Orders.Infrastructure/Orders.Infrastructure.csproj", "Orders.Infrastructure/"]
COPY ["src/Orders.Contracts/Orders.Contracts.csproj", "Orders.Contracts/"]
RUN dotnet restore "Orders.Api/Orders.Api.csproj"

COPY src/Orders.Api/ Orders.Api/
COPY src/Orders.Application/ Orders.Application/
COPY src/Orders.Domain/ Orders.Domain/
COPY src/Orders.Infrastructure/ Orders.Infrastructure/
COPY src/Orders.Contracts/ Orders.Contracts/
RUN dotnet publish "Orders.Api/Orders.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Orders.Api.dll"]
