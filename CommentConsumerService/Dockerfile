# Base runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
RUN apt-get update && apt-get install -y procps
EXPOSE 5002  
EXPOSE 5003 

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# 1. Copy only project files for dependency restore
COPY CommentConsumerService/CommentConsumerService.csproj CommentConsumerService/
COPY Common/Common.csproj Common/
COPY Common/appsettings.json /app/appsettings.json

# 2. Restore dependencies
RUN dotnet restore CommentConsumerService/CommentConsumerService.csproj

# 3. Copy the entire project source after restore
COPY CommentConsumerService/ CommentConsumerService/
COPY Common/ Common/

WORKDIR /src/CommentConsumerService

# 4. Build the project
RUN dotnet build -c $BUILD_CONFIGURATION -o /app/build

# 5. Publish the application (optimized for Linux)
RUN dotnet publish -c $BUILD_CONFIGURATION -o /app/publish --runtime linux-x64 --self-contained false /p:UseAppHost=false

# Final runtime stage
FROM base AS final
WORKDIR /app

# 6. Copy built files from the previous stage
COPY --from=build /app/publish .

# 8. Health check to detect crashes
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD sh -c "pgrep -f 'dotnet CommentConsumerService.dll' || exit 1"

# 9. Run the application
ENTRYPOINT ["dotnet", "CommentConsumerService.dll"]