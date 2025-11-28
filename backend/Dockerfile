# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# copy csproj and restore as distinct layers
COPY src/Apps/EpiSense.Api/EpiSense.Api.csproj src/Apps/EpiSense.Api/
RUN dotnet restore src/Apps/EpiSense.Api/EpiSense.Api.csproj

# copy everything else and build
COPY src/Apps/EpiSense.Api/ src/Apps/EpiSense.Api/
COPY src/Contexts/EpiSense.Analysis/ src/Contexts/EpiSense.Analysis/
COPY src/Contexts/EpiSense.Alerts/ src/Contexts/EpiSense.Alerts/
COPY src/Contexts/EpiSense.Ingestion/ src/Contexts/EpiSense.Ingestion/
WORKDIR /src/src/Apps/EpiSense.Api
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "EpiSense.Api.dll"]
