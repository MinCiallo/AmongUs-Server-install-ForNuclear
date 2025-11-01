using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;

class AmongUsResourceUpdater
{
    // 程序当前版本（仅在主界面显示）
    private const string CurrentVersion = "1.0.3";
    // 标题文本（不含版本号）
    private const string TitleText = "核电服私服安装工具箱";
    // 主界面显示文本（含版本号）
    private const string MainInterfaceTitle = "核电服安装工具箱";

    // 新增：公告功能相关定义
    private const string AnnouncementText = "【更新公告】1选项，核电+清风源添加了核电服的备用服务器"; // 公告内容，可直接修改
    private static readonly ConsoleColor AnnouncementColor = ConsoleColor.Blue; // 公告文字颜色（蓝色，醒目易识别）
    private const bool ShowAnnouncement = true; // 公告开关：true=显示，false=隐藏

    // 版本检查服务器地址
    private const string VersionCheckUrl = "https://pe.aunpp.cn/src/updater/version.json";
    private const string UpdateDownloadUrl = "https://pe.aunpp.cn/src/updater/核电服私服安装工具箱.exe";

    // 基础颜色定义
    private static readonly ConsoleColor TitleColor = ConsoleColor.Cyan;
    private static readonly ConsoleColor MenuColor = ConsoleColor.Green;
    private static readonly ConsoleColor HighlightColor = ConsoleColor.Yellow;
    private static readonly ConsoleColor ErrorColor = ConsoleColor.Red;
    private static readonly ConsoleColor SuccessColor = ConsoleColor.Green;
    private static readonly ConsoleColor InfoColor = ConsoleColor.Gray;
    private static bool _supportsColor;

    static void Main()
    {
        _supportsColor = CheckColorSupport();
        // 标题仅显示名称（不含版本号）
        Console.Title = TitleText;
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        bool continueRunning = true;

        // 启动时自动检查更新
        AutoCheckForUpdates();

        while (continueRunning)
        {
            Console.Clear();
            DrawTitle(); // 主界面显示带版本号的标题 + 公告
            ShowMainMenu();

            string input = Console.ReadLine()?.Trim().ToLower();
            switch (input)
            {
                case "1":
                case "2":
                case "3":
                    RunServerInstall(int.Parse(input) - 1);
                    break;
                case "4":
                    OpenOfficialWebsite();
                    break;
                case "5":
                    CheckForUpdatesManually();
                    break;
                case "6":
                    // 选择退出时直接关闭窗口
                    Console.Clear();
                    WriteWithColor("程序已退出，正在关闭窗口...\n", SuccessColor);
                    // 短暂延迟让用户看到提示后关闭
                    Thread.Sleep(500);
                    Environment.Exit(0); // 强制退出程序
                    break;
                default:
                    WriteWithColor("无效的选项，请重新输入\n", ErrorColor);
                    Thread.Sleep(1000);
                    break;
            }
        }
    }

