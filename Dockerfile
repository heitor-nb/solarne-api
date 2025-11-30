FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY "SolarneApi.sln" .
COPY "SolarneApi.csproj" .

RUN dotnet restore "SolarneApi.csproj"

COPY . .

RUN dotnet publish "SolarneApi.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "SolarneApi.dll"]