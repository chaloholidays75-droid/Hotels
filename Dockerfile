# -------------------------
# 1. Build stage
# -------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# copy csproj and restore
COPY *.csproj ./
RUN dotnet restore

# copy everything else
COPY . .
RUN dotnet publish -c Release -o /app/publish

# -------------------------
# 2. Runtime stage
# -------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# expose default port
EXPOSE 8080

# run app
ENTRYPOINT ["dotnet", "HotelAPI.dll"]
