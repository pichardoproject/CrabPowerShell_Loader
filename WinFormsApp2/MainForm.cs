using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using System.Security.Principal;

namespace WinFormsApp2
{
    // === Transparent RichTextBox class ===
    public class TransparentRichTextBox : RichTextBox
    {
        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x20; // WS_EX_TRANSPARENT
                return cp;
            }
        }
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // Skip background painting so panel behind is visible
        }
    }
    public partial class MainForm : Form
    {
        private readonly string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        private readonly string logFile;
        private Image? watermarkImage;
        private float watermarkOpacity = 0.15f;
        private Panel? overlayPanel;
        private bool overlayRefreshPending = false;

        public MainForm()
        {
            InitializeComponent();
            // --- Check for Administrator Privileges ---
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            bool isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);

            if (!isAdmin)
            {
                MessageBox.Show(
                    "This program must be run as Administrator.\n\n" +
                    "Please right-click the executable and choose 'Run as Administrator'.",
                    "Admin Rights Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                Environment.Exit(1);
                return;
            }
            Directory.CreateDirectory(logDir);
            logFile = Path.Combine(logDir, $"log_{DateTime.Now:yyyyMMdd_HHmm}.txt");
            Log("Help Desk Shell started.");

            // === Log Area Styling ===
            txtLog.BorderStyle = BorderStyle.None;
            txtLog.BackColor = Color.White;
            txtLog.ForeColor = Color.Black;        //  make sure text is visible
            txtLog.Font = new Font("Consolas", 10);
            txtLog.ReadOnly = true;
            txtLog.BringToFront();                 // keep log above any hidden overlay


            // Try loading crab watermark
            try
            {
                string imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crab.png");
                if (File.Exists(imagePath))
                {
                    watermarkImage = Image.FromFile(imagePath);
                    Log($"Loaded watermark: {imagePath}");
                }
                else
                {
                    Log($"[WARN] crab.png not found at: {imagePath}");
                }
            }
            catch (Exception ex)
            {
                Log("[ERROR] Failed to load watermark: " + ex.Message);
            }

            // Add the overlay *after* form load (ensures controls exist)
            this.Load += (_, __) => InitializeOverlay();

            // Hover styling
            foreach (Control ctrl in panelButtons.Controls)
            {
                if (ctrl is Button btn)
                {
                    btn.MouseEnter += (s, e) => btn.BackColor = Color.FromArgb(230, 240, 255);
                    btn.MouseLeave += (s, e) => btn.BackColor = Color.White;
                }
            }
        }
        // === Reduce flicker on crab + text updates ===
        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED (double-buffer entire window)
                return cp;
            }
        }
        // === Initialize watermark overlay ===
        private void InitializeOverlay()
        {
            if (txtLog == null)
            {
                Log("[WARN] txtLog was null during overlay init.");
                return;
            }

            if (overlayPanel != null)
                return;

            overlayPanel = new Panel
            {
                BackColor = txtLog.BackColor,
                Bounds = txtLog.Bounds
            };
            overlayPanel.Paint += OverlayPanel_Paint;

            // Add the panel to the same parent, but right behind txtLog
            Control parent = txtLog.Parent!;
            int logIndex = parent.Controls.GetChildIndex(txtLog);
            parent.Controls.Add(overlayPanel);
            parent.Controls.SetChildIndex(overlayPanel, logIndex + 1); // directly beneath txtLog

            // Keep it aligned when resizing/moving
            txtLog.LocationChanged += (s, e) => overlayPanel.Bounds = txtLog.Bounds;
            txtLog.SizeChanged += (s, e) => overlayPanel.Bounds = txtLog.Bounds;
            this.Resize += (s, e) => overlayPanel.Bounds = txtLog.Bounds;

            // Redraw watermark when log text changes
            txtLog.TextChanged += (s, e) => overlayPanel.Invalidate();

            overlayPanel.Invalidate();
        }
        // === Paint watermark on the background panel ===
        private void OverlayPanel_Paint(object? sender, PaintEventArgs e)
        {
            try
            {
                if (watermarkImage == null)
                    return;

                Graphics g = e.Graphics;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                int w = overlayPanel!.ClientSize.Width;
                int h = overlayPanel.ClientSize.Height;

                // Keep proportional scaling (smaller than half screen)
                float scale = Math.Min(
                    w / (float)watermarkImage.Width * 0.45f,
                    h / (float)watermarkImage.Height * 0.45f
                );

                int imgWidth = (int)(watermarkImage.Width * scale);
                int imgHeight = (int)(watermarkImage.Height * scale);
                int x = (w - imgWidth) / 2;
                int y = (h - imgHeight) / 2;

                // Faint glow
                using (GraphicsPath gp = new GraphicsPath())
                {
                    gp.AddEllipse(x - 50, y - 50, imgWidth + 100, imgHeight + 100);
                    using (PathGradientBrush glow = new PathGradientBrush(gp))
                    {
                        glow.CenterColor = Color.FromArgb(50, 0, 120, 255);
                        glow.SurroundColors = new[] { Color.FromArgb(0, 0, 120, 255) };
                        glow.CenterPoint = new PointF(x + imgWidth / 2f, y + imgHeight / 2f);
                        g.FillPath(glow, gp);
                    }
                }

                // Draw the crab itself
                using (ImageAttributes attr = new ImageAttributes())
                {
                    ColorMatrix matrix = new ColorMatrix { Matrix33 = watermarkOpacity }; // transparency from your field
                    attr.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

                    g.DrawImage(
                        watermarkImage,
                        new Rectangle(x, y, imgWidth, imgHeight),
                        0, 0, watermarkImage.Width, watermarkImage.Height,
                        GraphicsUnit.Pixel,
                        attr
                    );
                }
            }
            catch (Exception ex)
            {
                Log($"[WARN] Paint skipped: {ex.Message}");
            }
        }
        // === PowerShell Runner (fixed command string & live output) ===
        private async void RunPowerShellScript(string scriptName)
        {
            try
            {
                string scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts", scriptName);

                if (!File.Exists(scriptPath))
                {
                    Log($"[ERROR] Script not found: {scriptPath}");
                    MessageBox.Show($"Script not found:\n{scriptPath}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                Log($"Running PowerShell script silently: {scriptPath}");

                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    // Added -STA so message boxes and other WinForms GUI elements work
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -STA -File \"{scriptPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8,
                    StandardErrorEncoding = System.Text.Encoding.UTF8,
                    Verb = "runas" // <-- Optional: forces elevation prompt if not already admin
                };

                var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

                process.OutputDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        this.BeginInvoke(() => Log("[PS] " + e.Data));
                };

                process.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        this.BeginInvoke(() => Log("[ERROR] " + e.Data));
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await Task.Run(() => process.WaitForExit());
                Log($"[DONE] {scriptName} exited with code {process.ExitCode}");
            }
            catch (Exception ex)
            {
                Log("[ERROR] Failed to execute PowerShell: " + ex.Message);
            }
        }
        // === Button handlers ===
        private void btnSetup_Click(object sender, EventArgs e)
        {
            Log("=== SETUP START ===");
            RunPowerShellScript("Setup.ps1");
        }

        private void btnRestrict_Click(object sender, EventArgs e)
        {
            Log("=== APPLY RESTRICTIONS ===");
            RunPowerShellScript("Restrict.ps1");
        }

        private void btnUndo_Click(object sender, EventArgs e)
        {
            Log("=== UNDO START ===");
            RunPowerShellScript("Undo.ps1");
        }

        private void btnViewLog_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start("explorer.exe", logDir);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to open log folder: " + ex.Message);
            }
        }

        private void btnClearLog_Click(object sender, EventArgs e)
        {
            txtLog.Clear();
            Log("Log window cleared by user.");
        }

        // === Logging ===
        private void Log(string msg)
        {
            if (txtLog.InvokeRequired)
            {
                txtLog.BeginInvoke(new Action(() => Log(msg)));
                return;
            }

            string line = $"[{DateTime.Now:HH:mm:ss}] {msg}{Environment.NewLine}";
            bool isError = msg.Contains("[ERROR]");
            bool isPS = msg.StartsWith("[PS]");

            // === Color-coding ===
            txtLog.SelectionStart = txtLog.TextLength;
            txtLog.SelectionLength = 0;

            if (isError)
                txtLog.SelectionColor = Color.Red;
            else if (isPS)
                txtLog.SelectionColor = Color.MediumBlue;
            else
                txtLog.SelectionColor = Color.Black;

            // Suspend drawing while updating (prevents flicker)
            txtLog.SuspendLayout();
            txtLog.AppendText(line);
            txtLog.SelectionColor = txtLog.ForeColor; // reset color
            txtLog.ScrollToCaret();
            txtLog.ResumeLayout();

            // Write to file safely
            try
            {
                File.AppendAllText(logFile, line);
            }
            catch { }

            // === Gentle redraw timing (avoid flicker) ===
            if (!overlayRefreshPending)
            {
                overlayRefreshPending = true;
                var timer = new System.Windows.Forms.Timer { Interval = 300 };
                timer.Tick += (s, e) =>
                {
                    // Delay ensures text settles before crab redraw
                    try
                    {
                        if (!IsDisposed && overlayPanel != null && !overlayPanel.IsDisposed)
                        {
                            overlayPanel.BeginInvoke(new Action(() => overlayPanel.Invalidate()));
                        }
                    }
                    catch { }

                    overlayRefreshPending = false;
                    timer.Dispose();
                };
                timer.Start();
            }
        }

    }
}