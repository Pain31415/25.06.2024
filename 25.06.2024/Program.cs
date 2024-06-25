using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using System.IO;

class Program
{
    static int RemotePort;
    static int LocalPort;
    static IPAddress RemoteIPAddr;

    static Dictionary<string, DateTime> lastRequestTimes = new Dictionary<string, DateTime>();

    const int RequestLimitIntervalMinutes = 0;

    const int InactivityDisconnectIntervalMinutes = 1000;

    static string logFilePath = "server_logs.txt";

    [STAThread]
    static void Main(string[] args)
    {
        try
        {
            Console.SetWindowSize(40, 20);
            Console.Title = "Client";
            Console.WriteLine("Введіть віддалений IP сервера:");
            RemoteIPAddr = IPAddress.Parse(Console.ReadLine());
            Console.WriteLine("Введіть віддалений порт сервера:");
            RemotePort = Convert.ToInt32(Console.ReadLine());
            Console.WriteLine("Введіть локальний порт для прийому відповідей від сервера:");
            LocalPort = Convert.ToInt32(Console.ReadLine());

            Thread receiveThread = new Thread(new ThreadStart(ThreadFuncReceive));
            receiveThread.IsBackground = true;
            receiveThread.Start();

            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Введіть список продуктів для пошуку рецептів:");
                string query = Console.ReadLine();

                if (CanSendRequest())
                {
                    SendData(query);
                    RecordRequestTime();
                    LogClientActivity(query);
                }
                else
                {
                    Console.WriteLine("Досягнуто обмеження на кількість запитів. Спробуйте пізніше.");
                }
            }
        }
        catch (FormatException formExc)
        {
            Console.WriteLine("Перетворення неможливе: " + formExc.Message);
        }
        catch (Exception exc)
        {
            Console.WriteLine("Помилка: " + exc.Message);
        }
    }

    static void ThreadFuncReceive()
    {
        try
        {
            UdpClient uClient = new UdpClient(LocalPort);
            while (true)
            {
                IPEndPoint ipEnd = null;
                byte[] responce = uClient.Receive(ref ipEnd);
                string strResult = Encoding.Unicode.GetString(responce);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(strResult);
                Console.ForegroundColor = ConsoleColor.Red;

                UpdateClientActivity(ipEnd.Address.ToString());
            }
        }
        catch (SocketException sockEx)
        {
            Console.WriteLine("Помилка сокета: " + sockEx.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Помилка: " + ex.Message);
        }
    }

    static void SendData(string query)
    {
        UdpClient uClient = new UdpClient();
        IPEndPoint ipEnd = new IPEndPoint(RemoteIPAddr, RemotePort);
        try
        {
            byte[] bytes = Encoding.Unicode.GetBytes(query);
            uClient.Send(bytes, bytes.Length, ipEnd);
        }
        catch (SocketException sockEx)
        {
            Console.WriteLine("Помилка сокета: " + sockEx.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Помилка: " + ex.Message);
        }
        finally
        {
            uClient.Close();
        }
    }

    static bool CanSendRequest()
    {
        if (!lastRequestTimes.ContainsKey(RemoteIPAddr.ToString()))
        {
        }

        DateTime lastRequestTime = lastRequestTimes[RemoteIPAddr.ToString()];
        TimeSpan timeSinceLastRequest = DateTime.Now - lastRequestTime;

        if (timeSinceLastRequest.TotalMinutes >= RequestLimitIntervalMinutes)
        {
            return true;
        }

        return false;
    }

    static void RecordRequestTime()
    {
        lastRequestTimes[RemoteIPAddr.ToString()] = DateTime.Now;
    }

    static void UpdateClientActivity(string clientIP)
    {
        lastRequestTimes[clientIP] = DateTime.Now;
    }

    static void LogClientActivity(string query)
    {
        try
        {
            using (StreamWriter sw = new StreamWriter(logFilePath, true))
            {
                string logMessage = $"[{DateTime.Now}] Client {RemoteIPAddr} requested: {query}";
                sw.WriteLine(logMessage);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Помилка при логуванні: " + ex.Message);
        }
    }
}
