FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["CodeMetrics/CodeMetrics.csproj", "CodeMetrics/"]
RUN dotnet restore "CodeMetrics/CodeMetrics.csproj"
COPY . .
WORKDIR "/src/CodeMetrics"
RUN dotnet build "CodeMetrics.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CodeMetrics.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CodeMetrics.dll"]