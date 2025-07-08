# Use the official .NET SDK image for build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY TISOtpApi.csproj ./
RUN dotnet restore ./TISOtpApi.csproj

# Copy the rest of the source code
COPY . .

# Build the application
RUN dotnet publish TISOtpApi.csproj -c Release -o /app/publish --no-restore

# Use the official ASP.NET runtime image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Expose port 80 and 443
EXPOSE 80
EXPOSE 443

# Set environment variables (optional)
# ENV ASPNETCORE_URLS=http://+:80

ENTRYPOINT ["dotnet", "TISOtpApi.dll"]
