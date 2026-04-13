FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Orders.Gateway/Orders.Gateway.csproj", "Orders.Gateway/"]
RUN dotnet restore "Orders.Gateway/Orders.Gateway.csproj"

COPY src/Orders.Gateway/ Orders.Gateway/
RUN dotnet publish "Orders.Gateway/Orders.Gateway.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Orders.Gateway.dll"]
