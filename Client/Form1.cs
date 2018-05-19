using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
namespace Client
{
    
    public partial class Form1 : Form
    {
        IPEndPoint ipe;
        Socket client;
        Thread listen;
        Aes_EcbEncrypt aes = new Aes_EcbEncrypt();
        byte[] khoapublic, khoabimat;
        string keypublic, keysecret;
        byte[] nhankey, nhankeydadoi, data, tinnhan, keypublicduocguilai = new byte[1024];
        SHA sha256 = new SHA();
        DiffieHellman Diff = new DiffieHellman();
        string dateTimeIV;
        byte[] dateTimeIv;
        MD5 md5 = new MD5();
        string compare1, compare2;        

        public Form1()
        {
            CheckForIllegalCrossThreadCalls = false;
            InitializeComponent();
        }       
        void Receive()
        {
            try
            {
                c: while (true)
                {
                    byte[] data = new byte[1024];
                    client.Receive(data);                    
                    if(string.Equals(Encoding.UTF8.GetString(data), "guikey", StringComparison.InvariantCultureIgnoreCase))
                    {
                        txtServerKey.Clear();
                        txtFinalKey.Clear();
                        nhankeydadoi = new byte[140];
                        client.Receive(nhankeydadoi);
                        keypublic = Convert.ToBase64String(nhankeydadoi);
                        txtServerKey.Text = keypublic;
                        CreatePublicKey();
                        Diff.LayKhoaBiMat(nhankeydadoi);
                        khoabimat = Diff.aes.Key;
                        keysecret = Convert.ToBase64String(khoabimat);
                        txtFinalKey.Text = keysecret;
                        nhankey = nhankeydadoi;
                        guilaipublickey();
                    }else if(string.Equals(Encoding.UTF8.GetString(data), "guikeytoclient", StringComparison.InvariantCultureIgnoreCase))
                    {
                        txtServerKey.Clear();
                        txtFinalKey.Clear();
                        nhankeydadoi = new byte[140];
                        client.Receive(nhankeydadoi);
                        keypublic = Convert.ToBase64String(nhankeydadoi);
                        txtServerKey.Text = keypublic;
                        Diff.LayKhoaBiMat(nhankeydadoi);
                        khoabimat = Diff.aes.Key;
                        keysecret = Convert.ToBase64String(khoabimat);
                        txtFinalKey.Text = keysecret;
                        nhankey = nhankeydadoi;
                    }else
                    {
                        tinnhan = new byte[BitConverter.ToInt32(data, 0)];
                        client.Receive(tinnhan);
                        byte[] nhanvector = new byte[16];
                        client.Receive(nhanvector);
                        string message = Diff.GiaiMaDiffie(nhankey, tinnhan, nhanvector);
                        dateTimeIV = md5.maHoaMd5(DateTime.Now.ToString());
                        string time = dateTimeIV.Substring(0, 16);
                        dateTimeIv = Encoding.UTF8.GetBytes(time);                         
                        string a = txtFinalKey.Text.Substring(0, 32);
                        byte[] key = Encoding.ASCII.GetBytes(a);
                        string s = aes.DecryptString(message, key, dateTimeIv);
                        foreach (char c in s)
                        {
                            if (c.ToString() == ";")
                            {
                                string[] tokens = s.Trim().Split(';');
                                compare1 = tokens[1];
                                compare2 = md5.maHoaMd5(tokens[0]);
                                if (compare1 != compare2)
                                {
                                    LstMS.Items.Add("Server: "+tokens[0]);
                                    LstMS.Items.Add("Dữ liệu đã bị thay đổi trong quá trình chuyền:");
                                    LstMS.Items.Add("Chuỗi mã trước:" + compare1);
                                    LstMS.Items.Add("Chuỗi mã sau:" + compare2);
                                    goto c;
                                }

                            }

                        }
                        LstMS.Items.Add("Server: "+s);
                    }                    
                }
            }catch
            {
                Close();
            }
        }
        private void Client_FormClosed(object sender, FormClosedEventArgs e)
        {
            Close();
        }
        
