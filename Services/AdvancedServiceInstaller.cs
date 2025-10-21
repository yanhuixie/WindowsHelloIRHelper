using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Security.Principal;
using System.ServiceProcess;
using System.Threading.Tasks;
using WindowsHelloIRHelper.Models;

namespace WindowsHelloIRHelper.Services
{
    /// <summary>
    /// 高级服务安装器
    /// 提供更详细的服务安装、配置和管理功能
    /// </summary>
    public class AdvancedServiceInstaller
    {
        /// <summary>
        /// 使用SC命令安装服务
        /// </summary>
        /// <param name="servicePath">服务可执行文件路径</param>
        /// <returns>安装结果</returns>
        public static async Task<ServiceOperationResult> InstallServiceWithScAsync(string servicePath)
        {
            try
            {
                // 初始化语言文化设置，确保服务名称和描述使用正确的语言
                Utils.CultureHelper.InitializeCulture();
                
                // 检查管理员权限
                if (!ServiceManager.IsRunningAsAdministrator())
                {
                    return new ServiceOperationResult
                    {
                        Success = false,
                        Message = Properties.Resources.Log_AdvancedInstaller_AdminRequired
                    };
                }
                
                // 验证文件路径
                if (!File.Exists(servicePath))
                {
                    return new ServiceOperationResult
                    {
                        Success = false,
                        Message = string.Format(Properties.Resources.Log_AdvancedInstaller_FileNotFound, servicePath)
                    };
                }
                
                // 检查服务是否已存在
                if (ServiceManager.IsServiceInstalled())
                {
                    return new ServiceOperationResult
                    {
                        Success = false,
                        Message = Properties.Resources.Log_AdvancedInstaller_AlreadyInstalled
                    };
                }
                
                // 构建服务命令行参数
                var serviceCommand = $"\"{servicePath}\" --service";
                
                // 使用SC命令创建服务
                var createResult = await ExecuteScCommandAsync(
                    $"create \"{SimpleAutoCameraControlService.ServiceName}\" " +
                    $"binPath= \"{serviceCommand}\" " +
                    $"DisplayName= \"{SimpleAutoCameraControlService.ServiceDisplayName}\" " +
                    $"start= auto " +
                    $"obj= LocalSystem");
                
                if (!createResult.Success)
                {
                    return createResult;
                }
                
                // 等待一会儿确保服务已完全注册
                await Task.Delay(1000);
                
                // 设置服务描述 - 使用sc config命令，这通常更可靠
                var descriptionResult = await ExecuteScCommandAsync(
                    $"config \"{SimpleAutoCameraControlService.ServiceName}\" description= \"{SimpleAutoCameraControlService.ServiceDescription}\"");
                
                // 检查描述设置是否成功
                if (!descriptionResult.Success)
                {
                    // 记录警告但不影响服务安装
                    System.Diagnostics.Debug.WriteLine($"使用config命令设置服务描述失败: {descriptionResult.Message}");
                    
                    // 尝试使用description命令作为备用
                    await Task.Delay(500);
                    var altDescriptionResult = await ExecuteScCommandAsync(
                        $"description \"{SimpleAutoCameraControlService.ServiceName}\" \"{SimpleAutoCameraControlService.ServiceDescription}\"");
                    
                    if (!altDescriptionResult.Success)
                    {
                        System.Diagnostics.Debug.WriteLine($"使用description命令设置服务描述也失败: {altDescriptionResult.Message}");
                    }
                }
                
                // 配置服务恢复选项
                await ConfigureServiceRecoveryWithScAsync();
                
                // 配置服务依赖项（如果需要）
                await ConfigureServiceDependenciesAsync();
                
                return new ServiceOperationResult
                {
                    Success = true,
                    Message = string.Format(Properties.Resources.Log_AdvancedInstaller_InstallSuccess,
                        SimpleAutoCameraControlService.ServiceName,
                        SimpleAutoCameraControlService.ServiceDisplayName,
                        servicePath)
                };
            }
            catch (Exception ex)
            {
                return new ServiceOperationResult
                {
                    Success = false,
                    Message = string.Format(Properties.Resources.Log_AdvancedInstaller_InstallFailed, ex.Message),
                    ErrorDetails = ex.ToString()
                };
            }
        }
        
