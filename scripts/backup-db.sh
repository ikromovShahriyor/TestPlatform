#!/bin/bash
# ==============================================================================
# PostgreSQL Automatic Backup Script for TestPlatform
# ==============================================================================

BACKUP_DIR="/var/backups/testplatform"
DATE=$(date +%Y-%m-%d_%H-%M-%S)
CONTAINER_NAME="testplatform-db"
DB_USER="postgres"
DB_NAME="TestPlatformDb"

# Backup papkasini yaratish
mkdir -p $BACKUP_DIR

# Database dump olish (.sql.gz)
echo "[$(date)] Database backup boshlandi..."
docker exec -t $CONTAINER_NAME pg_dump -U $DB_USER $DB_NAME | gzip > "$BACKUP_DIR/db_backup_$DATE.sql.gz"

if [ $? -eq 0 ]; then
    echo "[$(date)] Backup muvaffaqiyatli saqlandi: $BACKUP_DIR/db_backup_$DATE.sql.gz"
else
    echo "[$(date)] Xatolik: Backup olishda muammo yuz berdi!"
fi

# 7 kundan eski backup'larni avtomatik o'chirish
find $BACKUP_DIR -type f -name "*.sql.gz" -mtime +7 -exec rm {} \;
echo "[$(date)] Eskirgan (7 kundan o'tgan) backup fayllar tozalandi."
