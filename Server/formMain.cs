using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;

namespace Server
{
    public partial class Server : Form
    {
        IPEndPoint ipe;
        Socket server,client;        
        IPHostEntry iphostentry;
        String strHostName;
        int Connection=0;
        NetworkStream ns;
        DiffieHellman diff;
        
        Aes_EcbEncrypt aes = new Aes_EcbEncrypt();
        byte[] khoapublic,khoabimat;
        string keypublic, keysecret, keyclient;
        byte[] nhankey,data,tinnhan,nhankeydadoi;
        string datimeIV;
        byte[] datetimeIV;
        string compare1, compare2;
        
        public Server()
        {
            CheckForIllegalCrossThreadCalls = false;
            InitializeComponent();            
            Connected();
        }

        string GetIPServer()
        {
            strHostName = Dns.GetHostName();
            iphostentry = Dns.GetHostByName(strHostName);
            string nIP = "";
            foreach (IPAddress ipaddress in iphostentry.AddressList)
            {
                nIP+=ipaddress.ToString();
            }
            return nIP;
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            txtIP.Text = GetIPServer();
        }
        public byte[] EncryptData(string data)
        {
            MD5CryptoServiceProvider md5Hasher = new MD5CryptoServiceProvider();
            byte[] hashedBytes;
            System.Text.UTF8Encoding encoder = new System.Text.UTF8Encoding();
            hashedBytes = md5Hasher.ComputeHash(encoder.GetBytes(data));
            return hashedBytes;
        }
        public string maHoaMd5(string data)
        {
            return BitConverter.ToString(EncryptData(data)).Replace("-", "").ToLower();
        }
        void Connected()
        {
            ipe = new IPEndPoint(IPAddress.Any, 9050);
            server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            server.Bind(ipe);
            
            Thread Listen = new Thread(() =>
            {
                try
                {
                    while (true)
                    {
                        server.Listen(100);
                        client = server.Accept();
                        Connection++;
                        //Gữi public key cho client
                        byte[] laydodai = BitConverter.GetBytes(khoapublic.Length);
                        client.Send(laydodai);
                        byte[] Keypublic = khoapublic;
                        client.Send(Keypublic);

                        //Nhận public key của client
                        byte[] nhandodai = new byte[1024];
                        client.Receive(nhandodai);
                        nhankey = new byte[BitConverter.ToInt32(nhandodai, 0)];
                        client.Receive(nhankey);
                        keyclient = Convert.ToBase64String(nhankey);
                        txtkeyclient.Text = keyclient;   
                        
                        //Tạo khóa bí mật
                        diff.LayKhoaBiMat(nhankey);
                        khoabimat = diff.aes.Key;
                        keysecret = Convert.ToBase64String(khoabimat);
                        txtSecretkey.Text = keysecret;

                        ns = new NetworkStream(client);
                        LstMS.Items.Add("Co:" + Connection + " client ket noi toi.");
                        string welcome = "Welcome to my test server";
                        byte[] data = new byte[1024];
                        data = Encoding.ASCII.GetBytes(welcome);
                        ns.Write(data, 0, data.Length);
                        Thread receive = new Thread(Receive);
                        receive.IsBackground = true;
                        receive.Start(client);
                    }
                }
                catch
                {
                    ipe = new IPEndPoint(IPAddress.Any, 9050);
                    server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                }
            }
            );
            CreatePublicKey();
            Listen.IsBackground = true;
            Listen.Start();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            LstMS.Items.Add("server: "+txtMS.Text);
            //tạo chuỗi mã hóa tin nhắn ban đầu + hash time
            string s = maHoaMd5(txtMS.Text);
            datimeIV = maHoaMd5(DateTime.Now.ToString());
            string Mahoa_time = s + ";" + datimeIV;

            //tạo các chữ random để chèn vào
            char[] chars = "abcdefghijklmnopqrstuvwxyz1234567890".ToCharArray();
            Random r = new Random();
            int i = r.Next(chars.Length);
            int vitri = r.Next(0, txtMS.TextLength);

            //Chuỗi Đã thay đổi
            string message = RandomString(txtMS.Text, chars[i].ToString(), vitri) + ";" + Mahoa_time;
            Send(message);            
            txtMS.Clear();
        }

        void Send(string message)
        {
            datimeIV = maHoaMd5(DateTime.Now.ToString());
            string time = datimeIV.Substring(0, 16);
            datetimeIV = Encoding.UTF8.GetBytes(time);
            string a = txtSecretkey.Text.Substring(0, 32);
            byte[] key = Encoding.ASCII.GetBytes(a);
            string s = aes.EncryptString(message, key, datetimeIV);
            byte[] mahoa = diff.MaHoaMessage(nhankey, s);
            byte[] dodai = BitConverter.GetBytes(mahoa.Length);
            byte[] initvector = diff.IV;
            if (client != null && txtMS.Text != string.Empty)
            {
                client.Send(dodai);
                client.Send(mahoa);
                client.Send(initvector);
            }
        }
        private static string RandomString(string baseString, string character, int position)
        {
            var sb = new StringBuilder(baseString);

            sb.Insert(position, character);

            return sb.ToString();
        }

