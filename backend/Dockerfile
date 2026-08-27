FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY WMS-mini/WMS.sln ./
COPY WMS-mini/src/ ./src/
# Chỉ restore project API để image không phụ thuộc project test (WMS.Tests).
RUN dotnet restore src/WMS.API/WMS.API.csproj
RUN dotnet publish src/WMS.API/WMS.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "WMS.API.dll"]
