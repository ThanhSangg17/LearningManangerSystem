# =========================
# BUILD STAGE
# =========================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files first
COPY ["PRN232.LearningManagerSystem.API/PRN232.LearningManagerSystem.API.csproj", "PRN232.LearningManagerSystem.API/"]
COPY ["PRN232.LearningManagerSystem.Services/PRN232.LearningManagerSystem.Services.csproj", "PRN232.LearningManagerSystem.Services/"]
COPY ["PRN232.LearningManagerSystem.Repositories/PRN232.LearningManagerSystem.Repositories.csproj", "PRN232.LearningManagerSystem.Repositories/"]

# Restore packages
RUN dotnet restore "PRN232.LearningManagerSystem.API/PRN232.LearningManagerSystem.API.csproj"

# Copy all source code
COPY . .

# Publish API
WORKDIR "/src/PRN232.LearningManagerSystem.API"
RUN dotnet publish "PRN232.LearningManagerSystem.API.csproj" -c Release -o /app/publish

# =========================
# RUNTIME STAGE
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "PRN232.LearningManagerSystem.API.dll"]