    // 自动检查更新
    static void AutoCheckForUpdates()
    {
        WriteWithColor("正在检查程序更新...", InfoColor);
        try
        {
            using (WebClient client = new WebClient())
            {
                string versionInfo = client.DownloadString(VersionCheckUrl);
                string latestVersion = versionInfo.Trim();

                if (IsNewVersionAvailable(latestVersion))
                {
                    Console.WriteLine();
                    WriteWithColor($"\n发现新版本 {latestVersion}！当前版本 {CurrentVersion}\n", HighlightColor);
                    WriteWithColor("是否更新？(y/n) ", InfoColor);

                    if (Console.ReadLine()?.Trim().ToLower() == "y")
                    {
                        DownloadAndUpdate();
                    }
                }
                else
                {
                    Console.WriteLine(" 当前已是最新版本");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(" 检查更新失败");
            WriteWithColor($"更新检查错误: {ex.Message}\n", ErrorColor);
        }
        Thread.Sleep(1000);
    }

    // 手动检查更新
    static void CheckForUpdatesManually()
    {
        Console.Clear();
        DrawTitle();
        WriteWithColor("正在手动检查更新...\n", InfoColor);

        try
        {
            using (WebClient client = new WebClient())
            {
                string versionInfo = client.DownloadString(VersionCheckUrl);
                string latestVersion = versionInfo.Trim();

                if (IsNewVersionAvailable(latestVersion))
                {
                    WriteWithColor($"\n发现新版本 {latestVersion}！当前版本 {CurrentVersion}\n", HighlightColor);
                    WriteWithColor("是否更新？(y/n) ", InfoColor);

                    if (Console.ReadLine()?.Trim().ToLower() == "y")
                    {
                        DownloadAndUpdate();
                    }
                }
                else
                {
                    WriteWithColor("当前已是最新版本，无需更新\n", SuccessColor);
                }
            }
        }
        catch (Exception ex)
        {
            WriteWithColor($"更新检查错误: {ex.Message}\n", ErrorColor);
        }

        WriteWithColor("\n按任意键返回主菜单...", InfoColor);
        Console.ReadKey();
    }

    // 版本比较
    static bool IsNewVersionAvailable(string latestVersion)
    {
        string[] currentParts = CurrentVersion.Split('.');
        string[] latestParts = latestVersion.Split('.');

        for (int i = 0; i < Math.Max(currentParts.Length, latestParts.Length); i++)
        {
            int current = i < currentParts.Length ? int.Parse(currentParts[i]) : 0;
            int latest = i < latestParts.Length ? int.Parse(latestParts[i]) : 0;

            if (latest > current) return true;
            if (latest < current) return false;
        }
        return false;
    }

    // 下载并更新程序（保持服务器原文件名）
    static void DownloadAndUpdate()
    {
        try
        {
            // 从下载URL中提取原始文件名
            string fileName = Path.GetFileName(UpdateDownloadUrl);
            // 桌面路径 + 原始文件名（不重命名）
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string updateFilePath = Path.Combine(desktopPath, fileName);

            // 如果桌面已有同名文件，先删除避免冲突
            if (File.Exists(updateFilePath))
            {
                File.Delete(updateFilePath);
            }

            WriteWithColor($"\n开始下载更新...（文件名将保持为：{fileName}）\n", InfoColor);

            using (WebClient client = new WebClient())
            {
                client.DownloadProgressChanged += (s, e) =>
                {
                    Console.Write($"\r下载进度: {e.ProgressPercentage}%");
                };
                client.DownloadFileCompleted += (s, e) =>
                {
                    Console.WriteLine($"\r下载完成！文件已保存到桌面（{fileName}）");
                };
                client.DownloadFile(UpdateDownloadUrl, updateFilePath);
            }

            // 启动更新程序并退出当前程序
            Process.Start(new ProcessStartInfo(updateFilePath) { UseShellExecute = true });
            WriteWithColor($"\n更新程序（{fileName}）已启动，正在安装新版本...\n", SuccessColor);
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            WriteWithColor($"\n更新失败: {ex.Message}\n", ErrorColor);
        }
    }

    // 显示主菜单
    static void ShowMainMenu()
    {
        var fileOptions = new[] {
            "核电服+清风服",
            "帆船服",
            "碧水港服",
            "打开官方网站",
            "检查程序更新",
            "退出程序"
        };

        WriteWithColor("\n+" + new string('-', 48) + "+\n", MenuColor);
        WriteWithColor("|                  请选择操作指令                  |\n", MenuColor);
        WriteWithColor("+" + new string('-', 48) + "+\n", MenuColor);

        for (int i = 0; i < fileOptions.Length; i++)
        {
            WriteWithColor("| ", MenuColor);
            WriteWithColor($"{i + 1}. ", HighlightColor);
            WriteWithColor($"{fileOptions[i],-38}", InfoColor);
            WriteWithColor("|\n", MenuColor);
        }
        WriteWithColor("+" + new string('-', 48) + "+\n", MenuColor);
        WriteWithColor($"\n请输入选项编号（1-{fileOptions.Length}）>> ", InfoColor);
    }

    // 运行服务器安装
    static void RunServerInstall(int index)
    {
        var fileOptions = new[] {
            new { DisplayName = "核电服+清风服", Url = "https://pe.aunpp.cn/src/serverlist.json" },
            new { DisplayName = "帆船服", Url = "https://pe.aunpp.cn/src/fanchuanserver.json" },
            new { DisplayName = "碧水港服", Url = "https://pe.aunpp.cn/src/bishuigang.json" }
        };

        try
        {
            var selectedOption = fileOptions[index];
            WriteWithColor("\n你选择了：", InfoColor);
            WriteWithColor($"{selectedOption.DisplayName}\n", HighlightColor);
            Console.WriteLine(new string('-', 50));

            string targetDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                @"AppData\LocalLow\Innersloth\Among Us");
            string targetFile = Path.Combine(targetDir, "regionInfo.json");
            string tempFile = Path.Combine(targetDir, "temp.tmp");

            WriteWithColor("开始下载文件... ", InfoColor);
            ShowProgressIndicator();

            using (WebClient client = new WebClient())
            {
                client.DownloadFile(selectedOption.Url, tempFile);
            }

            Directory.CreateDirectory(targetDir);
            if (File.Exists(targetFile)) File.Delete(targetFile);
            File.Move(tempFile, targetFile);

            Console.Write("\r");
            WriteWithColor($"✓ 安装完成：已成功安装 {selectedOption.DisplayName}\n", SuccessColor);
        }
        catch (Exception ex)
        {
            Console.Write("\r");
            WriteWithColor($"✗ 错误：{ex.Message}\n", ErrorColor);
        }

        WriteWithColor("\n按任意键返回主菜单...", InfoColor);
        Console.ReadKey();
    }

    // 打开官方网站
    static void OpenOfficialWebsite()
    {
        try
        {
            WriteWithColor("\n正在打开网址... ", InfoColor);
            ShowProgressIndicator();

            Process.Start(new ProcessStartInfo("https://ciallo.aunpp.cn") { UseShellExecute = true });
            Console.Write("\r");
            WriteWithColor($"✓ 网址已打开\n", SuccessColor);
        }
        catch (Exception ex)
        {
            Console.Write("\r");
            WriteWithColor($"✗ 错误：{ex.Message}\n", ErrorColor);
        }

        WriteWithColor("\n按任意键返回主菜单...", InfoColor);
        Console.ReadKey();
    }

    // 绘制标题（主界面显示带版本号的标题 + 公告）
    static void DrawTitle()
    {
        // 主界面标题 = "核电服安装工具箱" + 版本号
        string title = $"{MainInterfaceTitle}{CurrentVersion}";
        string border = new string('=', title.Length + 4);

        WriteWithColor($"{border}\n", TitleColor);
        WriteWithColor($"  {title}  \n", TitleColor);
        WriteWithColor($"{border}\n", TitleColor);

        // 新增：显示公告（仅当ShowAnnouncement为true且公告内容不为空时）
        if (ShowAnnouncement && !string.IsNullOrEmpty(AnnouncementText))
        {
            WriteWithColor($"  {AnnouncementText}\n", AnnouncementColor);
            WriteWithColor($"{new string('-', title.Length + 4)}\n", TitleColor); // 公告下方分隔线，区分标题与菜单
        }
    }

    // 显示加载动画
    static void ShowLoading(string message, int duration)
    {
        WriteWithColor($"{message} ", InfoColor);
        DateTime start = DateTime.Now;
        while (DateTime.Now - start < TimeSpan.FromMilliseconds(duration))
        {
            foreach (char c in new[] { '/', '-', '\\', '|' })
            {
                Console.Write($"\r{message} {c}");
                Thread.Sleep(100);
            }
        }
        Console.WriteLine();
    }

    // 显示进度指示器
    static void ShowProgressIndicator()
    {
        for (int i = 0; i < 3; i++)
        {
            Console.Write(".");
            Thread.Sleep(300);
        }
    }

    // 带颜色输出
    static void WriteWithColor(string text, ConsoleColor color)
    {
        if (_supportsColor)
        {
            Console.ForegroundColor = color;
            Console.Write(text);
            Console.ForegroundColor = InfoColor;
        }
        else
        {
            Console.Write(text);
        }
    }

    // 检测控制台是否支持颜色
    static bool CheckColorSupport()
    {
        try
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.ForegroundColor = ConsoleColor.Gray;
            return true;
        }
        catch
        {
            return false;
        }
    }
}