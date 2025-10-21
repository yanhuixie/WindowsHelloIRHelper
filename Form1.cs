using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using WindowsHelloIRHelper.Services;
using WindowsHelloIRHelper.Interfaces;

namespace WindowsHelloIRHelper
{
    public partial class Form1 : Form
    {
        private readonly IEventLogger _eventLogger;
        private readonly SimpleCameraHelper _cameraHelper;
        private readonly SimpleConfigHelper _configHelper;
        private List<CameraDevice> _availableCameras = [];
        private SimpleConfig? _currentConfig;
        private readonly System.Windows.Forms.Timer _statusUpdateTimer;
        private DateTime _lastEventLogCheck = DateTime.Now;

        public Form1(IEventLogger eventLogger, SimpleCameraHelper cameraHelper, SimpleConfigHelper configHelper)
        {
            InitializeComponent();
            _eventLogger = eventLogger;
            _cameraHelper = cameraHelper;
            _configHelper = configHelper;

            // 初始化事件日志记录器
            _ = InitializeEventLoggerAsync();

            // 初始化状态更新定时器
            _statusUpdateTimer = new System.Windows.Forms.Timer
            {
                Interval = 5000 // 每5秒更新一次状态
            };
            _statusUpdateTimer.Tick += StatusUpdateTimer_Tick;
            _statusUpdateTimer.Start();
        }

