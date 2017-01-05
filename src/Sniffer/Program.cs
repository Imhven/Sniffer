using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Text;
using System.IO; 
using System.Threading; 

namespace Sniffer
{
    public class Program
    { 
        static void Main(string[] args)
        { 
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);  
             var tasks = new List<Task>();
            var rnd = new Random();
            var ctr = 0; 
            while (true)
            {
                Console.WriteLine("开始创建线程{0}", ctr);
                if (tasks.Count >= 10)
                {
                    Console.WriteLine("当前线程过多");
                    Thread.Sleep(1000);
                }
                else
                {
                    var task = Task.Run(() => StartCode(ctr));
                    task.ContinueWith((t) =>
                    { 
                        Console.WriteLine("当前线程{0}完成", Thread.CurrentThread.ManagedThreadId);
                        tasks.Remove(t); 
                    });
                    task.Wait();
                    tasks.Add(task);
                }
            }
            Console.ReadLine();

        }
        private static void StartCode(object i)
        {
            //Console.WriteLine("开始执行子线程...{0}", i);
            //Thread.Sleep(10000);//模拟代码操作    
        }
    }
}
