FROM node:24-alpine AS frontend
WORKDIR /frontend
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci
COPY frontend/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY Directory.Build.props .
COPY src/ src/
RUN dotnet publish src/MatForge.Api -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
COPY --from=frontend /frontend/dist ./wwwroot
EXPOSE 8080
ENTRYPOINT ["dotnet", "MatForge.Api.dll"]
