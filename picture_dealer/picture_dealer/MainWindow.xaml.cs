using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using OpenCvSharp;
using Window = System.Windows.Window;
using Microsoft.Win32;
using System.Threading;

namespace picture_dealer
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    /// 
    public class ImageItem : INotifyPropertyChanged
    {
        public string ImagePath { get; set; }

        private string _status;
        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        private string _processingType;
        public string ProcessingType
        {
            get => _processingType;
            set
            {
                _processingType = value;
                OnPropertyChanged(nameof(ProcessingType));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ImageItem(string imagePath)
        {
            ImagePath = imagePath;
            Status = "[待处理]"; // 默认状态为 "待处理"
            ProcessingType = "Grayscale"; // 默认处理类型为 "Grayscale"
        }
    }

    public partial class MainWindow : Window
    {
        public ObservableCollection<ImageItem> ImageItems { get; set; } = new ObservableCollection<ImageItem>();
        private List<string> imagePaths;  // 存储图像文件路径
        private bool isProcessing = false;  // 标记是否正在处理
        private bool cancelProcessing = false;  // 标记是否需要取消处理
        public List<string> ProcessingTypes { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            imagePaths = new List<string>();
            DataContext = this;
            ProcessingTypes = new List<string> { "Grayscale", "Resize50" , "Resize200", "Rotate90Clockwise",  "Rotate90CounterClockwise","Blur" , "EdgeDetection", "InvertColor" };
        }

        // 选择图像文件并添加到列表框
        private void LoadImages()
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Multiselect = true,
                Filter = "Image Files|*.jpg;*.png;*.bmp;*.jpeg"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (var filePath in openFileDialog.FileNames)
                {
                    imagePaths.Add(filePath);
                    ImageItems.Add(new ImageItem(filePath));
                }
            }
        }

        private CancellationTokenSource cancellationTokenSource;

        // StartButton 点击事件，开始处理选中的图片
        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedImages = ImageListBox.SelectedItems.Cast<ImageItem>().ToList();

            // 检查是否选择了图片
            if (selectedImages.Count == 0)
            {
                MessageBox.Show("Please select images.");
                return;
            }

            // 如果当前有处理任务在进行中，直接返回
            if (isProcessing) return;

            // 设置处理标志为 true，表示正在处理
            isProcessing = true;

            // 创建一个新的 CancellationTokenSource
            cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            // 设置进度条的最大值为选择图片数量
            ProgressBar.Maximum = selectedImages.Count;
            ProgressBar.Value = 0;

            // 更新列表中每张图片的状态为 "[处理中]"
            foreach (var imageItem in selectedImages)
            {
                imageItem.Status = "[处理中]";
            }

            // 开始处理每张图片
            for (int i = 0; i < selectedImages.Count; i++)
            {
                // 如果收到取消信号，则退出循环并设置状态为已取消


                // 获取当前图像的处理类型
                var processingType = selectedImages[i].ProcessingType;
                if (cancellationToken.IsCancellationRequested)
                {
                    selectedImages[i].Status = "[处理已取消]";
                    continue;
                }
                try
                {
                   
                    bool useCustomPath = chkCustomSavePath.IsChecked == true;

                    // 处理图像
                    await ProcessImageAsync(selectedImages[i].ImagePath, processingType, cancellationTokenSource.Token, useCustomPath);
                
                    if (cancellationToken.IsCancellationRequested)
                    {
                        selectedImages[i].Status = "[处理已取消]";
                        continue;
                    }
                    // 更新当前图片状态为已完成
                    selectedImages[i].Status = "[处理完毕]";
                }
                catch (OperationCanceledException)
                {
                    // 捕获取消异常并更新状态
                    selectedImages[i].Status = "[处理已取消]";
                    break;
                }
                catch (Exception ex)
                {
                    // 如果发生其他错误，更新状态并显示错误信息
                    selectedImages[i].Status = "[处理失败]";
                    MessageBox.Show($"Error processing image {selectedImages[i].ImagePath}: {ex.Message}");
                    break;
                }

                // 更新进度条
                ProgressBar.Value = i + 1;
            }

            // 处理完毕后，更新标志并给出反馈
            isProcessing = false;

            if (cancellationToken.IsCancellationRequested)
            {
                MessageBox.Show("Image processing was canceled.");
            }
        }

        // 点击取消按钮时，取消处理任务
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // 发送取消请求
            cancellationTokenSource?.Cancel();
        }


        // 处理图像（异步方法）
        //private async Task ProcessImageAsync(string imagePath, string processingType, CancellationToken cancellationToken)
        //{
        //    // 弹出保存文件对话框，允许用户选择文件保存路径和文件名
        //    SaveFileDialog saveFileDialog = new SaveFileDialog();
        //    saveFileDialog.Filter = "JPEG Files|*.jpg|PNG Files|*.png|BMP Files|*.bmp";
        //    saveFileDialog.FileName = $"processed_{System.IO.Path.GetFileName(imagePath)}";

        //    // 如果用户选择了保存路径
        //    if (saveFileDialog.ShowDialog() == true)
        //    {
        //        try
        //        {
        //            // 读取图像
        //            using (var img = Cv2.ImRead(imagePath, ImreadModes.Color))
        //            {
        //                // 处理前检查取消标志
        //                cancellationToken.ThrowIfCancellationRequested();

        //                // 根据选中的处理类型执行相应的操作
        //                if (processingType == "Grayscale")
        //                {
        //                    var grayImage = new Mat();
        //                    Cv2.CvtColor(img, grayImage, ColorConversionCodes.BGR2GRAY);
        //                    cancellationToken.ThrowIfCancellationRequested();
        //                    Cv2.ImWrite(saveFileDialog.FileName, grayImage);
        //                }
        //                else if (processingType == "Resize50")
        //                {
        //                    var resizedImage = new Mat();
        //                    Cv2.Resize(img, resizedImage, new OpenCvSharp.Size(img.Width / 2, img.Height / 2));
        //                    cancellationToken.ThrowIfCancellationRequested();
        //                    Cv2.ImWrite(saveFileDialog.FileName, resizedImage);
        //                }
        //                else if (processingType == "Rotate90Clockwise")
        //                {
        //                    var rotationMatrix = Cv2.GetRotationMatrix2D(new OpenCvSharp.Point2f(img.Width / 2, img.Height / 2), -90, 1);
        //                    var rotatedImage = new Mat();
        //                    Cv2.WarpAffine(img, rotatedImage, rotationMatrix, img.Size());
        //                    cancellationToken.ThrowIfCancellationRequested();
        //                    Cv2.ImWrite(saveFileDialog.FileName, rotatedImage);
        //                }
        //                else if (processingType == "Rotate90CounterClockwise")
        //                {
        //                    var rotationMatrix = Cv2.GetRotationMatrix2D(new OpenCvSharp.Point2f(img.Width / 2, img.Height / 2), 90, 1);
        //                    var rotatedImage = new Mat();
        //                    Cv2.WarpAffine(img, rotatedImage, rotationMatrix, img.Size());
        //                    cancellationToken.ThrowIfCancellationRequested();
        //                    Cv2.ImWrite(saveFileDialog.FileName, rotatedImage);
        //                }
        //                else if (processingType == "Resize200")
        //                {
        //                    var zoomedImage = new Mat();
        //                    Cv2.Resize(img, zoomedImage, new OpenCvSharp.Size(img.Width * 2, img.Height * 2)); // 放大200%
        //                    cancellationToken.ThrowIfCancellationRequested();
        //                    Cv2.ImWrite(saveFileDialog.FileName, zoomedImage);
        //                }
        //                else if (processingType == "Blur")
        //                {
        //                    var blurredImage = new Mat();
        //                    Cv2.GaussianBlur(img, blurredImage, new OpenCvSharp.Size(15, 15), 0); // 高斯模糊
        //                    cancellationToken.ThrowIfCancellationRequested();
        //                    Cv2.ImWrite(saveFileDialog.FileName, blurredImage);
        //                }
        //                else if (processingType == "EdgeDetection")
        //                {
        //                    var grayImage = new Mat();
        //                    Cv2.CvtColor(img, grayImage, ColorConversionCodes.BGR2GRAY);
        //                    var edges = new Mat();
        //                    Cv2.Canny(grayImage, edges, 100, 200); // 边缘检测
        //                    cancellationToken.ThrowIfCancellationRequested();
        //                    Cv2.ImWrite(saveFileDialog.FileName, edges);
        //                }
        //                else
        //                {
        //                    MessageBox.Show("Unknown processing type.");
        //                }
        //            }
        //        }
        //        catch (OperationCanceledException)
        //        {
        //            // 处理已取消的情况
        //            MessageBox.Show("Image processing has been cancelled.");
        //            // 更新图片状态
        //            // selectedImages[i].Status = "[处理已取消]";  // 根据具体代码结构更新状态
        //        }
        //        catch (Exception ex)
        //        {
        //            MessageBox.Show($"Error processing image: {ex.Message}");
        //        }
        //    }
        //    else
        //    {
        //        MessageBox.Show("No file selected, image not saved.");
        //    }
        //}
        private async Task ProcessImageAsync(string imagePath, string processingType, CancellationToken cancellationToken, bool useCustomPath)
        {
            string saveFilePath;

            if (useCustomPath)
            {
                // 弹出保存文件对话框，允许用户选择文件保存路径和文件名
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "JPEG Files|*.jpg|PNG Files|*.png|BMP Files|*.bmp";
                saveFileDialog.FileName = $"processed_{System.IO.Path.GetFileName(imagePath)}"; // 默认文件名

                // 如果用户选择了保存路径
                if (saveFileDialog.ShowDialog() == true)
                {
                    saveFilePath = saveFileDialog.FileName;
                }
                else
                {
                    MessageBox.Show("No file selected, image not saved.");
                    return;
                }
            }
            else
            {
                // 获取项目的根目录
                string projectRootPath = AppDomain.CurrentDomain.BaseDirectory;

                // 设置默认保存路径为项目根目录下的 "ProcessedImages" 文件夹
                string defaultPath = System.IO.Path.Combine(projectRootPath, "ProcessedImages");

                // 如果该文件夹不存在，则创建它
                if (!System.IO.Directory.Exists(defaultPath))
                {
                    System.IO.Directory.CreateDirectory(defaultPath);
                }

                // 设置默认文件名
                string defaultFileName = $"processed_{System.IO.Path.GetFileName(imagePath)}";

                // 最终保存的完整路径
               saveFilePath = System.IO.Path.Combine(defaultPath, defaultFileName);
            }

         
                try
                {
                    // 读取图像
                    using (var img = Cv2.ImRead(imagePath, ImreadModes.Color))
                    {
                        // 处理前检查取消标志
                        cancellationToken.ThrowIfCancellationRequested();

                        // 根据选中的处理类型执行相应的操作
                        if (processingType == "Grayscale")
                        {
                            var grayImage = new Mat();
                            Cv2.CvtColor(img, grayImage, ColorConversionCodes.BGR2GRAY);

                            
                            await Task.Delay(5000);
                            cancellationToken.ThrowIfCancellationRequested();

                            Cv2.ImWrite(saveFilePath, grayImage);
                        }
                        else if (processingType == "Resize50")
                        {
                            var resizedImage = new Mat();
                            Cv2.Resize(img, resizedImage, new OpenCvSharp.Size(img.Width / 2, img.Height / 2));

                           
                            await Task.Delay(5000);
                            cancellationToken.ThrowIfCancellationRequested();

                            Cv2.ImWrite(saveFilePath, resizedImage);
                        }
                        else if (processingType == "Rotate90Clockwise")
                        {
                            var rotationMatrix = Cv2.GetRotationMatrix2D(new OpenCvSharp.Point2f(img.Width / 2, img.Height / 2), -90, 1);
                            var rotatedImage = new Mat();
                            Cv2.WarpAffine(img, rotatedImage, rotationMatrix, img.Size());

                            
                            await Task.Delay(5000);
                            cancellationToken.ThrowIfCancellationRequested();

                            Cv2.ImWrite(saveFilePath, rotatedImage);
                        }
                    else if (processingType == "InvertColor")
                    {
                        var invertedImage = new Mat();
                        Cv2.BitwiseNot(img, invertedImage);  // 反转图像颜色

                        // 等待模拟图像处理
                        await Task.Delay(5000);
                        cancellationToken.ThrowIfCancellationRequested();

                        Cv2.ImWrite(saveFilePath, invertedImage);
                    }
                    else if (processingType == "Rotate90CounterClockwise")
                        {
                            var rotationMatrix = Cv2.GetRotationMatrix2D(new OpenCvSharp.Point2f(img.Width / 2, img.Height / 2), 90, 1);
                            var rotatedImage = new Mat();
                            Cv2.WarpAffine(img, rotatedImage, rotationMatrix, img.Size());

                            // 模拟延时1秒
                            await Task.Delay(5000);
                            cancellationToken.ThrowIfCancellationRequested();

                            Cv2.ImWrite(saveFilePath, rotatedImage);
                        }
                        else if (processingType == "Resize200")
                        {
                            var zoomedImage = new Mat();
                            Cv2.Resize(img, zoomedImage, new OpenCvSharp.Size(img.Width * 2, img.Height * 2)); // 放大200%

                          
                            await Task.Delay(5000);
                            cancellationToken.ThrowIfCancellationRequested();

                            Cv2.ImWrite(saveFilePath, zoomedImage);
                        }
                        else if (processingType == "Blur")
                        {
                            var blurredImage = new Mat();
                            Cv2.GaussianBlur(img, blurredImage, new OpenCvSharp.Size(15, 15), 0); // 高斯模糊

                            
                            await Task.Delay(5000);
                            cancellationToken.ThrowIfCancellationRequested();

                            Cv2.ImWrite(saveFilePath, blurredImage);
                        }
                        else if (processingType == "EdgeDetection")
                        {
                            var grayImage = new Mat();
                            Cv2.CvtColor(img, grayImage, ColorConversionCodes.BGR2GRAY);
                            var edges = new Mat();
                            Cv2.Canny(grayImage, edges, 100, 200); // 边缘检测

                            
                            await Task.Delay(5000);
                            cancellationToken.ThrowIfCancellationRequested();

                            Cv2.ImWrite(saveFilePath, edges);
                        }
                        else
                        {
                            MessageBox.Show("Unknown processing type.");
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // 处理已取消的情况
                    MessageBox.Show("Image processing has been cancelled.");

                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error processing image: {ex.Message}");
                }
            

        }

        // 初始化界面时，自动加载图像
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadImages();
        }

        private void AddPicture_Click(object sender, RoutedEventArgs e)
        {
            LoadImages();
        }

        private void RemoveImage(string imagePath)
        {
            // 根据 imagePath 查找对应的 ImageItem
            var imageItemToRemove = ImageItems.FirstOrDefault(item => item.ImagePath == imagePath);

            if (imageItemToRemove != null)
            {
                // 从 ObservableCollection 中移除该项
                ImageItems.Remove(imageItemToRemove);

                // 从 imagePaths 列表中移除该项
                imagePaths.Remove(imagePath);
            }
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            // 获取所有选中的项
            var selectedItems = ImageListBox.SelectedItems.OfType<ImageItem>().ToList();

            if (selectedItems.Count > 0)
            {
                // 遍历选中的项并移除
                foreach (var item in selectedItems)
                {
                    RemoveImage(item.ImagePath);
                }
            }
            else
            {
                MessageBox.Show("请选择一个或多个图像项进行删除。");
            }
        }
    }
}
