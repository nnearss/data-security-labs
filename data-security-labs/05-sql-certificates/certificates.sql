-- Лабораторная работа № 5. Шифрование с помощью сертификатов в SQL Server
-- Lab 5. Encryption with certificates in SQL Server
-- Шаги 3.3–3.5 (создание и экспорт сертификата в Windows) лежат в make-certificate.ps1
--
-- Пароли заменены на плейсхолдеры вида <KEY_PASSWORD>, подставьте свои перед запуском.
-- Passwords are replaced with placeholders like <KEY_PASSWORD>; set your own before running.


-- ============================================================
-- 3.1. Создание сертификата и столбца (пункты 3–4)
-- ============================================================
USE Sale1;
GO

-- Пункт 3. Сертификат для пользователя manina
CREATE CERTIFICATE MANINACERT
    AUTHORIZATION manina
    ENCRYPTION BY PASSWORD = '<CERT_PASSWORD>'
    WITH SUBJECT = 'Сертификат Маниной Вероники',
         EXPIRY_DATE = '2030-12-31';
GO

-- Пункт 4. Столбец для зашифрованных названий товаров
ALTER TABLE dbo.product ADD Crypt_name_product varbinary(max) NULL;
GO

-- Права пользователю на таблицы
GRANT SELECT, INSERT, UPDATE ON dbo.product TO manina;
GRANT SELECT, INSERT, UPDATE ON dbo.[order] TO manina;
GO

-- ============================================================
-- 3.2. Шифрование названий товаров (пункты 5–6)
-- ============================================================
-- Пункт 5. Шифрование столбца name_product
UPDATE dbo.product
SET Crypt_name_product = ENCRYPTBYCERT(CERT_ID('MANINACERT'), name_product);
GO

-- Пункт 6. Просмотр зашифрованной информации
SELECT key_product, category, name_product, price, Crypt_name_product
FROM dbo.product;

-- Дополнительно: просмотр расшифрованной информации
SELECT key_product, category,
       CONVERT(nvarchar(40), DECRYPTBYCERT(CERT_ID('MANINACERT'),
                                           Crypt_name_product, N'<CERT_PASSWORD>')) AS Товар
FROM dbo.product;

-- ============================================================
-- 3.6. Создание сертификата SQL Server из файлов (пункт 9)
-- ============================================================
USE Sale1;
GO

CREATE CERTIFICATE MANINAFILECERT
    AUTHORIZATION manina
    FROM FILE = 'C:\sql\cert.cer'
    WITH PRIVATE KEY (FILE = 'C:\sql\key.pvk',
                      DECRYPTION BY PASSWORD = '<KEY_PASSWORD>');
GO

SELECT name AS Сертификат, subject AS Назначение, expiry_date AS Действует_до,
       pvt_key_encryption_type_desc AS Защита_закрытого_ключа
FROM sys.certificates;

-- ============================================================
-- 3.7. Столбец и симметричный ключ на основе сертификата (пункты 10–11)
-- ============================================================
-- Пункт 10. Столбец для зашифрованных кодов клиентов
ALTER TABLE dbo.[order] ADD Crypt_Key_Client varbinary(max) NULL;
GO

-- Пункт 11. Симметричный ключ, защищённый сертификатом из файла
CREATE SYMMETRIC KEY MANINAORDERKEY
    AUTHORIZATION manina
    WITH ALGORITHM = AES_256
    ENCRYPTION BY CERTIFICATE MANINAFILECERT;
GO

SELECT name AS Ключ, algorithm_desc AS Алгоритм, key_length AS Длина_бит
FROM sys.symmetric_keys;

-- ============================================================
-- 3.8. Шифрование кодов клиентов (пункты 12–13)
-- ============================================================
USE Sale1;
GO

OPEN SYMMETRIC KEY MANINAORDERKEY DECRYPTION BY CERTIFICATE MANINAFILECERT;

UPDATE dbo.[order]
SET Crypt_Key_Client = ENCRYPTBYKEY(KEY_GUID('MANINAORDERKEY'), key_client);

CLOSE SYMMETRIC KEY MANINAORDERKEY;
GO

-- Пункт 13. Просмотр зашифрованной информации
SELECT key_client, key_product, date_order, quantity_order, Crypt_Key_Client
FROM dbo.[order];

-- ============================================================
-- 3.9. Добавление двух новых записей (пункт 14)
-- ============================================================
OPEN SYMMETRIC KEY MANINAORDERKEY DECRYPTION BY CERTIFICATE MANINAFILECERT;

DECLARE @kp1 int = (SELECT MIN(key_product) FROM dbo.product);
DECLARE @kp2 int = (SELECT MAX(key_product) FROM dbo.product);

INSERT INTO dbo.[order]
    (key_client, key_product, date_order, quantity_order, date_sale, quantity_sale,
     Crypt_Key_Client)
VALUES
    (N'M001', @kp1, '2026-10-01', 10, NULL, NULL,
     ENCRYPTBYKEY(KEY_GUID('MANINAORDERKEY'), N'M001')),
    (N'M002', @kp2, '2026-10-02', 5, NULL, NULL,
     ENCRYPTBYKEY(KEY_GUID('MANINAORDERKEY'), N'M002'));

CLOSE SYMMETRIC KEY MANINAORDERKEY;
GO

-- ============================================================
-- 3.10. Итоговые запросы (пункты 15–16)
-- ============================================================
-- Зашифрованная информация о новых записях
SELECT key_client, key_product, date_order, quantity_order, Crypt_Key_Client
FROM dbo.[order]
WHERE key_client IN (N'M001', N'M002');

-- Расшифрованная информация о новых записях
OPEN SYMMETRIC KEY MANINAORDERKEY DECRYPTION BY CERTIFICATE MANINAFILECERT;

SELECT key_client, key_product, date_order,
       CONVERT(nvarchar(5), DECRYPTBYKEY(Crypt_Key_Client)) AS Код_клиента_расшифрованный
FROM dbo.[order]
WHERE key_client IN (N'M001', N'M002');

CLOSE SYMMETRIC KEY MANINAORDERKEY;
