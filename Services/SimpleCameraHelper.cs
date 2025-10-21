using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Security.Principal;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Devices.Sensors;
using WindowsHelloIRHelper.Models;
using WindowsHelloIRHelper.Interfaces;
using WindowsHelloIRHelper.Constants;
using WindowsHelloIRHelper.Properties;
using Windows.Devices.Enumeration;

namespace WindowsHelloIRHelper.Services
{
    /// <summary>
    /// 简化的摄像头工具类 - 提供基本的摄像头控制功能
    /// </summary>
    public class SimpleCameraHelper
    {
        private readonly IEventLogger _eventLogger;
        private readonly SecurityHelper _securityHelper;

        /// <summary>
        /// 初始化 SimpleCameraHelper 实例
        /// </summary>
        /// <param name="eventLogger">事件日志记录器</param>
        /// <param name="securityHelper">安全助手</param>
        public SimpleCameraHelper(IEventLogger eventLogger, SecurityHelper securityHelper)
        {
            _eventLogger = eventLogger ?? throw new ArgumentNullException(nameof(eventLogger));
            _securityHelper = securityHelper ?? throw new ArgumentNullException(nameof(securityHelper));
        }

        /// <summary>
        /// 检查是否以管理员权限运行
        /// </summary>
        public bool IsRunningAsAdministrator()
        {
            return _securityHelper.IsRunningAsAdministrator();
        }

        /// <summary>
        /// 获取所有可用的摄像头设备列表
        /// </summary>
        public async Task<List<CameraDevice>> GetAvailableCamerasAsync()
        {
            var cameras = new List<CameraDevice>();

            try
            {
                await _eventLogger.LogInformationAsync(EventIds.ServiceStarted, Properties.Resources.Log_Camera_EnumeratingDevices);

                var devices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
                foreach (var device in devices)
                {
                    var camera = new CameraDevice
                    {
                        DeviceId = device.Id,
                        Name = device.Name,
                        Description = "",
                        IsEnabled = device.IsEnabled
                    };
                    cameras.Add(camera);
                }
            }
            catch (Exception ex)
            {
                await _eventLogger.LogWarningAsync(EventIds.DeviceNotFound, string.Format(Properties.Resources.Log_Camera_EnumerationError, ex.Message));
                await _eventLogger.LogErrorAsync(EventIds.DeviceNotFound, string.Format(Properties.Resources.Log_Camera_EnumerationError, ex.Message), ex);
            }

            // 按设备类型和名称排序：集成摄像头优先，然后按名称排序
            return cameras.OrderByDescending(c => c.Name)
                         .ThenBy(c => c.IsEnabled)
                         .ToList();
        }

        public async Task<DeviceInformation?> FindDeviceById(string targetDeviceId)
        {
            var devices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
            if (devices == null || devices.Count == 0) { return null; }
            return devices.FirstOrDefault(d => d.Id == targetDeviceId);
        }

        public async Task<bool> GetDeviceEnabledAsync(string deviceId)
        {
            var device = await FindDeviceById(deviceId);
            if (device == null) {  return false; }
            return device.IsEnabled;
        }

        /// <summary>
        /// 启用摄像头
        /// </summary>
        public async Task<bool> EnableCameraAsync(string deviceId)
        {
            // 先检查当前状态，如果已经是启用状态则无需操作
            var currentState = await GetDeviceEnabledAsync(deviceId);
            if (currentState == true)
            {
                await _eventLogger.LogInformationAsync(EventIds.CameraAlreadyEnabled,
                    string.Format(Properties.Resources.Log_Camera_AlreadyEnabled, deviceId));
                return true;
            }
            
            return await ExecuteDeviceCommandAsync(deviceId, "/enable-device");
        }

        /// <summary>
        /// 禁用摄像头
        /// </summary>
        public async Task<bool> DisableCameraAsync(string deviceId)
        {
            // 先检查当前状态，如果已经是禁用状态则无需操作
            var currentState = await GetDeviceEnabledAsync(deviceId);
            if (currentState == false)
            {
                await _eventLogger.LogInformationAsync(EventIds.CameraAlreadyDisabled,
                    string.Format(Properties.Resources.Log_Camera_AlreadyDisabled, deviceId));
                return true;
            }
            
            return await ExecuteDeviceCommandAsync(deviceId, "/disable-device");
        }

