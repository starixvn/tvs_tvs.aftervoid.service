FROM hub.nexdev.net/base/dotnet/core/sdk:7.0-alpine AS builder
WORKDIR /src

COPY ./Sources/TicketVoidingSystem ./Sources/TicketVoidingSystem
COPY ./Sources/Fanex.TVS.AfterVoid.Service ./Sources/Fanex.TVS.AfterVoid.Service
COPY ./Fanex.TVS.AfterVoid.Website/Fanex.TVS.AfterVoid.Website.csproj ./Fanex.TVS.AfterVoid.Website/Fanex.TVS.AfterVoid.Website.csproj

WORKDIR /src/Fanex.TVS.AfterVoid.Website
RUN dotnet nuget add source http://nuget.nexdev.net/api/v2
RUN dotnet restore -v n

COPY ./Fanex.TVS.AfterVoid.Website .

RUN dotnet publish -r linux-x64 --self-contained false -c Release -o /app

FROM hub.nexdev.net/base/dotnet/core/aspnet:7.0 AS base

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
