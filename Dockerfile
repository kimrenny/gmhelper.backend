FROM mcr.microsoft.com/dotnet/sdk:9.0

WORKDIR /src

COPY . .

RUN dotnet restore MatHelper.API/MatHelper.API.csproj

RUN dotnet publish MatHelper.API/MatHelper.API.csproj -c Release -o /app

WORKDIR /app

EXPOSE 7057

ENTRYPOINT ["dotnet", "MatHelper.API.dll"]