        /// <summary>
        /// 执行设备命令
        /// </summary>
        private async Task<bool> ExecuteDeviceCommandAsync(string deviceId, string command)
        {
            if (!IsRunningAsAdministrator())
            {
                await _eventLogger.LogErrorAsync(EventIds.InsufficientPermissions, Properties.Resources.Log_Camera_AdminRequired);
                return false;
            }

            if (string.IsNullOrEmpty(deviceId))
            {
                await _eventLogger.LogErrorAsync(EventIds.DeviceNotFound, Properties.Resources.Log_Camera_EmptyDeviceId);
                return false;
            }

            try
            {
                deviceId = DeviceInstancePathToPnpDeviceId(deviceId);
                var startInfo = new ProcessStartInfo
                {
                    FileName = "pnputil.exe",
                    Arguments = $"{command} \"{deviceId}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process != null)
                {
                    await Task.Run(() => process.WaitForExit());
                    bool success = process.ExitCode == 0 || process.ExitCode == 3010 || process.ExitCode == 50;
                    
                    if (success)
                    {
                        string action = command.Contains("enable") ? Properties.Resources.Log_Camera_ActionEnable : Properties.Resources.Log_Camera_ActionDisable;
                        await _eventLogger.LogInformationAsync(
                            command.Contains("enable") ? EventIds.CameraEnabled : EventIds.CameraDisabled,
                            string.Format(Properties.Resources.Log_Camera_ActionSuccess, action, deviceId));
                    }
                    else
                    {
                        await _eventLogger.LogErrorAsync(EventIds.CameraControlFailed,
                            string.Format(Properties.Resources.Log_Camera_CommandFailedWithCode, process.ExitCode, deviceId));
                    }
                    
                    return success;
                }
            }
            catch (Exception ex)
            {
                await _eventLogger.LogErrorAsync(EventIds.CameraControlFailed, string.Format(Properties.Resources.Log_Camera_CommandFailed, ex.Message), ex);
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="deviceInstancePath"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static string DeviceInstancePathToPnpDeviceId(string deviceInstancePath)
        {
            // 示例输入: \\?\USB#VID_04F2&PID_B7E8&MI_00#7&37c306d&1&0000#{e5323777-f976-4f5b-9b55-b94699c46e44}\GLOBAL

            // 1. 移除前缀
            string withoutPrefix = deviceInstancePath.Replace(@"\\?\", "");

            // 2. 按 '#' 分割
            string[] parts = withoutPrefix.Split('#');

            if (parts.Length < 3)
            {
                throw new ArgumentException("Invalid device instance path format.");
            }

            // 3. 第一部分是硬件ID（需要将第一个'#'之前的'#'替换回'\'，但这里第一部分没有'#'，所以直接替换所有'#'）
            // 实际上，我们需要重构硬件ID部分。它可能是 parts[0] 和 parts[1] 的一部分？不，更可靠的方法是找到接口GUID之前的部分。
            // 更简单的方法：找到最后一个设备接口GUID的位置，然后取它前面的部分。
            int lastGuidIndex = deviceInstancePath.LastIndexOf('{');
            if (lastGuidIndex == -1)
            {
                throw new ArgumentException("No device interface GUID found.");
            }

            string deviceIdPart = deviceInstancePath.Substring(4, lastGuidIndex - 5); // 4 是 "\\?\" 的长度, -5 是为了去掉末尾的 '#' 和开头的 '#'
                                                                                      // 现在 deviceIdPart 是： USB#VID_04F2&PID_B7E8&MI_00#7&37c306d&1&0000

            // 4. 将 '#' 替换回 '\'
            string pnpDeviceId = deviceIdPart.Replace('#', '\\');
            // 结果: USB\VID_04F2&PID_B7E8&MI_00\7&37c306d&1&0000

            return pnpDeviceId;
        }
    }

    /// <summary>
    /// 简化的光线传感器工具类 - 提供基本的光线检测功能
    /// </summary>
    public static class SimpleLightSensorHelper
    {
        /// <summary>
        /// 检查光线传感器是否可用
        /// </summary>
        public static bool IsAvailable()
        {
            try
            {
                return LightSensor.GetDefault() != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 获取当前光亮度
        /// </summary>
        public static async Task<double?> GetCurrentLuxAsync()
        {
            try
            {
                return await Task.Run(() =>
                {
                    var sensor = LightSensor.GetDefault();
                    return sensor?.GetCurrentReading()?.IlluminanceInLux;
                });
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 获取当前光亮度（同步版本）
        /// </summary>
        public static double? GetCurrentLux()
        {
            try
            {
                var sensor = LightSensor.GetDefault();
                return sensor?.GetCurrentReading()?.IlluminanceInLux;
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// 简化的配置工具类 - 提供基本的配置加载功能
    /// </summary>
    public class SimpleConfigHelper
    {
        private readonly IEventLogger _eventLogger;
        private readonly string ConfigPath;

        /// <summary>
        /// 初始化 SimpleConfigHelper 实例
        /// </summary>
        /// <param name="eventLogger">事件日志记录器</param>
        public SimpleConfigHelper(IEventLogger eventLogger)
        {
            _eventLogger = eventLogger ?? throw new ArgumentNullException(nameof(eventLogger));
            ConfigPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "AutoCameraControl", "config.json");
        }

        /// <summary>
        /// 加载配置（同步版本）
        /// </summary>
        public SimpleConfig LoadConfig()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    var json = File.ReadAllText(ConfigPath);
                    var options = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };
                    var config = JsonSerializer.Deserialize<SimpleConfig>(json, options);
                    return ValidateAndFixConfig(config ?? CreateDefaultConfig());
                }
            }
            catch (Exception ex)
            {
                _eventLogger.LogErrorAsync(EventIds.ConfigLoadFailed, string.Format(Resources.Log_Config_LoadFailed, ex.Message), ex).GetAwaiter().GetResult();
            }

            return CreateDefaultConfig();
        }

        /// <summary>
        /// 创建默认配置
        /// </summary>
        private SimpleConfig CreateDefaultConfig()
        {
            return new SimpleConfig
            {
                TargetCameraDeviceId = null, // 将在服务启动时自动检测
                EnableLightSensor = false,
                LightThreshold = 10.0
            };
        }

        /// <summary>
        /// 验证并修正配置值
        /// </summary>
        /// <param name="config">要验证的配置</param>
        /// <returns>验证并修正后的配置</returns>
        private SimpleConfig ValidateAndFixConfig(SimpleConfig config)
        {
            if (config == null)
                return CreateDefaultConfig();

            // 确保光亮度阈值在合理范围内 (0.1 到 10000 lux)
            if (config.LightThreshold < 0.1)
                config.LightThreshold = 1.0;
            else if (config.LightThreshold > 10000)
                config.LightThreshold = 10000;

            return config;
        }

        /// <summary>
        /// 保存配置
        /// </summary>
        public async Task SaveConfigAsync(SimpleConfig config)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                await File.WriteAllTextAsync(ConfigPath, json);
            }
            catch (Exception ex)
            {
                await _eventLogger.LogErrorAsync(EventIds.ConfigSaveFailed, string.Format(Resources.Log_Config_SaveFailedWithError, ex.Message), ex);
                throw;
            }
        }

        /// <summary>
        /// 保存配置（同步版本）
        /// </summary>
        public void SaveConfig(SimpleConfig config)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                File.WriteAllText(ConfigPath, json);
            }
            catch (Exception ex)
            {
                _eventLogger.LogErrorAsync(EventIds.ConfigSaveFailed, string.Format(Resources.Log_Config_SaveFailedWithError, ex.Message), ex).GetAwaiter().GetResult();
                throw;
            }
        }
    }

    /// <summary>
    /// 简化的配置模型
    /// </summary>
    public class SimpleConfig
    {
        public string? TargetCameraDeviceId { get; set; }
        public bool EnableLightSensor { get; set; }
        public double LightThreshold { get; set; }
    }
}