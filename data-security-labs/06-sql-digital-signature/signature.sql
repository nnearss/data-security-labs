-- Лабораторная работа № 6. Цифровая подпись в SQL Server
-- Lab 6. Digital signatures in SQL Server: asymmetric key and certificate
--
-- Пароли заменены на плейсхолдеры вида <KEY_PASSWORD>, подставьте свои перед запуском.
-- Passwords are replaced with placeholders like <KEY_PASSWORD>; set your own before running.


-- ============================================================
-- 3.1. Создание асимметричного ключа и проверка сертификатов (пункт 3)
-- ============================================================
USE Sale1;
GO

-- Пункт 3. Асимметричный ключ RSA
CREATE ASYMMETRIC KEY MANINAASYMKEY
    AUTHORIZATION manina
    WITH ALGORITHM = RSA_2048
    ENCRYPTION BY PASSWORD = '<KEY_PASSWORD>';
GO

-- Проверка созданного ключа и сертификатов из лабораторной работы 5
SELECT name AS Асимметричный_ключ, algorithm_desc AS Алгоритм,
       key_length AS Длина_бит, pvt_key_encryption_type_desc AS Защита_ключа
FROM sys.asymmetric_keys;

SELECT name AS Сертификат, subject AS Назначение,
       pvt_key_encryption_type_desc AS Защита_закрытого_ключа
FROM sys.certificates;

-- ============================================================
-- 3.2. Столбцы для цифровых подписей (пункт 4)
-- ============================================================
ALTER TABLE dbo.client ADD
    Sign_phone     varbinary(max) NULL,
    Sign_post_code varbinary(max) NULL;
GO

-- ============================================================
-- 3.3. Подпись телефона асимметричным ключом (пункт 5)
-- ============================================================
USE Sale1;
GO

UPDATE dbo.client
SET Sign_phone = SignByAsymKey(AsymKey_Id('MANINAASYMKEY'), phone, N'<KEY_PASSWORD>');
GO

-- ============================================================
-- 3.4. Подпись почтового кода сертификатом (пункт 6)
-- ============================================================
UPDATE dbo.client
SET Sign_post_code = SignByCert(Cert_Id('MANINAFILECERT'), post_code);
GO

-- Просмотр полученных подписей
SELECT key_client, fam, phone, post_code, Sign_phone, Sign_post_code
FROM dbo.client;

-- ============================================================
-- 3.5. Верификация обеих подписей (пункт 7)
-- ============================================================
SELECT key_client, fam, phone, post_code,
       VerifySignedByAsymKey(AsymKey_Id('MANINAASYMKEY'), phone, Sign_phone)
           AS Подпись_телефона,
       VerifySignedByCert(Cert_Id('MANINAFILECERT'), post_code, Sign_post_code)
           AS Подпись_почтового_кода
FROM dbo.client;

-- ============================================================
-- 3.6. Записи с действительными подписями (пункты 8–9)
-- ============================================================
-- Пункт 8. Записи с действительной первой подписью
SELECT key_client, fam, phone
FROM dbo.client
WHERE VerifySignedByAsymKey(AsymKey_Id('MANINAASYMKEY'), phone, Sign_phone) = 1;

-- Пункт 9. Записи с действительной второй подписью
SELECT key_client, fam, post_code
FROM dbo.client
WHERE VerifySignedByCert(Cert_Id('MANINAFILECERT'), post_code, Sign_post_code) = 1;

-- ============================================================
-- 3.7. Изменение данных и повторная верификация (пункты 10–11)
-- ============================================================
-- Пункт 10. Изменение данных без обновления подписи
UPDATE dbo.client
SET phone = N'+7 999 000-11-22',
    post_code = N'999999'
WHERE key_client = N'ANATR';
GO

SELECT key_client, fam, phone, post_code
FROM dbo.client WHERE key_client = N'ANATR';

-- Пункт 11. Верификация подписей изменённой записи
SELECT key_client, fam, phone, post_code,
       VerifySignedByAsymKey(AsymKey_Id('MANINAASYMKEY'), phone, Sign_phone)
           AS Подпись_телефона,
       VerifySignedByCert(Cert_Id('MANINAFILECERT'), post_code, Sign_post_code)
           AS Подпись_почтового_кода
FROM dbo.client
WHERE key_client = N'ANATR';

-- Количество записей с действительной подписью телефона
SELECT COUNT(*) AS Действительных_подписей_телефона
FROM dbo.client
WHERE VerifySignedByAsymKey(AsymKey_Id('MANINAASYMKEY'), phone, Sign_phone) = 1;
