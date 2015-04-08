using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
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
        private ToolStripMenuItem PortMenuItem;
        private ToolStripMenuItem CloseMenuItem;

        private int UdpPort = 0;
        private UdpClient Client;

        private EventedQueue<string> CommandQueue = new EventedQueue<string>();

        public AnyTrayContext()
        {
            Application.ApplicationExit += new EventHandler(this.onApplicationExit);
            this.InitializeComponent();
            this.AnyTrayIcon.Visible = true;
        }

        private void InitializeComponent()
        {
            this.UdpPort = GetNextFreeUDPPort();

            if (this.UdpPort > 0)
            {
                this.Client = new UdpClient(this.UdpPort);
                this.Client.BeginReceive(new AsyncCallback(ReceiveCallback), null);
            }
            else
            {
                MessageBox.Show("No free UDP ports available. Do you have permission?");
                Application.Exit();
            }

            // AnyTrayIcon
            this.AnyTrayIcon = new NotifyIcon();
            this.AnyTrayIcon.Text = "AnyTray Notifier";
            this.AnyTrayIcon.Icon = GenerateSolidIcon("White");

            // PortMenuItem
            this.PortMenuItem = new ToolStripMenuItem();
            this.PortMenuItem.Name = "PortMenuItem";
            this.PortMenuItem.Text = String.Format("UDP port {0}", UdpPort);
            this.PortMenuItem.Click += new EventHandler(this.PortMenuItem_Click);

            // CloseMenuItem
            this.CloseMenuItem = new ToolStripMenuItem();
            this.CloseMenuItem.Name = "CloseMenuItem";
            this.CloseMenuItem.Text = "Exit AnyTray";
            this.CloseMenuItem.Click += new EventHandler(this.CloseMenuItem_Click);

            // AnyTrayIconContextMenu
            this.AnyTrayIconContextMenu = new ContextMenuStrip();
            this.AnyTrayIconContextMenu.SuspendLayout();
            this.AnyTrayIconContextMenu.Name = "TrayIconContextMenu";
            this.AnyTrayIconContextMenu.Items.AddRange(new ToolStripItem[] { this.PortMenuItem, this.CloseMenuItem });
            this.AnyTrayIcon.ContextMenuStrip = AnyTrayIconContextMenu;
            this.AnyTrayIconContextMenu.ResumeLayout(false);

            this.CommandQueue.Enqueued += OnQueuedCommand;
        }

        private void PortMenuItem_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(this.UdpPort.ToString());
        }

        //Based on: http://stackoverflow.com/questions/5879605/udp-port-open-check
        private int GetNextFreeUDPPort()
        {
            var startingAtPort = 1738;
            var maxNumberOfPortsToCheck = 500;
            var range = Enumerable.Range(startingAtPort, maxNumberOfPortsToCheck);
            var portsInUse =
                from p in range
                join used in System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners()
                    on p equals used.Port
                select p;

            return range.Except(portsInUse).FirstOrDefault();
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 8000);
            byte[] received = Client.EndReceive(ar, ref RemoteIpEndPoint);

            string recv_val = Encoding.UTF8.GetString(received);

            this.CommandQueue.Enqueue(recv_val);

            this.Client.BeginReceive(new AsyncCallback(ReceiveCallback), null);
        }

        private void CloseMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void OnQueuedCommand(object sender, EventArgs e)
        {
            while (CommandQueue.Count > 0)
            {
                string command = CommandQueue.Dequeue();
                switch (command)
                {
                    case "exclamation":
                        this.AnyTrayIcon.Icon = GenerateTextIcon("Red", "!");
                        break;
                    case "question":
                        this.AnyTrayIcon.Icon = GenerateTextIcon("black", "?");
                        break;
                    default:
                        this.AnyTrayIcon.Icon = GenerateSolidIcon(command);
                        break;
                }
            }
        }

        private void onApplicationExit(object sender, EventArgs e)
        {
            this.AnyTrayIcon.Visible = false;
        }

        // I don't like this very much, but it's better than embedded fixed value icons.
        // We'll do better later.
        private Icon GenerateSolidIcon(string color)
        {
            Bitmap bmp = new Bitmap(32, 32, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                SolidBrush b = new SolidBrush(Color.FromName(color));
                g.FillEllipse(b, 4, 4, 28, 28);
            }

            return Icon.FromHandle(bmp.GetHicon());
        }

        private Icon GenerateTextIcon(string color, string text)
        {
            Bitmap bmp = new Bitmap(32, 32, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                SolidBrush b = new SolidBrush(Color.FromName(color));
                g.FillEllipse(b, 4, 4, 28, 28);

                Font f = new Font("Consolas", 18);
                PointF p = new PointF(8, 4);
                g.DrawString(text, f, Brushes.White, p);
            }

            return Icon.FromHandle(bmp.GetHicon());
        }
    }
}
