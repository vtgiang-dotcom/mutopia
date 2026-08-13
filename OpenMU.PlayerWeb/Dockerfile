# Multi-stage build for OpenMU.PlayerWeb (Blazor Web App, .NET 9)
# Produces a small runtime image (~250MB) with static SSR.

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Restore dependencies
COPY ["OpenMU.PlayerWeb.csproj", "./"]
RUN dotnet restore "OpenMU.PlayerWeb.csproj"

# Copy the rest and publish
COPY . .
RUN dotnet publish "OpenMU.PlayerWeb.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "OpenMU.PlayerWeb.dll"]