        private void Form1_Load(object sender, EventArgs e)
        {
            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            LstMS.Items.Add("Client: "+txtMS.Text);
            //tạo chuỗi mã hóa tin nhắn ban đầu + hash time
            string s = md5.maHoaMd5(txtMS.Text);
            dateTimeIV = md5.maHoaMd5(DateTime.Now.ToString());
            string Mahoa_time = s + ";" + dateTimeIV;

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
            dateTimeIV = md5.maHoaMd5(DateTime.Now.ToString());
            string time = dateTimeIV.Substring(0, 16);
            dateTimeIv = Encoding.UTF8.GetBytes(time);
            string a = txtFinalKey.Text.Substring(0, 32);
            byte[] key = Encoding.ASCII.GetBytes(a);
            string s = aes.EncryptString(message, key, dateTimeIv);
            byte[] mahoa = Diff.MaHoaDiffie(nhankey, s);
            byte[] dodai = BitConverter.GetBytes(mahoa.Length);
            byte[] initvector = Diff.IV;
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

        private void btnConnect_Click_1(object sender, EventArgs e)
        {
            ipe = new IPEndPoint(IPAddress.Parse(txtIP.Text), 9050);
            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                client.Connect(ipe);
            }
            catch
            {
                MessageBox.Show("Không thể kết nối server", "Thông báo", MessageBoxButtons.OK);
                return;
            }
            byte[] nhandodai = new byte[1024];
            client.Receive(nhandodai);
            nhankey = new byte[BitConverter.ToInt32(nhandodai, 0)];
            client.Receive(nhankey);
            keypublic = Convert.ToBase64String(nhankey);
            txtServerKey.Text = keypublic;
            CreatePublicKey();
            Diff.LayKhoaBiMat(nhankey);
            khoabimat = Diff.aes.Key;
            keysecret = Convert.ToBase64String(khoabimat);
            txtFinalKey.Text = keysecret;
            byte[] laydodai = BitConverter.GetBytes(khoapublic.Length);
            client.Send(laydodai);
            byte[] Keypublic = khoapublic;
            client.Send(Keypublic);
            byte[] data = new byte[1024];
            int recv=client.Receive(data);
            string welcome = Encoding.UTF8.GetString(data, 0, recv);
            LstMS.Items.Add(welcome);
            listen = new Thread(Receive);
            listen.IsBackground = true;
            listen.Start();
            btnConnect.Enabled = false;
        }

        void CreatePublicKey()
        {
            Diff = new DiffieHellman();
            khoapublic = Diff.PublicKey;
            keypublic = Convert.ToBase64String(khoapublic);
            txtPublicKey.Text = keypublic;
        }

        void guilaipublickey()
        {
            byte[] batdauguikey = Encoding.UTF8.GetBytes("guikeyserver");
            client.Send(batdauguikey);
            byte[] Keypublic = khoapublic;
            client.Send(Keypublic);
        }

        void guiPublickeyChoServerKhiHetTime()
        {
            byte[] batdauguikey = Encoding.UTF8.GetBytes("guikeytoserver");
            client.Send(batdauguikey);
            byte[] Keypublic = khoapublic;
            client.Send(Keypublic);
        }

        private void btnSend_Click_1(object sender, EventArgs e)
        {
            if (txtMS.Text != string.Empty)
            {
                //byte[] data = new byte[1024];
                //data = Encoding.ASCII.GetBytes(txtMS.Text);
                //client.Send(data, data.Length, SocketFlags.None);
                //LstMS.Items.Add(txtMS.Text);
                //txtMS.Clear();                
                dateTimeIV = md5.maHoaMd5(DateTime.Now.ToString());
                string time = dateTimeIV.Substring(0, 16);
                dateTimeIv = Encoding.UTF8.GetBytes(time);
                string a = txtFinalKey.Text.Substring(0, 32);
                byte[] key = Encoding.ASCII.GetBytes(a);
                string s = aes.EncryptString(txtMS.Text, key, dateTimeIv);
                byte[] mahoa = Diff.MaHoaDiffie(nhankey, s);
                byte[] dodai = BitConverter.GetBytes(mahoa.Length);
                byte[] initvector = Diff.IV;
                if (client != null && txtMS.Text != string.Empty)
                {
                    LstMS.Items.Add("Client: "+txtMS.Text);
                    client.Send(dodai);
                    client.Send(mahoa);
                    client.Send(initvector);
                    txtMS.Clear();
                }
                
            }            
        }
        

    }
}