        /// <summary>
        /// 初始化事件日志记录器
        /// </summary>
        private async Task InitializeEventLoggerAsync()
        {
            try
            {
                await _eventLogger.InitializeAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"初始化事件日志记录器失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 异步记录信息日志
        /// </summary>
        /// <param name="eventId">事件ID</param>
        /// <param name="message">消息内容</param>
        private async Task LogInformationAsync(int eventId, string message)
        {
            try
            {
                await _eventLogger.LogInformationAsync(eventId, message);
            }
            catch (Exception ex)
            {
                // 如果日志记录失败，输出到调试控制台
                System.Diagnostics.Debug.WriteLine($"记录日志失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 异步记录错误日志
        /// </summary>
        /// <param name="eventId">事件ID</param>
        /// <param name="message">消息内容</param>
        /// <param name="exception">异常信息（可选）</param>
        private async Task LogErrorAsync(int eventId, string message, Exception? exception = null)
        {
            try
            {
                await _eventLogger.LogErrorAsync(eventId, message, exception);
            }
            catch (Exception ex)
            {
                // 如果日志记录失败，输出到调试控制台
                System.Diagnostics.Debug.WriteLine($"记录日志失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 同步记录信息日志（用于UI线程）
        /// </summary>
        /// <param name="eventId">事件ID</param>
        /// <param name="message">消息内容</param>
        private void LogInformation(int eventId, string message)
        {
            // 在UI线程中同步调用异步方法，避免死锁
            Task.Run(async () => await LogInformationAsync(eventId, message));
        }

        /// <summary>
        /// 同步记录错误日志（用于UI线程）
        /// </summary>
        /// <param name="eventId">事件ID</param>
        /// <param name="message">消息内容</param>
        /// <param name="exception">异常信息（可选）</param>
        private void LogError(int eventId, string message, Exception? exception = null)
        {
            // 在UI线程中同步调用异步方法，避免死锁
            Task.Run(async () => await LogErrorAsync(eventId, message, exception));
        }

        /// <summary>
        /// 将状态消息追加到状态文本框，自动添加时间戳并滚动到最新消息
        /// </summary>
        /// <param name="message">要显示的消息</param>
        private void AppendStatus(string message)
        {
            string timestampedMessage = $"[{DateTime.Now:HH:mm:ss}] {message}";

            if (txtStatus.Text.Length > 0)
            {
                txtStatus.AppendText(Environment.NewLine);
            }

            txtStatus.AppendText(timestampedMessage);

            // 自动滚动到最新消息
            txtStatus.SelectionStart = txtStatus.Text.Length;
            txtStatus.ScrollToCaret();
        }

        /// <summary>
        /// 窗体加载事件，执行初始化检查
        /// </summary>
        private void Form1_Load(object? sender, EventArgs e)
        {
            // 首次加载时应用本地化文本（避免被设计器覆盖）
            RefreshUILanguage();
            
            // 显示欢迎消息
            AppendStatus(Properties.Resources.UI_Form1_WelcomeMessage);
            AppendStatus(Properties.Resources.UI_Form1_FunctionDescription);
            AppendStatus(Properties.Resources.UI_Form1_Feature1);
            AppendStatus(Properties.Resources.UI_Form1_Feature2);
            AppendStatus("---");

            // 初始化界面
            _ = InitializeUIAsync();

            // 检查光线传感器可用性
            if (SimpleLightSensorHelper.IsAvailable())
            {
                AppendStatus(Properties.Resources.Status_LightSensorAvailable);
            }
            else
            {
                AppendStatus(Properties.Resources.Status_LightSensorNotDetected);
            }

            // 检查管理员权限
            if (_cameraHelper.IsRunningAsAdministrator())
            {
                AppendStatus(Properties.Resources.Status_RunningAsAdmin);
            }
            else
            {
                AppendStatus(Properties.Resources.Status_NotRunningAsAdmin);
                AppendStatus(Properties.Resources.Status_AdminRequiredHint);
                LogError(EventIds.InsufficientPermissions, "用户界面未以管理员权限运行");
            }

            AppendStatus("---");
            AppendStatus(Properties.Resources.Status_Ready);
        }

        /// <summary>
        /// 检测光亮度按钮点击事件处理
        /// </summary>
        private void btnDetectLight_Click(object? sender, EventArgs e)
        {
            try
            {
                // 禁用按钮防止重复操作
                btnDetectLight.Enabled = false;
                AppendStatus(Properties.Resources.Status_DetectingLight);

                // 调用简化的光线传感器工具获取光亮度
                var lux = SimpleLightSensorHelper.GetCurrentLux();

                // 显示结果
                if (lux.HasValue)
                {
                    lblDetectLight.Text = $"{lux.Value:F2} lux";
                    AppendStatus(string.Format(Properties.Resources.Status_LightValue, lux.Value));
                    LogInformation(EventIds.LightSensorEnabled, $"用户检测光亮度: {lux.Value:F2} lux");
                }
                else
                {
                    AppendStatus(Properties.Resources.Status_ErrorCannotGetLight);
                }
            }
            catch (Exception ex)
            {
                AppendStatus(string.Format(Properties.Resources.Status_Exception, ex.Message));
                LogError(EventIds.SensorUnavailable, "检测光亮度异常", ex);
            }
            finally
            {
                // 重新启用按钮
                btnDetectLight.Enabled = true;
            }
        }

        /// <summary>
        /// 启用/禁用摄像头按钮点击事件处理
        /// </summary>
        private async void btnToggleCamera_Click(object? sender, EventArgs e)
        {
            try
            {
                // 检查是否选择了摄像头
                if (cmbCameraList.SelectedIndex < 0 ||
                    cmbCameraList.SelectedIndex >= _availableCameras.Count)
                {
                    AppendStatus(Properties.Resources.Status_ErrorSelectCamera);
                    return;
                }

                // 检查管理员权限
                if (!_cameraHelper.IsRunningAsAdministrator())
                {
                    AppendStatus(Properties.Resources.Status_ErrorAdminRequired);
                    AppendStatus(Properties.Resources.Status_RunAsAdmin);
                    return;
                }

                // 禁用按钮防止重复操作
                btnToggleCamera.Enabled = false;
                var selectedCamera = _availableCameras[cmbCameraList.SelectedIndex];

                AppendStatus(string.Format(Properties.Resources.Status_TogglingCamera, selectedCamera.Name));

                // 简化版本：直接执行相反操作（假设当前状态与按钮文本相反）
                bool enable = btnToggleCamera.Text == Properties.Resources.UI_Button_EnableCamera;
                bool success;

                if (enable)
                {
                    success = await _cameraHelper.EnableCameraAsync(selectedCamera.DeviceId);
                }
                else
                {
                    success = await _cameraHelper.DisableCameraAsync(selectedCamera.DeviceId);
                    // 如果禁用摄像头，同时停止视频预览
                    if (success && _isVideoInitialized)
                    {
                        await StopVideoPreviewAsync();
                        btnPreview.Text = Properties.Resources.UI_Button_StartPreview;
                        AppendStatus(Properties.Resources.Status_CameraDisabledPreviewStopped);
                    }
                }

                // 显示操作结果
                if (success)
                {
                    string stateText = enable ? Properties.Resources.Status_CameraEnabled : Properties.Resources.Status_CameraDisabled;
                    AppendStatus(string.Format(Properties.Resources.Status_CameraStateChanged, selectedCamera.Name, stateText));

                    // 更新状态显示
                    await UpdateCameraStatusAsync();

                    // 记录用户操作
                    LogInformation(enable ? EventIds.CameraEnabled : EventIds.CameraDisabled,
                        $"用户通过界面{stateText}摄像头: {selectedCamera.Name}");
                }
                else
                {
                    AppendStatus(Properties.Resources.Status_OperationFailed);
                }
            }
            catch (Exception ex)
            {
                AppendStatus(string.Format(Properties.Resources.Status_Exception, ex.Message));
                LogError(EventIds.CameraControlFailed, "摄像头控制操作异常", ex);
            }
            finally
            {
                // 重新启用按钮
                btnToggleCamera.Enabled = true;
            }
        }

        /// <summary>
        /// 异步初始化用户界面
        /// </summary>
        private async Task InitializeUIAsync()
        {
            try
            {
                // 加载配置
                _currentConfig = _configHelper.LoadConfig();

                // 初始化界面控件
                await LoadCameraListAsync();
                UpdateConfigUI();
                UpdateServiceStatusUI();
            }
            catch (Exception ex)
            {
                AppendStatus(string.Format(Properties.Resources.Status_InitUIFailed, ex.Message));
                LogError(EventIds.ServiceException, "初始化界面失败", ex);
            }
        }

        /// <summary>
        /// 异步加载摄像头设备列表
        /// </summary>
        private async Task LoadCameraListAsync()
        {
            try
            {
                AppendStatus(Properties.Resources.Status_EnumeratingCameras);

                // 使用简化的摄像头工具枚举摄像头
                _availableCameras = await _cameraHelper.GetAvailableCamerasAsync();

                AppendStatus(string.Format(Properties.Resources.Status_EnumerationComplete, _availableCameras.Count));

                // 更新下拉列表
                cmbCameraList.Items.Clear();

                if (_availableCameras.Count == 0)
                {
                    cmbCameraList.Items.Add(Properties.Resources.UI_CameraList_NoCameraFound);
                    cmbCameraList.SelectedIndex = 0;
                    cmbCameraList.Enabled = false;
                    btnToggleCamera.Enabled = false;
                    lblCameraStatus.Text = Properties.Resources.Status_CameraStatusNoDevice;
                    AppendStatus(Properties.Resources.Status_NoCamerasFound);
                    AppendStatus(Properties.Resources.Status_PossibleReasons);
                    AppendStatus(Properties.Resources.Status_Reason1);
                    AppendStatus(Properties.Resources.Status_Reason2);
                }
                else
                {
                    AppendStatus(Properties.Resources.Status_DeviceList);
                    foreach (var camera in _availableCameras)
                    {
                        cmbCameraList.Items.Add(camera.Name);

                        string stateText = camera.IsEnabled switch
                        {
                            true => Properties.Resources.Status_CameraEnabled,
                            false => Properties.Resources.Status_CameraDisabled
                        };
                        AppendStatus(string.Format(Properties.Resources.Status_DeviceItem, camera.Name, stateText));
                    }

                    // 选择配置中指定的摄像头或第一个摄像头
                    int selectedIndex = 0;
                    if (_currentConfig != null && !string.IsNullOrEmpty(_currentConfig.TargetCameraDeviceId))
                    {
                        var configuredCamera = _availableCameras.FindIndex(c =>
                            c.DeviceId == _currentConfig.TargetCameraDeviceId);
                        if (configuredCamera >= 0)
                        {
                            selectedIndex = configuredCamera;
                            AppendStatus(string.Format(Properties.Resources.Status_SelectedConfiguredCamera, _availableCameras[selectedIndex].Name));
                        }
                    }
                    else
                    {
                        // 优先选择第一个摄像头
                        if (_availableCameras.Count >= 0)
                        {
                            selectedIndex = 0;
                            AppendStatus(string.Format(Properties.Resources.Status_SelectedIntegratedCamera, _availableCameras[selectedIndex].Name));
                        }
                    }

                    cmbCameraList.SelectedIndex = selectedIndex;
                    cmbCameraList.Enabled = true;
                    btnToggleCamera.Enabled = true;

                    AppendStatus(Properties.Resources.Status_CameraListLoaded);
                    await UpdateCameraStatusAsync();
                }
            }
            catch (Exception ex)
            {
                AppendStatus(string.Format(Properties.Resources.Status_LoadCameraListFailed, ex.Message));
                AppendStatus(string.Format(Properties.Resources.Status_ExceptionType, ex.GetType().Name));
                if (ex.InnerException != null)
                {
                    AppendStatus(string.Format(Properties.Resources.Status_InnerException, ex.InnerException.Message));
                }
                AppendStatus(string.Format(Properties.Resources.Status_StackTrace, ex.StackTrace));

                LogError(EventIds.MonitorStartFailed, "加载摄像头列表失败", ex);

                cmbCameraList.Items.Clear();
                cmbCameraList.Items.Add(Properties.Resources.UI_CameraList_LoadFailed);
                cmbCameraList.SelectedIndex = 0;
                cmbCameraList.Enabled = false;
                btnToggleCamera.Enabled = false;
                lblCameraStatus.Text = Properties.Resources.Status_CameraStatusLoadFailed;
            }
        }

        /// <summary>
        /// 异步更新摄像头状态显示
        /// </summary>
        private async Task UpdateCameraStatusAsync()
        {
            try
            {
                if (cmbCameraList.SelectedIndex < 0 ||
                    cmbCameraList.SelectedIndex >= _availableCameras.Count)
                {
                    lblCameraStatus.Text = Properties.Resources.Status_CameraStatusNotSelected;
                    return;
                }

                var selectedCamera = _availableCameras[cmbCameraList.SelectedIndex];

                // 实时查询摄像头状态，而不是使用缓存的状态
                var currentState = await _cameraHelper.GetDeviceEnabledAsync(selectedCamera.DeviceId);
                string stateText = currentState switch
                {
                    true => Properties.Resources.Status_CameraEnabled,
                    false => Properties.Resources.Status_CameraDisabled
                };

                lblCameraStatus.Text = string.Format(Properties.Resources.Status_CameraStatusLabel, stateText);

                // 更新按钮文本
                btnToggleCamera.Text = currentState ? Properties.Resources.UI_Button_DisableCamera : Properties.Resources.UI_Button_EnableCamera;
            }
            catch (Exception ex)
            {
                lblCameraStatus.Text = Properties.Resources.Status_CameraStatusQueryFailed;
                AppendStatus(string.Format(Properties.Resources.Status_QueryCameraStatusFailed, ex.Message));
            }
        }

        /// <summary>
        /// 更新配置界面显示
        /// </summary>
        private void UpdateConfigUI()
        {
            try
            {
                // 更新环境光传感器启用状态
                if (_currentConfig != null)
                {
                    chkEnableLightSensor.Checked = _currentConfig.EnableLightSensor;

                    // 更新参数配置 - 简化版本只有光亮度阈值
                    // 确保值在控件的有效范围内
                    decimal thresholdValue = (decimal)_currentConfig.LightThreshold;
                    if (thresholdValue < numThreshold.Minimum)
                        thresholdValue = numThreshold.Minimum;
                    else if (thresholdValue > numThreshold.Maximum)
                        thresholdValue = numThreshold.Maximum;

                    numThreshold.Value = thresholdValue;

                    // 根据环境光传感器启用状态启用/禁用相关控件
                    numThreshold.Enabled = _currentConfig.EnableLightSensor;
                    lblThreshold.Enabled = _currentConfig.EnableLightSensor;
                }

            }
            catch (Exception ex)
            {
                AppendStatus(string.Format(Properties.Resources.Status_UpdateConfigUIFailed, ex.Message));
            }
        }

        /// <summary>
        /// 更新服务状态界面显示
        /// </summary>
        private void UpdateServiceStatusUI()
        {
            try
            {
                var serviceStatus = ServiceManager.GetDetailedServiceStatus();

                string statusText = Properties.Resources.Status_ServiceNotInstalled;
                if (serviceStatus.IsInstalled)
                {
                    statusText = serviceStatus.Status switch
                    {
                        ServiceControllerStatus.Running => Properties.Resources.Status_ServiceRunning,
                        ServiceControllerStatus.Stopped => Properties.Resources.Status_ServiceStopped_State,
                        ServiceControllerStatus.StartPending => Properties.Resources.Status_ServiceStartPending,
                        ServiceControllerStatus.StopPending => Properties.Resources.Status_ServiceStopPending,
                        ServiceControllerStatus.Paused => Properties.Resources.Status_ServicePaused,
                        _ => Properties.Resources.Status_ServiceUnknown
                    };
                }

                lblServiceStatus.Text = string.Format(Properties.Resources.Status_ServiceStatusLabel, statusText);

                // 更新按钮状态
                btnInstallService.Enabled = !serviceStatus.IsInstalled && serviceStatus.HasAdminRights;
                btnUninstallService.Enabled = serviceStatus.IsInstalled && serviceStatus.HasAdminRights;
                btnStartService.Enabled = serviceStatus.CanStart && serviceStatus.HasAdminRights;
                btnStopService.Enabled = serviceStatus.CanStop && serviceStatus.HasAdminRights;

                if (!serviceStatus.HasAdminRights)
                {
                    lblServiceStatus.Text += Properties.Resources.Status_ServiceAdminRequired;
                }
            }
            catch (Exception ex)
            {
                lblServiceStatus.Text = Properties.Resources.Status_ServiceStatusQueryFailed;
                AppendStatus(string.Format(Properties.Resources.Status_QueryServiceStatusFailed, ex.Message));
            }
        }

        /// <summary>
        /// 摄像头选择变化事件处理
        /// </summary>
        private async void cmbCameraList_SelectedIndexChanged(object? sender, EventArgs e)
        {
            await UpdateCameraStatusAsync();
            
            // 当切换摄像头时，如果正在预览，则停止预览
            if (_isVideoInitialized)
            {
                await StopVideoPreviewAsync();
                btnPreview.Text = Properties.Resources.UI_Button_StartPreview;
            }
        }

        /// <summary>
        /// 刷新摄像头列表按钮点击事件处理
        /// </summary>
        private async void btnRefreshCameras_Click(object? sender, EventArgs e)
        {
            try
            {
                btnRefreshCameras.Enabled = false;
                AppendStatus(Properties.Resources.Status_RefreshingCameraList);

                await LoadCameraListAsync();

                AppendStatus(Properties.Resources.Status_CameraListRefreshed);
            }
            catch (Exception ex)
            {
                AppendStatus(string.Format(Properties.Resources.Status_Exception, ex.Message));
                LogError(EventIds.MonitorStartFailed, "刷新摄像头列表失败", ex);
            }
            finally
            {
                btnRefreshCameras.Enabled = true;
            }
        }

        /// <summary>
        /// 环境光传感器启用状态变化事件处理
        /// </summary>
        private void chkEnableLightSensor_CheckedChanged(object? sender, EventArgs e)
        {
            // 根据环境光传感器启用状态启用/禁用相关控件
            bool enabled = chkEnableLightSensor.Checked;
            numThreshold.Enabled = enabled;
            lblThreshold.Enabled = enabled;

            string state = enabled ? Properties.Resources.Status_Enabled : Properties.Resources.Status_Disabled;
            AppendStatus(string.Format(Properties.Resources.Status_LightSensorStateChanged, state));
        }

        /// <summary>
        /// 清空状态日志按钮点击事件处理
        /// </summary>
        private void btnClearStatus_Click(object? sender, EventArgs e)
        {
            txtStatus.Clear();
            AppendStatus(Properties.Resources.Status_LogCleared);
        }

        /// <summary>
        /// 保存配置按钮点击事件处理
        /// </summary>
        private async void btnSaveConfig_Click(object? sender, EventArgs e)
        {
            try
            {
                btnSaveConfig.Enabled = false;
                AppendStatus(Properties.Resources.Status_SavingConfig);

                // 创建新的简化配置对象
                var newConfig = new SimpleConfig
                {
                    // 更新环境光传感器启用状态
                    EnableLightSensor = chkEnableLightSensor.Checked,

                    // 更新环境光传感器配置 - 简化版本只有光亮度阈值
                    LightThreshold = (double)numThreshold.Value,

                    // 更新目标摄像头设备ID
                    TargetCameraDeviceId = (cmbCameraList.SelectedIndex >= 0 &&
                        cmbCameraList.SelectedIndex < _availableCameras.Count)
                        ? _availableCameras[cmbCameraList.SelectedIndex].DeviceId
                        : null
                };

                // 使用 SimpleConfigHelper 保存配置
                await _configHelper.SaveConfigAsync(newConfig);
                _currentConfig = newConfig;

                AppendStatus(Properties.Resources.Status_ConfigSaved);
                LogInformation(EventIds.ConfigReloaded, Properties.Resources.Log_Config_SavedByUser);
            }
            catch (Exception ex)
            {
                AppendStatus(string.Format(Properties.Resources.Status_Exception, ex.Message));
                LogError(EventIds.ConfigLoadFailed, Properties.Resources.Log_Config_SaveFailed, ex);
            }
            finally
            {
                btnSaveConfig.Enabled = true;
            }
        }

        /// <summary>
        /// 重置配置按钮点击事件处理
        /// </summary>
        private async void btnResetConfig_Click(object? sender, EventArgs e)
        {
            try
            {
                var result = MessageBox.Show(
                    Properties.Resources.Message_ConfirmResetConfig,
                    Properties.Resources.Message_ConfirmResetConfigTitle,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result != DialogResult.Yes)
                {
                    return;
                }

                btnResetConfig.Enabled = false;
                AppendStatus(Properties.Resources.Status_ResettingConfig);

                // 创建默认简化配置
                var defaultConfig = new SimpleConfig
                {
                    TargetCameraDeviceId = (cmbCameraList.SelectedIndex >= 0 &&
                        cmbCameraList.SelectedIndex < _availableCameras.Count)
                        ? _availableCameras[cmbCameraList.SelectedIndex].DeviceId
                        : null,
                    EnableLightSensor = false,
                    LightThreshold = 10.0
                };

                // 使用 SimpleConfigHelper 保存默认配置
                await _configHelper.SaveConfigAsync(defaultConfig);
                _currentConfig = defaultConfig;

                // 更新界面显示
                UpdateConfigUI();

                AppendStatus(Properties.Resources.Status_ConfigReset);
                LogInformation(EventIds.ConfigReloaded, Properties.Resources.Log_Config_ResetByUser);
            }
            catch (Exception ex)
            {
                AppendStatus(string.Format(Properties.Resources.Status_Exception, ex.Message));
                LogError(EventIds.ConfigLoadFailed, Properties.Resources.Log_Config_ResetFailed, ex);
            }
            finally
            {
                btnResetConfig.Enabled = true;
            }
        }

        /// <summary>
        /// 导出配置到文件
        /// </summary>
        private async void ExportConfig()
        {
            try
            {
                using var saveDialog = new SaveFileDialog
                {
                    Title = Properties.Resources.Dialog_ExportConfig_Title,
                    Filter = Properties.Resources.Dialog_ExportConfig_Filter,
                    DefaultExt = "json",
                    FileName = $"AutoCameraControl_Config_{DateTime.Now:yyyyMMdd_HHmmss}.json"
                };

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    AppendStatus(Properties.Resources.Status_ExportingConfig);

                    var configJson = System.Text.Json.JsonSerializer.Serialize(_currentConfig ?? new SimpleConfig(), new System.Text.Json.JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                    });

                    await System.IO.File.WriteAllTextAsync(saveDialog.FileName, configJson);

                    AppendStatus(string.Format(Properties.Resources.Status_ConfigExported, saveDialog.FileName));
                    LogInformation(EventIds.ConfigReloaded, string.Format(Properties.Resources.Log_Config_ExportedByUser, saveDialog.FileName));
                }
            }
            catch (Exception ex)
            {
                AppendStatus(string.Format(Properties.Resources.Status_ExportConfigFailed, ex.Message));
                LogError(EventIds.ConfigLoadFailed, Properties.Resources.Log_Config_ExportFailed, ex);
            }
        }

        /// <summary>
        /// 从文件导入配置
        /// </summary>
        private async void ImportConfig()
        {
            try
            {
                using var openDialog = new OpenFileDialog
                {
                    Title = Properties.Resources.Dialog_ImportConfig_Title,
                    Filter = Properties.Resources.Dialog_ImportConfig_Filter,
                    DefaultExt = "json"
                };

                if (openDialog.ShowDialog() == DialogResult.OK)
                {
                    AppendStatus(Properties.Resources.Status_ImportingConfig);

                    var configJson = await System.IO.File.ReadAllTextAsync(openDialog.FileName);
                    var importedConfig = System.Text.Json.JsonSerializer.Deserialize<SimpleConfig>(configJson, new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (importedConfig == null)
                    {
                        AppendStatus(Properties.Resources.Status_ImportFailed_InvalidFormat);
                        return;
                    }

                    // 使用 SimpleConfigHelper 保存导入的配置
                    await _configHelper.SaveConfigAsync(importedConfig);
                    _currentConfig = importedConfig;

                    // 更新界面显示
                    UpdateConfigUI();

                    AppendStatus(string.Format(Properties.Resources.Status_ConfigImported, openDialog.FileName));
                    LogInformation(EventIds.ConfigReloaded, string.Format(Properties.Resources.Log_Config_ImportedByUser, openDialog.FileName));
                }
            }
            catch (Exception ex)
            {
                AppendStatus(string.Format(Properties.Resources.Status_ImportConfigFailed, ex.Message));
                LogError(EventIds.ConfigLoadFailed, Properties.Resources.Log_Config_ImportFailed, ex);
            }
        }

        /// <summary>
        /// 注册服务按钮点击事件处理
        /// </summary>
        private async void btnInstallService_Click(object? sender, EventArgs e)
        {
            try
            {
                btnInstallService.Enabled = false;
                AppendStatus(Properties.Resources.Status_InstallingService);

                // 使用 AdvancedServiceInstaller 进行服务安装
                var exePath = Application.ExecutablePath;
                var installResult = await AdvancedServiceInstaller.InstallServiceWithScAsync(exePath);

                if (installResult.Success)
                {
                    AppendStatus(Properties.Resources.Status_ServiceInstalled);
                    AppendStatus(installResult.Message);
                    LogInformation(EventIds.ServiceStarted, "用户通过界面注册Windows服务");
                }
                else
                {
                    AppendStatus(string.Format(Properties.Resources.Status_ServiceInstallFailed, installResult.Message));
                    if (!string.IsNullOrEmpty(installResult.ErrorDetails))
                    {
                        AppendStatus(string.Format(Properties.Resources.Status_Details, installResult.ErrorDetails));
                    }
                }

                // 更新服务状态显示
                UpdateServiceStatusUI();
            }
            catch (Exception ex)
            {
                AppendStatus(string.Format(Properties.Resources.Status_Exception, ex.Message));
                LogError(EventIds.ServiceException, "注册服务异常", ex);
            }
            finally
            {
                btnInstallService.Enabled = true;
            }
        }

        /// <summary>
        /// 卸载服务按钮点击事件处理
        /// </summary>
        private async void btnUninstallService_Click(object? sender, EventArgs e)
        {
            try
            {
                var result = MessageBox.Show(
                    Properties.Resources.Message_ConfirmUninstallService,
                    Properties.Resources.Message_ConfirmUninstallServiceTitle,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result != DialogResult.Yes)
                {
                    return;
                }

                btnUninstallService.Enabled = false;
                AppendStatus(Properties.Resources.Status_UninstallingService);

                // 使用 AdvancedServiceInstaller 进行服务卸载
                var uninstallResult = await AdvancedServiceInstaller.UninstallServiceWithScAsync();

                if (uninstallResult.Success)
                {
                    AppendStatus(Properties.Resources.Status_ServiceUninstalled);
                    AppendStatus(uninstallResult.Message);
                    LogInformation(EventIds.ServiceStopped, "用户通过界面卸载Windows服务");
                }
                else
                {
                    AppendStatus(string.Format(Properties.Resources.Status_ServiceUninstallFailed, uninstallResult.Message));
                    if (!string.IsNullOrEmpty(uninstallResult.ErrorDetails))
                    {
                        AppendStatus(string.Format(Properties.Resources.Status_Details, uninstallResult.ErrorDetails));
                    }
                }

                // 更新服务状态显示
                UpdateServiceStatusUI();
            }
            catch (Exception ex)
            {
                AppendStatus(string.Format(Properties.Resources.Status_Exception, ex.Message));
                LogError(EventIds.ServiceException, "卸载服务异常", ex);
            }
            finally
            {
                btnUninstallService.Enabled = true;
            }
        }

        /// <summary>
        /// 启动服务按钮点击事件处理
        /// </summary>
        private void btnStartService_Click(object? sender, EventArgs e)
        {
            try
            {
                // 创建新的简化配置对象
                var newConfig = new SimpleConfig
                {
                    // 更新环境光传感器启用状态
                    EnableLightSensor = chkEnableLightSensor.Checked,

                    // 更新环境光传感器配置 - 简化版本只有光亮度阈值
                    LightThreshold = (double)numThreshold.Value,

                    // 更新目标摄像头设备ID
                    TargetCameraDeviceId = (cmbCameraList.SelectedIndex >= 0 &&
                        cmbCameraList.SelectedIndex < _availableCameras.Count)
                        ? _availableCameras[cmbCameraList.SelectedIndex].DeviceId
                        : null
                };

                // 使用 SimpleConfigHelper 保存配置
                _ = _configHelper.SaveConfigAsync(newConfig);
                _currentConfig = newConfig;

                AppendStatus(Properties.Resources.Status_ConfigSaved);

                btnStartService.Enabled = false;
                AppendStatus(Properties.Resources.Status_StartingService);

                var startInfo = new ProcessStartInfo
                {
                    FileName = "sc.exe",
                    Arguments = $"start \"{SimpleAutoCameraControlService.ServiceName}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                process?.WaitForExit();

                if (process?.ExitCode == 0)
                {
                    AppendStatus(Properties.Resources.Status_ServiceStarted);
                    LogInformation(EventIds.ServiceStarted, "用户通过界面启动Windows服务");
                }
                else
                {
                    AppendStatus(Properties.Resources.Status_ServiceStartFailed);
                }

                // 更新服务状态显示
                UpdateServiceStatusUI();
            }
            catch (Exception ex)
            {
                AppendStatus(string.Format(Properties.Resources.Status_Exception, ex.Message));
                LogError(EventIds.ServiceException, "启动服务异常", ex);
            }
            finally
            {
                btnStartService.Enabled = true;
            }
        }

        /// <summary>
        /// 停止服务按钮点击事件处理
        /// </summary>
        private void btnStopService_Click(object? sender, EventArgs e)
        {
            try
            {
                btnStopService.Enabled = false;
                AppendStatus(Properties.Resources.Status_StoppingService);

                var startInfo = new ProcessStartInfo
                {
                    FileName = "sc.exe",
                    Arguments = $"stop \"{SimpleAutoCameraControlService.ServiceName}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                process?.WaitForExit();

                if (process?.ExitCode == 0)
                {
                    AppendStatus(Properties.Resources.Status_ServiceStopped);
                    LogInformation(EventIds.ServiceStopped, "用户通过界面停止Windows服务");
                }
                else
                {
                    AppendStatus(Properties.Resources.Status_ServiceStopFailed);
                }

                // 更新服务状态显示
                UpdateServiceStatusUI();
            }
            catch (Exception ex)
            {
                AppendStatus(string.Format(Properties.Resources.Status_Exception, ex.Message));
                LogError(EventIds.ServiceException, "停止服务异常", ex);
            }
            finally
            {
                btnStopService.Enabled = true;
            }
        }

        /// <summary>
        /// 定期更新服务状态显示
        /// </summary>
        private async void RefreshServiceStatus()
        {
            try
            {
                UpdateServiceStatusUI();

                // 如果服务正在启动或停止中，继续监控状态变化
                var serviceStatus = ServiceManager.GetDetailedServiceStatus();
                if (serviceStatus.IsInstalled && serviceStatus.Status.HasValue)
                {
                    var status = serviceStatus.Status.Value;
                    if (status == ServiceControllerStatus.StartPending ||
                        status == ServiceControllerStatus.StopPending)
                    {
                        // 延迟后再次检查状态
                        await Task.Delay(2000);
                        UpdateServiceStatusUI();
                    }
                }
            }
            catch (Exception ex)
            {
                AppendStatus(string.Format(Properties.Resources.Status_RefreshServiceStatusFailed, ex.Message));
            }
        }

        /// <summary>
        /// 状态更新定时器事件处理
        /// </summary>
        private async void StatusUpdateTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                // 更新服务状态（简化版本）
                UpdateServiceStatusUI();

                // 更新摄像头状态（如果有选中的摄像头）
                if (cmbCameraList.SelectedIndex >= 0 &&
                    cmbCameraList.SelectedIndex < _availableCameras.Count)
                {
                    await UpdateCameraStatusAsync();
                }

                // 检查新的事件日志条目
                await CheckForNewEventLogEntriesAsync();
            }
            catch (Exception ex)
            {
                // 静默处理定时器异常，避免干扰用户操作
                LogError(EventIds.ServiceException, "状态更新定时器异常", ex);
            }
        }

        /// <summary>
        /// 检查新的事件日志条目
        /// </summary>
        private async Task CheckForNewEventLogEntriesAsync()
        {
            try
            {
                // 读取最近的事件日志条目
                var recentEvents = await ReadRecentEventLogEntriesAsync(_lastEventLogCheck);

                if (recentEvents.Count > 0)
                {
                    // 将服务日志记录到日志文件，而不是显示在UI上
                    foreach (var eventEntry in recentEvents)
                    {
                        await LogInformationAsync(EventIds.SessionSwitchDetected, $"[服务] {eventEntry}");
                    }

                    _lastEventLogCheck = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                // 静默处理事件日志读取异常
                LogError(EventIds.ServiceException, "读取事件日志异常", ex);
            }
        }

        /// <summary>
        /// 读取最近的事件日志条目
        /// </summary>
        /// <param name="since">起始时间</param>
        /// <returns>事件日志条目列表</returns>
        private async Task<List<string>> ReadRecentEventLogEntriesAsync(DateTime since)
        {
            return await Task.Run(() =>
            {
                var events = new List<string>();

                try
                {
                    using var eventLog = new System.Diagnostics.EventLog("Application");

                    // 查找与我们的应用程序相关的事件
                    var entries = eventLog.Entries.Cast<System.Diagnostics.EventLogEntry>()
                        .Where(entry => entry.TimeGenerated > since)
                        .Where(entry => entry.Source.Contains("AutoCameraControl") ||
                                       entry.Source.Contains("WindowsHelloIRHelper"))
                        .OrderBy(entry => entry.TimeGenerated)
                        .Take(10); // 限制最多10条

                    foreach (var entry in entries)
                    {
                        string levelText = entry.EntryType switch
                        {
                            System.Diagnostics.EventLogEntryType.Information => Properties.Resources.EventLog_Level_Information,
                            System.Diagnostics.EventLogEntryType.Warning => Properties.Resources.EventLog_Level_Warning,
                            System.Diagnostics.EventLogEntryType.Error => Properties.Resources.EventLog_Level_Error,
                            _ => entry.EntryType.ToString()
                        };

                        events.Add($"{entry.TimeGenerated:HH:mm:ss} [{levelText}] {entry.Message}");
                    }
                }
                catch (Exception ex)
                {
                    LogError(EventIds.ServiceException, "读取Windows事件日志失败", ex);
                }

                return events;
            });
        }

        /// <summary>
        /// 显示操作历史记录
        /// </summary>
        private async void ShowOperationHistory()
        {
            try
            {
                AppendStatus(Properties.Resources.Status_OperationHistoryHeader);

                // 读取最近24小时的事件日志
                var yesterday = DateTime.Now.AddDays(-1);
                var historyEvents = await ReadRecentEventLogEntriesAsync(yesterday);

                if (historyEvents.Count == 0)
                {
                    AppendStatus(Properties.Resources.Status_NoRecentOperations);
                }
                else
                {
                    AppendStatus(string.Format(Properties.Resources.Status_RecentOperations, historyEvents.Count));
                    foreach (var eventEntry in historyEvents)
                    {
                        AppendStatus($"  {eventEntry}");
                    }
                }

                AppendStatus(Properties.Resources.Status_OperationHistoryFooter);
            }
            catch (Exception ex)
            {
                AppendStatus(string.Format(Properties.Resources.Status_ReadHistoryFailed, ex.Message));
            }
        }

        /// <summary>
        /// 显示实时系统状态
        /// </summary>
        private void ShowSystemStatus()
        {
            try
            {
                AppendStatus(Properties.Resources.Status_SystemStatusHeader);

                // 服务状态 - 简化版本
                AppendStatus(Properties.Resources.Status_ServiceStatusSimplified);

                // 权限状态
                string adminRights = _cameraHelper.IsRunningAsAdministrator() ? 
                    Properties.Resources.Status_Yes : Properties.Resources.Status_No;
                AppendStatus(string.Format(Properties.Resources.Status_AdminRights, adminRights));

                // 摄像头状态
                AppendStatus(string.Format(Properties.Resources.Status_AvailableCameras, _availableCameras.Count));
                if (_availableCameras.Count > 0)
                {
                    if (cmbCameraList.SelectedIndex >= 0 &&
                        cmbCameraList.SelectedIndex < _availableCameras.Count)
                    {
                        var selectedCamera = _availableCameras[cmbCameraList.SelectedIndex];
                        string stateText = selectedCamera.IsEnabled switch
                        {
                            true => Properties.Resources.Status_CameraEnabled,
                            false => Properties.Resources.Status_CameraDisabled
                        };
                        AppendStatus(string.Format(Properties.Resources.Status_CurrentSelection, selectedCamera.Name, stateText));
                    }
                }

                // 传感器状态
                bool lightSensorAvailable = SimpleLightSensorHelper.IsAvailable();
                string sensorStatus = lightSensorAvailable ? 
                    Properties.Resources.Status_Available : Properties.Resources.Status_NotAvailable;
                AppendStatus(string.Format(Properties.Resources.Status_LightSensor, sensorStatus));

                // 配置状态
                AppendStatus(Properties.Resources.Status_WorkingMode);
                if (_currentConfig != null)
                {
                    string lightSensorState = _currentConfig.EnableLightSensor ? 
                        Properties.Resources.Status_Enabled : Properties.Resources.Status_Disabled;
                    AppendStatus(string.Format(Properties.Resources.Status_AmbientLightSensor, lightSensorState));
                    if (_currentConfig.EnableLightSensor)
                    {
                        AppendStatus(string.Format(Properties.Resources.Status_LightThreshold, _currentConfig.LightThreshold));
                    }
                }

                AppendStatus(Properties.Resources.Status_SystemStatusFooter);
            }
            catch (Exception ex)
            {
                AppendStatus(string.Format(Properties.Resources.Status_GenerateStatusReportFailed, ex.Message));
            }
        }

        /// <summary>
        /// 显示操作历史按钮点击事件处理
        /// </summary>
        private void btnShowHistory_Click(object? sender, EventArgs e)
        {
            ShowOperationHistory();
        }

        /// <summary>
        /// 显示系统状态按钮点击事件处理
        /// </summary>
        private void btnShowSystemStatus_Click(object? sender, EventArgs e)
        {
            ShowSystemStatus();
        }

        /// <summary>
        /// 导出配置按钮点击事件处理
        /// </summary>
        private void btnExportConfig_Click(object? sender, EventArgs e)
        {
            ExportConfig();
        }

        /// <summary>
        /// 导入配置按钮点击事件处理
        /// </summary>
        private void btnImportConfig_Click(object? sender, EventArgs e)
        {
            ImportConfig();
        }

        /// <summary>
        /// 测试功能按钮点击事件处理
        /// </summary>
        private async void btnTest_Click(object? sender, EventArgs e)
        {
            try
            {
                btnTest.Enabled = false;
                AppendStatus(Properties.Resources.Status_TestingSCCommand);
                
                // 调用测试方法
                var testResult = await AdvancedServiceInstaller.TestScDescriptionSyntaxAsync();
                
                AppendStatus(Properties.Resources.Status_TestResult);
                AppendStatus(testResult);
                
                // 验证当前服务描述
                AppendStatus(Properties.Resources.Status_VerifyingServiceDescription);
                var currentDescription = await GetCurrentServiceDescriptionAsync();
                AppendStatus(string.Format(Properties.Resources.Status_CurrentDescription, currentDescription));
            }
            catch (Exception ex)
            {
                AppendStatus(string.Format(Properties.Resources.Status_Exception, ex.Message));
                LogError(EventIds.ServiceException, "测试SC命令语法异常", ex);
            }
            finally
            {
                btnTest.Enabled = true;
            }
        }
        
        /// <summary>
        /// 获取当前服务描述
        /// </summary>
        /// <returns>服务描述文本</returns>
        private async Task<string> GetCurrentServiceDescriptionAsync()
        {
            try
            {
                return await Task.Run(() =>
                {
                    using var searcher = new System.Management.ManagementObjectSearcher(
                        $"SELECT Description FROM Win32_Service WHERE Name='{SimpleAutoCameraControlService.ServiceName}'");
                    
                    foreach (System.Management.ManagementObject service in searcher.Get())
                    {
                        return service["Description"]?.ToString() ?? Properties.Resources.Status_NoDescriptionSet;
                    }
                    
                    return Properties.Resources.Status_ServiceNotInstalledOrQueryFailed;
                });
            }
            catch (Exception ex)
            {
                return string.Format(Properties.Resources.Status_QueryDescriptionFailed, ex.Message);
            }
        }

        /// <summary>
        /// 窗体关闭时的清理工作
        /// </summary>
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            try
            {
                // 停止定时器
                _statusUpdateTimer?.Stop();
                _statusUpdateTimer?.Dispose();
            }
            catch (Exception ex)
            {
                LogError(EventIds.ServiceException, "窗体关闭清理异常", ex);
            }

            base.OnFormClosed(e);
            
            // 清理视频播放资源
            _ = StopVideoPreviewAsync();
        }

        /// <summary>
        /// 语言切换按钮点击事件处理
        /// </summary>
        private void btnSwitchLanguage_Click(object? sender, EventArgs e)
        {
            try
            {
                // 获取当前语言
                var currentCulture = System.Threading.Thread.CurrentThread.CurrentUICulture;
                
                // 切换语言：如果当前是中文，切换到英文；否则切换到中文
                var newCulture = currentCulture.Name.StartsWith("zh") 
                    ? new System.Globalization.CultureInfo("en-US")
                    : new System.Globalization.CultureInfo("zh-CN");
                
                // 设置新的语言文化
                System.Threading.Thread.CurrentThread.CurrentCulture = newCulture;
                System.Threading.Thread.CurrentThread.CurrentUICulture = newCulture;
                
                // 刷新界面文本
                RefreshUILanguage();
                
                AppendStatus($"语言已切换到: {newCulture.DisplayName} / Language switched to: {newCulture.DisplayName}");
                LogInformation(EventIds.ConfigReloaded, $"用户切换界面语言: {newCulture.Name}");
            }
            catch (Exception ex)
            {
                AppendStatus($"切换语言失败 / Failed to switch language: {ex.Message}");
                LogError(EventIds.ServiceException, "语言切换异常", ex);
            }
        }

        /// <summary>
        /// 刷新界面语言
        /// </summary>
        private void RefreshUILanguage()
        {
            // 更新窗体标题和主标签
            this.Text = Properties.Resources.UI_Form1_WindowTitle;
            lblTitle.Text = Properties.Resources.UI_Form1_Title;
            
            // 更新配置组
            grpConfig.Text = Properties.Resources.UI_Form1_GroupConfig;
            lblCameraList.Text = Properties.Resources.UI_Form1_LabelCameraList;
            chkEnableLightSensor.Text = Properties.Resources.UI_Form1_CheckEnableLightSensor;
            lblThreshold.Text = Properties.Resources.UI_Form1_LabelThreshold;
            btnSaveConfig.Text = Properties.Resources.UI_Form1_ButtonSaveConfig;
            btnResetConfig.Text = Properties.Resources.UI_Form1_ButtonResetConfig;
            btnRefreshCameras.Text = Properties.Resources.UI_Form1_ButtonRefreshCameras;
            btnDetectLight.Text = Properties.Resources.UI_Form1_ButtonDetectLight;
            btnPreview.Text = _isVideoInitialized 
                ? Properties.Resources.UI_Button_StopPreview 
                : Properties.Resources.UI_Button_StartPreview;
            
            // 更新服务组
            grpService.Text = Properties.Resources.UI_Form1_GroupService;
            btnInstallService.Text = Properties.Resources.UI_Form1_ButtonInstallService;
            btnUninstallService.Text = Properties.Resources.UI_Form1_ButtonUninstallService;
            btnStartService.Text = Properties.Resources.UI_Form1_ButtonStartService;
            btnStopService.Text = Properties.Resources.UI_Form1_ButtonStopService;
            
            // 更新状态组
            grpStatus.Text = Properties.Resources.UI_Form1_GroupStatus;
            btnClearStatus.Text = Properties.Resources.UI_Form1_ButtonClearStatus;
            btnShowHistory.Text = Properties.Resources.UI_Form1_ButtonShowHistory;
            btnShowSystemStatus.Text = Properties.Resources.UI_Form1_ButtonShowSystemStatus;
            btnExportConfig.Text = Properties.Resources.UI_Form1_ButtonExportConfig;
            btnImportConfig.Text = Properties.Resources.UI_Form1_ButtonImportConfig;
            
            // 更新帮助组
            groupBox1.Text = Properties.Resources.UI_Form1_GroupHelp;
            
            // 更新摄像头列表显示
            if (_availableCameras.Count > 0)
            {
                int selectedIndex = cmbCameraList.SelectedIndex;
                cmbCameraList.Items.Clear();
                
                foreach (var camera in _availableCameras)
                {
                    cmbCameraList.Items.Add(camera.Name);
                }
                
                if (selectedIndex >= 0 && selectedIndex < cmbCameraList.Items.Count)
                {
                    cmbCameraList.SelectedIndex = selectedIndex;
                }
            }
            else if (cmbCameraList.Items.Count > 0)
            {
                cmbCameraList.Items.Clear();
                cmbCameraList.Items.Add(Properties.Resources.UI_CameraList_NoCameraFound);
                cmbCameraList.SelectedIndex = 0;
            }
            
            // 更新状态标签
            UpdateServiceStatusUI();
            _ = UpdateCameraStatusAsync();
        }
    }
}
