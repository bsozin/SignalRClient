# Create a base image with code and restored NuGet packages
FROM mcr.microsoft.com/dotnet/sdk:5.0 as base
WORKDIR /src

COPY . .

# Collect application artifacts
FROM base as build
RUN dotnet build -c:Release
RUN dotnet publish -c:Release -o artifacts/publish --no-build --no-restore

# Create runtime image
FROM mcr.microsoft.com/dotnet/aspnet:5.0-alpine as runtime
RUN apk add icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
WORKDIR /app
COPY --from=build /src/artifacts/publish .
ENTRYPOINT ["dotnet","SignalRClient.dll"]
