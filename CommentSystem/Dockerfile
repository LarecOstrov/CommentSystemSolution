# Use the .NET 9 SDK for building the app
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY ["CommentSystem/CommentSystem.csproj", "./"]
RUN dotnet restore "./CommentSystem.csproj"

# Copy all source files
COPY . .

# Create necessary directories and set permissions
RUN mkdir -p /src/bin /src/obj /app/build /app/publish && \
    chmod -R 777 /src/bin /src/obj /app/build /app/publish

# Build and publish the app
RUN dotnet build "CommentSystem.csproj" -c Release -o /app/build
RUN dotnet publish "CommentSystem.csproj" -c Release -o /app/publish --no-build

# Use the ASP.NET Core runtime for running the app
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5000
EXPOSE 5001
ENTRYPOINT ["dotnet", "CommentSystem.dll"]





# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
#FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
#USER $APP_UID
#WORKDIR /app
#EXPOSE 8080
#EXPOSE 8081
#
#
## This stage is used to build the service project
#FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
#ARG BUILD_CONFIGURATION=Release
#WORKDIR /src
#COPY ["CommentSystem/CommentSystem.csproj", "CommentSystem/"]
#RUN dotnet restore "./CommentSystem/CommentSystem.csproj"
#COPY . .
#WORKDIR "/src/CommentSystem"
#RUN dotnet build "./CommentSystem.csproj" -c $BUILD_CONFIGURATION -o /app/build
#
## This stage is used to publish the service project to be copied to the final stage
#FROM build AS publish
#ARG BUILD_CONFIGURATION=Release
#RUN dotnet publish "./CommentSystem.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false
#
## This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
#FROM base AS final
#WORKDIR /app
#COPY --from=publish /app/publish .
#ENTRYPOINT ["dotnet", "CommentSystem.dll"]