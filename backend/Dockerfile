FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY WMS-mini/WMS.sln ./
COPY WMS-mini/src/ ./src/
RUN dotnet restore WMS.sln
RUN dotnet publish src/WMS.API/WMS.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "WMS.API.dll"]
