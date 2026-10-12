// Лабораторная работа № 3. Цифровая подпись: RSA + MD5
// Lab 3. Digital signature with RSA and MD5 — Windows Forms, .NET

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace lab3
{
    public partial class Form1 : Form
    {
        private string publicKeyXml = null;   // открытый ключ, прочитанный для верификации

        public Form1()
        {
            InitializeComponent();
        }

        private void mnuKeys_Click(object sender, EventArgs e)
        {
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(1024))
            {
                rsa.PersistKeyInCsp = false;

                // 1. Секретный ключ
                saveFileDialog1.Title = "Сохранить файл СЕКРЕТНОГО ключа";
                saveFileDialog1.Filter = "Файл ключа (*.xml)|*.xml|Все файлы (*.*)|*.*";
                saveFileDialog1.FileName = "private.xml";
                if (saveFileDialog1.ShowDialog() != DialogResult.OK) return;

                string privatePath = saveFileDialog1.FileName;
                File.WriteAllText(privatePath, rsa.ToXmlString(true), Encoding.UTF8);

                // 2. Открытый ключ
                saveFileDialog1.Title = "Сохранить файл ОТКРЫТОГО ключа";
                saveFileDialog1.FileName = "public.xml";
                if (saveFileDialog1.ShowDialog() != DialogResult.OK) return;

                string publicPath = saveFileDialog1.FileName;
                File.WriteAllText(publicPath, rsa.ToXmlString(false), Encoding.UTF8);

                lblStatus.ForeColor = System.Drawing.Color.Black;
                lblStatus.Text = "Создана новая пара ключей RSA, длина " + rsa.KeySize + " бит";

                richTextBox1.Text =
                    "Создана новая пара ключей RSA." + Environment.NewLine +
                    "Длина ключа: " + rsa.KeySize + " бит" + Environment.NewLine +
                    Environment.NewLine +
                    "Файл секретного ключа: " + privatePath + Environment.NewLine +
                    "Файл открытого ключа:  " + publicPath;
            }
        }

        private void mnuSignOpen_Click(object sender, EventArgs e)
        {
            openFileDialog1.Title = "Выберите текстовый файл для подписи";
            openFileDialog1.Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*";
            if (openFileDialog1.ShowDialog() != DialogResult.OK) return;

            richTextBox1.Text = File.ReadAllText(openFileDialog1.FileName, Encoding.Unicode);

            lblStatus.ForeColor = System.Drawing.Color.Black;
            lblStatus.Text = "Открыт файл: " + openFileDialog1.FileName;
        }

        private void mnuSign_Click(object sender, EventArgs e)
        {
            if (richTextBox1.Text.Length == 0)
            {
                MessageBox.Show("Сначала откройте файл (Подпись → Открыть файл).",
                                "Нет данных", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 1. Файл секретного ключа
            openFileDialog1.Title = "Выберите файл СЕКРЕТНОГО ключа";
            openFileDialog1.Filter = "Файл ключа (*.xml)|*.xml|Все файлы (*.*)|*.*";
            if (openFileDialog1.ShowDialog() != DialogResult.OK) return;

            try
            {
                string xml = File.ReadAllText(openFileDialog1.FileName, Encoding.UTF8);
                byte[] signature;

                using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
                {
                    rsa.PersistKeyInCsp = false;
                    rsa.FromXmlString(xml);

                    // Проверка по пункту 15 задания
                    if (rsa.PublicOnly)
                        throw new CryptographicException(
                            "Выбранный файл содержит только открытый ключ RSA. " +
                            "Создать цифровую подпись можно только с помощью секретного ключа.");

                    byte[] data = Encoding.Unicode.GetBytes(richTextBox1.Text);

                    // Хешируем текст по MD5 и подписываем хеш секретным ключом RSA
                    using (MD5 md5 = new MD5CryptoServiceProvider())
                    {
                        signature = rsa.SignData(data, md5);
                    }
                }

                // 2. Сохраняем подпись в двоичный файл
                saveFileDialog1.Title = "Сохранить файл подписи";
                saveFileDialog1.Filter = "Файл подписи (*.sig)|*.sig|Все файлы (*.*)|*.*";
                saveFileDialog1.FileName = "signature.sig";
                if (saveFileDialog1.ShowDialog() != DialogResult.OK) return;

                File.WriteAllBytes(saveFileDialog1.FileName, signature);

                lblStatus.ForeColor = System.Drawing.Color.Black;
                lblStatus.Text = "Подпись создана, " + signature.Length + " байт: " + saveFileDialog1.FileName;

                MessageBox.Show("Цифровая подпись создана и сохранена:" + Environment.NewLine +
                                saveFileDialog1.FileName + Environment.NewLine +
                                "Размер подписи: " + signature.Length + " байт",
                                "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (CryptographicException ex)
            {
                lblStatus.ForeColor = System.Drawing.Color.Red;
                lblStatus.Text = "Ошибка создания подписи";
                MessageBox.Show("Не удалось создать подпись." + Environment.NewLine + ex.Message,
                                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                lblStatus.ForeColor = System.Drawing.Color.Red;
                lblStatus.Text = "Ошибка";
                MessageBox.Show("Ошибка: " + ex.Message,
                                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void mnuVerOpen_Click(object sender, EventArgs e)
        {
            openFileDialog1.Title = "Выберите текстовый файл для проверки";
            openFileDialog1.Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*";
            if (openFileDialog1.ShowDialog() != DialogResult.OK) return;

            richTextBox1.Text = File.ReadAllText(openFileDialog1.FileName, Encoding.Unicode);

            lblStatus.ForeColor = System.Drawing.Color.Black;
            lblStatus.Text = "Открыт файл: " + openFileDialog1.FileName;
        }

        private void mnuVerKey_Click(object sender, EventArgs e)
        {
            openFileDialog1.Title = "Выберите файл ОТКРЫТОГО ключа";
            openFileDialog1.Filter = "Файл ключа (*.xml)|*.xml|Все файлы (*.*)|*.*";
            if (openFileDialog1.ShowDialog() != DialogResult.OK) return;

            try
            {
                string xml = File.ReadAllText(openFileDialog1.FileName, Encoding.UTF8);

                using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
                {
                    rsa.PersistKeyInCsp = false;
                    rsa.FromXmlString(xml);

                    publicKeyXml = xml;

                    lblStatus.ForeColor = System.Drawing.Color.Black;
                    lblStatus.Text = "Открытый ключ успешно прочитан: " +
                                     Path.GetFileName(openFileDialog1.FileName) +
                                     ", RSA, " + rsa.KeySize + " бит";
                }
            }
            catch (Exception ex)
            {
                publicKeyXml = null;
                lblStatus.ForeColor = System.Drawing.Color.Red;
                lblStatus.Text = "Не удалось прочитать открытый ключ";
                MessageBox.Show("Ошибка чтения ключа: " + ex.Message,
                                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void mnuVerify_Click(object sender, EventArgs e)
        {
            if (publicKeyXml == null)
            {
                MessageBox.Show("Сначала прочитайте открытый ключ (Верификация → Открытый ключ).",
                                "Нет ключа", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (richTextBox1.Text.Length == 0)
            {
                MessageBox.Show("Сначала откройте файл (Верификация → Открыть файл).",
                                "Нет данных", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            openFileDialog1.Title = "Выберите файл подписи";
            openFileDialog1.Filter = "Файл подписи (*.sig)|*.sig|Все файлы (*.*)|*.*";
            if (openFileDialog1.ShowDialog() != DialogResult.OK) return;

            try
            {
                byte[] signature = File.ReadAllBytes(openFileDialog1.FileName);
                byte[] data = Encoding.Unicode.GetBytes(richTextBox1.Text);
                bool ok;

                using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
                {
                    rsa.PersistKeyInCsp = false;
                    rsa.FromXmlString(publicKeyXml);

                    using (MD5 md5 = new MD5CryptoServiceProvider())
                    {
                        ok = rsa.VerifyData(data, md5, signature);
                    }
                }

                if (ok)
                {
                    lblStatus.ForeColor = System.Drawing.Color.Green;
                    lblStatus.Text = "ВЕРИФИКАЦИЯ УСПЕШНА: подпись подлинна, текст не изменялся";
                }
                else
                {
                    lblStatus.ForeColor = System.Drawing.Color.Red;
                    lblStatus.Text = "ВЕРИФИКАЦИЯ НЕ ПРОЙДЕНА: подпись не соответствует тексту";
                }
            }
            catch (Exception ex)
            {
                lblStatus.ForeColor = System.Drawing.Color.Red;
                lblStatus.Text = "Ошибка верификации";
                MessageBox.Show("Ошибка: " + ex.Message,
                                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
