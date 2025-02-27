# Base runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
RUN apt-get update && apt-get install -y curl
EXPOSE 5000  
EXPOSE 5001 

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# 1. Copy only project files for dependency restore
COPY CommentSystem/CommentSystem.csproj CommentSystem/
COPY Common/Common.csproj Common/
COPY Common/appsettings.json /app/appsettings.json

# 2. Restore dependencies
RUN dotnet restore CommentSystem/CommentSystem.csproj

# 3. Copy the entire project source after restore
COPY CommentSystem/ CommentSystem/
COPY Common/ Common/

WORKDIR /src/CommentSystem

# 4. Build the project
RUN dotnet build -c $BUILD_CONFIGURATION -o /app/build

# 5. Publish the application (optimized for Linux)
RUN dotnet publish -c $BUILD_CONFIGURATION -o /app/publish --runtime linux-x64 --self-contained false /p:UseAppHost=false

# Final runtime stage
FROM base AS final
WORKDIR /app

# 6. Copy built files from the previous stage
COPY --from=build /app/publish .

# 7. Verify contents
RUN ls -lah /app

# 8. Health check to detect crashes
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD curl --fail http://localhost:5001/health || exit 1

# 9. Run the application
ENTRYPOINT ["dotnet", "CommentSystem.dll"]