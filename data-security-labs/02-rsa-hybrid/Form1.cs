// Лабораторная работа № 2. Двухключевое (гибридное) шифрование: RSA + DES
// Lab 2. Hybrid encryption: RSA protects a one-time DES key — Windows Forms, .NET

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Security.Cryptography;

namespace lab2
{
    public partial class Form1 : Form
    {
        private string cipherFilePath = null;   // путь к выбранному зашифрованному файлу

        public Form1()
        {
            InitializeComponent();
        }

        private void mnuCreateKeys_Click(object sender, EventArgs e)
        {
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(1024))
            {
                rsa.PersistKeyInCsp = false;   // не сохранять ключ в хранилище Windows

                // 1. Сохраняем СЕКРЕТНЫЙ ключ
                saveFileDialog1.Title = "Сохранить файл СЕКРЕТНОГО ключа";
                saveFileDialog1.Filter = "Файл ключа (*.xml)|*.xml|Все файлы (*.*)|*.*";
                saveFileDialog1.FileName = "private.xml";
                if (saveFileDialog1.ShowDialog() != DialogResult.OK) return;

                string privatePath = saveFileDialog1.FileName;
                File.WriteAllText(privatePath, rsa.ToXmlString(true), Encoding.UTF8);

                // 2. Сохраняем ОТКРЫТЫЙ ключ
                saveFileDialog1.Title = "Сохранить файл ОТКРЫТОГО ключа";
                saveFileDialog1.FileName = "public.xml";
                if (saveFileDialog1.ShowDialog() != DialogResult.OK) return;

                string publicPath = saveFileDialog1.FileName;
                File.WriteAllText(publicPath, rsa.ToXmlString(false), Encoding.UTF8);

                richTextBox1.Text =
                    "Создана новая пара ключей RSA." + Environment.NewLine +
                    "Длина ключа: " + rsa.KeySize + " бит" + Environment.NewLine +
                    Environment.NewLine +
                    "Файл секретного ключа: " + privatePath + Environment.NewLine +
                    "Файл открытого ключа:  " + publicPath;
            }
        }

