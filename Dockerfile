FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["TestPlatform/TestPlatform.WebApi.csproj", "TestPlatform/"]
COPY ["TestPlatform.Service/TestPlatform.Service.csproj", "TestPlatform.Service/"]
COPY ["TestPlatform.Data/TestPlatform.Data.csproj", "TestPlatform.Data/"]
COPY ["TeastPlatform.Domain/TeastPlatform.Domain.csproj", "TeastPlatform.Domain/"]

RUN dotnet restore "TestPlatform/TestPlatform.WebApi.csproj"

COPY . .
WORKDIR "/src/TestPlatform"
RUN dotnet publish "TestPlatform.WebApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV PORT=5005
EXPOSE 5005
ENTRYPOINT ["dotnet", "TestPlatform.WebApi.dll"]