        /// <summary>
        /// 使用SC命令卸载服务
        /// </summary>
        /// <returns>卸载结果</returns>
        public static async Task<ServiceOperationResult> UninstallServiceWithScAsync()
        {
            try
            {
                // 检查管理员权限
                if (!ServiceManager.IsRunningAsAdministrator())
                {
                    return new ServiceOperationResult
                    {
                        Success = false,
                        Message = Properties.Resources.Log_AdvancedInstaller_UninstallAdminRequired
                    };
                }
                
                // 检查服务是否存在
                if (!ServiceManager.IsServiceInstalled())
                {
                    return new ServiceOperationResult
                    {
                        Success = false,
                        Message = Properties.Resources.Log_AdvancedInstaller_NotInstalled
                    };
                }
                
                // 先停止服务
                var stopResult = await ServiceManager.StopServiceAsync();
                if (!stopResult.Success && !stopResult.Message.Contains(Properties.Resources.Log_ServiceManager_AlreadyStopped))
                {
                    // 强制停止服务进程
                    await ForceStopServiceProcessAsync();
                }
                
                // 等待服务完全停止
                await Task.Delay(3000);
                
                // 使用SC命令删除服务
                var deleteResult = await ExecuteScCommandAsync($"delete \"{SimpleAutoCameraControlService.ServiceName}\"");
                
                if (!deleteResult.Success)
                {
                    return deleteResult;
                }
                
                return new ServiceOperationResult
                {
                    Success = true,
                    Message = Properties.Resources.Log_AdvancedInstaller_UninstallSuccess
                };
            }
            catch (Exception ex)
            {
                return new ServiceOperationResult
                {
                    Success = false,
                    Message = string.Format(Properties.Resources.Log_AdvancedInstaller_UninstallFailed, ex.Message),
                    ErrorDetails = ex.ToString()
                };
            }
        }
        
        /// <summary>
        /// 执行SC命令
        /// </summary>
        /// <param name="arguments">命令参数</param>
        /// <returns>执行结果</returns>
        private static async Task<ServiceOperationResult> ExecuteScCommandAsync(string arguments)
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = "sc.exe",
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    Verb = "runas" // 以管理员权限运行
                };
                
                using var process = Process.Start(processInfo);
                if (process == null)
                {
                    return new ServiceOperationResult
                    {
                        Success = false,
                        Message = Properties.Resources.Log_AdvancedInstaller_CannotStartProcess
                    };
                }
                
                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                
                await Task.Run(() => process.WaitForExit(30000)); // 最多等待30秒
                
                // 添加调试输出
                System.Diagnostics.Debug.WriteLine($"SC命令: sc.exe {arguments}");
                System.Diagnostics.Debug.WriteLine($"退出代码: {process.ExitCode}");
                System.Diagnostics.Debug.WriteLine($"标准输出: {output}");
                System.Diagnostics.Debug.WriteLine($"错误输出: {error}");
                
                if (process.ExitCode == 0)
                {
                    return new ServiceOperationResult
                    {
                        Success = true,
                        Message = Properties.Resources.Log_AdvancedInstaller_ScCommandSuccess,
                        ErrorDetails = output // 将输出信息存储在ErrorDetails中以便调试
                    };
                }
                else
                {
                    var errorMessage = !string.IsNullOrEmpty(error) ? error : output;
                    return new ServiceOperationResult
                    {
                        Success = false,
                        Message = string.Format(Properties.Resources.Log_AdvancedInstaller_ScCommandFailed, process.ExitCode, errorMessage),
                        ErrorDetails = $"命令: sc.exe {arguments}\n输出: {output}\n错误: {error}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ServiceOperationResult
                {
                    Success = false,
                    Message = string.Format(Properties.Resources.Log_AdvancedInstaller_ScCommandException, ex.Message),
                    ErrorDetails = ex.ToString()
                };
            }
        }
        
        /// <summary>
        /// 配置服务恢复选项
        /// </summary>
        private static async Task ConfigureServiceRecoveryWithScAsync()
        {
            try
            {
                // 配置服务失败时不自动重启
                // 失败了就失败了，不进行任何自动恢复动作
                await ExecuteScCommandAsync(
                    $"failure \"{SimpleAutoCameraControlService.ServiceName}\" " +
                    "reset= 86400 " + // 24小时后重置失败计数
                    "actions= none/0/none/0/none/0"); // 失败后不执行任何动作
            }
            catch
            {
                // 恢复选项配置失败不应该影响服务安装
            }
        }
        
        /// <summary>
        /// 配置服务依赖项
        /// </summary>
        private static async Task ConfigureServiceDependenciesAsync()
        {
            try
            {
                // 设置服务依赖项（如果需要）
                // 例如：依赖于Windows Management Instrumentation服务
                // await ExecuteScCommandAsync(
                //     $"config \"{SimpleAutoCameraControlService.ServiceName}\" depend= Winmgmt");
                
                // 这里可以根据需要添加其他依赖项
                await Task.CompletedTask;
            }
            catch
            {
                // 依赖项配置失败不应该影响服务安装
            }
        }
        
