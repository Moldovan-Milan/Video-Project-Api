# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

# Install ffpmeg (linux)
USER root
RUN apt-get update && apt-get install -y ffmpeg

# Set environment variables
ENV FFMPEG_PATH=/usr/bin/ffmpeg

WORKDIR /app
EXPOSE 5047
EXPOSE 7124

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["OmegaStreamWebAPI/OmegaStreamWebAPI.csproj", "OmegaStreamWebAPI/"]
COPY ["OmegaStreamServices/OmegaStreamServices.csproj", "OmegaStreamServices/"]
RUN dotnet restore "./OmegaStreamWebAPI/OmegaStreamWebAPI.csproj"
COPY . .
WORKDIR "/src/OmegaStreamWebAPI"
RUN dotnet build "./OmegaStreamWebAPI.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./OmegaStreamWebAPI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OmegaStreamWebAPI.dll"]
