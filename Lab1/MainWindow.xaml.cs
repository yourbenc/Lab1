using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace Lab1
{
    public partial class MainWindow : Window
    {
        private BitmapSource _originalImage;
        private WriteableBitmap _grayscale1Bitmap; // PAL/NTSC
        private WriteableBitmap _grayscale2Bitmap; // HDTV
        private WriteableBitmap _differenceBitmap;

        private WriteableBitmap _redChannelBitmap;
        private WriteableBitmap _greenChannelBitmap;
        private WriteableBitmap _blueChannelBitmap;

        private string _currentFilePath;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void LoadImage_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All files (*.*)|*.*",
                Title = "Выберите изображение"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _currentFilePath = openFileDialog.FileName;
                _originalImage = new BitmapImage(new Uri(_currentFilePath));
                OriginalImage.Source = _originalImage;

                ProcessImage();
                StatusText.Text = $"Загружено: {Path.GetFileName(_currentFilePath)}";
            }
        }

        private void SaveImage_Click(object sender, RoutedEventArgs e)
        {
            if (_differenceBitmap == null)
            {
                MessageBox.Show("Сначала загрузите изображение!", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var saveFileDialog = new SaveFileDialog
            {
                Filter = "PNG Image (*.png)|*.png|JPEG Image (*.jpg)|*.jpg|BMP Image (*.bmp)|*.bmp",
                Title = "Сохранить результат",
                DefaultExt = "png"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                SaveBitmapToFile(_differenceBitmap, saveFileDialog.FileName);
                StatusText.Text = $"Сохранено: {Path.GetFileName(saveFileDialog.FileName)}";
            }
        }

        private void ProcessImage()
        {
            if (_originalImage == null) return;

            // Преобразуем в формат с доступом к пикселям
            var sourceBitmap = ConvertToWritableBitmap(_originalImage);

            // Задание 1: два варианта grayscale
            _grayscale1Bitmap = ImageProcessor.ToGrayscale(sourceBitmap, GrayscaleFormula.PalNtsc);
            _grayscale2Bitmap = ImageProcessor.ToGrayscale(sourceBitmap, GrayscaleFormula.Hdtv);

            // Разность
            _differenceBitmap = ImageProcessor.GetDifference(_grayscale1Bitmap, _grayscale2Bitmap);

            // Отображаем
            Grayscale1Image.Source = _grayscale1Bitmap;
            Grayscale2Image.Source = _grayscale2Bitmap;
            DifferenceImage.Source = _differenceBitmap;

            // Гистограммы
            DrawHistogram(Histogram1Canvas, _grayscale1Bitmap, Colors.Blue);
            DrawHistogram(Histogram2Canvas, _grayscale2Bitmap, Colors.Red);


            // Задание 2: выделяем каналы R, G, B
            _redChannelBitmap = ImageProcessor.ExtractChannel(sourceBitmap, 'R');
            _greenChannelBitmap = ImageProcessor.ExtractChannel(sourceBitmap, 'G');
            _blueChannelBitmap = ImageProcessor.ExtractChannel(sourceBitmap, 'B');

            // Отображаем каналы
            RedChannelImage.Source = _redChannelBitmap;
            GreenChannelImage.Source = _greenChannelBitmap;
            BlueChannelImage.Source = _blueChannelBitmap;

            // Гистограммы RGB
            ImageProcessor.CalculateRgbHistograms(
                sourceBitmap,
                out int[] redHistogram,
                out int[] greenHistogram,
                out int[] blueHistogram);

            DrawColorHistogram(RedHistogramCanvas, redHistogram, Colors.Red);
            DrawColorHistogram(GreenHistogramCanvas, greenHistogram, Colors.Green);
            DrawColorHistogram(BlueHistogramCanvas, blueHistogram, Colors.Blue);



        }

        private WriteableBitmap ConvertToWritableBitmap(BitmapSource source)
        {
            var bitmap = new WriteableBitmap(
                new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0));
            return bitmap;
        }

        private void DrawHistogram(Canvas canvas, WriteableBitmap bitmap, Color barColor)
        {
            canvas.Children.Clear();

            int[] histogram = ImageProcessor.CalculateHistogram(bitmap);
            int maxCount = 0;
            foreach (int count in histogram)
            {
                if (count > maxCount) maxCount = count;
            }

            if (maxCount == 0) return;

            double canvasWidth = canvas.Width;
            double canvasHeight = canvas.Height;
            double barWidth = canvasWidth / 256.0;

            for (int i = 0; i < 256; i++)
            {
                double barHeight = (double)histogram[i] / maxCount * canvasHeight;

                var rectangle = new System.Windows.Shapes.Rectangle
                {
                    Width = Math.Max(barWidth, 1),
                    Height = barHeight,
                    Fill = new SolidColorBrush(barColor),
                    VerticalAlignment = VerticalAlignment.Bottom
                };

                Canvas.SetLeft(rectangle, i * barWidth);
                Canvas.SetBottom(rectangle, 0);
                canvas.Children.Add(rectangle);
            }
        }

        private void DrawColorHistogram(Canvas canvas, int[] histogram, Color barColor)
        {
            canvas.Children.Clear();

            int maxCount = 0;

            foreach (int count in histogram)
            {
                if (count > maxCount)
                    maxCount = count;
            }

            if (maxCount == 0) return;

            double canvasWidth = canvas.Width;
            double canvasHeight = canvas.Height;
            double barWidth = canvasWidth / 256.0;

            for (int i = 0; i < 256; i++)
            {
                double barHeight =
                    (double)histogram[i] / maxCount * canvasHeight;

                var rectangle = new System.Windows.Shapes.Rectangle
                {
                    Width = Math.Max(barWidth, 1),
                    Height = barHeight,
                    Fill = new SolidColorBrush(barColor)
                };

                Canvas.SetLeft(rectangle, i * barWidth);
                Canvas.SetBottom(rectangle, 0);

                canvas.Children.Add(rectangle);
            }
        }


        private void SaveBitmapToFile(WriteableBitmap bitmap, string filePath)
        {
            BitmapEncoder encoder;
            string ext = Path.GetExtension(filePath).ToLower();

            switch (ext)
            {
                case ".jpg":
                case ".jpeg":
                    encoder = new JpegBitmapEncoder();
                    break;
                case ".bmp":
                    encoder = new BmpBitmapEncoder();
                    break;
                default:
                    encoder = new PngBitmapEncoder();
                    break;
            }

            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                encoder.Save(fileStream);
            }
        }
    }
}