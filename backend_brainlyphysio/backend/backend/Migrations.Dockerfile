# Use the .NET SDK image for building and running migrations
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
# Add dotnet tools to PATH
ENV PATH="/root/.dotnet/tools:$PATH"

# Set working directory
WORKDIR /src

# Install required dependencies (e.g. mariadb-client)
RUN apt-get update && apt-get install -y mariadb-client

# Install the dotnet-ef tool globally
RUN dotnet tool install --global dotnet-ef

ENV DB_HOST=db
ENV DB_PORT=3306
ENV DB_USER=stefan12
ENV DB_PASSWORD=stefan12
ENV DB_NAME=BrainlyPhysioDb
ENV DOCKER=true
# Copy only the project file first to leverage Docker cache during restore
COPY backend/backend.csproj backend/

# Switch to the project directory and restore dependencies
WORKDIR /src/backend
RUN dotnet restore

# Now copy the rest of the source code
COPY backend/ ./

# Ensure the migration script is executable
RUN chmod +x ./migrations.sh

# Build the project in Release configuration (this generates all needed artifacts)
RUN dotnet build "backend.csproj" -c Release

# At runtime, execute the migration script.
CMD ["/bin/bash", "./migrations.sh"]
