using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace TestTaskServer
{
    public class ClientObject
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
                byte[] data = new byte[64];
                StringBuilder builder = new StringBuilder();
                int bytes = 0;
                do
                {
                    bytes = stream.Read(data, 0, data.Length);
                    builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                }
                while (stream.DataAvailable);

                string text = builder.ToString();
                bool otvet = IsThatPalindrom(text);
                data = Encoding.Unicode.GetBytes(otvet.ToString());
                stream.Write(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                Program.currentThreads--;
                if (stream != null)
                    stream.Close();
                if (client != null)
                    client.Close();
            }
        }
        private static bool IsThatPalindrom(string text)
        {
            string pol1, pol2 = "";
            text = text.Trim().Replace(" ", string.Empty).Replace(".", string.Empty).Replace(",", string.Empty).Replace(":", string.Empty).
                Replace(";", string.Empty).Replace("!", string.Empty).Replace("?", string.Empty).ToLower();
            text = text.Replace("ё", "е").Replace("й", "и");
            Thread.Sleep(1000);
            if (text.Length % 2 != 0)
            {
                int i = text.Length / 2;
                pol1 = text.Remove(i, i + 1);
                for (int j = 0; j < i; j++)
                    pol2 = $"{pol2}{text[text.Length - 1 - j]}";
                int otv = pol1.CompareTo(pol2);
                if (otv == 0) return true;
                else return false;
            }
            else return false;
        }
    }
    public class Program
    {
        static int N;
        static public int currentThreads=0;
        const int port = 8888;
        static TcpListener listener;
        static Queue<ClientObject> clientObjects = new Queue<ClientObject>();
        static void Main(string[] args)
        {

            Console.Write("Введите максимальное число запросов N: ");
            try
            {
                N = int.Parse(Console.ReadLine());
                IPEndPoint ep = new IPEndPoint(IPAddress.Loopback, port);
                listener = new TcpListener(ep);
                listener.Start();
                Console.WriteLine("Подключение: {0}:{1}  ",ep.Address, ep.Port);
                while (true)
                {                                          
                    while (clientObjects.Count != 0)
                        if (currentThreads < N && clientObjects.Count != 0)
                        {
                            ClientObject clientObject = clientObjects.Dequeue();
                            Thread clientThread = new Thread(new ThreadStart(clientObject.Process));
                            clientThread.Start();
                            currentThreads++;
                            if (currentThreads == N) Console.WriteLine("Количество запросов достигло максимального!");
                        }
                    if (currentThreads < N)
                    {
                        TcpClient client = listener.AcceptTcpClient();
                        ClientObject clientObject = new ClientObject(client);
                        clientObjects.Enqueue(clientObject);
                    }                   
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
            }
            finally
            {
                if (listener != null && clientObjects.Count == 0)
                    listener.Stop();
            }
        }
    }
}
