-- Лабораторная работа № 4. Шифрование в SQL Server
-- Lab 4. Column encryption in SQL Server with a symmetric key
--
-- Пароли заменены на плейсхолдеры вида <KEY_PASSWORD>, подставьте свои перед запуском.
-- Passwords are replaced with placeholders like <KEY_PASSWORD>; set your own before running.


-- ============================================================
-- 3.1. Создание столбцов, пользователя, схемы и ключа (пункты 3–6)
-- ============================================================
USE Sale1;
GO

-- Пункт 3. Столбцы для зашифрованных данных
ALTER TABLE dbo.client ADD
    Crypt_phone     varbinary(max) NULL,
    Crypt_region    varbinary(max) NULL,
    Crypt_adress    varbinary(max) NULL,
    Crypt_name_firm varbinary(max) NULL;
GO

-- Главный ключ базы данных
CREATE MASTER KEY ENCRYPTION BY PASSWORD = '<MASTER_KEY_PASSWORD>';
GO

-- Пункт 4. Пользователь с доступом только к базе Sale1
CREATE LOGIN manina
    WITH PASSWORD = '<LOGIN_PASSWORD>',
         DEFAULT_DATABASE = Sale1,
         CHECK_POLICY = OFF;
GO

CREATE USER manina FOR LOGIN manina WITH DEFAULT_SCHEMA = manina;
GO

-- Пункт 5. Схема, владельцем которой является созданный пользователь
CREATE SCHEMA manina AUTHORIZATION manina;
GO

-- Права на таблицу, без которых шифрование данных невозможно
GRANT SELECT, INSERT, UPDATE ON dbo.client TO manina;
GO

-- Пункт 6. Симметричный ключ с алгоритмом DES
CREATE SYMMETRIC KEY MANINAKEY
    AUTHORIZATION manina
    WITH ALGORITHM = DES
    ENCRYPTION BY PASSWORD = '<KEY_PASSWORD>';
GO

-- ============================================================
-- 3.2. Проверка созданных объектов
-- ============================================================
USE Sale1;
GO

SELECT name AS Ключ, algorithm_desc AS Алгоритм, key_length AS Длина_бит
FROM sys.symmetric_keys;

SELECT name AS Пользователь, type_desc AS Тип
FROM sys.database_principals WHERE name = 'manina';

SELECT s.name AS Схема, p.name AS Владелец
FROM sys.schemas s
JOIN sys.database_principals p ON s.principal_id = p.principal_id
WHERE s.name = 'manina';

-- ============================================================
-- 3.3. Шифрование столбцов (пункт 8)
-- ============================================================
USE Sale1;
GO

OPEN SYMMETRIC KEY MANINAKEY DECRYPTION BY PASSWORD = '<KEY_PASSWORD>';

UPDATE dbo.client
SET Crypt_phone     = ENCRYPTBYKEY(KEY_GUID('MANINAKEY'), phone),
    Crypt_region    = ENCRYPTBYKEY(KEY_GUID('MANINAKEY'), country),
    Crypt_adress    = ENCRYPTBYKEY(KEY_GUID('MANINAKEY'), address),
    Crypt_name_firm = ENCRYPTBYKEY(KEY_GUID('MANINAKEY'), name_firm);

CLOSE SYMMETRIC KEY MANINAKEY;
GO

-- ============================================================
-- 3.4. Запрос для просмотра зашифрованной информации (пункт 9)
-- ============================================================
SELECT key_client, fam, name,
       Crypt_phone, Crypt_region, Crypt_adress, Crypt_name_firm
FROM dbo.client;

-- ============================================================
-- 3.5. Запрос для просмотра расшифрованной информации (пункт 10)
-- ============================================================
OPEN SYMMETRIC KEY MANINAKEY DECRYPTION BY PASSWORD = '<KEY_PASSWORD>';

SELECT key_client, fam,
       CONVERT(nvarchar(24), DECRYPTBYKEY(Crypt_phone))     AS Телефон,
       CONVERT(nvarchar(20), DECRYPTBYKEY(Crypt_region))    AS Страна,
       CONVERT(nvarchar(60), DECRYPTBYKEY(Crypt_adress))    AS Адрес,
       CONVERT(nvarchar(50), DECRYPTBYKEY(Crypt_name_firm)) AS Фирма
FROM dbo.client;

CLOSE SYMMETRIC KEY MANINAKEY;

-- ============================================================
-- 3.6. Добавление двух новых клиентов (пункт 11)
-- ============================================================
OPEN SYMMETRIC KEY MANINAKEY DECRYPTION BY PASSWORD = '<KEY_PASSWORD>';

INSERT INTO dbo.client
    (key_client, fam, name, patron, phone, post_code, country, city, region,
     address, name_firm, credit,
     Crypt_phone, Crypt_region, Crypt_adress, Crypt_name_firm)
VALUES
    (N'M001', N'Соколова', N'Анна', N'Петровна',
     NULL, N'101000', NULL, N'Москва', N'Московская', NULL,
     N'Around the Horn', 1,
     ENCRYPTBYKEY(KEY_GUID('MANINAKEY'), N'+7 495 111-22-33'),
     ENCRYPTBYKEY(KEY_GUID('MANINAKEY'), N'Россия'),
     ENCRYPTBYKEY(KEY_GUID('MANINAKEY'), N'ул. Тверская, д. 12'),
     ENCRYPTBYKEY(KEY_GUID('MANINAKEY'), N'Around the Horn')),

    (N'M002', N'Орлов', N'Дмитрий', N'Сергеевич',
     NULL, N'190000', NULL, N'Санкт-Петербург', N'Ленинградская', NULL,
     N'Berglunds snabbkop', 0,
     ENCRYPTBYKEY(KEY_GUID('MANINAKEY'), N'+7 812 444-55-66'),
     ENCRYPTBYKEY(KEY_GUID('MANINAKEY'), N'Россия'),
     ENCRYPTBYKEY(KEY_GUID('MANINAKEY'), N'Невский пр., д. 28'),
     ENCRYPTBYKEY(KEY_GUID('MANINAKEY'), N'Berglunds snabbkop'));

CLOSE SYMMETRIC KEY MANINAKEY;
GO

-- ============================================================
-- 3.7. Итоговые запросы (пункты 12 и 13)
-- ============================================================
-- Зашифрованная информация о новых клиентах
SELECT key_client, fam, phone, country, address, name_firm,
       Crypt_phone, Crypt_region, Crypt_adress, Crypt_name_firm
FROM dbo.client
WHERE key_client IN (N'M001', N'M002');

-- Расшифрованная информация о новых клиентах
OPEN SYMMETRIC KEY MANINAKEY DECRYPTION BY PASSWORD = '<KEY_PASSWORD>';

SELECT key_client, fam,
       CONVERT(nvarchar(24), DECRYPTBYKEY(Crypt_phone))     AS Телефон,
       CONVERT(nvarchar(20), DECRYPTBYKEY(Crypt_region))    AS Страна,
       CONVERT(nvarchar(60), DECRYPTBYKEY(Crypt_adress))    AS Адрес,
       CONVERT(nvarchar(50), DECRYPTBYKEY(Crypt_name_firm)) AS Фирма
FROM dbo.client
WHERE key_client IN (N'M001', N'M002');

CLOSE SYMMETRIC KEY MANINAKEY;
