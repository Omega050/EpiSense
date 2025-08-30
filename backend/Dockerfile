# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# copy csproj and restore as distinct layers
COPY src/EpiSense.Api/EpiSense.Api.csproj src/EpiSense.Api/
RUN dotnet restore src/EpiSense.Api/EpiSense.Api.csproj

# copy everything else and build
COPY src/EpiSense.Api/ src/EpiSense.Api/
WORKDIR /src/src/EpiSense.Api
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "EpiSense.Api.dll"]