        private void mnuReadPublic_Click(object sender, EventArgs e)
        {
            openFileDialog1.Title = "Выберите файл открытого ключа";
            openFileDialog1.Filter = "Файл ключа (*.xml)|*.xml|Все файлы (*.*)|*.*";
            if (openFileDialog1.ShowDialog() != DialogResult.OK) return;

            try
            {
                string xml = File.ReadAllText(openFileDialog1.FileName, Encoding.UTF8);

                using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
                {
                    rsa.PersistKeyInCsp = false;
                    rsa.FromXmlString(xml);

                    RSAParameters pk = rsa.ExportParameters(false);

                    richTextBox1.Text =
                        "Файл ключа: " + openFileDialog1.FileName + Environment.NewLine +
                        "Алгоритм: RSA (асимметричный, двухключевой)" + Environment.NewLine +
                        "Длина ключа: " + rsa.KeySize + " бит" + Environment.NewLine +
                        "Содержит секретную часть: " + (rsa.PublicOnly ? "нет" : "да") + Environment.NewLine +
                        Environment.NewLine +
                        "Открытая экспонента E (" + pk.Exponent.Length + " байт):" + Environment.NewLine +
                        BitConverter.ToString(pk.Exponent) + Environment.NewLine +
                        Environment.NewLine +
                        "Модуль N (" + pk.Modulus.Length + " байт):" + Environment.NewLine +
                        BitConverter.ToString(pk.Modulus);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось прочитать файл ключа: " + ex.Message,
                                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void mnuReadPrivate_Click(object sender, EventArgs e)
        {
            openFileDialog1.Title = "Выберите файл секретного ключа";
            openFileDialog1.Filter = "Файл ключа (*.xml)|*.xml|Все файлы (*.*)|*.*";
            if (openFileDialog1.ShowDialog() != DialogResult.OK) return;

            try
            {
                string xml = File.ReadAllText(openFileDialog1.FileName, Encoding.UTF8);

                using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
                {
                    rsa.PersistKeyInCsp = false;
                    rsa.FromXmlString(xml);

                    if (rsa.PublicOnly)
                    {
                        MessageBox.Show("Выбранный файл содержит только открытый ключ. " +
                                        "Секретной части в нём нет.",
                                        "Это не секретный ключ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    RSAParameters sk = rsa.ExportParameters(true);

                    richTextBox1.Text =
                        "Файл ключа: " + openFileDialog1.FileName + Environment.NewLine +
                        "Алгоритм: RSA (асимметричный, двухключевой)" + Environment.NewLine +
                        "Длина ключа: " + rsa.KeySize + " бит" + Environment.NewLine +
                        Environment.NewLine +
                        "Модуль N (" + sk.Modulus.Length + " байт):" + Environment.NewLine +
                        BitConverter.ToString(sk.Modulus) + Environment.NewLine +
                        Environment.NewLine +
                        "Секретная экспонента D (" + sk.D.Length + " байт):" + Environment.NewLine +
                        BitConverter.ToString(sk.D) + Environment.NewLine +
                        Environment.NewLine +
                        "Простой множитель P (" + sk.P.Length + " байт):" + Environment.NewLine +
                        BitConverter.ToString(sk.P) + Environment.NewLine +
                        Environment.NewLine +
                        "Простой множитель Q (" + sk.Q.Length + " байт):" + Environment.NewLine +
                        BitConverter.ToString(sk.Q);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось прочитать файл секретного ключа: " + ex.Message,
                                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void mnuSelectPlain_Click(object sender, EventArgs e)
        {
            openFileDialog1.Title = "Выберите текстовый файл для шифрования";
            openFileDialog1.Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*";
            if (openFileDialog1.ShowDialog() != DialogResult.OK) return;

            richTextBox1.Text = File.ReadAllText(openFileDialog1.FileName, Encoding.Unicode);
        }

        private void mnuEncrypt_Click(object sender, EventArgs e)
        {
            if (richTextBox1.Text.Length == 0)
            {
                MessageBox.Show("Сначала выберите файл (Шифрование → Выбрать файл).",
                                "Нет данных", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 1. Файл открытого ключа
            openFileDialog1.Title = "Выберите файл ОТКРЫТОГО ключа";
            openFileDialog1.Filter = "Файл ключа (*.xml)|*.xml|Все файлы (*.*)|*.*";
            if (openFileDialog1.ShowDialog() != DialogResult.OK) return;
            string publicXml = File.ReadAllText(openFileDialog1.FileName, Encoding.UTF8);

            // 2. Имя файла результата
            saveFileDialog1.Title = "Сохранить зашифрованный файл";
            saveFileDialog1.Filter = "Зашифрованный файл (*.enc)|*.enc|Все файлы (*.*)|*.*";
            saveFileDialog1.FileName = "encrypted.enc";
            if (saveFileDialog1.ShowDialog() != DialogResult.OK) return;

            try
            {
                byte[] encKey, encIv, cipher;

                // 3. Новый одноразовый симметричный ключ DES
                using (DESCryptoServiceProvider des = new DESCryptoServiceProvider())
                {
                    des.GenerateKey();
                    des.GenerateIV();

                    // 3а. Шифруем сам текст симметричным ключом
                    byte[] plainBytes = Encoding.Unicode.GetBytes(richTextBox1.Text);
                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (CryptoStream cs = new CryptoStream(ms, des.CreateEncryptor(), CryptoStreamMode.Write))
                        {
                            cs.Write(plainBytes, 0, plainBytes.Length);
                            cs.FlushFinalBlock();
                        }
                        cipher = ms.ToArray();
                    }

                    // 3б. Шифруем сам ключ DES и вектор инициализации открытым ключом RSA
                    using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
                    {
                        rsa.PersistKeyInCsp = false;
                        rsa.FromXmlString(publicXml);
                        encKey = rsa.Encrypt(des.Key, false);
                        encIv = rsa.Encrypt(des.IV, false);
                    }
                }

                // 4. Записываем всё в один файл: длина блока, затем сам блок
                using (BinaryWriter bw = new BinaryWriter(File.Open(saveFileDialog1.FileName, FileMode.Create)))
                {
                    bw.Write(encKey.Length); bw.Write(encKey);
                    bw.Write(encIv.Length); bw.Write(encIv);
                    bw.Write(cipher.Length); bw.Write(cipher);
                }

                richTextBox2.Text =
                    "Файл: " + saveFileDialog1.FileName + Environment.NewLine +
                    "Зашифрованный ключ DES: " + encKey.Length + " байт" + Environment.NewLine +
                    "Зашифрованный вектор инициализации: " + encIv.Length + " байт" + Environment.NewLine +
                    "Зашифрованные данные: " + cipher.Length + " байт" + Environment.NewLine +
                    Environment.NewLine +
                    "Зашифрованный ключ DES (hex):" + Environment.NewLine +
                    BitConverter.ToString(encKey).Replace("-", " ") + Environment.NewLine +
                    Environment.NewLine +
                    "Зашифрованные данные (hex):" + Environment.NewLine +
                    BitConverter.ToString(cipher).Replace("-", " ");
            }
            catch (CryptographicException ex)
            {
                MessageBox.Show("Ошибка шифрования: " + ex.Message,
                                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void mnuSelectCipher_Click(object sender, EventArgs e)
        {
            openFileDialog1.Title = "Выберите зашифрованный файл";
            openFileDialog1.Filter = "Зашифрованный файл (*.enc)|*.enc|Все файлы (*.*)|*.*";
            if (openFileDialog1.ShowDialog() != DialogResult.OK) return;

            cipherFilePath = openFileDialog1.FileName;
            byte[] raw = File.ReadAllBytes(cipherFilePath);

            richTextBox1.Text =
                "Файл: " + cipherFilePath + Environment.NewLine +
                "Размер: " + raw.Length + " байт" + Environment.NewLine +
                Environment.NewLine +
                "Содержимое файла (hex):" + Environment.NewLine +
                BitConverter.ToString(raw).Replace("-", " ");
        }

        private void mnuDecrypt_Click(object sender, EventArgs e)
        {
            if (cipherFilePath == null)
            {
                MessageBox.Show("Сначала выберите файл (Дешифрование → Выбрать файл).",
                                "Нет файла", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            openFileDialog1.Title = "Выберите файл СЕКРЕТНОГО ключа";
            openFileDialog1.Filter = "Файл ключа (*.xml)|*.xml|Все файлы (*.*)|*.*";
            if (openFileDialog1.ShowDialog() != DialogResult.OK) return;

            try
            {
                byte[] encKey, encIv, cipher;

                // 1. Разбираем файл на три блока
                using (BinaryReader br = new BinaryReader(File.Open(cipherFilePath, FileMode.Open, FileAccess.Read)))
                {
                    encKey = br.ReadBytes(br.ReadInt32());
                    encIv = br.ReadBytes(br.ReadInt32());
                    cipher = br.ReadBytes(br.ReadInt32());
                }

                // 2. Секретным ключом RSA восстанавливаем ключ DES и вектор инициализации
                byte[] desKey, desIv;
                string privateXml = File.ReadAllText(openFileDialog1.FileName, Encoding.UTF8);

                using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
                {
                    rsa.PersistKeyInCsp = false;
                    rsa.FromXmlString(privateXml);

                    if (rsa.PublicOnly)
                        throw new CryptographicException(
                            "Выбранный файл содержит только открытый ключ RSA. " +
                            "Расшифровать информацию открытым ключом невозможно.");

                    desKey = rsa.Decrypt(encKey, false);
                    desIv = rsa.Decrypt(encIv, false);
                }

                // 3. Восстановленным ключом DES расшифровываем данные
                using (DESCryptoServiceProvider des = new DESCryptoServiceProvider())
                {
                    des.Key = desKey;
                    des.IV = desIv;

                    using (MemoryStream input = new MemoryStream(cipher))
                    using (CryptoStream cs = new CryptoStream(input, des.CreateDecryptor(), CryptoStreamMode.Read))
                    using (MemoryStream output = new MemoryStream())
                    {
                        cs.CopyTo(output);
                        richTextBox2.Text = Encoding.Unicode.GetString(output.ToArray());
                    }
                }
            }
            catch (CryptographicException ex)
            {
                MessageBox.Show("Не удалось расшифровать файл." + Environment.NewLine + ex.Message,
                                "Ошибка расшифровки", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message,
                                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
