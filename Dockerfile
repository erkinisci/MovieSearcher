# Development stage
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build

WORKDIR /src

COPY ["src/MovieSearcher.Core/MovieSearcher.Core.csproj", "MovieSearcher.Core/"]
COPY ["src/MovieSearcher.Services/MovieSearcher.Services.csproj", "MovieSearcher.Services/"]
COPY ["src/MovieSearcher.VimeoWrapper/MovieSearcher.VimeoWrapper.csproj", "MovieSearcher.VimeoWrapper/"]
COPY ["src/MovieSearcher.YoutubeWrapper/MovieSearcher.YoutubeWrapper.csproj", "MovieSearcher.YoutubeWrapper/"]
COPY ["src/MovieSearcher.WebAPI/MovieSearcher.WebAPI.csproj", "MovieSearcher.WebAPI/"]

RUN dotnet restore "MovieSearcher.WebAPI/MovieSearcher.WebAPI.csproj"

COPY . .

RUN dotnet publish -c Release -o /app "src/MovieSearcher.WebAPI/MovieSearcher.WebAPI.csproj"

# Publish stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS publish
WORKDIR /app

COPY --from=build /app ./

ENTRYPOINT ["dotnet", "MovieSearcher.WebAPI.dll"]
