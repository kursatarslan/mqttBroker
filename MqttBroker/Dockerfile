FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 1883

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["MqttBroker.csproj", "MqttBroker/"]
RUN dotnet restore "MqttBroker/MqttBroker.csproj"
COPY . .
WORKDIR "/src/MqttBroker"
COPY . .
RUN dotnet build "MqttBroker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MqttBroker.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MqttBroker.dll"]