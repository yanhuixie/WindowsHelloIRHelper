using System;
// using System.Configuration.Install; // 不再支持.NET 8，使用SC命令代替
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Principal;
using System.ServiceProcess;
using System.Threading.Tasks;
using WindowsHelloIRHelper.Models;

namespace WindowsHelloIRHelper.Services
{
    /// <summary>
    /// 服务管理器，负责Windows服务的注册、卸载、启动和停止操作
    /// </summary>
    public class ServiceManager
    {
        /// <summary>
        /// 检查当前用户是否具有管理员权限
        /// </summary>
        /// <returns>是否具有管理员权限</returns>
        public static bool IsRunningAsAdministrator()
        {
            try
            {
                var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// 检查服务是否已安装
        /// </summary>
        /// <returns>服务是否已安装</returns>
        public static bool IsServiceInstalled()
        {
            try
            {
                using var serviceController = new ServiceController(SimpleAutoCameraControlService.ServiceName);
                // 尝试访问服务状态，如果服务不存在会抛出异常
                var status = serviceController.Status;
                return true;
            }
            catch (InvalidOperationException)
            {
                // 服务不存在
                return false;
            }
            catch (Exception)
            {
                // 其他异常，假设服务不存在
                return false;
            }
        }
        
        /// <summary>
        /// 获取服务状态
        /// </summary>
        /// <returns>服务状态</returns>
        public static ServiceControllerStatus? GetServiceStatus()
        {
            try
            {
                if (!IsServiceInstalled())
                    return null;
                
                using var serviceController = new ServiceController(SimpleAutoCameraControlService.ServiceName);
                return serviceController.Status;
            }
            catch
            {
                return null;
            }
        }
        
        /// <summary>
        /// 注册Windows服务
        /// </summary>
        /// <returns>操作结果</returns>
        public static async Task<ServiceOperationResult> InstallServiceAsync()
        {
            try
            {
                // 检查管理员权限
                if (!IsRunningAsAdministrator())
                {
                    return new ServiceOperationResult
                    {
                        Success = false,
                        Message = Properties.Resources.Log_ServiceManager_AdminRequired
                    };
                }
                
                // 检查服务是否已安装
                if (IsServiceInstalled())
                {
                    return new ServiceOperationResult
                    {
                        Success = false,
                        Message = Properties.Resources.Log_ServiceManager_AlreadyInstalled
                    };
                }
                
                // 获取当前可执行文件路径
                var executablePath = GetExecutablePath();
                if (string.IsNullOrEmpty(executablePath))
                {
                    return new ServiceOperationResult
                    {
                        Success = false,
                        Message = Properties.Resources.Log_ServiceManager_CannotDetermineExePath
                    };
                }
                
                // 验证可执行文件
                var validationResult = ValidateExecutablePath(executablePath);
                if (!validationResult.Success)
                {
                    return validationResult;
                }
                
                // 尝试使用高级安装器（SC命令）
                var scResult = await AdvancedServiceInstaller.InstallServiceWithScAsync(executablePath);
                if (scResult.Success)
                {
                    // 验证安装结果
                    var validationResult2 = await AdvancedServiceInstaller.ValidateServiceInstallationAsync();
                    if (validationResult2.Success)
                    {
                        return new ServiceOperationResult
                        {
                            Success = true,
                            Message = scResult.Message + "\n\n" + validationResult2.Message
                        };
                    }
                }
                
                // SC命令失败，返回错误
                return scResult;
            }
            catch (Exception ex)
            {
                return new ServiceOperationResult
                {
                    Success = false,
                    Message = string.Format(Properties.Resources.Log_ServiceManager_InstallFailed, ex.Message),
                    ErrorDetails = ex.ToString()
                };
            }
        }
        
        // InstallUtil方法已弃用，.NET 8不再支持System.Configuration.Install
        // 现在统一使用SC命令进行服务安装
        
        /// <summary>
        /// 卸载Windows服务
        /// </summary>
        /// <returns>操作结果</returns>
        public static async Task<ServiceOperationResult> UninstallServiceAsync()
        {
            try
            {
                // 检查管理员权限
                if (!IsRunningAsAdministrator())
                {
                    return new ServiceOperationResult
                    {
                        Success = false,
                        Message = Properties.Resources.Log_ServiceManager_UninstallAdminRequired
                    };
                }
                
                // 检查服务是否已安装
                if (!IsServiceInstalled())
                {
                    return new ServiceOperationResult
                    {
                        Success = false,
                        Message = Properties.Resources.Log_ServiceManager_NotInstalled
                    };
                }
                
                // 尝试使用高级卸载器（SC命令）
                var scResult = await AdvancedServiceInstaller.UninstallServiceWithScAsync();
                if (scResult.Success)
                {
                    return scResult;
                }
                
                // SC命令失败，返回错误
                return scResult;
            }
            catch (Exception ex)
            {
                return new ServiceOperationResult
                {
                    Success = false,
                    Message = string.Format(Properties.Resources.Log_ServiceManager_UninstallFailed, ex.Message),
                    ErrorDetails = ex.ToString()
                };
            }
        }
        
        /// <summary>
        // UninstallServiceWithInstallUtilAsync方法已弃用，.NET 8不再支持System.Configuration.Install
        // 现在统一使用SC命令进行服务卸载
        
        /// <summary>
        /// 启动Windows服务
        /// </summary>
        /// <returns>操作结果</returns>
        public static async Task<ServiceOperationResult> StartServiceAsync()
        {
            try
            {
                if (!IsServiceInstalled())
                {
                    return new ServiceOperationResult
                    {
                        Success = false,
                        Message = Properties.Resources.Log_ServiceManager_StartNotInstalled
                    };
                }
                
                using var serviceController = new ServiceController(SimpleAutoCameraControlService.ServiceName);
                
                // 检查当前状态
                serviceController.Refresh();
                if (serviceController.Status == ServiceControllerStatus.Running)
                {
                    return new ServiceOperationResult
                    {
                        Success = true,
                        Message = Properties.Resources.Log_ServiceManager_AlreadyRunning
                    };
                }
                
                if (serviceController.Status == ServiceControllerStatus.StartPending)
                {
                    return new ServiceOperationResult
                    {
                        Success = true,
                        Message = Properties.Resources.Log_ServiceManager_StartPending
                    };
                }
                
                // 启动服务
                await Task.Run(() =>
                {
                    serviceController.Start();
                    serviceController.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
                });
                
                return new ServiceOperationResult
                {
                    Success = true,
                    Message = Properties.Resources.Log_ServiceManager_StartSuccess
                };
            }
            catch (System.TimeoutException)
            {
                return new ServiceOperationResult
                {
                    Success = false,
                    Message = Properties.Resources.Log_ServiceManager_StartTimeout
                };
            }
            catch (Exception ex)
            {
                return new ServiceOperationResult
                {
                    Success = false,
                    Message = string.Format(Properties.Resources.Log_ServiceManager_StartFailed, ex.Message)
                };
            }
        }
        
        /// <summary>
        /// 停止Windows服务
        /// </summary>
        /// <returns>操作结果</returns>
        public static async Task<ServiceOperationResult> StopServiceAsync()
        {
            try
            {
                if (!IsServiceInstalled())
                {
                    return new ServiceOperationResult
                    {
                        Success = false,
                        Message = Properties.Resources.Log_ServiceManager_StopNotInstalled
                    };
                }
                
                using var serviceController = new ServiceController(SimpleAutoCameraControlService.ServiceName);
                
                // 检查当前状态
                serviceController.Refresh();
                if (serviceController.Status == ServiceControllerStatus.Stopped)
                {
                    return new ServiceOperationResult
                    {
                        Success = true,
                        Message = Properties.Resources.Log_ServiceManager_AlreadyStopped
                    };
                }
                
                if (serviceController.Status == ServiceControllerStatus.StopPending)
                {
                    return new ServiceOperationResult
                    {
                        Success = true,
                        Message = Properties.Resources.Log_ServiceManager_StopPending
                    };
                }
                
                // 停止服务
                await Task.Run(() =>
                {
                    if (serviceController.CanStop)
                    {
                        serviceController.Stop();
                        serviceController.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                    }
                    else
                    {
                        throw new InvalidOperationException(Properties.Resources.Log_ServiceManager_CannotStop);
                    }
                });
                
                return new ServiceOperationResult
                {
                    Success = true,
                    Message = Properties.Resources.Log_ServiceManager_StopSuccess
                };
            }
            catch (System.TimeoutException)
            {
                return new ServiceOperationResult
                {
                    Success = false,
                    Message = Properties.Resources.Log_ServiceManager_StopTimeout
                };
            }
            catch (Exception ex)
            {
                return new ServiceOperationResult
                {
                    Success = false,
                    Message = string.Format(Properties.Resources.Log_ServiceManager_StopFailed, ex.Message)
                };
            }
        }
        
        /// <summary>
        /// 重启Windows服务
        /// </summary>
        /// <returns>操作结果</returns>
        public static async Task<ServiceOperationResult> RestartServiceAsync()
        {
            try
            {
                // 先停止服务
                var stopResult = await StopServiceAsync();
                if (!stopResult.Success && !stopResult.Message.Contains(Properties.Resources.Log_ServiceManager_AlreadyStopped))
                {
                    return new ServiceOperationResult
                    {
                        Success = false,
                        Message = string.Format(Properties.Resources.Log_ServiceManager_RestartStopFailed, stopResult.Message)
                    };
                }
                
                // 等待一小段时间确保服务完全停止
                await Task.Delay(2000);
                
                // 启动服务
                var startResult = await StartServiceAsync();
                if (!startResult.Success)
                {
                    return new ServiceOperationResult
                    {
                        Success = false,
                        Message = string.Format(Properties.Resources.Log_ServiceManager_RestartStartFailed, startResult.Message)
                    };
                }
                
                return new ServiceOperationResult
                {
                    Success = true,
                    Message = Properties.Resources.Log_ServiceManager_RestartSuccess
                };
            }
            catch (Exception ex)
            {
                return new ServiceOperationResult
                {
                    Success = false,
                    Message = string.Format(Properties.Resources.Log_ServiceManager_RestartFailed, ex.Message)
                };
            }
        }
        
        /// <summary>
        /// 配置服务恢复选项
        /// </summary>
        private static async Task ConfigureServiceRecoveryAsync()
        {
            try
            {
                await Task.Run(() =>
                {
                    // 使用sc命令配置服务恢复选项
                    var processInfo = new ProcessStartInfo
                    {
                        FileName = "sc",
                        Arguments = $"failure \"{SimpleAutoCameraControlService.ServiceName}\" reset= 86400 actions= restart/5000/restart/10000/restart/30000",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };
                    
                    using var process = Process.Start(processInfo);
                    if (process != null)
                    {
                        process.WaitForExit(10000); // 最多等待10秒
                    }
                });
            }
            catch (Exception)
            {
                // 配置恢复选项失败不应该影响服务安装
                // 忽略异常，服务仍然可以正常工作
            }
        }
        
        /// <summary>
        /// 获取服务详细状态信息
        /// </summary>
        /// <returns>服务状态信息</returns>
        public static ServiceStatus GetDetailedServiceStatus()
        {
            try
            {
                var status = new ServiceStatus
                {
                    IsInstalled = IsServiceInstalled(),
                    HasAdminRights = IsRunningAsAdministrator()
                };
                
                if (status.IsInstalled)
                {
                    using var serviceController = new ServiceController(SimpleAutoCameraControlService.ServiceName);
                    serviceController.Refresh();
                    
                    status.Status = serviceController.Status;
                    status.CanStart = serviceController.Status == ServiceControllerStatus.Stopped;
                    status.CanStop = serviceController.Status == ServiceControllerStatus.Running && serviceController.CanStop;
                    status.CanPause = serviceController.Status == ServiceControllerStatus.Running && serviceController.CanPauseAndContinue;
                    
                    // 获取服务启动类型
                    try
                    {
                        var startType = GetServiceStartType();
                        status.StartType = startType;
                        status.IsAutoStart = startType == CustomServiceStartMode.Automatic;
                    }
                    catch
                    {
                        status.StartType = null;
                        status.IsAutoStart = false;
                    }
                }
                
                return status;
            }
            catch (Exception ex)
            {
                return new ServiceStatus
                {
                    IsInstalled = false,
                    HasAdminRights = IsRunningAsAdministrator(),
                    Status = null,
                    ErrorMessage = ex.Message
                };
            }
        }
        
        /// <summary>
        /// 获取服务启动类型
        /// </summary>
        /// <returns>启动类型</returns>
        private static CustomServiceStartMode GetServiceStartType()
        {
            try
            {
                using var process = new Process();
                process.StartInfo.FileName = "sc";
                process.StartInfo.Arguments = $"qc \"{SimpleAutoCameraControlService.ServiceName}\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.CreateNoWindow = true;
                
                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                
                if (output.Contains("AUTO_START"))
                    return CustomServiceStartMode.Automatic;
                else if (output.Contains("DEMAND_START"))
                    return CustomServiceStartMode.Manual;
                else if (output.Contains("DISABLED"))
                    return CustomServiceStartMode.Disabled;
                else
                    return CustomServiceStartMode.Manual;
            }
            catch
            {
                return CustomServiceStartMode.Manual;
            }
        }
        
        /// <summary>
        /// 获取可执行文件路径
        /// </summary>
        /// <returns>可执行文件路径</returns>
        private static string GetExecutablePath()
        {
            try
            {
                // 优先获取进程主模块文件名（.exe 文件）
                // 这对于 Windows 服务很重要，因为服务必须注册 .exe 而不是 .dll
                var processFileName = Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(processFileName) && File.Exists(processFileName))
                {
                    // 确保是 .exe 文件
                    if (Path.GetExtension(processFileName).Equals(".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        return processFileName;
                    }
                }
                
                // 如果进程文件名无效，尝试从程序集位置推断 .exe 路径
                var assemblyLocation = Assembly.GetExecutingAssembly().Location;
                if (!string.IsNullOrEmpty(assemblyLocation))
                {
                    // 将 .dll 替换为 .exe
                    var exePath = Path.ChangeExtension(assemblyLocation, ".exe");
                    if (File.Exists(exePath))
                    {
                        return exePath;
                    }
                }
                
                // 最后尝试从应用程序域基目录查找
                var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                var assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
                if (!string.IsNullOrEmpty(assemblyName))
                {
                    var exePath = Path.Combine(baseDirectory, assemblyName + ".exe");
                    if (File.Exists(exePath))
                    {
                        return exePath;
                    }
                }
                
                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
        
        /// <summary>
        /// 验证可执行文件路径
        /// </summary>
        /// <param name="executablePath">可执行文件路径</param>
        /// <returns>验证结果</returns>
        private static ServiceOperationResult ValidateExecutablePath(string executablePath)
        {
            try
            {
                if (string.IsNullOrEmpty(executablePath))
                {
                    return new ServiceOperationResult
                    {
                        Success = false,
                        Message = Properties.Resources.Log_ServiceManager_ExePathEmpty
                    };
                }
                
                if (!File.Exists(executablePath))
                {
                    return new ServiceOperationResult
                    {
                        Success = false,
                        Message = string.Format(Properties.Resources.Log_ServiceManager_ExeNotFound, executablePath)
                    };
                }
                
                // 检查文件是否可读
                try
                {
                    using var fileStream = File.OpenRead(executablePath);
                }
                catch (Exception ex)
                {
                    return new ServiceOperationResult
                    {
                        Success = false,
                        Message = string.Format(Properties.Resources.Log_ServiceManager_CannotReadExe, ex.Message)
                    };
                }
                
                // 检查路径是否包含特殊字符（可能导致服务安装问题）
                var invalidChars = Path.GetInvalidPathChars();
                if (executablePath.Any(c => invalidChars.Contains(c)))
                {
                    return new ServiceOperationResult
                    {
                        Success = false,
                        Message = Properties.Resources.Log_ServiceManager_InvalidPathChars
                    };
                }
                
                // 检查路径长度
                if (executablePath.Length > 260)
                {
                    return new ServiceOperationResult
                    {
                        Success = false,
                        Message = Properties.Resources.Log_ServiceManager_PathTooLong
                    };
                }
                
                return new ServiceOperationResult
                {
                    Success = true,
                    Message = Properties.Resources.Log_ServiceManager_PathValidationPassed
                };
            }
            catch (Exception ex)
            {
                return new ServiceOperationResult
                {
                    Success = false,
                    Message = string.Format(Properties.Resources.Log_ServiceManager_PathValidationException, ex.Message),
                    ErrorDetails = ex.ToString()
                };
            }
        }
        
        /// <summary>
        /// 检查系统环境
        /// </summary>
        /// <returns>环境检查结果</returns>
        public static ServiceOperationResult CheckSystemEnvironment()
        {
            try
            {
                var issues = new List<string>();
                var warnings = new List<string>();
                
                // 检查操作系统版本
                var osVersion = Environment.OSVersion;
                if (osVersion.Platform != PlatformID.Win32NT)
                {
                    issues.Add(Properties.Resources.Log_ServiceManager_UnsupportedPlatform);
                }
                else if (osVersion.Version.Major < 6)
                {
                    issues.Add(Properties.Resources.Log_ServiceManager_RequiresVistaOrHigher);
                }
                
                // 检查.NET运行时版本
                var netVersion = Environment.Version;
                if (netVersion.Major < 4)
                {
                    warnings.Add(string.Format(Properties.Resources.Log_ServiceManager_NetVersionWarning, netVersion));
                }
                
                // 检查管理员权限
                if (!IsRunningAsAdministrator())
                {
                    warnings.Add(Properties.Resources.Log_ServiceManager_NotRunningAsAdminWarning);
                }
                
                // 检查服务控制管理器访问权限
                try
                {
                    using var scManager = new ServiceController("Themes"); // 使用一个系统服务测试访问权限
                    var status = scManager.Status; // 尝试读取状态
                }
                catch
                {
                    warnings.Add(Properties.Resources.Log_ServiceManager_CannotAccessSCM);
                }
                
                // 检查Windows事件日志访问权限
                try
                {
                    using var eventLog = new System.Diagnostics.EventLog("Application");
                    // 尝试创建事件源（需要管理员权限）
                }
                catch
                {
                    warnings.Add(Properties.Resources.Log_ServiceManager_CannotAccessEventLog);
                }
                
                var result = new ServiceOperationResult
                {
                    Success = issues.Count == 0,
                    Message = Properties.Resources.Log_ServiceManager_EnvironmentCheckComplete
                };
                
                if (issues.Count > 0)
                {
                    result.Message += Properties.Resources.Log_ServiceManager_IssuesFound + string.Join("\n• ", issues);
                }
                
                if (warnings.Count > 0)
                {
                    result.Message += Properties.Resources.Log_ServiceManager_WarningsFound + string.Join("\n• ", warnings);
                }
                
                if (issues.Count == 0 && warnings.Count == 0)
                {
                    result.Message += Properties.Resources.Log_ServiceManager_EnvironmentOK;
                }
                
                return result;
            }
            catch (Exception ex)
            {
                return new ServiceOperationResult
                {
                    Success = false,
                    Message = string.Format(Properties.Resources.Log_ServiceManager_EnvironmentCheckException, ex.Message),
                    ErrorDetails = ex.ToString()
                };
            }
        }
    }
}