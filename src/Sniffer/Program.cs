using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sniffer
{
    public class Program
    {
        private static int TaskCount = 0;
        public static Mutex mutex = new Mutex();
        public static Random rnd = new Random();
        private static void UpdateTaskCount(bool isIncrease = true)
        {
            var seed = isIncrease ? 1 : -1;
            lock (mutex)
            {
                TaskCount += seed;
                Console.WriteLine("Current TaskCount:{0},Current Thread Id:{1}",
                    TaskCount,
                    Thread.CurrentThread.ManagedThreadId);
            }
        }
        static void Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Console.WriteLine("测试输出中文");
            //ShowProcesses();
            var manageTcpServers = ManageTcpServer(ips: new byte[] { 127, 0, 0, 1 });
            var manageTcpClients = new List<ManageTcpServer>();
            //ManageTask();
            //ManageTcpClient();
            //ManageSocketClient();
            // var client = new MongoClient("mongodb://localhost:32774");
            // var database = client.GetDatabase("foo");  
            var action = new Action(() =>
            {
                Console.WriteLine("1、新建客户端");
                Console.WriteLine("2、客户端发送信息");
                Console.WriteLine("3、服务端发送信息");
            });
            action();
            while (true)
            {
                var read = Console.ReadLine();
                if (read == "1")
                {
                    var client = new TcpClient();

                    var clientTask = client.ConnectAsync(new System.Net.IPAddress(new byte[] { 127, 0, 0, 1 }), 2222);
                    clientTask.ContinueWith((t) =>
                    {
                        var stream = client.GetStream();
                        manageTcpClients.Add(new ManageTcpServer(stream));
                    });
                }
                else if (read.StartsWith("2"))
                {
                    Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            foreach (var server in manageTcpClients)
                            {
                                server.Write("客户端发送信息:" + read.Substring(2));
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    });
                }
                else if (read.StartsWith("3"))
                {
                    Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            foreach (var server in manageTcpServers)
                            {
                                server.Write("服务端发送信息:" + read.Substring(2));
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    });
                }
                action();
            }
        }

        private static async Task<Socket> ConnectSocket(string server, int port)
        {
            Socket s = null;
            IPHostEntry hostEntry = null;

            // Get host related information.
            hostEntry = await Dns.GetHostEntryAsync(server);

            // Loop through the AddressList to obtain the supported AddressFamily. This is to avoid
            // an exception that occurs when the host IP Address is not compatible with the address family
            // (typical in the IPv6 case).
            foreach (IPAddress address in hostEntry.AddressList)
            {
                IPEndPoint ipe = new IPEndPoint(address, port);
                Socket tempSocket =
                    new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                tempSocket.Connect(ipe);

                if (tempSocket.Connected)
                {
                    s = tempSocket;
                    break;
                }
                else
                {
                    continue;
                }
            }
            return s;
        }

        // This method requests the home page content for the specified server.
        private static async Task<string> SocketSendReceive(string server, int port)
        {
            var sb = new StringBuilder();
            sb.AppendLine("GET / HTTP/1.1");
            sb.AppendLine("Connection: close");
            sb.AppendLine("Host: blog.csdn.net");
            sb.AppendLine("User-Agent: Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; SV1; .NET CLR 2.0.50727; .NET CLR 3.0.04506.648; .NET CLR 3.5.21022)");
            sb.AppendLine("Accept: */*");
            sb.AppendLine();
            Byte[] bytesSent = Encoding.UTF8.GetBytes(sb.ToString());
            Byte[] bytesReceived = new Byte[256];

            // Create a socket connection with the specified server and port.
            Socket s = await ConnectSocket(server, port);

            if (s == null)
                return ("Connection failed");

            // Send request to the server.
            s.Send(bytesSent, bytesSent.Length, 0);

            // Receive the server home page content.
            int bytes = 0;
            string page = "Default HTML page on " + server + ":\r\n";

            // The following will block until te page is transmitted.
            do
            {
                bytes = s.Receive(bytesReceived, bytesReceived.Length, 0);
                page = page + Encoding.UTF8.GetString(bytesReceived, 0, bytes);
            }
            while (bytes > 0);

            return page;
        }

        private static void ManageSocketClient()
        {
            string result = string.Empty;
            Task.Factory.StartNew(async () =>
            {
                result = await SocketSendReceive("blog.csdn.net", 80);
                Console.WriteLine(result);
            });
        }



        private static void ManageTcpClient()
        {
            string result = string.Empty;
            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    using (var client = new TcpClient())
                    {
                        await client.ConnectAsync("http://blog.csdn.net", 80);
                        using (var stream = client.GetStream())
                        {
                            var sb = new StringBuilder();
                            sb.AppendLine("GET / HTTP/1.1");
                            //sb.AppendLine("Connection: close");
                            sb.AppendLine("Host: www.baidu.com");
                            sb.AppendLine("User-Agent: Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; SV1; .NET CLR 2.0.50727; .NET CLR 3.0.04506.648; .NET CLR 3.5.21022)");
                            sb.AppendLine("Accept: */*");
                            sb.AppendLine();
                            var header = Encoding.UTF8.GetBytes(sb.ToString());
                            await stream.WriteAsync(header, 0, header.Length);
                            var chars = new byte[1024];
                            var p = stream.Read(chars, 0, chars.Length);
                            while (p > 0)
                            {
                                Console.WriteLine(Encoding.UTF8.GetChars(chars, 0, p));
                                p = stream.Read(chars, 0, chars.Length);
                            }
                        }
                    }

                }
            });
        }

        /// <summary>
        /// 服务器管理
        /// </summary>
        /// <param name="ips"></param>
        /// <param name="port"></param>
        private static List<ManageTcpServer> ManageTcpServer(byte[] ips, int port = 2222)
        {
            var list = new List<ManageTcpServer>();
            var listener = new TcpListener(new System.Net.IPAddress(ips), port);

            listener.Start();

            //用专门的线程来接受请求
            Task.Factory.StartNew(async () =>
            {
                //不间断的接受客户端请求
                while (true)
                {
                    //接受客户端的连接请求
                    var myclientTask = await listener.AcceptTcpClientAsync();
                    var ManageTcpServer = new ManageTcpServer(myclientTask.GetStream());
                    if (list != null)
                    {
                        list.Add(ManageTcpServer);
                    }
                }
            });

            Console.WriteLine("服务端已经启动...");
            return list;
        }

        /// <summary>
        /// 管理Task
        /// </summary>
        private static void ManageTask()
        {
            while (true)
            {
                if (TaskCount >= 10)
                {
                    //任务太多等待
                    //Console.WriteLine("CurrentThread is too many");
                    Task.WaitAny();
                    break;
                }
                else
                {
                    var url = "http://msdn.microsoft.com";
                    var task = StartRequestAsync(url);
                    UpdateTaskCount();
                    task.ContinueWith(async (t) =>
                    {
                        var Content = await t.Result.Content.ReadAsStringAsync();
                        //Console.WriteLine("CurrentTask {0} is finished,content length is:{1}", 
                        //    url,
                        //    Content.Length);
                        UpdateTaskCount(false);
                        ManageTask();
                    });
                }
            }
        }

        /// <summary>
        /// 异步请求
        /// </summary>
        /// <param name="requestUrl"></param>
        /// <returns></returns>
        private static async Task<HttpResponseMessage> StartRequestAsync(string requestUrl)
        {
            Console.WriteLine("开始执行任务...{0}", Task.CurrentId);
            using (var client = new HttpClient())
            {
                var response = client.GetAsync(requestUrl);
                //Console.WriteLine("Start Request {0}", requestUrl);
                var result = await response;
                Thread.Sleep(5000);
                //Console.WriteLine("Done Request {0}", requestUrl);
                return result;
            }
        }

        /// <summary>
        /// 显示进程列表
        /// </summary>
        private static void ShowProcesses()
        {
            foreach (var p in Process.GetProcesses().OrderBy(m => m.Id))
            {
                try
                {
                    Console.WriteLine("{0}\t{1}\t{2}M\t\t{3}\t{4}",
                       p.Id,
                       p.ProcessName.Trim(),
                       p.WorkingSet64 / 1024.0F / 1024.0F,
                       p.StartTime,
                       p.MainModule.FileName);
                }
                catch (Exception)
                {
                    Console.WriteLine("{0}\t{1}\t{2}M", p.Id, p.ProcessName, p.WorkingSet64 / 1024.0F / 1024.0F);
                    //Console.WriteLine(ex.Message);
                }

            }
        }
    }

    /// <summary>
    /// 服务器连接对象
    /// </summary>
    public class ManageTcpServer
    {
        private NetworkStream NetworkStream { get; set; }

        public ManageTcpServer(NetworkStream networkStream)
        {
            NetworkStream = networkStream;
            StartReaderTask();
        }

        public void Write(string input)
        {
            var buffers = Encoding.UTF8.GetBytes(input);
            NetworkStream.Write(buffers, 0, buffers.Length);
        }

        private void StartReaderTask()
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    //var ip = (IPEndPoint)myclientTask.Client.RemoteEndPoint;

                    while (true)
                    {
                        var chars = new byte[1024];
                        var p = NetworkStream.Read(chars, 0, chars.Length);
                        while (p > 0)
                        {
                            Console.WriteLine(Encoding.UTF8.GetString(chars, 0, p));
                            p = NetworkStream.Read(chars, 0, chars.Length);
                        };
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                finally
                {
                    NetworkStream.Flush();
                };

            });
        }
    }
}
