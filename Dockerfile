FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props .
COPY src/ECommerce.API/ECommerce.API.csproj src/ECommerce.API/
COPY src/ECommerce.Shared/ECommerce.Shared.csproj src/ECommerce.Shared/
COPY src/ECommerce.Modules.Catalog/ECommerce.Modules.Catalog.csproj src/ECommerce.Modules.Catalog/
COPY src/ECommerce.Modules.Ordering/ECommerce.Modules.Ordering.csproj src/ECommerce.Modules.Ordering/
COPY src/ECommerce.Modules.Billing/ECommerce.Modules.Billing.csproj src/ECommerce.Modules.Billing/

RUN dotnet restore src/ECommerce.API/ECommerce.API.csproj

COPY src/ src/
RUN dotnet publish src/ECommerce.API/ECommerce.API.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
RUN apt-get update && apt-get install -y --no-install-recommends libgssapi-krb5-2 && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .

EXPOSE 8080
ENTRYPOINT ["dotnet", "ECommerce.API.dll"]
