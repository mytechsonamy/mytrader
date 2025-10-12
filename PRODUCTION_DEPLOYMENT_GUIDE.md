# 🚀 MyTrader Production Deployment - Quick Start Guide

**Hedef Sunucu**: 213.238.180.201
**Domain**: mytrader.tech
**Tarih**: 2025-10-11

## ✅ Hazırlık Durumu

- [x] Docker kurulu (v28.1.1)
- [x] Domain kayıtlı (mytrader.tech)
- [ ] DNS ayarları yapılandırılacak
- [ ] SSL sertifikası alınacak (otomatik)
- [ ] Alpaca API keyleri eklenecek (sonra)
- [ ] Database migration yapılacak

---

## 🎯 Hızlı Başlangıç (3 Adım)

### Adım 1: Environment Variables Oluştur

**Sunucuya bağlan:**
```bash
ssh root@213.238.180.201
mkdir -p /opt/mytrader
cd /opt/mytrader
```

**Güçlü şifreler oluştur:**
```bash
# PostgreSQL şifresi (örnek - değiştirin!)
POSTGRES_PASS=$(openssl rand -base64 32)

# JWT Secret (256-bit)
JWT_SECRET=$(openssl rand -base64 64)

echo "POSTGRES_PASSWORD: $POSTGRES_PASS"
echo "JWT_SECRET_KEY: $JWT_SECRET"
```

Bu değerleri bir yere kaydedin!

### Adım 2: Otomatik Deployment Çalıştır

**Yerel makinenizde:**
```bash
cd /Users/mustafayildirim/Documents/Personal\ Documents/Projects/myTrader
./scripts/deploy-to-production.sh
```

Script sırayla:
1. SSH bağlantısını kontrol eder
2. Projeyi sunucuya transfer eder
3. SSL sertifikası oluşturur
4. Docker container'ları başlatır

### Adım 3: Database Migration

```bash
# Yerel database'i production'a taşı
./scripts/migrate-database-to-prod.sh
```

---

## 📋 Detaylı Adımlar

### 1. Sunucu Hazırlığı

#### SSH Bağlantısını Test Et
```bash
ssh root@213.238.180.201
```

#### Proje Dizinini Oluştur
```bash
mkdir -p /opt/mytrader
cd /opt/mytrader
```

### 2. Environment Variables Yapılandır

#### .env.production Dosyası Oluştur

```bash
cd /opt/mytrader
nano .env.production
```

**Aşağıdaki içeriği yapıştırın:**

```env
# Database Configuration
POSTGRES_DB=mytrader
POSTGRES_USER=postgres
POSTGRES_PASSWORD=BURAYA_GÜÇLÜ_ŞİFRE

# JWT Secret (openssl rand -base64 64 ile oluşturun)
JWT_SECRET_KEY=BURAYA_RASTGELE_UZUN_KEY

# Alpaca API (şimdilik placeholder bırakın)
ALPACA_API_KEY=your-alpaca-api-key-here
ALPACA_API_SECRET=your-alpaca-api-secret-here
```

**Kaydet ve çık:** `Ctrl+X`, `Y`, `Enter`

### 3. DNS Ayarlarını Yapılandır

Domain registrar'ınızda (GoDaddy, Namecheap, vs.) A kayıtları ekleyin:

```
Kayıt Tipi    Host    Değer               TTL
A             @       213.238.180.201     3600
A             www     213.238.180.201     3600
```

**DNS kontrolü:**
```bash
# 10-15 dakika sonra kontrol edin
dig mytrader.tech
ping mytrader.tech
```

### 4. Deployment Scriptini Çalıştır

**Yerel makinenizde:**

```bash
cd /Users/mustafayildirim/Documents/Personal\ Documents/Projects/myTrader
./scripts/deploy-to-production.sh
```

Script şunları yapar:
1. ✓ SSH bağlantısını kontrol eder
2. ✓ Docker kurulumunu doğrular
3. ✓ Proje dosyalarını transfer eder
4. ✓ Let's Encrypt SSL sertifikası alır
5. ✓ Docker container'ları build eder
6. ✓ Servisleri başlatır

### 5. Database Migration

**Yerel database'i production'a taşıyın:**

```bash
./scripts/migrate-database-to-prod.sh
```

**Manuel migration (alternatif):**

