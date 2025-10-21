using System;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Threading.Tasks;
using Microsoft.Win32;
using WindowsHelloIRHelper.Services;
using WindowsHelloIRHelper.Interfaces;
using WindowsHelloIRHelper.Constants;

namespace WindowsHelloIRHelper.Services
{
    /// <summary>
    /// 简化的自动摄像头控制Windows服务
    /// 移除所有过度复杂的组件，只保留核心功能
    /// </summary>
    public class SimpleAutoCameraControlService : ServiceBase
    {
        private readonly IEventLogger _eventLogger;
        private readonly SimpleCameraHelper _cameraHelper;
        private readonly SimpleConfigHelper _configHelper;
        private SimpleConfig? _config;
        private string? _cameraDeviceId;
        private bool _isRunning = false;

        /// <summary>
        /// 服务名称
        /// </summary>
        public new const string ServiceName = "AutoCameraControlService";
        
        /// <summary>
        /// 服务显示名称（从资源文件获取）
        /// </summary>
        public static string ServiceDisplayName => Properties.Resources.Service_DisplayName;
        
        /// <summary>
        /// 服务描述（从资源文件获取）
        /// </summary>
        public static string ServiceDescription => Properties.Resources.Service_Description;

        /// <summary>
        /// 构造函数
        /// </summary>
        public SimpleAutoCameraControlService(IEventLogger eventLogger, SimpleCameraHelper cameraHelper, SimpleConfigHelper configHelper)
        {
            _eventLogger = eventLogger ?? throw new ArgumentNullException(nameof(eventLogger));
            _cameraHelper = cameraHelper ?? throw new ArgumentNullException(nameof(cameraHelper));
            _configHelper = configHelper ?? throw new ArgumentNullException(nameof(configHelper));
            
            base.ServiceName = ServiceName;
            this.CanStop = true;
            this.CanPauseAndContinue = false;
            this.CanShutdown = true;
            this.CanHandleSessionChangeEvent = true;
            this.AutoLog = false; // 使用自定义日志
        }

        /// <summary>
        /// 服务启动
        /// </summary>
        protected override async void OnStart(string[] args)
        {
            // 初始化语言文化设置
            Utils.CultureHelper.InitializeCulture();
            
            await _eventLogger.LogInformationAsync(EventIds.ServiceStarted, Properties.Resources.Log_Service_Starting);
            
            try
            {
                // Debugger.Launch();
                // 1. 加载配置（同步版本）
                _config = _configHelper.LoadConfig();
                
                // 2. 获取摄像头设备ID（同步版本）
                if (!string.IsNullOrEmpty(_config.TargetCameraDeviceId))
                {
                    _cameraDeviceId = _config.TargetCameraDeviceId;
                }
                else
                {
                    throw new InvalidOperationException(Properties.Resources.Log_Service_ConfigError);
                }
                
                // 3. 检查管理员权限
                if (!_cameraHelper.IsRunningAsAdministrator())
                {
                    await _eventLogger.LogErrorAsync(EventIds.InsufficientPermissions, Properties.Resources.Log_Service_InsufficientPermissions);
                    throw new InvalidOperationException(Properties.Resources.Log_Service_PermissionsRequired);
                }
                
                _isRunning = true;
                await _eventLogger.LogInformationAsync(EventIds.ServiceStarted, string.Format(Properties.Resources.Log_Service_StartSuccess, _cameraDeviceId));
            }
            catch (Exception ex)
            {
                await _eventLogger.LogErrorAsync(EventIds.ServiceException, string.Format(Properties.Resources.Log_Service_StartFailed, ex.Message), ex);
                throw;
            }
        }

        /// <summary>
        /// 服务停止
        /// </summary>
        protected override async void OnStop()
        {
            await _eventLogger.LogInformationAsync(EventIds.ServiceStopped, Properties.Resources.Log_Service_Stopping);
            _isRunning = false;
            await _eventLogger.LogInformationAsync(EventIds.ServiceStopped, Properties.Resources.Log_Service_Stopped);
        }

