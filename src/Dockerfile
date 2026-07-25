FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY LifeInsuranceCRM.slnx ./
COPY main/LifeInsuranceCRM.API.csproj main/
COPY main/LifeInsuranceCRM.Core/LifeInsuranceCRM.Core.csproj main/LifeInsuranceCRM.Core/
COPY main/LifeInsuranceCRM.Data/LifeInsuranceCRM.Data.csproj main/LifeInsuranceCRM.Data/
COPY main/LifeInsuranceCRM.Utilities/LifeInsuranceCRM.Utilities.csproj main/LifeInsuranceCRM.Utilities/
COPY orchestration/LifeInsuranceCRM.ServiceDefaults/LifeInsuranceCRM.ServiceDefaults.csproj orchestration/LifeInsuranceCRM.ServiceDefaults/

RUN dotnet restore main/LifeInsuranceCRM.API.csproj

COPY main/ main/
COPY orchestration/LifeInsuranceCRM.ServiceDefaults/ orchestration/LifeInsuranceCRM.ServiceDefaults/

RUN dotnet publish main/LifeInsuranceCRM.API.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "LifeInsuranceCRM.API.dll"]
