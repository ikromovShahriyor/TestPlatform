# 🚀 TestPlatform Deployment Guide (GitHub -> Contabo VPS Docker)

Ushbu qo'llanma loyihani GitHub'ga joylash va Contabo VPS serverida Docker orqali 1-marta va keyingi yangilanishlarda muammosiz ishga tushirish uchun tayyorlandi.

---

## 1-Qadam: GitHub'ga yuklash (Local Kompyuteringizda)

Terminallizda loyiha ildiz papkasida turib quyidagi buyruqlarni bajaring:

```bash
# 1. O'zgarishlarni git'ga qo'shish
git add .

# 2. Commit qilish
git commit -m "Add Docker and production deployment configuration"

# 3. GitHub repository'ingizga push qilish
git push origin main
```

---

## 2-Qadam: Contabo VPS Serveriga ulanish va Docker o'rnatish

Serveringizga SSH orqali ulaning:

```bash
ssh root@YOUR_SERVER_IP
```

Agar VPS serveringizda **Docker** va **Docker Compose** hali o'rnatilmagan bo'lsa, quyidagi buyruqlar orqali o'rnating:

```bash
# Paketlarni yangilash va Docker o'rnatish
apt update && apt upgrade -y
apt install -y docker.io docker-compose-plugin

# Docker xizmatini yoqish
systemctl enable --now docker
```

---

## 3-Qadam: Loyihani Serverda ishga tushirish (Deploy)

Serveringizda loyihani clone qiling va Docker Compose orqali ishga tushiring:

```bash
# 1. GitHub'dan loyihani ko'chirib olish
git clone https://github.com/USERNAME/REPOSITORY_NAME.git testplatform
cd testplatform

# 2. Docker orqali konteynerlarni qurish va fonda ishga tushirish
docker compose up -d --build
```

---

## 4-Qadam: Tekshirish va Ishlatish

- Loyihangiz serveringizning IP manzilida 5005-portda ishga tushadi:
  - **Veb-sayt va API:** `http://YOUR_SERVER_IP:5005`
  - **Database (PostgreSQL):** Avtomatik `testplatform-db` konteynerida ishlaydi va barcha jadvallar hamda seed ma'lumotlari ilova birinchi marta ishga tushganda o'zi yaratiladi.

---

## 💡 Foydali Buyruqlar (Serverda)

```bash
# Konteynerlar holatini ko'rish
docker compose ps

# Web API loglarini real vaqtda kuzatish
docker compose logs -f webapi

# Konteynerlarni to'xtatish
docker compose down

# Loyihani kelajakda yangilash (Update qilish)
git pull
docker compose up -d --build
```
