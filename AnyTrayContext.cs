using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace AnyTray
{
    class AnyTrayContext : ApplicationContext
    {
        private NotifyIcon AnyTrayIcon;
        private ContextMenuStrip AnyTrayIconContextMenu;
        private ToolStripMenuItem CloseMenuItem;

        private UdpClient Client;

        private EventedQueue<string> CommandQueue = new EventedQueue<string>();

        private List<string> IconChoices = new List<string>
        {
            "black",
            "blue",
            "cyan",
            "exclamation",
            "green",
            "orange",
            "purple",
            "question",
            "red",
            "white",
            "yellow"
        };

        public AnyTrayContext()
        {
            Application.ApplicationExit += new EventHandler(this.onApplicationExit);
            this.InitializeComponent();
            this.AnyTrayIcon.Visible = true;
        }

        private void InitializeComponent()
        {
            this.AnyTrayIcon = new NotifyIcon();
            this.AnyTrayIcon.Text = "AnyTray Notifier";

            this.AnyTrayIcon.BalloonTipIcon = ToolTipIcon.Info;
            this.AnyTrayIcon.BalloonTipTitle = "AnyTray Notifier";
            this.AnyTrayIcon.BalloonTipText = "Listening on UDP port: {}";

            this.AnyTrayIcon.Icon = ImageAsIcon(Properties.Resources.blue);
            this.AnyTrayIcon.DoubleClick += AnyTrayIcon_DoubleClick;

            // CloseMenuItem
            this.CloseMenuItem = new ToolStripMenuItem();
            this.CloseMenuItem.Name = "CloseMenuItem";
            this.CloseMenuItem.Text = "Exit AnyTray";
            this.CloseMenuItem.Click += new EventHandler(this.CloseMenuItem_Click);

            // AnyTrayIconContextMenu
            this.AnyTrayIconContextMenu = new ContextMenuStrip();
            this.AnyTrayIconContextMenu.SuspendLayout();
            this.AnyTrayIconContextMenu.Name = "TrayIconContextMenu";
            this.AnyTrayIconContextMenu.Items.AddRange(new ToolStripItem[] { this.CloseMenuItem });
            this.AnyTrayIcon.ContextMenuStrip = AnyTrayIconContextMenu;
            this.AnyTrayIconContextMenu.ResumeLayout(false);

            this.CommandQueue.Enqueued += OnQueuedCommand;

            this.Client = new UdpClient(1738);
            this.Client.BeginReceive(new AsyncCallback(ReceiveCallback), null);
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 8000);
            byte[] received = Client.EndReceive(ar, ref RemoteIpEndPoint);

            string recv_val = Encoding.UTF8.GetString(received);

            CommandQueue.Enqueue(recv_val);

            this.Client.BeginReceive(new AsyncCallback(ReceiveCallback), null);
        }

        private Icon ImageAsIcon(Image input)
        {
            return Icon.FromHandle(((Bitmap)input).GetHicon());
        }

        private void CloseMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void AnyTrayIcon_DoubleClick(object sender, EventArgs e)
        {
            this.AnyTrayIcon.ShowBalloonTip(10000);
        }

        private void OnQueuedCommand(object sender, EventArgs e)
        {
            while (CommandQueue.Count > 0)
            {
                string command = CommandQueue.Dequeue();
                if (this.IconChoices.Contains(command))
                {
                    Image new_icon = (Image)(Properties.Resources.ResourceManager.GetObject(command));
                    this.AnyTrayIcon.Icon = ImageAsIcon(new_icon);
                }
            }
        }

        private void onApplicationExit(object sender, EventArgs e)
        {
            this.AnyTrayIcon.Visible = false;
        }
    }
}
