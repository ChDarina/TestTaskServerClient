using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TestTaskClient
{
    public partial class Form1 : Form
    {
        const int port = 8888;
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
                    string text = System.IO.File.ReadAllText(@s);

                    client = new TcpClient("127.0.0.1", port);
                    NetworkStream stream = client.GetStream();
                    byte[] data = System.Text.Encoding.Unicode.GetBytes(text);
                    stream.Write(data, 0, data.Length);

                    // получаем ответ
                    data = new byte[64]; // буфер для получаемых данных
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
                    listView1.Items.Add(item);
                    listView1.Refresh();
                }
            }
            catch (Exception exc)   
            {
                MessageBox.Show(exc.Message, "Ошибка", MessageBoxButtons.OK);
            }
            finally
            {
                client.Close();
            }
        }
    }
}
