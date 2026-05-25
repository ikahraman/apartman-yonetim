FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/ApartmanYonetim.Web/ApartmanYonetim.Web.csproj", "src/ApartmanYonetim.Web/"]
COPY ["src/ApartmanYonetim.Application/ApartmanYonetim.Application.csproj", "src/ApartmanYonetim.Application/"]
COPY ["src/ApartmanYonetim.Infrastructure/ApartmanYonetim.Infrastructure.csproj", "src/ApartmanYonetim.Infrastructure/"]
COPY ["src/ApartmanYonetim.Domain/ApartmanYonetim.Domain.csproj", "src/ApartmanYonetim.Domain/"]
RUN dotnet restore "src/ApartmanYonetim.Web/ApartmanYonetim.Web.csproj"
COPY . .
RUN dotnet publish "src/ApartmanYonetim.Web/ApartmanYonetim.Web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p /app/data/sites
VOLUME /app/data
ENTRYPOINT ["dotnet", "ApartmanYonetim.Web.dll"]
