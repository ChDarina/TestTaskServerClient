using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TestTaskClient
{
    public partial class Form1 : Form
    {
        const int port = 8888;
        static public int currentThreads = 0;
        public static Queue<string> filenames = new Queue<string>();
        public static Queue<string> texts = new Queue<string>();
        public static Queue<ListViewItem> ListViewItems = new Queue<ListViewItem>();
        public Form1()
        {
            InitializeComponent();
        }

        private void ChooseFolder_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog ofd = new FolderBrowserDialog();
            DialogResult result = ofd.ShowDialog();
            if (result == DialogResult.OK)
            {
                textBox1.Text = ofd.SelectedPath;
                FindPalindroms.Enabled = true;
                listView1.Items.Clear() ;
            }
        }

        private void FindPalindroms_Click(object sender, EventArgs e)
        {
            listView1.Items.Clear();
            TcpClient client = null;
            try
            {
                string path = textBox1.Text;
                string[] files = System.IO.Directory.GetFiles(path, "*.txt");
                foreach (string s in files)
                {
                    string filename = Path.GetFileName(s);
                    filenames.Enqueue(filename);
                    string text = System.IO.File.ReadAllText(@s);
                    texts.Enqueue(text);
                    client = new TcpClient(IPAddress.Loopback.ToString(), port);
                    ClientObject clientObject = new ClientObject(client);
                    Thread clientThread = new Thread(new ThreadStart(clientObject.Process));
                    clientThread.Start();
                    currentThreads++;                
                }
                while (currentThreads > 0)
                {
                    if (ListViewItems.Count != 0)
                    {
                        listView1.Items.Add(ListViewItems.Dequeue());
                        listView1.Refresh();
                        currentThreads--;
                    }
                }
            }
            catch (Exception exc)   
            {
                MessageBox.Show(exc.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {                    

                if (client != null && currentThreads == 0)
                    client.Close();
            }
        }
    }    
    public class ClientObject : Form1
    {
        public TcpClient client;
        public ClientObject(TcpClient tcpClient)
        {
            client = tcpClient;
        }
        public void Process()
        {
            NetworkStream stream = null;
            try
            {
                stream = client.GetStream();
                string text = texts.Dequeue();                
                string filename = filenames.Dequeue();
                byte[] data = System.Text.Encoding.Unicode.GetBytes(text);
                stream.Write(data, 0, data.Length);
                data = new byte[64];
                StringBuilder builder = new StringBuilder();
                int bytes = 0;
                do
                {
                    bytes = stream.Read(data, 0, data.Length);
                    builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                }
                while (stream.DataAvailable);
                string otvet = builder.ToString();
                var item = new ListViewItem(new[] { filename, text, otvet });
                ListViewItems.Enqueue(item);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (stream != null)
                    stream.Close();
                if (client != null)
                    client.Close();
            }
        }
    }
}
