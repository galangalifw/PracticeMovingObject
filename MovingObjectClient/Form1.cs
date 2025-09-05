using System;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;

namespace MovingObjectClient
{
    public partial class Form1 : Form
    {
        TcpClient client;
        Thread receiveThread;
        private PictureBox pictureBox1;

        public Form1()
        {
            InitializeComponent();
            pictureBox1 = new PictureBox();
            pictureBox1.Dock = DockStyle.Fill;
            this.Controls.Add(pictureBox1);
            ConnectToServer();
        }

        private void ConnectToServer()
        {
            client = new TcpClient();
            client.Connect("127.0.0.1", 5000); // Ganti IP jika server di komputer lain
            receiveThread = new Thread(ReceiveLoop);
            receiveThread.IsBackground = true;
            receiveThread.Start();
        }

        private void ReceiveLoop()
        {
            var ns = client.GetStream();
            while (true)
            {
                try
                {
                    byte[] lenBuf = new byte[4];
                    int read = ns.Read(lenBuf, 0, 4);
                    if (read < 4) break;
                    int len = BitConverter.ToInt32(lenBuf, 0);
                    byte[] imgBuf = new byte[len];
                    int total = 0;
                    while (total < len)
                    {
                        int r = ns.Read(imgBuf, total, len - total);
                        if (r <= 0) break;
                        total += r;
                    }
                    using (var ms = new MemoryStream(imgBuf))
                    {
                        var img = Image.FromStream(ms);
                        Invoke((Action)(() => pictureBox1.Image = img));
                    }
                }
                catch
                {
                    break;
                }
            }
        }
    }
}