FROM mcr.microsoft.com/dotnet/sdk:8.0

WORKDIR /src

COPY . .

RUN dotnet restore MatHelper.API/MatHelper.API.csproj

RUN dotnet build MatHelper.API/MatHelper.API.csproj -c Debug --no-restore

EXPOSE 7057

ENTRYPOINT ["dotnet", "watch", "--project", "MatHelper.API/MatHelper.API.csproj", "run", "--launch-profile", "http"]
