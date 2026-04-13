FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Orders.Worker/Orders.Worker.csproj", "Orders.Worker/"]
COPY ["src/Orders.Contracts/Orders.Contracts.csproj", "Orders.Contracts/"]
RUN dotnet restore "Orders.Worker/Orders.Worker.csproj"

COPY src/Orders.Worker/ Orders.Worker/
COPY src/Orders.Contracts/ Orders.Contracts/
RUN dotnet publish "Orders.Worker/Orders.Worker.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Orders.Worker.dll"]
