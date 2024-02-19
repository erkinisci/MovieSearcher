# Development stage
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS development

COPY . /source

WORKDIR /source/src/MovieSearcher.WebAPI

# Set the ASPNETCORE_ENVIRONMENT explicitly for dotnet watch
ENV ASPNETCORE_ENVIRONMENT=Development

# Use "dotnet watch run" for development
CMD dotnet watch run --no-launch-profile

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final

WORKDIR /app

COPY --from=build /app .

ARG UID=10001

RUN adduser \
    --disabled-password \
    --gecos "" \
    --home "/nonexistent" \
    --shell "/sbin/nologin" \
    --no-create-home \
    --uid "${UID}" \
    appuser

USER appuser

ENTRYPOINT ["dotnet", "MovieSearcher.WebAPI.dll"]