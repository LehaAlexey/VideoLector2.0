using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WpfApplication.Core.Models
{
    public class SlideViewModel : INotifyPropertyChanged
    {
        private string _slidesFolder;
        private string _currentImagePath;
        private int _currentSlideNumber;
        private int _totalSlides;
        public BitmapImage _currentBitmapImage;
        private VideoPosition _position;
        public string OriginalImagePath => Path.Combine(_slidesFolder, "slides", $"Слайд{CurrentSlideNumber}.PNG");
        public bool IsImageAvailable => File.Exists(OriginalImagePath);

        public async Task<string> GetExportImagePathAsync()
        {
            if (!IsImageAvailable)
            {
                Debug.WriteLine($"Slide image not found: {OriginalImagePath}");
                return null;
            }

            // Если уже PNG - используем оригинал
            if (Path.GetExtension(OriginalImagePath).Equals(".png", StringComparison.OrdinalIgnoreCase))
            {
                Debug.WriteLine($"Using original PNG: {OriginalImagePath}");
                return OriginalImagePath;
            }

            // Конвертируем в PNG
            return await ConvertToPngAsync(OriginalImagePath);
        }

        private async Task<string> ConvertToPngAsync(string sourcePath)
        {
            string tempPath = Path.GetTempFileName();
            tempPath = Path.ChangeExtension(tempPath, ".png");

            try
            {
                Debug.WriteLine($"Converting {sourcePath} to PNG...");

                string arguments = $"-i \"{sourcePath}\" \"{tempPath}\"";
                await RunFFmpegAsync(arguments);

                Debug.WriteLine($"Conversion successful: {tempPath}");
                return tempPath;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Conversion failed: {ex.Message}");
                File.Delete(tempPath);
                throw;
            }
        }

        private Task RunFFmpegAsync(string arguments)
        {
            return Task.Run(() =>
            {
                using (var process = new Process())
                {
                    process.StartInfo = new ProcessStartInfo
                    {
                        FileName = "ffmpeg",
                        Arguments = arguments,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardError = true
                    };

                    process.Start();
                    string errorOutput = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        throw new Exception($"FFmpeg error: {errorOutput}");
                    }
                }
            });
        }

        
        
        public VideoPosition Position
        {
            get => _position;
            set
            {
                if (_position != value)
                {
                    _position = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(VideoHorizontalAlignment));
                    OnPropertyChanged(nameof(VideoVerticalAlignment));
                }
            }
        }

        public System.Windows.HorizontalAlignment VideoHorizontalAlignment
        {
            get
            {
                return _position switch
                {
                    VideoPosition.TopLeft or VideoPosition.BottomLeft => System.Windows.HorizontalAlignment.Left,
                    VideoPosition.TopRight or VideoPosition.BottomRight => System.Windows.HorizontalAlignment.Right,
                    _ => System.Windows.HorizontalAlignment.Center
                };
            }
        }

        public VerticalAlignment VideoVerticalAlignment
        {
            get
            {
                return _position switch
                {
                    VideoPosition.TopLeft or VideoPosition.TopRight => VerticalAlignment.Top,
                    VideoPosition.BottomLeft or VideoPosition.BottomRight => VerticalAlignment.Bottom,
                    _ => VerticalAlignment.Center
                };
            }
        }
        public ImageSource CurrentImage
        {
            get
            {
                if (_currentBitmapImage != null)
                    return _currentBitmapImage;

                if (!string.IsNullOrEmpty(CurrentImagePath) && File.Exists(CurrentImagePath))
                {
                    try
                    {
                        _currentBitmapImage = new BitmapImage();
                        _currentBitmapImage.BeginInit();
                        _currentBitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                        _currentBitmapImage.UriSource = new Uri(CurrentImagePath);
                        _currentBitmapImage.EndInit();
                        _currentBitmapImage.Freeze();
                        return _currentBitmapImage;
                    }
                    catch
                    {
                        return CreateBlankImage();
                    }
                }
                return CreateBlankImage();
            }
        }
        public string CurrentImagePath
        {
            get => _currentImagePath;
            private set
            {
                _currentImagePath = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CurrentImage));
            }
        }

        public int CurrentSlideNumber
        {
            get => _currentSlideNumber;
            set
            {
                if (value >= 1 && value <= TotalSlides)
                {
                    _currentSlideNumber = value;
                    UpdateCurrentImage();
                    OnPropertyChanged();
                }
            }
        }

        public int TotalSlides
        {
            get => _totalSlides;
            private set
            {
                _totalSlides = value;
                OnPropertyChanged();
            }
        }

        public string VideoPath { get; }

        public SlideViewModel(int initialSlideNumber, string videoPath, string pptxPath,
                            string slidesFolder, VideoPosition position)
        {
            if (string.IsNullOrWhiteSpace(pptxPath))
                throw new ArgumentNullException(nameof(pptxPath));
            if (string.IsNullOrWhiteSpace(slidesFolder))
                throw new ArgumentNullException(nameof(slidesFolder));

            VideoPath = videoPath;
            Position = position;
            _slidesFolder = slidesFolder;

            string slidesSubFolder = Path.Combine(slidesFolder, "slides");
            Directory.CreateDirectory(slidesSubFolder);

            if (!Directory.GetFiles(slidesSubFolder, "*.png").Any())
            {
                ConvertPptxToImages(pptxPath);
            }

            _currentImagePath = Path.Combine(slidesSubFolder, $"Слайд{initialSlideNumber}.PNG");
        }

        private bool AllSlideImagesExist()
        {
            for (int i = 1; i <= TotalSlides; i++)
            {
                string imagePath = Path.Combine(_slidesFolder, $"{i}.png");
                if (!File.Exists(imagePath)) return false;
            }
            return true;
        }

        private bool HasPowerPointInstalled()
        {
            try
            {
                var powerpointType = Type.GetTypeFromProgID("PowerPoint.Application");
                return powerpointType != null;
            }
            catch
            {
                return false;
            }
        }

        private void ConvertPptxToImages(string pptxPath)
        {
            try
            {
                // Проверяем количество слайдов
                using (var presentationDoc = PresentationDocument.Open(pptxPath, false))
                {
                    var slides = presentationDoc.PresentationPart?.SlideParts;
                    TotalSlides = slides?.Count() ?? 0;
                }

                if (TotalSlides == 0)
                {
                    throw new InvalidOperationException("Presentation contains no slides");
                }

                // Проверяем, нужно ли конвертировать
                bool needConversion = !AllSlideImagesExist();

                if (needConversion)
                {
                    // Используем PowerPoint для конвертации, если установлен
                    if (HasPowerPointInstalled())
                    {
                        ConvertUsingPowerPoint(pptxPath);
                    }
                    else
                    {
                        throw new InvalidOperationException("Microsoft PowerPoint is not installed");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting PPTX: {ex.Message}");
                throw;
            }
        }

        private void ConvertUsingPowerPoint(string pptxPath)
        {
            dynamic powerpointApp = null;
            dynamic presentation = null;

            try
            {
                string outputFolder = Path.Combine(_slidesFolder, "slides");
                Directory.CreateDirectory(outputFolder);

                powerpointApp = Activator.CreateInstance(Type.GetTypeFromProgID("PowerPoint.Application"));
                powerpointApp.Visible = -1;

                presentation = powerpointApp.Presentations.Open(pptxPath);

                // Сохраняем каждый слайд отдельно
                for (int i = 1; i <= TotalSlides; i++)
                {
                    string slidePath = Path.Combine(outputFolder, $"Слайд{i}.PNG");

                    // Удаляем старый файл, если существует
                    if (File.Exists(slidePath))
                    {
                        try { File.Delete(slidePath); }
                        catch { /* Игнорируем ошибки удаления */ }
                    }

                    presentation.Slides[i].Export(slidePath, "PNG");
                }
            }
            finally
            {
                // Правильное освобождение ресурсов PowerPoint
                if (presentation != null)
                {
                    presentation.Close();
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(presentation);
                }
                if (powerpointApp != null)
                {
                    powerpointApp.Quit();
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(powerpointApp);
                }
            }
        }
        public void Dispose()
        {
            _currentBitmapImage = null;
        }

        public void UpdateCurrentImage()
        {
            string imagePath = Path.Combine(_slidesFolder, "slides", $"Слайд{CurrentSlideNumber}.PNG");

            // Сбрасываем текущее изображение
            _currentBitmapImage = null;

            CurrentImagePath = File.Exists(imagePath) ? imagePath : null;
            OnPropertyChanged(nameof(CurrentImage));
        }

        private BitmapImage CreateBlankImage()
        {
            // Создаем пустое изображение, когда нет слайда
            var blankImage = new BitmapImage();
            blankImage.BeginInit();
            blankImage.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
            blankImage.CacheOption = BitmapCacheOption.OnLoad;
            blankImage.UriSource = new Uri("pack://application:,,,/Resources/blank_slide.png");
            blankImage.EndInit();
            blankImage.Freeze();
            return blankImage;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}