FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

COPY . .

RUN dotnet restore MatHelper.API/MatHelper.API.csproj

RUN dotnet publish MatHelper.API/MatHelper.API.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 7057

ENV ASPNETCORE_URLS=http://+:7057

ENTRYPOINT ["dotnet", "MatHelper.API.dll"]