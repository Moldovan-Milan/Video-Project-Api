
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER root
RUN apt-get update && apt-get install -y ffmpeg
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
USER root
RUN apt-get update && apt-get install -y ffmpeg
WORKDIR /src


COPY ["OmegaStreamWebAPI/OmegaStreamWebAPI.csproj", "OmegaStreamWebAPI/"]
COPY ["OmegaStreamServices/OmegaStreamServices.csproj", "OmegaStreamServices/"]
RUN dotnet restore "OmegaStreamWebAPI/OmegaStreamWebAPI.csproj"


COPY . .
WORKDIR "/src/OmegaStreamWebAPI"
RUN dotnet build "OmegaStreamWebAPI.csproj" -c Release -o /app/build


FROM build AS publish
RUN dotnet publish "OmegaStreamWebAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OmegaStreamWebAPI.dll"]