```bash
# 1. Yerel database'i backup al
docker exec mytrader_postgres pg_dump -U postgres -d mytrader > backup.sql

# 2. Sunucuya transfer et
scp backup.sql root@213.238.180.201:/tmp/

# 3. Sunucuda restore et
ssh root@213.238.180.201
docker exec -i mytrader_postgres_prod psql -U postgres -d mytrader < /tmp/backup.sql
rm /tmp/backup.sql
```

### 6. Doğrulama

#### Container Durumunu Kontrol Et
```bash
ssh root@213.238.180.201
cd /opt/mytrader
docker-compose -f docker-compose.prod.yml ps
```

Şu container'ları görmelisiniz:
- ✓ mytrader_postgres_prod (healthy)
- ✓ mytrader_api_prod (running)
- ✓ mytrader_nginx_prod (running)
- ✓ mytrader_certbot (running)

#### API Health Check
```bash
# Sunucu içinden
docker exec mytrader_api_prod curl http://localhost:8080/health

# Dışarıdan (DNS yayıldıktan sonra)
curl https://mytrader.tech/health
```

#### Logları Kontrol Et
```bash
# Tüm loglar
docker-compose -f docker-compose.prod.yml logs -f

# Sadece API
docker-compose -f docker-compose.prod.yml logs -f mytrader_api

# Hata logları
docker-compose -f docker-compose.prod.yml logs | grep -i error
```

---

## 🔧 Manuel Deployment (Alternatif)

Otomatik script sorun verirse manuel yapabilirsiniz:

### 1. Projeyi Sunucuya Transfer Et

**Yerel makinenizde:**
```bash
cd /Users/mustafayildirim/Documents/Personal\ Documents/Projects/myTrader

# Arşiv oluştur
tar -czf mytrader.tar.gz \
    --exclude='node_modules' \
    --exclude='bin' \
    --exclude='obj' \
    --exclude='.git' \
    --exclude='backups' \
    backend docker-compose.prod.yml nginx .env.production.template

# Transfer et
scp mytrader.tar.gz root@213.238.180.201:/opt/mytrader/
```

**Sunucuda:**
```bash
cd /opt/mytrader
tar -xzf mytrader.tar.gz
rm mytrader.tar.gz
```

### 2. SSL Sertifikası Al

```bash
cd /opt/mytrader

# Nginx'i geçici başlat
docker-compose -f docker-compose.prod.yml up -d nginx

# Sertifika al
docker-compose -f docker-compose.prod.yml run --rm certbot certonly \
    --webroot --webroot-path /var/www/certbot \
    --email admin@mytrader.tech \
    --agree-tos --no-eff-email \
    -d mytrader.tech -d www.mytrader.tech

# Nginx'i durdur
docker-compose -f docker-compose.prod.yml down
```

### 3. Servisleri Başlat

```bash
cd /opt/mytrader

# Environment variables yükle
export $(cat .env.production | xargs)

# Build ve start
docker-compose -f docker-compose.prod.yml build
docker-compose -f docker-compose.prod.yml up -d
```

---

## 🔄 Günlük İşlemler

### Logları İzleme
```bash
ssh root@213.238.180.201
cd /opt/mytrader
docker-compose -f docker-compose.prod.yml logs -f mytrader_api
```

### Servis Yeniden Başlatma
```bash
# API'yi yeniden başlat
docker-compose -f docker-compose.prod.yml restart mytrader_api

# Tüm servisleri yeniden başlat
docker-compose -f docker-compose.prod.yml restart
```

### Database Backup
```bash
# Sunucuda
docker exec mytrader_postgres_prod pg_dump -U postgres -d mytrader | gzip > /opt/mytrader/backups/backup_$(date +%Y%m%d).sql.gz
```

### Yeni Build Deploy Etme
```bash
# Sunucuda
cd /opt/mytrader
git pull  # veya yeni dosyaları transfer edin
docker-compose -f docker-compose.prod.yml build
docker-compose -f docker-compose.prod.yml up -d
```

---

## 🐛 Sorun Giderme

### 1. Container Başlamıyor

```bash
# Hata loglarını görüntüle
docker-compose -f docker-compose.prod.yml logs

# Spesifik container logları
docker logs mytrader_api_prod

# Container'ı yeniden oluştur
docker-compose -f docker-compose.prod.yml up -d --force-recreate mytrader_api
```

### 2. Database Bağlantı Hatası

