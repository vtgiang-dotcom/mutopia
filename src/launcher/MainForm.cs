using System.Diagnostics;
using System.Net.Sockets;
using System.Text.Json;

namespace MuLauncher;

public class MainForm : Form
{
    private readonly List<ServerConfig> _servers = new();
    private ServerConfig? _selectedServer;
    private readonly Panel _headerPanel = new();
    private readonly Label _titleLabel = new();
    private readonly Label _subtitleLabel = new();
    private readonly FlowLayoutPanel _serverListPanel = new();
    private readonly Panel _bottomPanel = new();
    private readonly Button _playButton = new();
    private readonly Button _webButton = new();
    private readonly Button _refreshButton = new();
    private readonly Label _statusLabel = new();
    private readonly System.Windows.Forms.Timer _pingTimer = new();

    public MainForm()
    {
        InitializeUi();
        LoadServers();
        StartPingTimer();
    }

    private void InitializeUi()
    {
        Text = "MU Online — Dual Realm Launcher (S6 & S16)";
        Size = new Size(680, 520);
        MinimumSize = new Size(600, 480);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(18, 24, 38);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

        // Header
        _headerPanel.Dock = DockStyle.Top;
        _headerPanel.Height = 85;
        _headerPanel.BackColor = Color.FromArgb(24, 32, 52);
        _headerPanel.Padding = new Padding(24, 16, 24, 16);

        _titleLabel.Text = "MU ONLINE — DUAL REALM";
        _titleLabel.Font = new Font("Segoe UI", 16f, FontStyle.Bold);
        _titleLabel.ForeColor = Color.FromArgb(245, 158, 11);
        _titleLabel.AutoSize = true;
        _titleLabel.Location = new Point(24, 14);

        _subtitleLabel.Text = "Chọn máy chủ bạn muốn tham gia: Season 6 Classic hoặc Season 16 Modern";
        _subtitleLabel.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
        _subtitleLabel.ForeColor = Color.FromArgb(156, 163, 175);
        _subtitleLabel.AutoSize = true;
        _subtitleLabel.Location = new Point(25, 48);

        _headerPanel.Controls.Add(_titleLabel);
        _headerPanel.Controls.Add(_subtitleLabel);

        // Server List Panel
        _serverListPanel.Dock = DockStyle.Fill;
        _serverListPanel.Padding = new Padding(20);
        _serverListPanel.AutoScroll = true;
        _serverListPanel.FlowDirection = FlowDirection.TopDown;
        _serverListPanel.WrapContents = false;

        // Bottom Panel
        _bottomPanel.Dock = DockStyle.Bottom;
        _bottomPanel.Height = 80;
        _bottomPanel.BackColor = Color.FromArgb(24, 32, 52);
        _bottomPanel.Padding = new Padding(24, 18, 24, 18);

        _statusLabel.Text = "Sẵn sàng khởi động game";
        _statusLabel.Font = new Font("Segoe UI", 9.5f, FontStyle.Italic);
        _statusLabel.ForeColor = Color.FromArgb(156, 163, 175);
        _statusLabel.AutoSize = true;
        _statusLabel.Location = new Point(24, 28);

        _playButton.Text = "VÀO GAME";
        _playButton.Size = new Size(150, 44);
        _playButton.Location = new Point(480, 18);
        _playButton.BackColor = Color.FromArgb(16, 185, 129);
        _playButton.ForeColor = Color.White;
        _playButton.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
        _playButton.FlatStyle = FlatStyle.Flat;
        _playButton.FlatAppearance.BorderSize = 0;
        _playButton.Cursor = Cursors.Hand;
        _playButton.Click += OnPlayClicked;

        _webButton.Text = "Web Portal";
        _webButton.Size = new Size(110, 44);
        _webButton.Location = new Point(355, 18);
        _webButton.BackColor = Color.FromArgb(37, 99, 235);
        _webButton.ForeColor = Color.White;
        _webButton.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        _webButton.FlatStyle = FlatStyle.Flat;
        _webButton.FlatAppearance.BorderSize = 0;
        _webButton.Cursor = Cursors.Hand;
        _webButton.Click += (s, e) => OpenWebPortal();

        _refreshButton.Text = "Làm mới";
        _refreshButton.Size = new Size(85, 44);
        _refreshButton.Location = new Point(255, 18);
        _refreshButton.BackColor = Color.FromArgb(55, 65, 81);
        _refreshButton.ForeColor = Color.White;
        _refreshButton.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
        _refreshButton.FlatStyle = FlatStyle.Flat;
        _refreshButton.FlatAppearance.BorderSize = 0;
        _refreshButton.Cursor = Cursors.Hand;
        _refreshButton.Click += async (s, e) => await CheckAllServersPingAsync();

        _bottomPanel.Controls.Add(_statusLabel);
        _bottomPanel.Controls.Add(_refreshButton);
        _bottomPanel.Controls.Add(_webButton);
        _bottomPanel.Controls.Add(_playButton);

        Controls.Add(_serverListPanel);
        Controls.Add(_bottomPanel);
        Controls.Add(_headerPanel);
    }

