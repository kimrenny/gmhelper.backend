FROM mcr.microsoft.com/dotnet/sdk:8.0

WORKDIR /app

COPY . .

RUN dotnet restore MatHelper.CORE/MatHelper.CORE.csproj
RUN dotnet restore MatHelper.DAL/MatHelper.DAL.csproj
RUN dotnet restore MatHelper.BLL/MatHelper.BLL.csproj
RUN dotnet restore MatHelper.API/MatHelper.API.csproj

EXPOSE 7057

ENTRYPOINT ["dotnet", "watch", "--project", "MatHelper.API/MatHelper.API.csproj", "run", "--launch-profile", "http"]
