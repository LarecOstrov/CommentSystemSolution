# Use the official .NET 9 SDK image for building the app
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src/CommentSystem  # Ensure correct working directory

# Set explicit NuGet package location
ENV NUGET_PACKAGES=/root/.nuget/packages

# Copy the project file and restore dependencies
COPY CommentSystem.csproj ./
RUN dotnet restore --force --verbosity detailed

# Copy the remaining source files
COPY . .  

# Create the publish directory
RUN mkdir -p /app/publish

# Compile and publish the project
RUN dotnet publish -c Release -o /app/publish --no-restore

# Use the official .NET 9 ASP.NET Core runtime image for running the app
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Copy the published application from the build stage
COPY --from=build /app/publish .

# Expose the ports
EXPOSE 5000
EXPOSE 5001

# Define the entry point
ENTRYPOINT ["dotnet", "CommentSystem.dll"]

# Override the default command to set URLs dynamically
CMD ["--urls", "http://+:5000"]
