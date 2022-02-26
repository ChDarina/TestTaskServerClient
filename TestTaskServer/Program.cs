using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace TestTaskServer
{
    public class ClientObject //Класс клиент-объекта
    {
        public TcpClient client; //Клиент
        public ClientObject(TcpClient tcpClient) //Конструктор класса
        {
            client = tcpClient;
        }

        public void Process() //Процесс принятия запроса с клиента, его обработка и отправка обратно 
        {
            NetworkStream stream = null; //Объявление потока
            try
            {
                stream = client.GetStream(); //Получение активного потока с клиента
                byte[] data = new byte[64]; //Массив байтов
                StringBuilder builder = new StringBuilder(); //Инструмент для построения строки из байтов
                int bytes = 0; //Переменная для байтов
                do //Пока доступны данные в активном потоке с клиента
                {
                    bytes = stream.Read(data, 0, data.Length); //Получение байтов с активного потока с клиента
                    builder.Append(Encoding.Unicode.GetString(data, 0, bytes)); //Их декодирование с учётом кодировки Юникод
                }
                while (stream.DataAvailable);
                string text = builder.ToString(); //Перевод текста с клиента в строку
                bool otvet = IsThatPalindrom(text); //Вызов функции проверки на то, является ли строка палиндромом
                data = Encoding.Unicode.GetBytes(otvet.ToString()); //Перевод ответа в байты с учётом кодировки Юникод
                stream.Write(data, 0, data.Length); //Передача ответа в байтовом формате в клиент
            }
            catch (Exception ex) //"Ловец" ошибок
            {
                Console.WriteLine(ex.Message); //Вывод текста ошибки
            }
            finally //Завершающее действие
            {
                Program.currentThreads--; // Уменьшение счётчика активных потоков
                if (stream != null) //Если есть работающий поток с клиента
                    stream.Close(); //Закрытие потока с клиента
                if (client != null) //Если есть работающий клиент
                    client.Close(); //Закрытие клиента
            }
        }
        private static bool IsThatPalindrom(string text) //Проверка на то, является ли строка палиндромом
        {
            string pol1, pol2 = ""; //Объявление двух переменных строк, отвечающих за ту или иную половину строки
            text = text.Trim().Replace(" ", string.Empty).Replace(".", string.Empty).Replace(",", string.Empty).Replace(":", string.Empty).
                Replace(";", string.Empty).Replace("!", string.Empty).Replace("?", string.Empty).ToLower(); //Убираются знаки препинания и переводится в один регистр
            text = text.Replace("ё", "е").Replace("й", "и"); //Замена ё на е и й на и 
            Thread.Sleep(1000); //Имитация задержки на секунду
            if (text.Length % 2 != 0) //Чётное ли количество знаков в строке?
            {
                int i = text.Length / 2; //Середина строки
                pol1 = text.Remove(i, i + 1);//Оставляется первая половина строки
                for (int j = 0; j < i; j++)
                    pol2 = $"{pol2}{text[text.Length - 1 - j]}"; //Оставляется вторая, но в обратном порядке
                int otv = pol1.CompareTo(pol2); //Одинаковыми ли получились половины?
                if (otv == 0) return true; //Если да, то это палиндром
                else return false; //Если нет, то это не палиндром
            }
            else return false; //Если нет, то это точно не палиндром
        }
    }
    public class Program
    {
        static int N; //Максимум обрабатываемых запросов
        static public int currentThreads=0; //Количество активно обрабатываемых запросов/потоков
        const int port = 8888; //Порт
        static TcpListener listener; //Приёмник для запросов со стороны клиента
        static Queue<ClientObject> clientObjects = new Queue<ClientObject>(); //Очередь из запросов
        static void Main(string[] args)
        {
            Console.Write("Введите максимальное число запросов N: ");
            try
            { 
                N = int.Parse(Console.ReadLine()); //Считывание с консоли числа запросов N
                IPEndPoint ep = new IPEndPoint(IPAddress.Loopback, port); //Объявление сетевой конечной точки
                listener = new TcpListener(ep); //Объявление приёмника из сетевой конечной точки
                listener.Start(); //Начало работы приёмника
                Console.WriteLine("Подключение: {0}:{1}  ",ep.Address, ep.Port);
                while (true)
                {                                          
                    while (clientObjects.Count != 0) //Пока в очереди есть запросы
                        if (currentThreads < N && clientObjects.Count != 0) //Если число активных потоков меньше максимального и в очереди есть звпросы
                        {
                            ClientObject clientObject = clientObjects.Dequeue(); //"Вытащить" из очереди клиент-объект
                            Thread clientThread = new Thread(new ThreadStart(clientObject.Process)); //Объявление нового потока
                            clientThread.Start(); //Начало нового потока
                            currentThreads++; //Увлечение числа активных потоков
                            if (currentThreads == N) Console.WriteLine("Количество запросов достигло максимального!"); //Если число активных потоков достигло максимального
                        }
                    if (currentThreads < N) //Если число активных потоков меньше максимального
                    {
                        TcpClient client = listener.AcceptTcpClient(); //Ожидание новых запросов со стороны клиента
                        ClientObject clientObject = new ClientObject(client); //Объявление клиент-объекта из запроса
                        clientObjects.Enqueue(clientObject); //Загрузка клиент-объекта в очередь
                    }                   
                }
            }
            catch (Exception ex) //"Ловец" ошибок
            {
                Console.WriteLine(ex.Message.ToString()); //Вывод текста ошибки
            }
            finally //Завершающее действие
            {
                if (listener != null && clientObjects.Count == 0) //Если есть работающий приёмник и при этом никаких активных потоков
                    listener.Stop(); //Закрытие приёмника
            }
        }
    }
}
