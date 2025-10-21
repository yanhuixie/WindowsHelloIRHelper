using System.ServiceProcess;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.EventLog;
using WindowsHelloIRHelper.Services;
using WindowsHelloIRHelper.Interfaces;
using WindowsHelloIRHelper.Constants;
using WindowsHelloIRHelper.Utils;

namespace WindowsHelloIRHelper
{
    internal static class Program
    {
        /// <summary>
        /// 应用程序的主入口点
        /// 支持Windows Forms UI模式和Windows服务模式
        /// </summary>
        /// <param name="args">命令行参数</param>
        [STAThread]
        static void Main(string[] args)
        {
            // 初始化语言文化设置
            CultureHelper.InitializeCulture();
            
            // 检查是否以服务模式运行
            if (args.Length > 0 && args[0].Equals("--service", StringComparison.OrdinalIgnoreCase))
            {
                // 以传统 ServiceBase 模式运行（保留用于兼容）
                RunAsServiceBase();
            }
            else
            {
                // 以Windows Forms UI模式运行
                RunAsApplication();
            }
        }

        /// <summary>
        /// 以传统 ServiceBase 模式运行（保留用于兼容）
        /// </summary>
        private static void RunAsServiceBase()
        {
            try
            {
                // 创建服务提供者
                var services = new ServiceCollection();
                ConfigureServices(services);
                var serviceProvider = services.BuildServiceProvider();
                
                var servicesToRun = new ServiceBase[]
                {
                    serviceProvider.GetRequiredService<SimpleAutoCameraControlService>()
                };
                
                ServiceBase.Run(servicesToRun);
            }
            catch (Exception ex)
            {
                // 服务模式下的异常处理
                LogServiceStartupError(ex).GetAwaiter().GetResult();
                
                throw;
            }
        }
        
        /// <summary>
        /// 记录服务启动错误
        /// </summary>
        /// <param name="exception">异常信息</param>
        /// <returns>异步任务</returns>
        private static async Task LogServiceStartupError(Exception exception)
        {
            try
            {
                // 创建服务提供者以获取 IEventLogger 实例
                var services = new ServiceCollection();
                services.AddSingleton<IEventLogger, EventLogger>();
                var serviceProvider = services.BuildServiceProvider();
                
                var eventLogger = serviceProvider.GetRequiredService<IEventLogger>();
                await eventLogger.InitializeAsync();
                
                // 使用正确的事件ID记录服务启动失败
                await eventLogger.LogErrorAsync(EventIds.ServiceException,
                    $"服务启动失败: {exception.Message}", exception);
            }
            catch
            {
                // 如果日志记录失败，尝试使用原始方法作为最后手段
                try
                {
                    using var eventLog = new System.Diagnostics.EventLog("Application");
                    eventLog.Source = "AutoCameraControlService";
                    eventLog.WriteEntry($"服务启动失败: {exception.Message}",
                        System.Diagnostics.EventLogEntryType.Error);
                }
                catch
                {
                    // 如果连事件日志都无法写入，则无法做更多处理
                }
            }
        }
        
        /// <summary>
        /// 以Windows Forms应用程序模式运行
        /// </summary>
        private static void RunAsApplication()
        {
            try
            {
                // 配置应用程序
                ApplicationConfiguration.Initialize();
                
                // 配置依赖注入
                var services = new ServiceCollection();
                ConfigureServices(services);
                var serviceProvider = services.BuildServiceProvider();
                
                // 启动主窗体
                Application.Run(serviceProvider.GetRequiredService<Form1>());
            }
            catch (Exception ex)
            {
                // UI模式下的异常处理
                MessageBox.Show(
                    string.Format(Properties.Resources.Message_ApplicationStartupFailed, ex.Message),
                    Properties.Resources.Message_ApplicationStartupFailedTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        
        /// <summary>
        /// 配置依赖注入服务
        /// </summary>
        /// <param name="services">服务集合</param>
        private static void ConfigureServices(IServiceCollection services)
        {
            // System.Diagnostics.Debugger.Launch();
            // 注册事件日志记录器
            services.AddSingleton<IEventLogger, EventLogger>();
            
            // 注册安全助手
            services.AddSingleton<SecurityHelper>();
            
            // 注册全局异常处理器
            services.AddSingleton<GlobalExceptionHandler>();
            
            // 注册摄像头助手
            services.AddSingleton<SimpleCameraHelper>();
            
            // 注册配置助手
            services.AddSingleton<SimpleConfigHelper>();
            
            // 注册服务
            services.AddTransient<SimpleAutoCameraControlService>();
            
            // 注册窗体
            services.AddTransient<Form1>();
        }
        
    }
}