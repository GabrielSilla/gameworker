using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;

namespace GamingWorkerService
{
    public class ProcessLauncher
    {
        // Declarações da API do Windows para privilégios
        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, out LUID lpLuid);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, bool DisableAllPrivileges, ref TOKEN_PRIVILEGES NewState, uint BufferLength, IntPtr PreviousState, IntPtr ReturnLength);

        // Declarações da API para CreateProcessAsUser (existentes)
        [DllImport("wtsapi32.dll", SetLastError = true)]
        private static extern bool WTSQueryUserToken(uint sessionId, out IntPtr token);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool DuplicateTokenEx(IntPtr hExistingToken, uint dwDesiredAccess, IntPtr lpTokenAttributes, int impersonationLevel, int tokenType, out IntPtr hNewToken);

        [DllImport("userenv.dll", SetLastError = true)]
        private static extern bool CreateEnvironmentBlock(out IntPtr lpEnvironment, IntPtr hToken, bool bInherit);

        [DllImport("userenv.dll", SetLastError = true)]
        private static extern bool DestroyEnvironmentBlock(IntPtr lpEnvironment);

        [DllImport("kernel32.dll")]
        private static extern uint WTSGetActiveConsoleSessionId();

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool CreateProcessAsUser(IntPtr hToken, string lpApplicationName, string lpCommandLine, IntPtr lpProcessAttributes, IntPtr lpThreadAttributes, bool bInheritHandles, uint dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory, ref STARTUPINFO lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);

        // Declarações da API para gerenciamento de janela
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll")]
        private static extern uint GetLastError();

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        // Constantes e estruturas de dados
        private const int TOKEN_DUPLICATE = 0x0002;
        private const int TOKEN_ASSIGN_PRIMARY = 0x0001;
        private const int TOKEN_QUERY = 0x0008;
        private const int CREATE_UNICODE_ENVIRONMENT = 0x00000400;
        private const int STARTF_USESHOWWINDOW = 0x00000001;
        private const int SW_SHOW = 5;
        private const int SECURITY_IMPERSONATION = 2;
        private const int TOKEN_TYPE_PRIMARY = 1;
        private const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
        private const uint TOKEN_QUERY_PRIVILEGES = 0x0008;
        private const uint SE_PRIVILEGE_ENABLED = 0x00000002;
        private const int SW_RESTORE = 9;

        [StructLayout(LayoutKind.Sequential)]
        private struct LUID
        {
            public uint LowPart;
            public int HighPart;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct LUID_AND_ATTRIBUTES
        {
            public LUID Luid;
            public uint Attributes;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TOKEN_PRIVILEGES
        {
            public uint PrivilegeCount;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
            public LUID_AND_ATTRIBUTES[] Privileges;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct STARTUPINFO
        {
            public int cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public int dwX;
            public int dwY;
            public int dwXSize;
            public int dwYSize;
            public int dwXCountChars;
            public int dwYCountChars;
            public int dwFillAttribute;
            public int dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public uint dwProcessId;
            public uint dwThreadId;
        }

        private static bool EnablePrivileges(string privilegeName)
        {
            IntPtr processToken = IntPtr.Zero;
            try
            {
                if (!OpenProcessToken(Process.GetCurrentProcess().Handle, TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY_PRIVILEGES, out processToken))
                {
                    return false;
                }

                LUID luid;
                if (!LookupPrivilegeValue(null, privilegeName, out luid))
                {
                    return false;
                }

                TOKEN_PRIVILEGES tp = new TOKEN_PRIVILEGES();
                tp.PrivilegeCount = 1;
                tp.Privileges = new LUID_AND_ATTRIBUTES[1];
                tp.Privileges[0].Luid = luid;
                tp.Privileges[0].Attributes = SE_PRIVILEGE_ENABLED;

                if (!AdjustTokenPrivileges(processToken, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero))
                {
                    return false;
                }
                return true;
            }
            finally
            {
                if (processToken != IntPtr.Zero) Marshal.FreeHGlobal(processToken);
            }
        }

        public static bool LaunchProcessAsUser(string applicationPath)
        {
            IntPtr userToken = IntPtr.Zero;
            IntPtr duplicatedToken = IntPtr.Zero;
            IntPtr environment = IntPtr.Zero;

            if (!EnablePrivileges("SeAssignPrimaryTokenPrivilege") || !EnablePrivileges("SeIncreaseQuotaPrivilege"))
            {
                return false;
            }

            uint sessionId = WTSGetActiveConsoleSessionId();
            if (sessionId == 0xFFFFFFFF)
            {
                return false;
            }

            try
            {
                if (!WTSQueryUserToken(sessionId, out userToken))
                {
                    return false;
                }

                uint desiredAccess = TOKEN_DUPLICATE | TOKEN_ASSIGN_PRIMARY | TOKEN_QUERY;
                if (!DuplicateTokenEx(userToken, desiredAccess, IntPtr.Zero, SECURITY_IMPERSONATION, TOKEN_TYPE_PRIMARY, out duplicatedToken))
                {
                    return false;
                }

                if (!CreateEnvironmentBlock(out environment, duplicatedToken, false))
                {
                    return false;
                }

                var startupInfo = new STARTUPINFO();
                startupInfo.cb = Marshal.SizeOf(startupInfo);
                startupInfo.dwFlags = STARTF_USESHOWWINDOW;
                startupInfo.wShowWindow = SW_RESTORE;
                startupInfo.lpDesktop = "winsta0\\default";

                var processInfo = new PROCESS_INFORMATION();
                string workingDir = System.IO.Path.GetDirectoryName(applicationPath);

                const uint CREATE_NEW_CONSOLE = 0x00000010;

                bool result = CreateProcessAsUser(
                    duplicatedToken,
                    null,
                    $"\"{applicationPath}\"",
                    IntPtr.Zero,
                    IntPtr.Zero,
                    false,
                    CREATE_UNICODE_ENVIRONMENT | CREATE_NEW_CONSOLE,
                    environment,
                    workingDir,
                    ref startupInfo,
                    out processInfo);

                if (result)
                {
                    Thread.Sleep(2000);
                    IntPtr gameWindow = IntPtr.Zero;
                    EnumWindows((hWnd, lParam) =>
                    {
                        uint windowProcessId;
                        GetWindowThreadProcessId(hWnd, out windowProcessId);
                        if (windowProcessId == processInfo.dwProcessId)
                        {
                            gameWindow = hWnd;
                            return false;
                        }
                        return true;
                    }, IntPtr.Zero);

                    if (gameWindow != IntPtr.Zero)
                    {
                        ShowWindow(gameWindow, SW_RESTORE);
                        SetForegroundWindow(gameWindow);
                    }
                }

                return result;
            }
            finally
            {
                if (userToken != IntPtr.Zero) CloseHandle(userToken);
                if (duplicatedToken != IntPtr.Zero) CloseHandle(duplicatedToken);
                if (environment != IntPtr.Zero) DestroyEnvironmentBlock(environment);
            }
        }
    }
}
