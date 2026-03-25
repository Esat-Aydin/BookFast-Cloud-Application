 # Build stage
 FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
 WORKDIR /src
 
 COPY ["BookFast.API.csproj", "./"]
 RUN dotnet restore "BookFast.API.csproj"
 
 COPY . .
 RUN dotnet publish "BookFast.API.csproj" -c Release -o /app/publish
 
 # Runtime stage
 FROM mcr.microsoft.com/dotnet/aspnet:10.0
 WORKDIR /app
 COPY --from=build /app/publish .
 
 EXPOSE 5000
 ENTRYPOINT ["dotnet", "BookFast.API.dll"]
