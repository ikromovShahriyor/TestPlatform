# 🚀 TestPlatform Deployment Guide (GitHub -> Contabo VPS Docker)

Ushbu qo'llanma loyihani GitHub'ga joylash va **Contabo VPS** serverida **Docker**, **Docker Compose** hamda **Nginx + SSL (Certbot)** orqali muammosiz ishga tushirish uchun tayyorlangan.

---

## 📋 Mundarija
1. [1-Qadam: Local kompyuteringizda o'zgarishlarni GitHub'ga push qilish](#1-qadam-local-kompyuteringizda-ozgarishlarni-githubga-push-qilish)
2. [2-Qadam: Contabo VPS Serverga ulanish va Docker o'rnatish](#2-qadam-contabo-vps-serverga-ulanish-va-docker-ornatish)
3. [3-Qadam: Loyihani Serverda ishga tushirish (Docker Compose)](#3-qadam-loyihani-serverda-ishga-tushirish-docker-compose)
4. [4-Qadam: Nginx va Bepul SSL (Certbot) o'rnatish (Domen ulash uchun)](#4-qadam-nginx-va-bepul-ssl-certbot-ornatish-domen-ulash-uchun)
5. [5-Qadam: Avtomatik Deploy (GitHub Actions CI/CD) sozlash](#5-qadam-avtomatik-deploy-github-actions-cicd-sozlash)
6. [💡 Foydali Buyruqlar va Troubleshooting](#-foydali-buyruqlar-va-troubleshooting)

---

## 1-Qadam: Local kompyuteringizda o'zgarishlarni GitHub'ga push qilish

Terminallizda loyiha papkasida turib quyidagi buyruqlarni bajaring:

```bash
git add .
git commit -m "Configure Docker, Compose, and GitHub Actions CI/CD"
git push origin main
```

---

## 2-Qadam: Contabo VPS Serverga ulanish va Docker o'rnatish

Serveringizga SSH orqali ulaning:

```bash
ssh root@YOUR_SERVER_IP
```

VPS'da **Docker** va **Docker Compose** hali o'rnatilmagan bo'lsa, quyidagi buyruqlarni birma-bir kiriting:

```bash
# System paketlarini yangilash
apt update && apt upgrade -y

# Docker va kerakli paketlarni o'rnatish
apt install -y docker.io docker-compose-plugin git curl

# Docker xizmatini avtomatik yonadigan qilish
systemctl enable --now docker
```

---

## 3-Qadam: Loyihani Serverda ishga tushirish (Docker Compose)

Serveringizda loyihani clone qiling va Docker konteynerlarini ishga tushiring:

```bash
# 1. GitHub'dan loyihani ko'chirish
cd /root
git clone https://github.com/USERNAME/REPOSITORY_NAME.git testplatform
cd testplatform

# 2. Docker container'larni qurish va ishga tushirish
docker compose up -d --build
```

**Natijani tekshirish:**
- Loyiha `http://YOUR_SERVER_IP:8080` manzilida ishga tushadi.
- PostgreSQL bazasi avtomatik `testplatform-db` konteynerida ishlaydi va ilova ilk marotaba ishga tushganda jadvallar va boshlang'ich ma'lumotlar (Admin/Student foydalanuvchilar, testlar) avtomatik joylanadi (EF Core Migrations).

---

## 4-Qadam: Nginx va Bepul SSL (Certbot) o'rnatish (Domen ulash uchun)

Agar VPS'ingizga domen biriktirmoqchi bo'lsangiz (masalan `mytestsite.uz`):

### A. Nginx o'rnatish:
```bash
apt install -y nginx certbot python3-certbot-nginx
```

### B. Nginx konfiguratsiyasini yaratish:
`/etc/nginx/sites-available/testplatform` faylini yarating:

```bash
nano /etc/nginx/sites-available/testplatform
```

Fayl ichiga quyidagini joylang:
```nginx
server {
    server_name YOUR_DOMAIN.COM www.YOUR_DOMAIN.COM;

    location / {
        proxy_pass http://localhost:5005;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

Konfiguratsiyani yoqish:
```bash
ln -s /etc/nginx/sites-available/testplatform /etc/nginx/sites-enabled/
nginx -t
systemctl restart nginx
```

### C. HTTPS / SSL sertifikatini (Certbot) yoqish:
```bash
certbot --nginx -d YOUR_DOMAIN.COM -d www.YOUR_DOMAIN.COM
```
Certbot avtomatik SSL sertifikat o'rnatadi va HTTP so'rovlarni HTTPS'ga yo'naltiradi!

---

## 5-Qadam: Avtomatik Deploy (GitHub Actions CI/CD) sozlash

Har safar `git push origin main` qilganingizda serverda loyiha avtomatik yangilanishi uchun:

1. SSH kalitingizni yarating yoki mavjudini oling (`cat ~/.ssh/id_rsa`).
2. GitHub Repository -> **Settings** -> **Secrets and variables** -> **Actions** bo'limiga kirib quyidagi secret'larni qo'shing:
   - `VPS_HOST`: Serveringiz IP manzili (masalan: `194.163.x.x`).
   - `VPS_USERNAME`: `root`
   - `VPS_SSH_KEY`: Serveringiz SSH private key kontenti.
3. `.github/workflows/deploy.yml` faylida `deploy-to-vps` qismini izohdan chiqaring.

---

## 💡 Foydali Buyruqlar va Troubleshooting

```bash
# 1. Konteynerlar holatini ko'rish
docker compose ps

# 2. Real vaqtda Web API loglarini ko'rish
docker compose logs -f webapi

# 3. Database loglarini ko'rish
docker compose logs -f postgres

# 4. Konteynerlarni qayta ishga tushirish
docker compose restart

# 5. Loyihani qo'lda yangilash (Update)
cd /root/testplatform
git pull
docker compose up -d --build
```

