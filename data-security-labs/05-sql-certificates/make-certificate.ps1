# Лабораторная работа № 5: личный сертификат Windows -> файлы .cer и .pvk для SQL Server
# Lab 5: create a Windows certificate and convert it to .cer + .pvk for SQL Server
#
# Пароль задаётся один раз в переменной $KeyPassword.

$KeyPassword = "<KEY_PASSWORD>"

# ============================================================
# 3.3. Создание личного сертификата в Windows (пункт 7)
# ============================================================
# Проверка имеющихся личных сертификатов
Get-ChildItem Cert:\CurrentUser\My | Format-List Subject, Thumbprint, HasPrivateKey

# Создание личного сертификата
$cert = New-SelfSignedCertificate `
  -Subject "CN=Manina Veronika" `
  -CertStoreLocation "Cert:\CurrentUser\My" `
  -KeyExportPolicy Exportable `
  -KeySpec KeyExchange `
  -KeyLength 2048 `
  -NotAfter (Get-Date).AddYears(5)

$cert | Format-List Subject, Thumbprint, NotAfter, HasPrivateKey

# ============================================================
# 3.4. Экспорт сертификата в файлы
# ============================================================
New-Item -ItemType Directory -Force -Path C:\sql | Out-Null
$pw = ConvertTo-SecureString -String $KeyPassword -Force -AsPlainText

# Закрытый ключ вместе с сертификатом
Export-PfxCertificate -Cert $cert -FilePath C:\sql\cert.pfx `
                      -Password $pw -CryptoAlgorithmOption AES256_SHA256

# Только открытая часть, формат DER
Export-Certificate -Cert $cert -FilePath C:\sql\cert.cer -Type CERT

# ============================================================
# 3.5. Конвертация закрытого ключа в формат PVK (пункт 8)
# ============================================================
$ssl = "C:\Program Files\OpenSSL-Win64-ARM\bin\openssl.exe"
$mod = "C:\Program Files\OpenSSL-Win64-ARM\bin"
cd C:\sql

# pfx -> pem
& $ssl pkcs12 -in cert.pfx -nocerts -nodes -out key.pem -passin pass:$KeyPassword

# pem -> pvk (с подключением провайдера legacy)
& $ssl rsa -in key.pem -outform PVK -pvk-strong -out key.pvk `
           -passout pass:$KeyPassword -provider-path $mod -provider legacy -provider default