    private void LoadServers()
    {
        try
        {
            var jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "servers.json");
            if (!File.Exists(jsonPath))
            {
                jsonPath = "servers.json";
            }

            if (File.Exists(jsonPath))
            {
                var json = File.ReadAllText(jsonPath);
                var config = JsonSerializer.Deserialize<LauncherConfig>(json);
                if (config?.Servers != null)
                {
                    _servers.Clear();
                    _servers.AddRange(config.Servers);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi nạp danh sách máy chủ: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        RenderServerCards();
        if (_servers.Count > 0)
        {
            SelectServer(_servers[0]);
        }
    }

    private void RenderServerCards()
    {
        _serverListPanel.Controls.Clear();

        foreach (var server in _servers)
        {
            var card = new Panel
            {
                Width = 610,
                Height = 110,
                Margin = new Padding(0, 0, 0, 16),
                BackColor = server == _selectedServer ? Color.FromArgb(30, 41, 59) : Color.FromArgb(24, 32, 47),
                Cursor = Cursors.Hand,
                Tag = server
            };

            // Border / Selection accent
            var accentBar = new Panel
            {
                Width = 6,
                Dock = DockStyle.Left,
                BackColor = server == _selectedServer ? ColorTranslator.FromHtml(server.Color) : Color.FromArgb(75, 85, 99)
            };
            card.Controls.Add(accentBar);

            // Server Name & Badge
            var nameLabel = new Label
            {
                Text = server.Name,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 14),
                AutoSize = true
            };
            card.Controls.Add(nameLabel);

            var badgeLabel = new Label
            {
                Text = server.Badge,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = ColorTranslator.FromHtml(server.Color),
                Padding = new Padding(4, 2, 4, 2),
                Location = new Point(260, 16),
                AutoSize = true
            };
            card.Controls.Add(badgeLabel);

            // Description
            var descLabel = new Label
            {
                Text = server.Description,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Color.FromArgb(156, 163, 175),
                Location = new Point(20, 44),
                Size = new Size(420, 38)
            };
            card.Controls.Add(descLabel);

            // Status & Ping
            var statusBadge = new Label
            {
                Text = server.IsOnline ? $"● ONLINE ({server.PingMs} ms)" : "○ OFFLINE",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = server.IsOnline ? Color.FromArgb(16, 185, 129) : Color.FromArgb(239, 68, 68),
                Location = new Point(460, 42),
                AutoSize = true
            };
            card.Controls.Add(statusBadge);

            // Port info
            var portLabel = new Label
            {
                Text = $"Port: {server.Port}",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(107, 114, 128),
                Location = new Point(460, 68),
                AutoSize = true
            };
            card.Controls.Add(portLabel);

            // Card Click Events
            void HandleClick(object? s, EventArgs e) => SelectServer(server);
            card.Click += HandleClick;
            nameLabel.Click += HandleClick;
            descLabel.Click += HandleClick;
            statusBadge.Click += HandleClick;
            portLabel.Click += HandleClick;

            _serverListPanel.Controls.Add(card);
        }
    }

    private void SelectServer(ServerConfig server)
    {
        _selectedServer = server;
        _statusLabel.Text = $"Đã chọn: {server.Name} (Port {server.Port})";
        RenderServerCards();
    }

    private void StartPingTimer()
    {
        _pingTimer.Interval = 10000; // Check every 10s
        _pingTimer.Tick += async (s, e) => await CheckAllServersPingAsync();
        _pingTimer.Start();
        _ = CheckAllServersPingAsync();
    }

    private async Task CheckAllServersPingAsync()
    {
        foreach (var s in _servers)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                using var client = new TcpClient();
                var connectTask = client.ConnectAsync(s.Host, s.Port);
                var delayTask = Task.Delay(1000);
                var completedTask = await Task.WhenAny(connectTask, delayTask);

                if (completedTask == connectTask && client.Connected)
                {
                    sw.Stop();
                    s.IsOnline = true;
                    s.PingMs = sw.ElapsedMilliseconds;
                }
                else
                {
                    s.IsOnline = false;
                    s.PingMs = -1;
                }
            }
            catch
            {
                s.IsOnline = false;
                s.PingMs = -1;
            }
        }