        void CreatePublicKey()
        {
            diff = new DiffieHellman();
            khoapublic = diff.PublicKey;
            keypublic = Convert.ToBase64String(khoapublic);
            txtKey.Text = keypublic;
        }
        void guilaipublickey()
        {
            byte[] batdauguikey = Encoding.UTF8.GetBytes("guikey");
            client.Send(batdauguikey);
            byte[] Keypublic = khoapublic;
            client.Send(Keypublic);
        }
        void guiLaiKeyChoClientVuaGui()
        {
            byte[] batdauguikey = Encoding.UTF8.GetBytes("guikeytoclient");
            client.Send(batdauguikey);
            byte[] Keypublic = khoapublic;
            client.Send(Keypublic);
        }
        void Receive(object obj)
        {
            Socket client = obj as Socket;
            try
            {
                c: while (true)
                {
                    byte[] data = new byte[1024];
                    client.Receive(data);                    
                    if (string.Equals(Encoding.UTF8.GetString(data), "guikeyserver", StringComparison.InvariantCultureIgnoreCase))
                    {
                        nhankeydadoi = new byte[140];
                        client.Receive(nhankeydadoi);
                        keypublic = Convert.ToBase64String(nhankeydadoi);
                        txtkeyclient.Text = keypublic;
                        diff.LayKhoaBiMat(nhankeydadoi);
                        khoabimat = diff.aes.Key;
                        keysecret = Convert.ToBase64String(khoabimat);
                        txtSecretkey.Text = keysecret;
                        nhankey = nhankeydadoi;
                    }
                    else if (string.Equals(Encoding.UTF8.GetString(data), "guikeytoserver", StringComparison.InvariantCultureIgnoreCase))
                    {
                        nhankeydadoi = new byte[140];
                        client.Receive(nhankeydadoi);
                        keypublic = Convert.ToBase64String(nhankeydadoi);
                        txtkeyclient.Text = keypublic;
                        CreatePublicKey();
                        diff.LayKhoaBiMat(nhankeydadoi);
                        khoabimat = diff.aes.Key;
                        keysecret = Convert.ToBase64String(khoabimat);
                        txtSecretkey.Text = keysecret;
                        nhankey = nhankeydadoi;
                        guiLaiKeyChoClientVuaGui();

                    }
                    else
                    {
                        tinnhan = new byte[BitConverter.ToInt32(data, 0)];
                        client.Receive(tinnhan);
                        byte[] nhanvector = new byte[16];
                        client.Receive(nhanvector);
                        string message = diff.GiaiMaMassage(nhankey, tinnhan, nhanvector);

                        datimeIV = maHoaMd5(DateTime.Now.ToString());
                        string time = datimeIV.Substring(0, 16);
                        datetimeIV = Encoding.UTF8.GetBytes(time);
                        string a = txtSecretkey.Text.Substring(0, 32);
                        byte[] key = Encoding.ASCII.GetBytes(a);
                        string s = aes.DecryptString(message, key, datetimeIV);
                        foreach (char c in s)
                        {
                            if (c.ToString() == ";")
                            {
                                string[] tokens = s.Trim().Split(';');
                                compare1 = tokens[1];
                                compare2 = maHoaMd5(tokens[0]);
                                if (compare1 != compare2)
                                {
                                    LstMS.Items.Add("Client: "+tokens[0]);
                                    LstMS.Items.Add("Dữ liệu đã bị thay đổi trong quá trình chuyền:");
                                    LstMS.Items.Add("Chuỗi mã trước:" + compare1);
                                    LstMS.Items.Add("Chuỗi mã sau:" + compare2);
                                    goto c;
                                }

                            }

                        }
                        LstMS.Items.Add("Client: "+s);
                    }
                }
            }

            catch
            {
                client.Close();
                Connection--;
                LstMS.Items.Add("Con lai:" + Connection + " client ket noi toi.");
            }
        }
        private void btnSend_Click(object sender, EventArgs e)
        {
            //byte[] data = new byte[1024];
            //data = Encoding.UTF8.GetBytes(txtMS.Text);
            //ns.Write(data, 0, data.Length);
            //LstMS.Items.Add(txtMS.Text);
            //txtMS.Clear();  
            
            datimeIV = maHoaMd5(DateTime.Now.ToString());
            string time = datimeIV.Substring(0, 16);
            datetimeIV = Encoding.UTF8.GetBytes(time);
            string a = txtSecretkey.Text.Substring(0, 32);
            byte[] key = Encoding.ASCII.GetBytes(a);
            string s = aes.EncryptString(txtMS.Text, key, datetimeIV);
            byte[] mahoa = diff.MaHoaMessage(nhankey, s);
            byte[] dodai = BitConverter.GetBytes(mahoa.Length);
            byte[] initvector = diff.IV;
            if (client != null && txtMS.Text != string.Empty)
            {
                LstMS.Items.Add("Server: "+txtMS.Text);
                client.Send(dodai);
                client.Send(mahoa);
                client.Send(initvector);
                txtMS.Clear();
            }
            
        }

        private void Server_FormClosed(object sender, FormClosedEventArgs e)
        {
            server.Close();
        }

       
    }
}
