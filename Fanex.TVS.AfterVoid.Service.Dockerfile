FROM hub.nexdev.net/base/dotnet/core/sdk:5.0.102-ca-patch-buster-slim AS builder
WORKDIR /src

COPY ./Sources/TicketVoidingSystem ./Sources/TicketVoidingSystem
COPY ./Sources/Fanex.TVS.AfterVoid.Service ./Sources/Fanex.TVS.AfterVoid.Service
COPY ./Fanex.TVS.AfterVoid.Website/Fanex.TVS.AfterVoid.Website.csproj ./Fanex.TVS.AfterVoid.Website/Fanex.TVS.AfterVoid.Website.csproj

WORKDIR /src/Fanex.TVS.AfterVoid.Website
RUN dotnet nuget add source http://nuget.nexdev.net/api/v2
RUN dotnet restore -v n

COPY ./Fanex.TVS.AfterVoid.Website .

RUN dotnet publish -r linux-x64 --self-contained false -o /app

FROM hub.nexdev.net/base/dotnet/core/aspnet:5.0-buster-slim AS base

RUN sed -i 's/DEFAULT@SECLEVEL=2/DEFAULT@SECLEVEL=1/g' /etc/ssl/openssl.cnf
RUN sed -i 's/MinProtocol = TLSv1.2/MinProtocol = TLSv1/g' /etc/ssl/openssl.cnf
RUN sed -i 's/DEFAULT@SECLEVEL=2/DEFAULT@SECLEVEL=1/g' /usr/lib/ssl/openssl.cnf
RUN sed -i 's/MinProtocol = TLSv1.2/MinProtocol = TLSv1/g' /usr/lib/ssl/openssl.cnf

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
