# Azure Container Apps Deploy

## İlk Kurulum

```bash
# Azure CLI giriş
az login

# Resource group
az group create --name apartman-rg --location westeurope

# Container Registry
az acr create --resource-group apartman-rg --name apartmanacr --sku Basic

# Container Apps ortamı
az containerapp env create \
  --name apartman-env \
  --resource-group apartman-rg \
  --location westeurope

# Azure Files (kalıcı depolama)
az storage account create \
  --name apartmanstorage \
  --resource-group apartman-rg \
  --sku Standard_LRS

az storage share create \
  --name apartman-data \
  --account-name apartmanstorage
```

## Her Güncellemede (Kod veya DB Şeması)

```bash
# 1. Yeni migration varsa ekle (şema değiştiyse)
dotnet ef migrations add YeniOzellik \
  --project src/ApartmanYonetim.Infrastructure \
  --startup-project src/ApartmanYonetim.Web \
  --context MainDbContext \
  --output-dir Migrations/Main

dotnet ef migrations add YeniOzellik \
  --project src/ApartmanYonetim.Infrastructure \
  --startup-project src/ApartmanYonetim.Web \
  --context SiteDbContext \
  --output-dir Migrations/Site

# 2. Image build & push
az acr login --name apartmanacr

docker build -t apartmanacr.azurecr.io/apartman-yonetim:latest .
docker push apartmanacr.azurecr.io/apartman-yonetim:latest

# 3. Container Apps güncelle (migration'lar startup'ta otomatik çalışır)
az containerapp update \
  --name apartman-app \
  --resource-group apartman-rg \
  --image apartmanacr.azurecr.io/apartman-yonetim:latest
```

## Notlar
- Migration'lar `Program.cs`'deki `db.Database.MigrateAsync()` ile startup'ta otomatik uygulanır
- `data/` klasörü Azure Files'a mount edilir — veriler deployment'larda korunur
- Volume mount path: `/app/data`
