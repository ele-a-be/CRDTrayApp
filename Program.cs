using System;
using System.IO;
using System.Drawing;
using System.ServiceProcess;
using System.Windows.Forms;

namespace CRDTrayApp
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new TrayContext());
        }
    }

    class TrayContext : ApplicationContext
    {
        private readonly NotifyIcon trayIcon;
        private const string ServiceName = "chromoting";

        public TrayContext()
        {
            trayIcon = new NotifyIcon
            {
                Icon = new Icon(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crd.ico")),
                Text = "Chrome Remote Desktop",
                Visible = true
            };

            trayIcon.MouseUp += TrayIcon_MouseUp;
        }

        private void TrayIcon_MouseUp(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
                ShowCustomMenu(Cursor.Position);
        }

        private void ShowCustomMenu(Point position)
        {
            // Determine service status
            bool isRunning;
            try
            {
                using var sc = new ServiceController(ServiceName);
                isRunning = sc.Status == ServiceControllerStatus.Running;
            }
            catch
            {
                isRunning = false;
            }

            // Create the custom menu
            var menu = new CustomMenuForm(isRunning, ToggleCRD, ExitApp)
            {
                StartPosition = FormStartPosition.Manual
            };

            // Position it relative to the click
            menu.Location = new Point(
                position.X - menu.Width + 10,
                position.Y - menu.Height - 5
            );

            // Show and activate so OnDeactivate will fire
            menu.Show();
            menu.Activate();
        }

        private void ToggleCRD()
        {
            try
            {
                using var sc = new ServiceController(ServiceName);
                if (sc.Status == ServiceControllerStatus.Running)
                    sc.Stop();
                else
                    sc.Start();

                sc.WaitForStatus(
                    sc.Status == ServiceControllerStatus.Running
                        ? ServiceControllerStatus.Stopped
                        : ServiceControllerStatus.Running,
                    TimeSpan.FromSeconds(5)
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error toggling service:\n{ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void ExitApp()
        {
            trayIcon.Visible = false;
            trayIcon.Dispose();
            Application.Exit();
        }
    }

    class CustomMenuForm : Form
    {
        public CustomMenuForm(bool isRunning, Action toggleAction, Action exitAction)
        {
            // Form styling: border + content
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            BackColor = Color.Gray;
            Padding = new Padding(1);
            DoubleBuffered = true;
            TopMost = true;

            // Close when losing focus
            this.Deactivate += (s, e) => this.Close();

            var layout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                Padding = new Padding(8, 4, 8, 4),
                AutoSize = true,
                WrapContents = false,
                BackColor = Color.FromArgb(37, 37, 38)
            };

            // Status label
            var statusLabel = new Label
            {
                Text = isRunning ? "CRD is enabled" : "CRD is disabled",
                AutoSize = true,
                ForeColor = isRunning ? Color.Green : Color.Red,
                Font = new Font("Segoe UI", 9),
                Margin = new Padding(2, 2, 2, 6)
            };
            layout.Controls.Add(statusLabel);

            // Separator
            layout.Controls.Add(new Panel
            {
                Height = 1,
                Width = 180,
                BackColor = Color.DimGray,
                Margin = new Padding(2, 0, 2, 4)
            });

            // Toggle button
            layout.Controls.Add(CreateMenuButton(isRunning ? "Disable CRD" : "Enable CRD", toggleAction));

            layout.Controls.Add(new Panel
            {
                Height = 1,
                Width = 180,
                BackColor = Color.DimGray,
                Margin = new Padding(2, 4, 2, 4)
            });

            // Exit button
            layout.Controls.Add(CreateMenuButton("Exit", exitAction));

            Controls.Add(layout);
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
        }

        private Button CreateMenuButton(string text, Action onClick)
        {
            var btn = new Button
            {
                Text = text,
                TextAlign = ContentAlignment.MiddleCenter,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(37, 37, 38),
                Cursor = Cursors.Hand,
                Width = 180,
                Height = 32,
                Margin = new Padding(2)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.MouseEnter += (s, e) => btn.BackColor = Color.FromArgb(62, 62, 64);
            btn.MouseLeave += (s, e) => btn.BackColor = Color.FromArgb(37, 37, 38);
            btn.Click += (s, e) => { onClick(); this.Close(); };
            return btn;
        }
    }
}
