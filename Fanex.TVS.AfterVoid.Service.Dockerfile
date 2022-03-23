FROM registry.nexdev.net:5050/docker/dotnet/core/sdk:5.0.102-ca-patch-buster-slim AS builder
WORKDIR /src
# Install the tools
RUN dotnet tool install --tool-path /tools dotnet-trace
RUN dotnet tool install --tool-path /tools dotnet-dump
RUN dotnet tool install --tool-path /tools dotnet-counters

COPY ./Sources/ .
RUN ls /src -l

WORKDIR /src/Fanex.TVS.AfterVoid.Service
RUN dotnet nuget add source http://nuget.nexdev.net/api/v2
RUN dotnet restore -v n
RUN dotnet publish -r linux-x64 --self-contained false -o /app 

FROM registry.nexdev.net:5050/docker/dotnet/core/aspnet:5.0-buster-slim AS base
RUN sed -i 's/DEFAULT@SECLEVEL=2/DEFAULT@SECLEVEL=1/g' /etc/ssl/openssl.cnf
RUN sed -i 's/MinProtocol = TLSv1.2/MinProtocol = TLSv1/g' /etc/ssl/openssl.cnf
RUN sed -i 's/DEFAULT@SECLEVEL=2/DEFAULT@SECLEVEL=1/g' /usr/lib/ssl/openssl.cnf
RUN sed -i 's/MinProtocol = TLSv1.2/MinProtocol = TLSv1/g' /usr/lib/ssl/openssl.cnf
RUN apt-get update --allow-releaseinfo-change
RUN apt-get update && apt-get install -y libgdiplus
RUN apt-get update && apt install fonts-indic -y
RUN apt-get update && apt-get install -y libfontconfig1
RUN apt-get install smbclient -y
RUN apt update && apt install fonts-indic -y

ENV TZ="America/La_Paz" 

WORKDIR /app
COPY --from=builder /app .
COPY --from=builder /tools /tools

ENTRYPOINT ["dotnet", "Fanex.TVS.AfterVoid.Service.dll"]