        /// <summary>
        /// 强制停止服务进程
        /// </summary>
        private static async Task ForceStopServiceProcessAsync()
        {
            try
            {
                await Task.Run(() =>
                {
                    var processes = Process.GetProcessesByName("WindowsHelloIRHelper");
                    foreach (var process in processes)
                    {
                        try
                        {
                            if (!process.HasExited)
                            {
                                process.Kill();
                                process.WaitForExit(5000);
                            }
                        }
                        catch
                        {
                            // 忽略单个进程终止失败
                        }
                        finally
                        {
                            process.Dispose();
                        }
                    }
                });
            }
            catch
            {
                // 强制停止失败不应该阻止卸载过程
            }
        }
        
        /// <summary>
        /// 验证服务安装
        /// </summary>
        /// <returns>验证结果</returns>
        public static async Task<ServiceOperationResult> ValidateServiceInstallationAsync()
        {
            try
            {
                if (!ServiceManager.IsServiceInstalled())
                {
                    return new ServiceOperationResult
                    {
                        Success = false,
                        Message = Properties.Resources.Log_AdvancedInstaller_ValidationFailed
                    };
                }
                
                // 检查服务配置
                using var serviceController = new ServiceController(SimpleAutoCameraControlService.ServiceName);
                serviceController.Refresh();
                
                var validationMessages = new List<string>();
                
                // 检查服务状态
                validationMessages.Add(string.Format(Properties.Resources.Log_AdvancedInstaller_ServiceStatus, GetServiceStatusDescription(serviceController.Status)));
                
                // 检查启动类型
                var startType = await GetServiceStartTypeAsync();
                validationMessages.Add(string.Format(Properties.Resources.Log_AdvancedInstaller_StartupType, GetStartTypeDescription(startType)));
                
                // 检查服务账户
                var serviceAccount = await GetServiceAccountAsync();
                validationMessages.Add(string.Format(Properties.Resources.Log_AdvancedInstaller_RunAccount, serviceAccount));
                
                // 检查服务路径
                var servicePath = await GetServicePathAsync();
                validationMessages.Add(string.Format(Properties.Resources.Log_AdvancedInstaller_ServicePath, servicePath));
                
                // 检查文件是否存在
                if (!string.IsNullOrEmpty(servicePath) && File.Exists(servicePath))
                {
                    validationMessages.Add(Properties.Resources.Log_AdvancedInstaller_FileExists);
                }
                else
                {
                    validationMessages.Add(Properties.Resources.Log_AdvancedInstaller_FileNotExists);
                }
                
                return new ServiceOperationResult
                {
                    Success = true,
                    Message = string.Format(Properties.Resources.Log_AdvancedInstaller_ValidationSuccess, string.Join("\n", validationMessages))
                };
            }
            catch (Exception ex)
            {
                return new ServiceOperationResult
                {
                    Success = false,
                    Message = string.Format(Properties.Resources.Log_AdvancedInstaller_ValidationException, ex.Message),
                    ErrorDetails = ex.ToString()
                };
            }
        }
        
        /// <summary>
        /// 获取服务启动类型
        /// </summary>
        /// <returns>启动类型</returns>
        private static async Task<CustomServiceStartMode> GetServiceStartTypeAsync()
        {
            try
            {
                return await Task.Run(() =>
                {
                    using var process = new Process();
                    process.StartInfo.FileName = "sc.exe";
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
                });
            }
            catch
            {
                return CustomServiceStartMode.Manual;
            }
        }
        
        /// <summary>
        /// 获取服务账户
        /// </summary>
        /// <returns>服务账户</returns>
        private static async Task<string> GetServiceAccountAsync()
        {
            try
            {
                return await Task.Run(() =>
                {
                    using var searcher = new ManagementObjectSearcher(
                        $"SELECT StartName FROM Win32_Service WHERE Name='{SimpleAutoCameraControlService.ServiceName}'");
                    
                    foreach (ManagementObject service in searcher.Get())
                    {
                        return service["StartName"]?.ToString() ?? Properties.Resources.Log_AdvancedInstaller_UnknownValue;
                    }
                    
                    return Properties.Resources.Log_AdvancedInstaller_UnknownValue;
                });
            }
            catch
            {
                return Properties.Resources.Log_AdvancedInstaller_UnknownValue;
            }
        }
        
        /// <summary>
        /// 获取服务路径
        /// </summary>
        /// <returns>服务路径</returns>
        private static async Task<string> GetServicePathAsync()
        {
            try
            {
                return await Task.Run(() =>
                {
                    using var searcher = new ManagementObjectSearcher(
                        $"SELECT PathName FROM Win32_Service WHERE Name='{SimpleAutoCameraControlService.ServiceName}'");
                    
                    foreach (ManagementObject service in searcher.Get())
                    {
                        var pathName = service["PathName"]?.ToString();
                        if (!string.IsNullOrEmpty(pathName))
                        {
                            // 移除命令行参数，只保留可执行文件路径
                            var parts = pathName.Split(new[] { " --service" }, StringSplitOptions.None);
                            return parts[0].Trim('"');
                        }
                    }
                    
                    return Properties.Resources.Log_AdvancedInstaller_UnknownValue;
                });
            }
            catch
            {
                return Properties.Resources.Log_AdvancedInstaller_UnknownValue;
            }
        }
        
