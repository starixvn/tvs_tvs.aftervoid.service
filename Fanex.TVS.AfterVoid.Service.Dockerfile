ARG CI_REGISTRY_IMAGE
FROM $CI_REGISTRY_IMAGE:builder AS deps
RUN mkdir -p /src/packages

FROM hub.nexdev.net/base/dotnet/core/sdk:9.0-alpine AS builder

ENV NUGET_PACKAGES=/src/packages
COPY --from=deps $NUGET_PACKAGES/. $NUGET_PACKAGES/

WORKDIR /src

COPY ./Sources/TicketVoidingSystem ./Sources/TicketVoidingSystem
COPY ./Sources/Fanex.TVS.AfterVoid.Service ./Sources/Fanex.TVS.AfterVoid.Service
COPY ./Fanex.TVS.AfterVoid.Website/Fanex.TVS.AfterVoid.Website.csproj ./Fanex.TVS.AfterVoid.Website/Fanex.TVS.AfterVoid.Website.csproj

WORKDIR /src/Fanex.TVS.AfterVoid.Website
RUN dotnet nuget add source https://nuget.nexdev.net/api/v2
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore -v n -r linux-x64

COPY ./Fanex.TVS.AfterVoid.Website .

RUN dotnet publish -r linux-x64 --self-contained false -o /app

FROM hub.nexdev.net/base/dotnet/core/aspnet:9.0-alpine AS base

# Don’t run as root
RUN adduser --disabled-password \
  --home /app \
  --gecos '' dotnetuser && chown -R dotnetuser /app
USER dotnetuser
#

WORKDIR /app
COPY --from=builder /app .
ENV TZ="America/La_Paz" 

ENTRYPOINT ["dotnet", "Fanex.TVS.AfterVoid.Website.dll"]
