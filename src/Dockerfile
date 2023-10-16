FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore

WORKDIR /src/Frouros
RUN dotnet publish -c release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app ./

ENV DEBIAN_FRONTEND=noninteractive
RUN apt update && apt install -y libpcap-dev

ENTRYPOINT ["dotnet", "Frouros.dll"]