        /// <summary>
        /// 会话变化事件处理
        /// </summary>
        protected override async void OnSessionChange(SessionChangeDescription changeDescription)
        {
            try
            {
                await _eventLogger.LogInformationAsync(EventIds.SessionSwitchDetected, string.Format(Properties.Resources.Log_Service_SessionChange, changeDescription.Reason));
                
                if (!_isRunning || string.IsNullOrEmpty(_cameraDeviceId))
                    return;

                switch (changeDescription.Reason)
                {
                    case SessionChangeReason.SessionLock:
                        HandleSessionLock();
                        break;
                        
                    case SessionChangeReason.SessionUnlock:
                        HandleSessionUnlock();
                        break;
                }
            }
            catch (Exception ex)
            {
                await _eventLogger.LogErrorAsync(EventIds.ServiceException, string.Format(Properties.Resources.Log_Service_SessionChangeException, ex.Message), ex);
            }
        }

        /// <summary>
        /// 处理锁屏事件
        /// </summary>
        private async void HandleSessionLock()
        {
            await _eventLogger.LogInformationAsync(EventIds.CameraControlTriggered, Properties.Resources.Log_Service_HandleLockScreen);
            
            // 使用Task.Run处理操作，避免阻塞OnSessionChange
            _ = Task.Run(async () =>
            {
                try
                {
                    // 如果启用了光线传感器，检查光亮度
                    if (_config?.EnableLightSensor == true && SimpleLightSensorHelper.IsAvailable())
                    {
                        var lux = SimpleLightSensorHelper.GetCurrentLux();
                        if (lux.HasValue && lux >= _config.LightThreshold)
                        {
                            await _eventLogger.LogInformationAsync(EventIds.LockScreenLightSufficient, string.Format(Properties.Resources.Log_Service_LightSufficient, lux));
                            return;
                        }
                    }
                    
                    // 禁用摄像头
                    var success = await _cameraHelper.DisableCameraAsync(_cameraDeviceId!);
                    if (success)
                    {
                        await _eventLogger.LogInformationAsync(EventIds.CameraDisabled, Properties.Resources.Log_Service_CameraDisabled);
                    }
                    else
                    {
                        await _eventLogger.LogErrorAsync(EventIds.CameraControlFailed, Properties.Resources.Log_Service_CameraDisableFailed);
                    }
                }
                catch (Exception ex)
                {
                    await _eventLogger.LogErrorAsync(EventIds.ServiceException, string.Format(Properties.Resources.Log_Service_HandleLockException, ex.Message), ex);
                }
            });
        }

        /// <summary>
        /// 处理解锁事件
        /// </summary>
        private async void HandleSessionUnlock()
        {
            await _eventLogger.LogInformationAsync(EventIds.CameraControlTriggered, Properties.Resources.Log_Service_HandleUnlock);
            
            // 使用Task.Run处理操作，避免阻塞OnSessionChange
            _ = Task.Run(async () =>
            {
                try
                {
                    // 启用摄像头
                    var success = await _cameraHelper.EnableCameraAsync(_cameraDeviceId!);
                    if (success)
                    {
                        await _eventLogger.LogInformationAsync(EventIds.CameraEnabled, Properties.Resources.Log_Service_CameraEnabled);
                    }
                    else
                    {
                        await _eventLogger.LogErrorAsync(EventIds.CameraControlFailed, Properties.Resources.Log_Service_CameraEnableFailed);
                    }
                }
                catch (Exception ex)
                {
                    await _eventLogger.LogErrorAsync(EventIds.ServiceException, string.Format(Properties.Resources.Log_Service_HandleUnlockException, ex.Message), ex);
                }
            });
        }

        // 移除所有自定义日志方法，现在使用注入的 IEventLogger
    }
}