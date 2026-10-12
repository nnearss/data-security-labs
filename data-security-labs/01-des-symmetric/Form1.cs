// Лабораторная работа № 1. Одноключевое шифрование (DES)
// Lab 1. Symmetric encryption with DES — Windows Forms, .NET

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

namespace lab1
{
    public partial class Form1 : Form
    {
        private byte[] key = null;   // ключ DES, 8 байт
        private byte[] iv  = null;   // вектор инициализации, 8 байт

        public Form1()
        {
            InitializeComponent();
        }

        private void создатьКлючToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Генерируем новый ключ DES и вектор инициализации
            using (DESCryptoServiceProvider des = new DESCryptoServiceProvider())
            {
                des.GenerateKey();   // 8 случайных байт; слабые ключи DES отсеиваются автоматически
                des.GenerateIV();    // 8 случайных байт
                key = des.Key;
                iv = des.IV;
            }

            // Пользователь выбирает имя файла ключа
            saveFileDialog1.Title = "Сохранить файл ключа";
            saveFileDialog1.Filter = "Файл ключа (*.key)|*.key|Все файлы (*.*)|*.*";
            saveFileDialog1.FileName = "des.key";
            if (saveFileDialog1.ShowDialog() != DialogResult.OK) return;

            // Записываем ключ и IV двумя строками в кодировке Base64
            using (StreamWriter sw = new StreamWriter(saveFileDialog1.FileName, false, Encoding.ASCII))
            {
                sw.WriteLine(Convert.ToBase64String(key));
                sw.WriteLine(Convert.ToBase64String(iv));
            }

            richTextBox1.Text =
                "Создан новый ключ DES." + Environment.NewLine +
                "Файл ключа: " + saveFileDialog1.FileName + Environment.NewLine +
                "Key (hex): " + BitConverter.ToString(key) + Environment.NewLine +
                "IV  (hex): " + BitConverter.ToString(iv);
        }

        private void mnuEncrypt_Click(object sender, EventArgs e)
        {
            if (key == null || iv == null)
            {
                MessageBox.Show("Сначала создайте ключ (Шифрование → Создать ключ).",
                                "Нет ключа", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 1. Выбираем исходный файл
            openFileDialog1.Title = "Выберите файл для шифрования";
            openFileDialog1.Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*";
            if (openFileDialog1.ShowDialog() != DialogResult.OK) return;

            // Читаем его как Юникод (UTF-16) и показываем пользователю
            string plainText = File.ReadAllText(openFileDialog1.FileName, Encoding.Unicode);
            richTextBox1.Text = plainText;
            byte[] plainBytes = Encoding.Unicode.GetBytes(plainText);

            // 2. Выбираем, куда сохранить результат
            saveFileDialog1.Title = "Сохранить зашифрованный файл";
            saveFileDialog1.Filter = "Зашифрованный файл (*.des)|*.des|Все файлы (*.*)|*.*";
            saveFileDialog1.FileName = "encrypted.des";
            if (saveFileDialog1.ShowDialog() != DialogResult.OK) return;

            // 3. Шифруем потоком
            using (DESCryptoServiceProvider des = new DESCryptoServiceProvider())
            {
                des.Key = key;
                des.IV = iv;

                using (FileStream fs = new FileStream(saveFileDialog1.FileName, FileMode.Create))
                using (CryptoStream cs = new CryptoStream(fs, des.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(plainBytes, 0, plainBytes.Length);
                    cs.FlushFinalBlock();
                }
            }

            MessageBox.Show("Файл зашифрован:" + Environment.NewLine + saveFileDialog1.FileName,
                            "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void mnuShowEncrypted_Click(object sender, EventArgs e)
        {
            openFileDialog1.Title = "Выберите зашифрованный файл";
            openFileDialog1.Filter = "Зашифрованный файл (*.des)|*.des|Все файлы (*.*)|*.*";
            if (openFileDialog1.ShowDialog() != DialogResult.OK) return;

            byte[] cipher = File.ReadAllBytes(openFileDialog1.FileName);

            richTextBox2.Text =
                "Файл: " + openFileDialog1.FileName + Environment.NewLine +
                "Размер: " + cipher.Length + " байт" + Environment.NewLine + Environment.NewLine +
                "Шифротекст (шестнадцатеричный вид):" + Environment.NewLine +
                BitConverter.ToString(cipher).Replace("-", " ") + Environment.NewLine + Environment.NewLine +
                "Шифротекст (Base64):" + Environment.NewLine +
                Convert.ToBase64String(cipher);
        }

        private void mnuReadKey_Click(object sender, EventArgs e)
        {
            openFileDialog1.Title = "Выберите файл ключа";
            openFileDialog1.Filter = "Файл ключа (*.key)|*.key|Все файлы (*.*)|*.*";
            if (openFileDialog1.ShowDialog() != DialogResult.OK) return;

            try
            {
                string[] lines = File.ReadAllLines(openFileDialog1.FileName, Encoding.ASCII);
                key = Convert.FromBase64String(lines[0]);
                iv = Convert.FromBase64String(lines[1]);

                richTextBox1.Text =
                    "Файл ключа: " + openFileDialog1.FileName + Environment.NewLine +
                    "Алгоритм: DES (симметричный, одноключевой)" + Environment.NewLine +
                    "Режим шифрования: CBC, дополнение: PKCS7" + Environment.NewLine +
                    "Длина ключа: " + (key.Length * 8) + " бит (из них 56 значащих)" + Environment.NewLine +
                    "Длина блока / IV: " + (iv.Length * 8) + " бит" + Environment.NewLine +
                    "Слабый ключ: " + (DES.IsWeakKey(key) ? "да" : "нет") + Environment.NewLine +
                    Environment.NewLine +
                    "Key (hex): " + BitConverter.ToString(key) + Environment.NewLine +
                    "IV  (hex): " + BitConverter.ToString(iv);
            }
            catch (Exception ex)
            {
                key = null; iv = null;
                MessageBox.Show("Не удалось прочитать файл ключа: " + ex.Message,
                                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void mnuDecrypt_Click(object sender, EventArgs e)
        {
            if (key == null || iv == null)
            {
                MessageBox.Show("Сначала прочитайте файл ключа (Дешифрование → Прочесть информацию о ключе).",
                                "Нет ключа", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            openFileDialog1.Title = "Выберите зашифрованный файл";
            openFileDialog1.Filter = "Зашифрованный файл (*.des)|*.des|Все файлы (*.*)|*.*";
            if (openFileDialog1.ShowDialog() != DialogResult.OK) return;

            try
            {
                using (DESCryptoServiceProvider des = new DESCryptoServiceProvider())
                {
                    des.Key = key;
                    des.IV = iv;

                    using (FileStream fs = new FileStream(openFileDialog1.FileName, FileMode.Open, FileAccess.Read))
                    using (CryptoStream cs = new CryptoStream(fs, des.CreateDecryptor(), CryptoStreamMode.Read))
                    using (MemoryStream ms = new MemoryStream())
                    {
                        cs.CopyTo(ms);
                        richTextBox2.Text = Encoding.Unicode.GetString(ms.ToArray());
                    }
                }
            }
            catch (CryptographicException)
            {
                MessageBox.Show("Не удалось расшифровать файл: неверный ключ или файл повреждён.",
                                "Ошибка расшифровки", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