```bash
# Database durumunu kontrol et
docker exec mytrader_postgres_prod pg_isready -U postgres

# Connection string'i kontrol et
docker exec mytrader_api_prod env | grep ConnectionStrings

# Database'i yeniden başlat
docker-compose -f docker-compose.prod.yml restart postgres
```

### 3. SSL Sertifika Sorunları

```bash
# Sertifika dosyalarını kontrol et
ls -la /opt/mytrader/certbot/conf/live/mytrader.tech/

# Sertifika detaylarını görüntüle
openssl x509 -in /opt/mytrader/certbot/conf/live/mytrader.tech/fullchain.pem -text -noout

# Nginx yapılandırmasını test et
docker exec mytrader_nginx_prod nginx -t

# Nginx'i yeniden başlat
docker-compose -f docker-compose.prod.yml restart nginx
```

### 4. WebSocket Bağlantı Sorunları

```bash
# SignalR hub loglarını kontrol et
docker-compose -f docker-compose.prod.yml logs -f | grep -i "signalr\|websocket"

# Binance WebSocket durumu
docker-compose -f docker-compose.prod.yml logs -f | grep -i "binance"

# Alpaca WebSocket durumu
docker-compose -f docker-compose.prod.yml logs -f | grep -i "alpaca"
```

### 5. High Memory/CPU Usage

```bash
# Resource kullanımını görüntüle
docker stats

# Disk kullanımı
df -h
du -sh /opt/mytrader/*

# Database boyutu
docker exec mytrader_postgres_prod psql -U postgres -d mytrader -c \
    "SELECT pg_size_pretty(pg_database_size('mytrader'));"

# Log dosyalarını temizle
docker system prune -a
```

---

## 📊 Monitoring

### Health Check Endpoints

```bash
# API Health
curl https://mytrader.tech/health

# Database Health
docker exec mytrader_postgres_prod pg_isready -U postgres
```

### Performance Monitoring

```bash
# Real-time resource usage
docker stats

# Container memory usage
docker stats --format "table {{.Container}}\t{{.CPUPerc}}\t{{.MemUsage}}"

# Check logs for errors
docker-compose -f docker-compose.prod.yml logs --tail=100 | grep -i error
```

---

## 🔐 Güvenlik Kontrol Listesi

- [ ] `.env.production` dosyası sunucuda güvenli bir yerde
- [ ] Güçlü PostgreSQL şifresi kullanıldı
- [ ] JWT secret rastgele ve güvenli
- [ ] SSH key-based authentication aktif
- [ ] Firewall sadece 22, 80, 443 portlarını açıyor
- [ ] SSL sertifikası aktif ve geçerli
- [ ] Database düzenli backup alınıyor
- [ ] Log dosyaları düzenli temizleniyor

---

## 📞 Sonraki Adımlar

1. **DNS Yayılımını Bekleyin** (10-30 dakika)
   - Test: `dig mytrader.tech`

2. **Alpaca API Keylerini Ekleyin**
   ```bash
   ssh root@213.238.180.201
   nano /opt/mytrader/.env.production
   # ALPACA_API_KEY ve ALPACA_API_SECRET değerlerini güncelleyin
   docker-compose -f docker-compose.prod.yml restart mytrader_api
   ```

3. **Frontend Deploy Edin**
   - React web app veya React Native app'i production'a deploy edin
   - API URL: `https://mytrader.tech/api/`
   - WebSocket URL: `wss://mytrader.tech/hubs/`

4. **Monitoring Kurun** (Opsiyonel)
   - Application Insights
   - Sentry
   - ELK Stack
   - Grafana + Prometheus

5. **Otomatik Backup Kurun**
   - Cron job ile günlük backup
   - S3 veya external storage'a yedekleme

---

**Önemli Dosyalar:**

- Docker Compose: `/opt/mytrader/docker-compose.prod.yml`
- Environment: `/opt/mytrader/.env.production`
- Nginx Config: `/opt/mytrader/nginx/conf.d/mytrader.conf`
- SSL Certs: `/opt/mytrader/certbot/conf/live/mytrader.tech/`
- Backups: `/opt/mytrader/backups/`
- Logs: `docker-compose -f docker-compose.prod.yml logs`

**Deployment Scripts:**

- Full Deploy: `./scripts/deploy-to-production.sh`
- Database Migration: `./scripts/migrate-database-to-prod.sh`
- Database Backup: `./scripts/backup-database.sh`

---

**Son Güncelleme**: 2025-10-11
**Deployment Versiyonu**: 1.0.0
