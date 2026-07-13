FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/Atlas.PARS.Api/Atlas.PARS.Api.csproj src/Atlas.PARS.Api/
RUN dotnet restore src/Atlas.PARS.Api/Atlas.PARS.Api.csproj

COPY src/ src/
RUN dotnet publish src/Atlas.PARS.Api/Atlas.PARS.Api.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

RUN addgroup --system atlas \
    && adduser --system --uid 10001 --ingroup atlas atlas

COPY --from=build /app/publish .

USER atlas
ENTRYPOINT ["dotnet", "Atlas.PARS.Api.dll"]