        if (InvokeRequired)
        {
            Invoke(new Action(RenderServerCards));
        }
        else
        {
            RenderServerCards();
        }
    }

    private void OnPlayClicked(object? sender, EventArgs e)
    {
        if (_selectedServer == null)
        {
            MessageBox.Show("Vui lòng chọn một máy chủ trong danh sách.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var clientPath = _selectedServer.ClientPath;
        if (!Path.IsPathRooted(clientPath))
        {
            clientPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", clientPath);
        }

        if (!File.Exists(clientPath))
        {
            // Kiểm tra các đường dẫn mặc định
            var altPath1 = Path.Combine("d:\\MuServers", _selectedServer.Id == "s6" ? "Client_S6\\Main.exe" : "Client_S16\\main.exe");
            if (File.Exists(altPath1))
            {
                clientPath = altPath1;
            }
            else
            {
                MessageBox.Show(
                    $"Không tìm thấy tệp thực thi của Client tại:\n{clientPath}\n\nVui lòng kiểm tra lại cấu hình client trong servers.json.",
                    "Thiếu Client Executable",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }
        }

        try
        {
            var workDir = Path.GetDirectoryName(clientPath) ?? "";

            var startInfo = new ProcessStartInfo
            {
                FileName = clientPath,
                Arguments = _selectedServer.Arguments,
                WorkingDirectory = workDir,
                UseShellExecute = true
            };
            Process.Start(startInfo);

            _statusLabel.Text = $"Đang chạy: {_selectedServer.Name}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi khởi chạy game client: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OpenWebPortal()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "http://localhost:3007/register",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không thể mở trình duyệt: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

internal static class NativeInjector
{
    [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CreateProcess(
        string? lpApplicationName,
        string lpCommandLine,
        IntPtr lpProcessAttributes,
        IntPtr lpThreadAttributes,
        bool bInheritHandles,
        uint dwCreationFlags,
        IntPtr lpEnvironment,
        string lpCurrentDirectory,
        ref STARTUPINFO lpStartupInfo,
        out PROCESS_INFORMATION lpProcessInformation);

    [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
    private static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

    [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out UIntPtr lpNumberOfBytesWritten);

    [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

    [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, out IntPtr lpThreadId);

    [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint ResumeThread(IntPtr hThread);

    [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

    [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct STARTUPINFO
    {
        public uint cb;
        public string lpReserved;
        public string lpDesktop;
        public string lpTitle;
        public uint dwX;
        public uint dwY;
        public uint dwXSize;
        public uint dwYSize;
        public uint dwXCountChars;
        public uint dwYCountChars;
        public uint dwFillAttribute;
        public uint dwFlags;
        public short wShowWindow;
        public short cbReserved2;
        public IntPtr lpReserved2;
        public IntPtr hStdInput;
        public IntPtr hStdOutput;
        public IntPtr hStdError;
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct PROCESS_INFORMATION
    {
        public IntPtr hProcess;
        public IntPtr hThread;
        public uint dwProcessId;
        public uint dwThreadId;
    }

    private const uint CREATE_SUSPENDED = 0x00000004;
    private const uint MEM_COMMIT = 0x00001000;
    private const uint MEM_RESERVE = 0x00002000;
    private const uint PAGE_READWRITE = 0x04;

    public static bool LaunchWithDll(string exePath, string dllPath, string workDir, string args)
    {
        try
        {
            var si = new STARTUPINFO();
            si.cb = (uint)System.Runtime.InteropServices.Marshal.SizeOf(si);
            var pi = new PROCESS_INFORMATION();

            string cmdLine = "\"" + exePath + "\" " + args;
            bool created = CreateProcess(null, cmdLine, IntPtr.Zero, IntPtr.Zero, false, CREATE_SUSPENDED, IntPtr.Zero, workDir, ref si, out pi);
            if (!created) return false;

            byte[] dllBytes = System.Text.Encoding.ASCII.GetBytes(dllPath + "\0");
            IntPtr allocAddr = VirtualAllocEx(pi.hProcess, IntPtr.Zero, (uint)dllBytes.Length, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
            if (allocAddr == IntPtr.Zero)
            {
                ResumeThread(pi.hThread);
                CloseHandle(pi.hThread);
                CloseHandle(pi.hProcess);
                return false;
            }

            WriteProcessMemory(pi.hProcess, allocAddr, dllBytes, (uint)dllBytes.Length, out _);

            IntPtr loadLibAddr = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");
            IntPtr hRemoteThread = CreateRemoteThread(pi.hProcess, IntPtr.Zero, 0, loadLibAddr, allocAddr, 0, out _);
            if (hRemoteThread != IntPtr.Zero)
            {
                WaitForSingleObject(hRemoteThread, 2000);
                CloseHandle(hRemoteThread);
            }

            ResumeThread(pi.hThread);
            CloseHandle(pi.hThread);
            CloseHandle(pi.hProcess);
            return true;
        }
        catch
        {
            return false;
        }
    }
}