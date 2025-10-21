using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage.Streams;

namespace WindowsHelloIRHelper
{
    partial class Form1
    {
        
        // 视频播放相关字段
        private MediaCapture? _mediaCapture;
        private bool _isVideoInitialized = false;
        private System.Windows.Forms.Timer? _frameTimer;
        
        /// <summary>
        /// 预览视频按钮点击事件处理
        /// </summary>
        private async void btnPreview_Click(object? sender, EventArgs e)
        {
            try
            {
                // 检查是否选择了摄像头
                if (cmbCameraList.SelectedIndex < 0 ||
                    cmbCameraList.SelectedIndex >= _availableCameras.Count)
                {
                    AppendStatus(Properties.Resources.Status_ErrorSelectCameraForPreview);
                    return;
                }

                var selectedCamera = _availableCameras[cmbCameraList.SelectedIndex];

                if (_isVideoInitialized)
                {
                    // 当前正在预览，停止预览
                    await StopVideoPreviewAsync();
                    btnPreview.Text = Properties.Resources.UI_Button_StartPreview;
                    AppendStatus(string.Format(Properties.Resources.Status_StopPreview, selectedCamera.Name));
                }
                else
                {
                    // 当前没有预览，开始预览
                    await StartVideoPreviewAsync(selectedCamera.DeviceId);
                    btnPreview.Text = Properties.Resources.UI_Button_StopPreview;
                    AppendStatus(string.Format(Properties.Resources.Status_StartPreview, selectedCamera.Name));
                }
            }
            catch (Exception ex)
            {
                AppendStatus(string.Format(Properties.Resources.Status_Exception, ex.Message));
                LogError(EventIds.CameraControlFailed, "视频预览操作异常", ex);
            }
        }

        /// <summary>
        /// 开始视频预览
        /// </summary>
        /// <param name="deviceId">摄像头设备ID</param>
        private async Task StartVideoPreviewAsync(string deviceId)
        {
            try
            {
                // 清理之前的资源
                await StopVideoPreviewAsync();
                
                // 创建 MediaCapture 实例
                _mediaCapture = new MediaCapture();
                
                // 配置初始化设置
                var settings = new MediaCaptureInitializationSettings
                {
                    VideoDeviceId = deviceId,
                    StreamingCaptureMode = StreamingCaptureMode.Video,
                    SharingMode = MediaCaptureSharingMode.ExclusiveControl
                };
                
                // 初始化 MediaCapture
                await _mediaCapture.InitializeAsync(settings);
                _isVideoInitialized = true;
                
                // 启动帧捕获定时器
                StartFrameTimer();
                
                AppendStatus(string.Format(Properties.Resources.Status_PreviewStarted, _availableCameras[cmbCameraList.SelectedIndex].Name));
            }
            catch (Exception ex)
            {
                AppendStatus(string.Format(Properties.Resources.Status_InitVideoPreviewFailed, ex.Message));
                await StopVideoPreviewAsync();
            }
        }
        
        /// <summary>
        /// 停止视频预览
        /// </summary>
        private async Task StopVideoPreviewAsync()
        {
            try
            {
                StopFrameTimer();
                
                if (_mediaCapture != null && _isVideoInitialized)
                {
                    _isVideoInitialized = false;
                    
                    // 清理图像资源
                    if (pictureBoxVideo != null && pictureBoxVideo.InvokeRequired)
                    {
                        pictureBoxVideo.Invoke(new Action(() =>
                        {
                            if (pictureBoxVideo.Image != null)
                            {
                                pictureBoxVideo.Image.Dispose();
                                pictureBoxVideo.Image = null!;
                            }
                        }));
                    }
                    else if (pictureBoxVideo != null)
                    {
                        if (pictureBoxVideo.Image != null)
                        {
                            pictureBoxVideo.Image.Dispose();
                            pictureBoxVideo.Image = null!;
                        }
                    }
                    
                    // 释放 MediaCapture 资源
                    await Task.Run(() => _mediaCapture.Dispose());
                    _mediaCapture = null;
                }
            }
            catch (Exception ex)
            {
                AppendStatus(string.Format(Properties.Resources.Status_StopVideoPreviewException, ex.Message));
            }
        }
        
        /// <summary>
        /// 启动帧捕获定时器
        /// </summary>
        private void StartFrameTimer()
        {
            _frameTimer = new System.Windows.Forms.Timer();
            _frameTimer.Interval = 150; // 每150ms捕获一帧
            _frameTimer.Tick += FrameTimer_Tick;
            _frameTimer.Start();
        }
        
        /// <summary>
        /// 停止帧捕获定时器
        /// </summary>
        private void StopFrameTimer()
        {
            if (_frameTimer != null)
            {
                _frameTimer.Stop();
                _frameTimer.Dispose();
                _frameTimer = null!;
            }
        }
        
        /// <summary>
        /// 帧捕获定时器事件处理
        /// </summary>
        private async void FrameTimer_Tick(object? sender, EventArgs e)
        {
            if (_isVideoInitialized && _mediaCapture != null)
            {
                await CaptureAndDisplayFrameAsync();
            }
        }
        
        /// <summary>
        /// 捕获并显示视频帧
        /// </summary>
        private async Task CaptureAndDisplayFrameAsync()
        {
            try
            {
                if (_mediaCapture == null)
                {
                    return;
                }
                
                // 捕获照片到内存流
                using (var stream = new InMemoryRandomAccessStream())
                {
                    // 使用 JPEG 格式捕获照片
                    var encodingProperties = ImageEncodingProperties.CreateJpeg();
                    await _mediaCapture.CapturePhotoToStreamAsync(encodingProperties, stream);
                    
                    // 重置流位置
                    stream.Seek(0);
                    
                    // 转换为 .NET Bitmap
                    using (var bitmap = new Bitmap(stream.AsStream()))
                    {
                        // 在 UI 线程中更新 PictureBox
                        if (pictureBoxVideo.InvokeRequired)
                        {
                            pictureBoxVideo.Invoke(new Action(() =>
                            {
                                UpdatePictureBoxImage(bitmap);
                            }));
                        }
                        else
                        {
                            UpdatePictureBoxImage(bitmap);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"捕获帧失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 更新PictureBox图像
        /// </summary>
        /// <param name="bitmap">新的位图</param>
        private void UpdatePictureBoxImage(Bitmap bitmap)
        {
            if (pictureBoxVideo != null)
            {
                // 释放之前的图像资源
                if (pictureBoxVideo.Image != null)
                {
                    pictureBoxVideo.Image.Dispose();
                }
                
                // 显示新捕获的图像（PictureBox 会自动缩放）
                pictureBoxVideo.Image = new Bitmap(bitmap);
            }
        }

    }
    
}