        /// <summary>
        /// 获取服务状态描述
        /// </summary>
        /// <param name="status">服务状态</param>
        /// <returns>状态描述</returns>
        private static string GetServiceStatusDescription(ServiceControllerStatus status)
        {
            return status switch
            {
                ServiceControllerStatus.Running => Properties.Resources.Log_AdvancedInstaller_StatusRunning,
                ServiceControllerStatus.Stopped => Properties.Resources.Log_AdvancedInstaller_StatusStopped,
                ServiceControllerStatus.Paused => Properties.Resources.Log_AdvancedInstaller_StatusPaused,
                ServiceControllerStatus.StartPending => Properties.Resources.Log_AdvancedInstaller_StatusStartPending,
                ServiceControllerStatus.StopPending => Properties.Resources.Log_AdvancedInstaller_StatusStopPending,
                ServiceControllerStatus.PausePending => Properties.Resources.Log_AdvancedInstaller_StatusPausePending,
                ServiceControllerStatus.ContinuePending => Properties.Resources.Log_AdvancedInstaller_StatusContinuePending,
                _ => Properties.Resources.Log_AdvancedInstaller_StatusUnknown
            };
        }
        
        /// <summary>
        /// 获取启动类型描述
        /// </summary>
        /// <param name="startMode">启动模式</param>
        /// <returns>启动类型描述</returns>
        private static string GetStartTypeDescription(CustomServiceStartMode startMode)
        {
            return startMode switch
            {
                CustomServiceStartMode.Automatic => Properties.Resources.Log_AdvancedInstaller_StartTypeAutomatic,
                CustomServiceStartMode.Manual => Properties.Resources.Log_AdvancedInstaller_StartTypeManual,
                CustomServiceStartMode.Disabled => Properties.Resources.Log_AdvancedInstaller_StartTypeDisabled,
                _ => Properties.Resources.Log_AdvancedInstaller_StartTypeUnknown
            };
        }
        
        /// <summary>
        /// 测试SC命令描述语法
        /// </summary>
        /// <returns>测试结果</returns>
        public static async Task<string> TestScDescriptionSyntaxAsync()
        {
            if (!ServiceManager.IsServiceInstalled())
            {
                return Properties.Resources.Log_AdvancedInstaller_ServiceNotInstalledCannotTest;
            }
            
            var results = new List<string>();
            
            // 测试方法1: 标准语法
            try
            {
                var result1 = await ExecuteScCommandAsync(
                    $"description \"{SimpleAutoCameraControlService.ServiceName}\" \"测试描述1\"");
                var status1 = result1.Success ? Properties.Resources.Log_AdvancedInstaller_TestSuccess : Properties.Resources.Log_AdvancedInstaller_TestFailed;
                results.Add(string.Format(Properties.Resources.Log_AdvancedInstaller_TestMethod1, status1, result1.Message));
            }
            catch (Exception ex)
            {
                results.Add(string.Format(Properties.Resources.Log_AdvancedInstaller_TestMethodException, "1", ex.Message));
            }
            
            await Task.Delay(1000);
            
            // 测试方法2: 不加引号
            try
            {
                var result2 = await ExecuteScCommandAsync(
                    $"description \"{SimpleAutoCameraControlService.ServiceName}\" 测试描述2");
                var status2 = result2.Success ? Properties.Resources.Log_AdvancedInstaller_TestSuccess : Properties.Resources.Log_AdvancedInstaller_TestFailed;
                results.Add(string.Format(Properties.Resources.Log_AdvancedInstaller_TestMethod2, status2, result2.Message));
            }
            catch (Exception ex)
            {
                results.Add(string.Format(Properties.Resources.Log_AdvancedInstaller_TestMethodException, "2", ex.Message));
            }
            
            await Task.Delay(1000);
            
            // 测试方法3: 使用config命令
            try
            {
                var result3 = await ExecuteScCommandAsync(
                    $"config \"{SimpleAutoCameraControlService.ServiceName}\" description= \"测试描述3\"");
                var status3 = result3.Success ? Properties.Resources.Log_AdvancedInstaller_TestSuccess : Properties.Resources.Log_AdvancedInstaller_TestFailed;
                results.Add(string.Format(Properties.Resources.Log_AdvancedInstaller_TestMethod3, status3, result3.Message));
            }
            catch (Exception ex)
            {
                results.Add(string.Format(Properties.Resources.Log_AdvancedInstaller_TestMethodException, "3", ex.Message));
            }
            
            return string.Join("\n", results);
        }
    }
}