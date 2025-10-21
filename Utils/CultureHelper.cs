using System.Globalization;

namespace WindowsHelloIRHelper.Utils
{
    /// <summary>
    /// 语言文化辅助类
    /// 用于检测操作系统语言并初始化应用程序的语言设置
    /// </summary>
    public static class CultureHelper
    {
        /// <summary>
        /// 初始化应用程序的语言文化设置
        /// 根据操作系统语言自动选择中文或英文
        /// </summary>
        public static void InitializeCulture()
        {
            // 获取操作系统的当前UI语言
            var osLanguage = CultureInfo.CurrentUICulture.Name;
            
            // 根据操作系统语言设置应用语言
            CultureInfo culture;
            if (osLanguage.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
            {
                // 如果是中文系统，使用简体中文
                culture = new CultureInfo("zh-CN");
            }
            else
            {
                // 其他语言系统，使用英文
                culture = new CultureInfo("en");
            }
            
            // 设置当前线程的UI文化和文化
            Thread.CurrentThread.CurrentUICulture = culture;
            Thread.CurrentThread.CurrentCulture = culture;
            
            // 设置默认文化（用于新创建的线程）
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
        }
    }
}
