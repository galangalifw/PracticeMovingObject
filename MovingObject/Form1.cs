using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;

namespace MovingObject
{
    public partial class Form1 : Form
    {
        Pen red = new Pen(Color.Red);
        Rectangle rect = new Rectangle(20, 20, 30, 30);
        SolidBrush fillBlue = new SolidBrush(Color.Blue);
        int slide = 10;

        TcpListener server;
        List<TcpClient> clients = new List<TcpClient>();
        Thread serverThread;

        public Form1()
        {
            InitializeComponent();
            timer1.Interval = 50;
            timer1.Enabled = true;
            StartServer();
        }

        private void StartServer()
        {
            server = new TcpListener(IPAddress.Any, 5000);
            server.Start();
            serverThread = new Thread(AcceptClients);
            serverThread.IsBackground = true;
            serverThread.Start();
        }

        private void AcceptClients()
        {
            while (true)
            {
                try
                {
                    var client = server.AcceptTcpClient();
                    lock (clients)
                    {
                        clients.Add(client);
                    }
                }
                catch { }
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            back();
            rect.X += slide;
            Invalidate();
            SendFrameToClients();
        }

        private void back()
        {
            if (rect.X >= this.Width - rect.Width * 2)
                slide = -10;
            else if (rect.X <= rect.Width / 2)
                slide = 10;
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.DrawRectangle(red, rect);
            g.FillRectangle(fillBlue, rect);
        }

        private void SendFrameToClients()
        {
            using (Bitmap bmp = new Bitmap(this.Width, this.Height))
            {
                this.DrawToBitmap(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height));
                using (MemoryStream ms = new MemoryStream())
                {
                    bmp.Save(ms, ImageFormat.Jpeg);
                    byte[] data = ms.ToArray();
                    lock (clients)
                    {
                        for (int i = clients.Count - 1; i >= 0; i--)
                        {
                            try
                            {
                                var ns = clients[i].GetStream();
                                ns.Write(BitConverter.GetBytes(data.Length), 0, 4);
                                ns.Write(data, 0, data.Length);
                            }
                            catch
                            {
                                clients[i].Close();
                                clients.RemoveAt(i);
                            }
                        }
                    }
                }
            }
        }
    }
}