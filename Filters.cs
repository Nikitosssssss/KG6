using System;
using System.Drawing;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComputerGraphics_Filters
{
    public abstract class Filter
    {
        protected abstract Color calculateNewPixelColor(Bitmap sourceImage, int x, int y);

        public virtual Bitmap processImage(Bitmap sourceImage, BackgroundWorker worker,int MaxPercent = 100, int add = 0)
        {
            Bitmap resultImage = new Bitmap(sourceImage.Width, sourceImage.Height);

            for (int i = 0; i < sourceImage.Width; i++)
            {
                worker.ReportProgress((int)((double)i / resultImage.Width * MaxPercent) + add);
                if (worker.CancellationPending)
                    return null;
                for (int j = 0; j < sourceImage.Height; j++)
                {
                    resultImage.SetPixel(i, j, calculateNewPixelColor(sourceImage, i, j));
                }
            }

            return resultImage;
        }

        protected int Clamp(int value, int min, int max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }
    }

    public class NegativFilter : Filter
    {
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            Color sourceColor = sourceImage.GetPixel(x, y);
            return Color.FromArgb(255 - sourceColor.R, 255 - sourceColor.G, 255 - sourceColor.B);
        }
    }

    public class GrayScaleFilter : Filter
    {
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            Color sourceColor = sourceImage.GetPixel(x, y);
            int Intensity = (int)(0.299 * sourceColor.R + 0.5876 * sourceColor.G + 0.114 * sourceColor.B);
            Intensity = Clamp(Intensity, 0, 255);
            return Color.FromArgb(Intensity, Intensity, Intensity);
        }
    }

    public class YarkostFilter : Filter
    {
        private int amount;

        public YarkostFilter(int amount)
        {
            this.amount = amount;
        }

        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            Color sourceColor = sourceImage.GetPixel(x, y);
            return Color.FromArgb(Clamp(sourceColor.R + amount, 0, 255),
                Clamp(sourceColor.G + amount, 0, 255),
                Clamp(sourceColor.B + amount, 0, 255));
        }
    }

    public class ContrastFilter : GlobalFilter
    {
        protected int brightness = 0;

        private double amount;

        public ContrastFilter(double amount)
        {
            this.amount = amount;
        }

        public override Bitmap processImage(Bitmap sourceImage, BackgroundWorker worker, int MaxPercent = 100, int add = 0)
        {
            Bitmap resultImage = new Bitmap(sourceImage.Width, sourceImage.Height);

            brightness = GetBrightness(sourceImage, worker, 50);

            for (int i = 0; i < sourceImage.Width; i++)
            {
                worker.ReportProgress((int)((double)i / resultImage.Width * 50) + 50);
                if (worker.CancellationPending)
                    return null;
                for (int j = 0; j < sourceImage.Height; j++)
                {
                    resultImage.SetPixel(i, j, calculateNewPixelColor(sourceImage, i, j));
                }
            }

            return resultImage;
        }

        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            double c = amount;
            Color sourceColor = sourceImage.GetPixel(x, y);
            return Color.FromArgb(Clamp((int)(brightness + (sourceColor.R - brightness) * c), 0, 255),
                                  Clamp((int)(brightness + (sourceColor.G - brightness) * c), 0, 255),
                                  Clamp((int)(brightness + (sourceColor.B - brightness) * c), 0, 255));
        }
    }

    public abstract class GlobalFilter : Filter
    {
        /// <summary>
        /// Возвращает среднюю яркость по всем каналам
        /// </summary>
        public int GetBrightness(Bitmap sourceImage, BackgroundWorker worker, int MaxPercent = 100)
        {
            long brightness = 0;
            for (int i = 0; i < sourceImage.Width; i++)
            {
                worker.ReportProgress((int)((double)i / sourceImage.Width * MaxPercent));
                if (worker.CancellationPending)
                    return 0;
                for (int j = 0; j < sourceImage.Height; j++)
                {
                    long pix = 0;
                    Color color = sourceImage.GetPixel(i, j);
                    pix += color.R;
                    pix += color.G;
                    pix += color.B;
                    pix /= 3;
                    brightness += pix;
                }
            }
            brightness /= sourceImage.Width * sourceImage.Height;
            return (int)brightness;
        }
    }
    
    public class MatrixFilter : Filter
    {
        protected double[,] kernel = null;

        protected MatrixFilter() { }
        public MatrixFilter(double[,] kernel)
        {
            this.kernel = kernel;
        }

        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            int radiusX = kernel.GetLength(0) / 2;
            int radiusY = kernel.GetLength(1) / 2;

            double resultR = 0;
            double resultG = 0;
            double resultB = 0;

            for (int l = -radiusX; l <= radiusX; l++)
            {
                for (int k = -radiusY; k <= radiusY; k++)
                {
                    int idX = Clamp(x + k, 0, sourceImage.Width - 1);
                    int idY = Clamp(y + l, 0, sourceImage.Height - 1);
                    Color neighbourColor = sourceImage.GetPixel(idX, idY);
                    resultR += neighbourColor.R * kernel[k + radiusX, l + radiusY];
                    resultG += neighbourColor.G * kernel[k + radiusX, l + radiusY];
                    resultB += neighbourColor.B * kernel[k + radiusX, l + radiusY];
                }
            }

            return Color.FromArgb(
                Clamp((int)resultR, 0, 255),
                Clamp((int)resultG, 0, 255),
                Clamp((int)resultB, 0, 255)
            );
        }
    }

    public class MedianFilter : Filter
    {
        private int radius;
        public MedianFilter(int radius = 1)
        {
            this.radius = radius;
        }

        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            int size = (radius * 2 + 1) * (radius * 2 + 1);
            List<int> reds = new List<int>();
            List<int> greens = new List<int>();
            List<int> blues = new List<int>();

            for (int l = -radius; l <= radius; l++)
            {
                for (int k = -radius; k <= radius; k++)
                {
                    int idX = Clamp(x + k, 0, sourceImage.Width - 1);
                    int idY = Clamp(y + l, 0, sourceImage.Height - 1);
                    Color neighbourColor = sourceImage.GetPixel(idX, idY);
                    reds.Add(neighbourColor.R);
                    greens.Add(neighbourColor.G);
                    blues.Add(neighbourColor.B);
                }
            }

            // Сортировка и нахождение медианы
            reds.Sort();
            greens.Sort();
            blues.Sort();

            int medianR = reds[reds.Count / 2];
            int medianG = greens[greens.Count / 2];
            int medianB = blues[blues.Count / 2];

            return Color.FromArgb(
                Clamp(medianR, 0, 255),
                Clamp(medianG, 0, 255),
                Clamp(medianB, 0, 255)
            );
        }
    }
    
    public class SharpenFilter : MatrixFilter
    {
        public SharpenFilter()
        {
           // Определение ядра для повышения резкости
           kernel = new double[,]
           {
           { 0, -1, 0 },
           { -1, 5, -1 },
           { 0, -1, 0 }
           };
        }
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            // Возвращает цвет нового пикселя после применения фильтра
            int radiusX = kernel.GetLength(0) / 2;
            int radiusY = kernel.GetLength(1) / 2;
            double resultR = 0;
            double resultG = 0;
            double resultB = 0;
            // Применение ядерной свертки
            for (int l = -radiusX; l <= radiusX; l++)
            {
                for (int k = -radiusY; k <= radiusY; k++)
                {
                    int idX = Clamp(x + k, 0, sourceImage.Width - 1);
                    int idY = Clamp(y + l, 0, sourceImage.Height - 1);
                    Color neighbourColor = sourceImage.GetPixel(idX, idY);

                    resultR += neighbourColor.R * kernel[k + radiusX, l + radiusY];
                    resultG += neighbourColor.G * kernel[k + radiusX, l + radiusY];
                    resultB += neighbourColor.B * kernel[k + radiusX, l + radiusY];
                }
            }

            return Color.FromArgb(  Clamp((int)resultR, 0, 255),
                                    Clamp((int)resultG, 0, 255),
                                    Clamp((int)resultB, 0, 255));
        }
     }
  

    public class GaussianFilter : MatrixFilter
    {
        public GaussianFilter(int radius = 3, double sigma = 2)
        {
            int size = radius * 2 + 1;
            kernel = new double[size, size];
            double norm = 0;
            for (int i = -radius; i <= radius; i++)
            {
                for (int j = -radius; j <= radius; j++)
                {
                    kernel[i + radius, j + radius] = Math.Exp(-(i * i + j * j) / (sigma * sigma));
                    norm += kernel[i + radius, j + radius];
                }
            }
            for (int i = 0; i < size; i++)
                for (int j = 0; j < size; j++)
                    kernel[i, j] /= norm;
        }
    }

    public class WavesFilter : Filter
    {
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            int new_x = x + (int)(20 * Math.Sin(2 * Math.PI * y / 30));
            int new_y = y;
            new_x = Clamp(new_x, 0, sourceImage.Width - 1);
            new_y = Clamp(new_y, 0, sourceImage.Height - 1);
            return sourceImage.GetPixel(new_x, new_y);
        }
    }

    public class NoiseDotsFilter : Filter
    {
        protected readonly Random random = new Random();
        protected double p_white; // Вероятность белых точек
        protected double p_black; // Вероятность черных точек

        public NoiseDotsFilter(double pWhite = 0.02, double pBlack = 0.02)
        {
            p_white = pWhite;
            p_black = pBlack;
        }

        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            double p = random.NextDouble(); // Случайное число от 0 до 1
            if (p < p_white)
                return Color.White; // Белый шум
            else if (p + p_black > 1)
                return Color.Black; // Черный шум
            else
                return sourceImage.GetPixel(x, y); // Оригинальный пиксель
        }
    }

    public class NoiseLinesFilter : Filter
    {
        protected readonly Random random = new Random();
        protected int numberOfLines; // Количество линий

        public NoiseLinesFilter(int numberOfLines = 50, int maxLength = 40)
        {
            this.numberOfLines = numberOfLines;
            this.maxLength = maxLength;
        }

        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            // Вернем оригинальный цвет, линии обрабатываются в процессе
            return sourceImage.GetPixel(x, y);
        }

        public override Bitmap processImage(Bitmap sourceImage, BackgroundWorker worker, int MaxPercent = 100, int add = 0)
        {
            Bitmap resultImage = new Bitmap(sourceImage);

            for (int i = 0; i < numberOfLines; i++)
            {
                for (int j = 0; j < maxLength; j++)
                {
                    // Случайная начальная точка
                    int startX = random.Next(0, sourceImage.Width);
                    int startY = random.Next(0, sourceImage.Height);

                    // Случайный угол и длина
                    double angle = random.NextDouble() * 2 * Math.PI; // Угол в радианах
                    int length = random.Next(10, maxLength);

                    // Случайный цвет линии
                    bool isBlack = random.Next(2) == 0;
                    Color lineColor = isBlack ? Color.Black : Color.White;

                    // Рисуем линию
                    DrawLine(resultImage, startX, startY, angle, length, lineColor);
                }
            }

            return resultImage;
        }

        private void DrawLine(Bitmap image, int startX, int startY, double angle, int length, Color color)
        {
            for (int i = 0; i < length; i++)
            {
                // Вычисляем координаты следующего пикселя вдоль линии
                int x = startX + (int)(i * Math.Cos(angle));
                int y = startY + (int)(i * Math.Sin(angle));

                // Проверяем, находится ли пиксель в границах изображения
                if (x >= 0 && x < image.Width && y >= 0 && y < image.Height)
                {
                    image.SetPixel(x, y, color);
                }
                else
                {
                    break; // Выходим, если линия выходит за границы изображения
                }
            }
        }

        private int maxLength;
    }

    public class NoiseCirclesFilter : Filter
    {
        protected readonly Random random = new Random();
        private readonly int numberOfCircles; // Количество окружностей
        private readonly int maxRadius;      // Максимальный радиус окружности
        private readonly double p_white;     // Вероятность белой окружности
        private readonly double p_black;     // Вероятность черной окружности

        public NoiseCirclesFilter(int numberOfCircles = 1000, int maxRadius = 30, double pWhite = 0.5, double pBlack = 0.5)
        {
            this.numberOfCircles = numberOfCircles;
            this.maxRadius = maxRadius;
            this.p_white = pWhite;
            this.p_black = pBlack;
        }

        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            // Окружности рисуются в процессе обработки изображения
            return sourceImage.GetPixel(x, y);
        }

        public override Bitmap processImage(Bitmap sourceImage, BackgroundWorker worker, int MaxPercent = 100, int add = 0)
        {
            Bitmap resultImage = new Bitmap(sourceImage);

            for (int i = 0; i < numberOfCircles; i++)
            {
                // Случайные параметры окружности
                int centerX = random.Next(0, sourceImage.Width);   // Координата центра
                int centerY = random.Next(0, sourceImage.Height);  // Координата центра
                int radius = random.Next(5, maxRadius);           // Радиус окружности

                // Случайный цвет
                Color circleColor = random.NextDouble() < p_white
                    ? Color.White
                    : random.NextDouble() < p_black ? Color.Black : Color.Transparent;

                if (circleColor != Color.Transparent)
                {
                    DrawCircle(resultImage, centerX, centerY, radius, circleColor);
                }
            }

            return resultImage;
        }

        private void DrawCircle(Bitmap image, int centerX, int centerY, int radius, Color color)
        {
            for (int angle = 0; angle < 360; angle++)
            {
                // Вычисляем координаты точки на окружности
                int x = centerX + (int)(radius * Math.Cos(angle * Math.PI / 180.0));
                int y = centerY + (int)(radius * Math.Sin(angle * Math.PI / 180.0));

                // Проверяем границы изображения
                if (x >= 0 && x < image.Width && y >= 0 && y < image.Height)
                {
                    image.SetPixel(x, y, color);
                }
            }
        }
    }

    public class ContourFilter : Filter
    {
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            Color currentColor = sourceImage.GetPixel(x, y);
            Color leftColor = sourceImage.GetPixel(Clamp(x - 1, 0, sourceImage.Width - 1), y);
            Color rightColor = sourceImage.GetPixel(Clamp(x + 1, 0, sourceImage.Width - 1), y);
            Color topColor = sourceImage.GetPixel(x, Clamp(y - 1, 0, sourceImage.Height - 1));
            Color bottomColor = sourceImage.GetPixel(x, Clamp(y + 1, 0, sourceImage.Height - 1));

            // Определение различия между текущим цветом и окружающими
            bool isDifferent = IsDifferentColor(currentColor, leftColor) ||
                               IsDifferentColor(currentColor, rightColor) ||
                               IsDifferentColor(currentColor, topColor) ||
                               IsDifferentColor(currentColor, bottomColor);

            // Если разница, возвращаем черный цвет для контура
            return isDifferent ? Color.Blue : currentColor; 
        }

        private bool IsDifferentColor(Color c1, Color c2)
        {
            int threshold = 10; // Порог для определения различия цветов
            return Math.Abs(c1.R - c2.R) > threshold ||
                   Math.Abs(c1.G - c2.G) > threshold ||
                   Math.Abs(c1.B - c2.B) > threshold;
        }

        public override Bitmap processImage(Bitmap sourceImage, BackgroundWorker worker, int MaxPercent = 100, int add = 0)
        {
            Bitmap resultImage = new Bitmap(sourceImage.Width, sourceImage.Height);

            for (int i = 0; i < sourceImage.Width; i++)
            {
                worker.ReportProgress((int)((double)i / resultImage.Width * MaxPercent) + add);
                if (worker.CancellationPending)
                    return null;
                for (int j = 0; j < sourceImage.Height; j++)
                {
                    resultImage.SetPixel(i, j, calculateNewPixelColor(sourceImage, i, j));
                }
            }

            return resultImage;
        }
    }
}
