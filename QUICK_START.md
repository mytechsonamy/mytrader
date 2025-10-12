# 🚀 MyTrader Production Deployment - Hızlı Başlangıç

**Sunucu**: 213.238.180.201
**Domain**: mytrader.tech

---

## Adım 1: Sunucu Kurulumunu Başlat

Yerel makinenizde terminal açın ve şu komutu çalıştırın:

```bash
cd /Users/mustafayildirim/Documents/Personal\ Documents/Projects/myTrader
./scripts/setup-production-server.sh
```

Bu script:
- ✓ SSH bağlantısını test eder
- ✓ Docker kurulumunu kontrol eder (yoksa kurar)
- ✓ `/opt/mytrader` dizinini oluşturur
- ✓ Gerekli klasörleri hazırlar
- ✓ Güvenli şifreler üretir

**Çıktı:** Script size PostgreSQL şifresi ve JWT secret verecek. **Bunları kaydedin!**

---

## Adım 2: Sunucuya Bağlan ve .env.production Oluştur

```bash
# Sunucuya SSH ile bağlan
ssh root@213.238.180.201

# Proje dizinine git
cd /opt/mytrader

# .env.production dosyası oluştur
nano .env.production
```

**Aşağıdaki içeriği yapıştırın** (script'ten aldığınız şifrelerle):

```env
# Database Configuration
POSTGRES_DB=mytrader
POSTGRES_USER=postgres
POSTGRES_PASSWORD=BURAYA_SCRIPT_TEN_ALINAN_ŞİFRE

# JWT Secret
JWT_SECRET_KEY=BURAYA_SCRIPT_TEN_ALINAN_JWT_SECRET

# Alpaca API (şimdilik böyle bırakın)
ALPACA_API_KEY=your-alpaca-api-key-here
ALPACA_API_SECRET=your-alpaca-api-secret-here
```

**Kaydet:** `Ctrl+X` → `Y` → `Enter`

**Çıkış yap:**
```bash
exit
```

---

## Adım 3: DNS Ayarlarını Yapın

Domain registrar'ınızda (GoDaddy, Namecheap, vb.) A kayıtları ekleyin:

| Tip | Host | Değer | TTL |
|-----|------|-------|-----|
| A | @ | 213.238.180.201 | 3600 |
| A | www | 213.238.180.201 | 3600 |

**DNS kontrolü** (10-15 dakika sonra):
```bash
dig mytrader.tech
ping mytrader.tech
```

---

## Adım 4: Projeyi Deploy Et

Yerel makinenizde:

```bash
cd /Users/mustafayildirim/Documents/Personal\ Documents/Projects/myTrader
./scripts/deploy-to-production.sh
```

Script şunları yapacak:
1. ✓ Proje dosyalarını sunucuya transfer eder
2. ✓ SSL sertifikası alır (Let's Encrypt)
3. ✓ Docker container'ları build eder
4. ✓ Servisleri başlatır

**Süre:** ~5-10 dakika

---

## Adım 5: Database'i Migrate Et

```bash
./scripts/migrate-database-to-prod.sh
```

Bu script:
1. Yerel database'i backup alır
2. Sunucuya transfer eder
3. Production'a restore eder

---

## Adım 6: Doğrulama

### Container'ları Kontrol Et
```bash
ssh root@213.238.180.201
cd /opt/mytrader
docker-compose -f docker-compose.prod.yml ps
```

**Görmeniz gerekenler:**
```
NAME                       STATUS
mytrader_postgres_prod     Up (healthy)
mytrader_api_prod          Up
mytrader_nginx_prod        Up
mytrader_certbot           Up
```

### API Health Check
```bash
# Sunucu içinden
docker exec mytrader_api_prod curl http://localhost:8080/health

# Dışarıdan (DNS yayıldıktan sonra)
curl https://mytrader.tech/health
```

### Logları İncele
```bash
# Tüm loglar
docker-compose -f docker-compose.prod.yml logs -f

# Sadece API logları
docker-compose -f docker-compose.prod.yml logs -f mytrader_api

# Hata kontrolü
docker-compose -f docker-compose.prod.yml logs | grep -i error
```

---

## ✅ Başarılı Deployment Sonrası

Eğer herşey yolunda gittiyse:

- ✓ `https://mytrader.tech` adresine gidebiliyorsunuz
- ✓ SSL sertifikası geçerli (yeşil kilit)
- ✓ API health endpoint çalışıyor
- ✓ WebSocket bağlantıları aktif (Binance)

---

## 🔧 Sorun mu Yaşıyorsunuz?

### Problem: SSH bağlantısı kurulamıyor
```bash
# SSH key kullanıyorsanız
ssh -i ~/.ssh/your_key root@213.238.180.201

# Port 22 dışındaysa
ssh -p PORT_NUMBER root@213.238.180.201

# Verbose mode ile debug
ssh -v root@213.238.180.201
```

### Problem: Docker yok
```bash
# Sunucuda manuel kurulum
ssh root@213.238.180.201
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh
docker --version
```

### Problem: /opt/mytrader dizini yok
```bash
# Sunucuda manuel oluştur
ssh root@213.238.180.201
sudo mkdir -p /opt/mytrader
cd /opt/mytrader
pwd  # Dizinin var olduğunu doğrula
```

### Problem: SSL sertifikası alınamıyor
```bash
# DNS yayılımını kontrol et
dig mytrader.tech

# Certbot loglarını kontrol et
docker-compose -f docker-compose.prod.yml logs certbot

# Manuel sertifika alma
docker-compose -f docker-compose.prod.yml run --rm certbot certonly \
    --webroot --webroot-path /var/www/certbot \
    --email admin@mytrader.tech \
    --agree-tos --no-eff-email \
    -d mytrader.tech -d www.mytrader.tech
```

### Problem: Container başlamıyor
```bash
# Detaylı loglar
docker-compose -f docker-compose.prod.yml logs

# Spesifik container
docker logs mytrader_api_prod --tail 100

# Container'ı yeniden oluştur
docker-compose -f docker-compose.prod.yml up -d --force-recreate mytrader_api
```

---

## 📞 Yardım ve Destek

**Detaylı dokümantasyon:**
- `PRODUCTION_DEPLOYMENT_GUIDE.md` - Kapsamlı deployment rehberi
- `DEPLOYMENT.md` - Genel deployment bilgileri

**Log dosyaları:**
```bash
# API logları
docker-compose -f docker-compose.prod.yml logs mytrader_api > api-logs.txt

# Database logları
docker-compose -f docker-compose.prod.yml logs postgres > db-logs.txt

# Nginx logları
docker-compose -f docker-compose.prod.yml logs nginx > nginx-logs.txt
```

**Önemli komutlar:**
```bash
# Servisleri yeniden başlat
docker-compose -f docker-compose.prod.yml restart

# Servisleri durdur
docker-compose -f docker-compose.prod.yml down

# Servisleri başlat
docker-compose -f docker-compose.prod.yml up -d

# Durum kontrol
docker-compose -f docker-compose.prod.yml ps

# Resource kullanımı
docker stats
```

---

## 🎯 Sonraki Adımlar

1. **Alpaca API Key Ekle** (hazır olduğunda)
   ```bash
   ssh root@213.238.180.201
   nano /opt/mytrader/.env.production
   # ALPACA_API_KEY ve ALPACA_API_SECRET değerlerini güncelle
   docker-compose -f docker-compose.prod.yml restart mytrader_api
   ```

2. **Frontend Deploy Et**
   - React web app
   - React Native mobile app
   - API URL: `https://mytrader.tech/api/`
   - WebSocket: `wss://mytrader.tech/hubs/`

3. **Monitoring Kur** (opsiyonel)
   - Grafana + Prometheus
   - Sentry (error tracking)
   - Application Insights

4. **Backup Otomasyonu**
   ```bash
   # Crontab ekle (günlük backup)
   0 2 * * * cd /opt/mytrader && docker exec mytrader_postgres_prod pg_dump -U postgres mytrader | gzip > backups/daily_$(date +\%Y\%m\%d).sql.gz
   ```

---

**İyi şanslar! 🚀**
