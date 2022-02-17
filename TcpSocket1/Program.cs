using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TcpSocket1
{
    class Program
    {
        static List<TcpClient> tcpClients = new();

        static void Main(string[] args)
        {
            Thread tcpListenerThread = new(StartTcpListener)
            {
                IsBackground = true,
                Name = "tcpListenerThread"
            };

            tcpListenerThread.Start();
            tcpListenerThread.Join();

            Thread tcpClientsCheckerThread = new(CheckForTcpClients)
            {
                IsBackground = true,
                Name = "TcpClientsCheckerThread"
            };

            tcpClientsCheckerThread.Start();
            tcpClientsCheckerThread.Join();

            Console.WriteLine("Press enter to exit...");
            Console.ReadLine();
        }

        public static async void StartTcpListener()
        {
            TcpListener tcpListener;
            try
            {
                IPEndPoint endPoint = new(IPAddress.Any, 9090);
                tcpListener = new TcpListener(endPoint.Address, endPoint.Port);

                tcpListener.Start();

                Console.WriteLine($"TCP Listener Started. Listening to TCP clients at {endPoint.Address}:{endPoint.Port}");

                while (true)
                {
                    Console.WriteLine("Waiting for a client ...");
                    TcpClient tcpClient = await tcpListener.AcceptTcpClientAsync();

                    if (true)
                    {
                        Console.WriteLine("Checking if new connections allowed...");
                    }

                    lock (tcpClients)
                    {
                        tcpClients.Add(tcpClient);

                    }
                    Console.WriteLine("A Client connected.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Tcp Listener Error : " + ex.Message);
            }
            finally
            {

            }
        }

        public static void CheckForTcpClients()
        {
            static void SendInformationMessage(double threadSleepTimeInSeconds, int connectedCount = 0, int disconnectedCount = 0)
            {
                Console.WriteLine($"TcpClients Checked at {DateTime.UtcNow:u}\nStill Connected: {connectedCount} | Disconnected Now: {disconnectedCount} | Next Sleep Time (seconds): {threadSleepTimeInSeconds}");
            }

            Console.WriteLine("TcpClients Control Thread started");
            int disconnectedTcpClientsCount = 0;
            double threadSleepTimeInSecondsDefault = 3;
            double threadSleepTimeInSecondsMax = threadSleepTimeInSecondsDefault * 2;
            double threadSleepTimeIncreasePercentage = 0.1;
            double threadSleepTimeInSeconds = threadSleepTimeInSecondsDefault;
            while (true)
            {
                Thread.Sleep(TimeSpan.FromSeconds(threadSleepTimeInSeconds));

                if (tcpClients.Count == 0)
                {
                    threadSleepTimeInSeconds += threadSleepTimeInSeconds * threadSleepTimeIncreasePercentage;
                    if (threadSleepTimeInSeconds >= threadSleepTimeInSecondsMax)
                        threadSleepTimeInSeconds = threadSleepTimeInSecondsDefault;
                    SendInformationMessage(threadSleepTimeInSeconds);
                    continue;
                }

                disconnectedTcpClientsCount = 0;
                lock (tcpClients)
                {
                    var disconnectedTcpClients = tcpClients.Where(tcpClient => tcpClient.Connected == false || tcpClient?.Client?.Connected == false || tcpClient.Client.IsDisconnected() == true).ToList();
                    disconnectedTcpClientsCount = disconnectedTcpClients.Count;
                    if (disconnectedTcpClientsCount == 0)
                    {
                        threadSleepTimeInSeconds += threadSleepTimeInSeconds * threadSleepTimeIncreasePercentage;
                        if (threadSleepTimeInSeconds >= threadSleepTimeInSecondsMax)
                            threadSleepTimeInSeconds = threadSleepTimeInSecondsDefault;
                        SendInformationMessage(threadSleepTimeInSeconds, tcpClients.Count, disconnectedTcpClientsCount);
                        continue;
                    }

                    var status =
                        Parallel.ForEach(disconnectedTcpClients,
                        new ParallelOptions { MaxDegreeOfParallelism = -1 },
                        (tcpClient) =>
                            {
                                tcpClient.Client.Close();
                                tcpClient.Client.Dispose();
                                tcpClient.Close();
                                tcpClient.Dispose();
                            }
                        );

                    tcpClients.RemoveAll(tcpClient => tcpClient.Connected == false);
                }

                threadSleepTimeInSeconds = threadSleepTimeInSecondsDefault;
                SendInformationMessage(threadSleepTimeInSeconds, tcpClients.Count, disconnectedTcpClientsCount);
            }
        }
    }
}
