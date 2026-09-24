using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Lab1
{
    public enum GrayscaleFormula
    {
        PalNtsc,  // Y' = 0.299R + 0.587G + 0.114B
        Hdtv      // Y' = 0.2126R + 0.7152G + 0.0722B
    }

    public static class ImageProcessor
    {
        /// <summary>
        /// Преобразует изображение в оттенки серого по выбранной формуле
        /// </summary>
        public static WriteableBitmap ToGrayscale(WriteableBitmap source, GrayscaleFormula formula)
        {
            int width = source.PixelWidth;
            int height = source.PixelHeight;
            int stride = width * 4; // Bgra32 = 4 байта на пиксель
            byte[] pixels = new byte[height * stride];

            source.CopyPixels(pixels, stride, 0);

            double rCoeff, gCoeff, bCoeff;

            switch (formula)
            {
                case GrayscaleFormula.PalNtsc:
                    rCoeff = 0.299;
                    gCoeff = 0.587;
                    bCoeff = 0.114;
                    break;
                case GrayscaleFormula.Hdtv:
                    rCoeff = 0.2126;
                    gCoeff = 0.7152;
                    bCoeff = 0.0722;
                    break;
                default:
                    rCoeff = 0.299;
                    gCoeff = 0.587;
                    bCoeff = 0.114;
                    break;
            }

            for (int i = 0; i < pixels.Length; i += 4)
            {
                byte b = pixels[i];
                byte g = pixels[i + 1];
                byte r = pixels[i + 2];
                // byte a = pixels[i + 3]; // альфа-канал не трогаем

                double gray = rCoeff * r + gCoeff * g + bCoeff * b;
                byte grayByte = (byte)Math.Clamp(gray, 0, 255);

                pixels[i] = grayByte;     // B
                pixels[i + 1] = grayByte; // G
                pixels[i + 2] = grayByte; // R
                // pixels[i + 3] остаётся без изменений (альфа)
            }

            var result = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
            result.WritePixels(new System.Windows.Int32Rect(0, 0, width, height), pixels, stride, 0);

            return result;
        }

        /// <summary>
        /// Вычисляет разность двух изображений (по модулю)
        /// </summary>
        public static WriteableBitmap GetDifference(WriteableBitmap img1, WriteableBitmap img2)
        {
            int width = img1.PixelWidth;
            int height = img1.PixelHeight;
            int stride = width * 4;
            byte[] pixels1 = new byte[height * stride];
            byte[] pixels2 = new byte[height * stride];

            img1.CopyPixels(pixels1, stride, 0);
            img2.CopyPixels(pixels2, stride, 0);

            byte[] resultPixels = new byte[height * stride];

            for (int i = 0; i < pixels1.Length; i += 4)
            {
                // Разность по каждому каналу (для grayscale все каналы одинаковы)
                byte diffB = (byte)Math.Abs(pixels1[i] - pixels2[i]);
                byte diffG = (byte)Math.Abs(pixels1[i + 1] - pixels2[i + 1]);
                byte diffR = (byte)Math.Abs(pixels1[i + 2] - pixels2[i + 2]);

                resultPixels[i] = diffB;
                resultPixels[i + 1] = diffG;
                resultPixels[i + 2] = diffR;
                resultPixels[i + 3] = pixels1[i + 3]; // альфа
            }

            var result = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
            result.WritePixels(new System.Windows.Int32Rect(0, 0, width, height), resultPixels, stride, 0);

            return result;
        }

        /// <summary>
        /// Вычисляет гистограмму интенсивности (256 бинов)
        /// </summary>
        public static int[] CalculateHistogram(WriteableBitmap bitmap)
        {
            int width = bitmap.PixelWidth;
            int height = bitmap.PixelHeight;
            int stride = width * 4;
            byte[] pixels = new byte[height * stride];

            bitmap.CopyPixels(pixels, stride, 0);

            int[] histogram = new int[256];

            for (int i = 0; i < pixels.Length; i += 4)
            {
                // Для grayscale все каналы одинаковы, берём любой
                byte intensity = pixels[i + 2]; // R канал
                histogram[intensity]++;
            }

            return histogram;
        }
    }
}