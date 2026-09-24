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
        /// Преобразует RGB в HSV, применяет сдвиг оттенка и относительные изменения
        /// насыщенности и яркости, затем преобразует результат обратно в RGB
        /// </summary>
        /// <param name="source">Исходное изображение</param>
        /// <param name="hueShiftDegrees">Сдвиг оттенка в градусах</param>
        /// <param name="saturationPercent">Относительное изменение насыщенности в процентах</param>
        /// <param name="valuePercent">Относительное изменение яркости в процентах</param>
        /// <returns>Новое изображение с применёнными HSV-настройками и RGB-пикселями</returns>
        public static WriteableBitmap AdjustHsv(WriteableBitmap source, double hueShiftDegrees,
            double saturationPercent, double valuePercent)
        {
            int width = source.PixelWidth;
            int height = source.PixelHeight;
            int step = width * 4;
            byte[] pixels = new byte[height * step];
            source.CopyPixels(pixels, step, 0);

            double saturationFactor = 1.0 + saturationPercent / 100.0;
            double valueFactor = 1.0 + valuePercent / 100.0;

            for (int i = 0; i < pixels.Length; i += 4)
            {
                double b = pixels[i] / 255.0;
                double g = pixels[i + 1] / 255.0;
                double r = pixels[i + 2] / 255.0;
                RgbToHsv(r, g, b, out double hue, out double saturation, out double value);

                hue = (hue + hueShiftDegrees) % 360.0;
                if (hue < 0) hue += 360.0;
                saturation = Math.Clamp(saturation * saturationFactor, 0.0, 1.0);
                value = Math.Clamp(value * valueFactor, 0.0, 1.0);

                HsvToRgb(hue, saturation, value, out r, out g, out b);
                pixels[i] = ToByte(b);
                pixels[i + 1] = ToByte(g);
                pixels[i + 2] = ToByte(r);
            }

            var result = new WriteableBitmap(width, height, source.DpiX, source.DpiY, PixelFormats.Bgra32, null);
            result.WritePixels(new System.Windows.Int32Rect(0, 0, width, height), pixels, step, 0);
            return result;
        }

        /// <summary>Ограничивает канал RGB диапазоном 0–1, переводит его в 0–255 и округляет</summary>
        /// <param name="channel">Нормированное значение цветового канала</param>
        /// <returns>Значение цветового канала в диапазоне байта</returns>
        private static byte ToByte(double channel) =>
            (byte)Math.Clamp((int)Math.Round(channel * 255.0), 0, 255);

        /// <summary>Преобразует нормированные каналы RGB в компоненты HSV</summary>
        /// <param name="r">Красный канал RGB в диапазоне 0–1</param>
        /// <param name="g">Зелёный канал RGB в диапазоне 0–1</param>
        /// <param name="b">Синий канал RGB в диапазоне 0–1</param>
        /// <param name="hue">Выходной оттенок в градусах, от 0 до 360</param>
        /// <param name="saturation">Выходная насыщенность в диапазоне 0–1</param>
        /// <param name="value">Выходная яркость (Value) в диапазоне 0–1</param>
        private static void RgbToHsv(double r, double g, double b, out double hue,
            out double saturation, out double value)
        {
            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            double delta = max - min;
            value = max;
            saturation = max == 0 ? 0 : delta / max;

            if (delta == 0) hue = 0;
            else if (max == r) hue = 60.0 * (((g - b) / delta) % 6.0);
            else if (max == g) hue = 60.0 * ((b - r) / delta + 2.0);
            else hue = 60.0 * ((r - g) / delta + 4.0);

            if (hue < 0) hue += 360.0;
        }

        /// <summary>Преобразует компоненты HSV в нормированные каналы RGB</summary>
        /// <param name="hue">Оттенок в градусах, от 0 до 360</param>
        /// <param name="saturation">Насыщенность в диапазоне 0–1</param>
        /// <param name="value">Яркость (Value) в диапазоне 0–1</param>
        /// <param name="r">Выходной красный канал RGB в диапазоне 0–1</param>
        /// <param name="g">Выходной зелёный канал RGB в диапазоне 0–1</param>
        /// <param name="b">Выходной синий канал RGB в диапазоне 0–1</param>
        private static void HsvToRgb(double hue, double saturation, double value,
            out double r, out double g, out double b)
        {
            double chroma = value * saturation;
            double x = chroma * (1.0 - Math.Abs((hue / 60.0) % 2.0 - 1.0));
            double m = value - chroma;
            double r1, g1, b1;

            if (hue < 60) (r1, g1, b1) = (chroma, x, 0);
            else if (hue < 120) (r1, g1, b1) = (x, chroma, 0);
            else if (hue < 180) (r1, g1, b1) = (0, chroma, x);
            else if (hue < 240) (r1, g1, b1) = (0, x, chroma);
            else if (hue < 300) (r1, g1, b1) = (x, 0, chroma);
            else (r1, g1, b1) = (chroma, 0, x);

            r = r1 + m;
            g = g1 + m;
            b = b1 + m;
        }

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

        public static WriteableBitmap ExtractChannel(WriteableBitmap source, char channel)
        {
            int width = source.PixelWidth;
            int height = source.PixelHeight;
            int stride = width * 4;

            byte[] pixels = new byte[height * stride];

            source.CopyPixels(pixels, stride, 0);

            for (int i = 0; i < pixels.Length; i += 4)
            {
                byte b = pixels[i];
                byte g = pixels[i + 1];
                byte r = pixels[i + 2];

                switch (char.ToUpper(channel))
                {
                    case 'R':
                        pixels[i] = 0;
                        pixels[i + 1] = 0;
                        pixels[i + 2] = r;
                        break;

                    case 'G':
                        pixels[i] = 0;
                        pixels[i + 1] = g;
                        pixels[i + 2] = 0;
                        break;

                    case 'B':
                        pixels[i] = b;
                        pixels[i + 1] = 0;
                        pixels[i + 2] = 0;
                        break;

                    default:
                        throw new ArgumentException("Канал должен быть R, G или B");
                }
            }

            var result = new WriteableBitmap(
                width,
                height,
                96,
                96,
                PixelFormats.Bgra32,
                null);

            result.WritePixels(
                new System.Windows.Int32Rect(0, 0, width, height),
                pixels,
                stride,
                0);

            return result;
        }
        public static void CalculateRgbHistograms(WriteableBitmap bitmap, out int[] redHistogram, out int[] greenHistogram, out int[] blueHistogram)
        {
            int width = bitmap.PixelWidth;
            int height = bitmap.PixelHeight;
            int stride = width * 4;

            byte[] pixels = new byte[height * stride];

            bitmap.CopyPixels(pixels, stride, 0);

            redHistogram = new int[256];
            greenHistogram = new int[256];
            blueHistogram = new int[256];

            for (int i = 0; i < pixels.Length; i += 4)
            {
                byte b = pixels[i];
                byte g = pixels[i + 1];
                byte r = pixels[i + 2];

                redHistogram[r]++;
                greenHistogram[g]++;
                blueHistogram[b]++;
            }
        }


    }
}
