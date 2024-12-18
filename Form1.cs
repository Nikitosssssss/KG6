﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ComputerGraphics_Filters;

namespace ComputerGraphics_Filters
{
    public partial class Form1 : Form
    {
        Bitmap previous_image = null;
        Bitmap image = null;
        Filter lastFilter = null;

        public Form1()
        {
            InitializeComponent();
        }

        private int[] OutputHistogramm(Bitmap image)
        {
            int[] histogram = new int[256]; // Массив для хранения частот яркости (от 0 до 255)

            for (int x = 0; x < image.Width; x++)
            {
                for (int y = 0; y < image.Height; y++)
                {
                    Color pixelColor = image.GetPixel(x, y);

                    // Рассчитываем интенсивность (яркость) пикселя
                    int brightness = (int)(0.299 * pixelColor.R + 0.5876 * pixelColor.G + 0.114 * pixelColor.B);

                    histogram[brightness]++; // Увеличиваем частоту соответствующей яркости
                }
            }

            return histogram;
        }

        private void DrawHistogram(int[] histogram, PictureBox pictureBox)
        {
            int width = 256 + 55; // Ширина гистограммы с учётом разметки
            int height = 540; // Полная высота для отображения гистограммы
            int paddingTop = 10; // Отступ сверху
            int paddingBottom = 10; // Отступ снизу

            Bitmap histogramBitmap = new Bitmap(width, height);
            int totalPixels = histogram.Sum(); // Общее количество пикселей

            // Преобразуем значения в проценты
            double[] percentages = histogram.Select(value => (double)value / totalPixels * 100).ToArray();

            using (Graphics g = Graphics.FromImage(histogramBitmap))
            {
                g.Clear(Color.White); // Заливаем фон белым цветом

                // Рисуем шкалу оси Y
                int numberOfTicks = 20; // Количество отметок на оси Y
                Font font = new Font("Arial", 10); // Шрифт для текста
                Brush brush = Brushes.Black;
                Pen pen = new Pen(Color.Gray, 1);
                for (int i = 0; i <= numberOfTicks; i++)
                {
                    int y = height - paddingBottom - (i * (height - paddingTop - paddingBottom) / numberOfTicks);
                    int labelValue = i * 5; // Шаг в процентах (0, 20, 40, ..., 100)

                    // Линия шкалы
                    g.DrawLine(pen, 45, y, width - 10, y);

                    // Текстовая разметка
                    g.DrawString(labelValue.ToString() + "%", font, brush, 5, y - 7);
                }

                // Рисуем столбцы гистограммы
                for (int i = 0; i < percentages.Length; i++)
                {
                    int barHeight = (int)(percentages[i] / 100 * (height - paddingTop - paddingBottom)); // Нормализуем высоту столбцов
                    int barTop = height - paddingBottom - barHeight; // Верхняя точка столбца
                    g.DrawLine(Pens.Black, i + 45, height - paddingBottom, i + 45, barTop); // Столбец от нижней границы вверх
                }
            }

            pictureBox.Image = histogramBitmap; // Отображаем гистограмму
            pictureBox.Refresh();
        }


        private void UpdateHistogram()
        {
            if (image != null)
            {
                int[] histogram = OutputHistogramm(image); // Вычисляем гистограмму
                DrawHistogram(histogram, pictureBox2); // Отображаем её в PictureBox
            }
            else
            {
                MessageBox.Show("Загрузите изображение!");
            }
        }


        // Файл

        private void Open_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.Title = "Выбор исходного изображения:";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                previous_image = image;
                image = new Bitmap(openFileDialog1.FileName);
                pictureBox1.Image = image;
                pictureBox1.Refresh();
                UpdateHistogram(); // Обновляем гистограмму
            }
        }

        private void Save_as_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (image != null)
            {
                saveFileDialog1.Title = "Сохранение результата:";
                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    image.Save(saveFileDialog1.FileName);
                }
            }
            else
            {
                MessageBox.Show("Загрузите изображение!");
            }
        }

        // Отмена

        private void Cancel_button_Click(object sender, EventArgs e)
        {
            backgroundWorker1.CancelAsync();
        }

        // Правка

        private void Undo_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            image = previous_image;
            pictureBox1.Image = image;
            pictureBox1.Refresh();
            UpdateHistogram(); // Обновляем гистограмму
        }

        private void Repeat_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            image = previous_image;
            pictureBox1.Image = image;
            pictureBox1.Refresh();
            UpdateHistogram(); // Обновляем гистограмму
        }

        // BackgroundWorker1

        private void BackgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            if (image != null)
            {
                Bitmap resultImage = ((Filter)e.Argument).processImage(image, backgroundWorker1);
               // Bitmap resultImage = ((Filter)e.Argument).processImage(new Bitmap(image), backgroundWorker1);

                if (!backgroundWorker1.CancellationPending)
                {
                    previous_image = image;
                    lastFilter = (Filter)e.Argument;
                    image = resultImage;
                }
            }
            else
            {
                MessageBox.Show("Загрузите изображение!");
            }
        }

        private void BackgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (!e.Cancelled)
            {
                pictureBox1.Image = image;
                pictureBox1.Refresh();
                UpdateHistogram(); // Обновляем гистограмму
            }
        }

        private void StartFilter(Filter filter)
        {
            if (backgroundWorker1.IsBusy == false)
                backgroundWorker1.RunWorkerAsync(filter);
        }

       
        private void button1_Click(object sender, EventArgs e)
        {
            StartFilter(new NegativFilter());
        }

        private void button2_Click(object sender, EventArgs e)
        {
            int amount = Convert.ToInt32(amountTextBox.Text);
            StartFilter(new YarkostFilter(amount));
        }

        private void button3_Click(object sender, EventArgs e)
        {
            int amount = Convert.ToInt32(amountTextBox.Text);
            StartFilter(new YarkostFilter(-amount));
        }

        private void button4_Click(object sender, EventArgs e)
        {
            StartFilter(new GrayScaleFilter());
        }

        private void button6_Click(object sender, EventArgs e)
        {
            double amount = Convert.ToDouble(amountTextBox.Text);
            StartFilter(new ContrastFilter(1/amount));
        }

        private void button5_Click(object sender, EventArgs e)
        {
            double amount = Convert.ToDouble(amountTextBox.Text);
            StartFilter(new ContrastFilter(amount));
        }

        private void button7_Click(object sender, EventArgs e)
        {
            double amount = Convert.ToDouble(amountTextBox.Text);
            StartFilter(new NoiseDotsFilter());
        }
        private void button8_Click(object sender, EventArgs e)
        {
            double amount = Convert.ToDouble(amountTextBox.Text);
            StartFilter(new NoiseLinesFilter());
        }

        private void button9_Click(object sender, EventArgs e)
        {
            double amount = Convert.ToDouble(amountTextBox.Text);
            StartFilter(new NoiseCirclesFilter());
        }

        private void button10_Click(object sender, EventArgs e)
        {
            StartFilter(new GaussianFilter());
        }

        private void button11_Click(object sender, EventArgs e)
        {
            StartFilter(new MedianFilter()); 
        }

        private void button12_Click(object sender, EventArgs e)
        {
            StartFilter(new ContourFilter());
        }

        private void button13_Click(object sender, EventArgs e)
        {
            StartFilter(new SharpenFilter());
        }
    }
}
