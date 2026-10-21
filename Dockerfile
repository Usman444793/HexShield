# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files for caching restore layer
COPY ["HexShield.Shared/HexShield.Shared.csproj", "HexShield.Shared/"]
COPY ["HexShield.Client/HexShield.Client.csproj", "HexShield.Client/"]
COPY ["HexShield/HexShield.csproj", "HexShield/"]
COPY ["HexShield.slnx", "./"]

RUN dotnet restore "HexShield/HexShield.csproj"

# Copy all source files
COPY . .

# Build and publish release
WORKDIR "/src/HexShield"
RUN dotnet publish "HexShield.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

# Run as non-root user for enterprise container security
USER $APP_UID

ENTRYPOINT ["dotnet", "HexShield.dll"]
