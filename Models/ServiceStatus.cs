using System;
using System.ServiceProcess;

namespace WindowsHelloIRHelper.Models
{
    /// <summary>
    /// 服务状态信息
    /// </summary>
    public class ServiceStatus
    {
        /// <summary>
        /// 是否已安装
        /// </summary>
        public bool IsInstalled { get; set; }

        /// <summary>
        /// 是否有管理员权限
        /// </summary>
        public bool HasAdminRights { get; set; }

        /// <summary>
        /// 服务状态
        /// </summary>
        public ServiceControllerStatus? Status { get; set; }

        /// <summary>
        /// 是否可以启动
        /// </summary>
        public bool CanStart { get; set; }

        /// <summary>
        /// 是否可以停止
        /// </summary>
        public bool CanStop { get; set; }

        /// <summary>
        /// 是否可以暂停
        /// </summary>
        public bool CanPause { get; set; }

        /// <summary>
        /// 启动类型
        /// </summary>
        public CustomServiceStartMode? StartType { get; set; }

        /// <summary>
        /// 是否自动启动
        /// </summary>
        public bool IsAutoStart { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// 服务启动模式
    /// </summary>
    public enum CustomServiceStartMode
    {
        /// <summary>
        /// 自动启动
        /// </summary>
        Automatic,

        /// <summary>
        /// 手动启动
        /// </summary>
        Manual,

        /// <summary>
        /// 禁用
        /// </summary>
        Disabled
    }
}