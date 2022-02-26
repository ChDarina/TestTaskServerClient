using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace TestTaskClient
{
    public partial class Form1 : Form
    {
        const int port = 8888; //Порт
        static public int currentThreads = 0; //Количество текущих потоков/запроса от клиента
        public static Queue<string> filenames = new Queue<string>(); //Очередь из названий файлов
        public static Queue<string> texts = new Queue<string>(); //Очередь из текстов файлов
        public static Queue<ListViewItem> ListViewItems = new Queue<ListViewItem>(); //Очередь из элементов списка палиндромов
        public Form1() //Инициализация формы
        {
            InitializeComponent();
        }

        private void ChooseFolder_Click(object sender, EventArgs e) //Нажатие на кнопку выбора папки
        {
            FolderBrowserDialog ofd = new FolderBrowserDialog(); 
            DialogResult result = ofd.ShowDialog(); //Показ диалогового окна с выбором папки
            if (result == DialogResult.OK) //Если папка была выбрана
            {
                textBox1.Text = ofd.SelectedPath; //В текстовом окне появляется путь к папке
                FindPalindroms.Enabled = true; //Открывается кнопка для поиска палиндрома
                listView1.Items.Clear(); //Очищается список из палиндромов
            }
        }

        private void FindPalindroms_Click(object sender, EventArgs e) //Нажатие на кнопку поиска палиндромов
        {
            listView1.Items.Clear(); //Очищается список из палиндромов
            TcpClient server = null; //Объявление сервера
            try
            {
                string path = textBox1.Text; //В переменную загружается путь к файлам
                string[] files = System.IO.Directory.GetFiles(path, "*.txt"); //В массив загружаются все файлы из папки с расширением txt
                foreach (string s in files) //Цикл для каждого файла из папки
                {
                    string filename = Path.GetFileName(s); //Получение название файла
                    filenames.Enqueue(filename); //Загрузка названия файла в очередь
                    string text = System.IO.File.ReadAllText(@s); //Получение текста файла
                    texts.Enqueue(text); //Загрузка текста файла в очередь
                    server = new TcpClient(IPAddress.Loopback.ToString(), port); //Соединение с сервером
                    ServerObject clientObject = new ServerObject(server); //Создание сервер-объекта
                    Thread serverThread = new Thread(new ThreadStart(clientObject.Process)); //Объявление нового потока
                    serverThread.Start(); //Начало нового потока
                    currentThreads++;  //Увеличение счётчика активных потоков              
                }
                while (currentThreads > 0) //Пока есть активные потоки
                {
                    if (ListViewItems.Count != 0) //Если в очереди из элементов списка палиндромов есть какие-либо элементы
                    {
                        listView1.Items.Add(ListViewItems.Dequeue()); //Добавление в список на форме элемента из очереди
                        listView1.Refresh(); //Обновление списка на форме
                        currentThreads--; //Уменьшение счётчика активных потоков
                    }
                }
            }
            catch (Exception exc)   //"Ловец" ошибок
            {
                MessageBox.Show(exc.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); //Вывод текста ошибки
            }
            finally //Завершающее действие
            {                    
                if (server != null && currentThreads == 0) //Если есть работающий сервер и при этом никаких активных потоков
                    server.Close(); //Закрытие сервера
            }
        }
    }    
    public class ServerObject : Form1 //Класс сервер-объекта, унаследованный от формы (в целях использования полей из формы в этом классе)
    {
        public TcpClient server; //Сервер
        public ServerObject(TcpClient tcpClient) //Конструктор класса
        {
            server = tcpClient;
        }
        public void Process() //Процесс отправки запроса и принятия ответа с сервера
        {
            NetworkStream stream = null; //Объявление потока
            try
            {
                stream = server.GetStream(); //Получение активного потока с сервера
                string text = texts.Dequeue(); //Получение текста файла            
                string filename = filenames.Dequeue(); //Получение названия файла
                byte[] data = System.Text.Encoding.Unicode.GetBytes(text); //Перевод текста файла в байты с учётом кодировки Юникод
                stream.Write(data, 0, data.Length); //Передача текста файла в байтовом формате в сервер
                data = new byte[64]; //Массив байтов
                StringBuilder builder = new StringBuilder(); //Инструмент для построения строки из байтов
                int bytes = 0; //Переменная для байтов
                do //Пока доступны данные в активном потоке с сервере
                {
                    bytes = stream.Read(data, 0, data.Length); //Получение байтов с активного потока с сервера
                    builder.Append(Encoding.Unicode.GetString(data, 0, bytes)); //Их декодирование с учётом кодировки Юникод
                }
                while (stream.DataAvailable); 
                string otvet = builder.ToString(); //Перевод ответа с сервера в строку
                var item = new ListViewItem(new[] { filename, text, otvet }); //Сбор всех данных (название файла, текста, ответа сервера) в один элемент
                ListViewItems.Enqueue(item); //Загрузка элемента списка палиндромов в очередь
            }
            catch (Exception ex) //"Ловец" ошибок
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); //Вывод текста ошибки
            }
            finally //Завершающее действие
            {
                if (stream != null) //Если есть работающий поток с сервера
                    stream.Close(); //Закрытие потока с сервера
                if (server != null) //Если есть работающий сервер
                    server.Close(); //Закрытие сервера
            }
        }
    }
}
