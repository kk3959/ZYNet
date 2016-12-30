﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZYNet.CloudSystem;
using ZYNet.CloudSystem.Client;
using ZYNet.CloudSystem.Frame;
using ZYNet.CloudSystem.SocketClient;

namespace ZYNETClientForNetCore
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            LogAction.LogOut += LogAction_LogOut;
            CloudClient client = new CloudClient(new SocketClient(), 500000, 1024 * 1024); //最大数据包能够接收 1M
            PackHander tmp = new PackHander();
            client.Install(tmp);
            client.Disconnect += Client_Disconnect;

            if (client.Connect("127.0.0.1", 2285))
            {
                ZYSync Sync = client.Sync;
                IPacker ServerPack = Sync.Get<IPacker>();

                var res = ServerPack.IsLogOn("AAA", "BBB")?[0]?.Value<bool>();

                if (res != null && res == true)
                {

                    var html = ServerPack.StartDown("http://www.baidu.com")?[0]?.Value<string>();
                    if (html != null)
                    {
                        Console.WriteLine("BaiduHtml:" + html.Length);

                        var time = ServerPack.GetTime();

                        Console.WriteLine("ServerTime:" + time);

                        ServerPack.SetPassWord("123123");

                        var x = ServerPack.StartDown("http://www.qq.com");

                        Console.WriteLine("QQHtml:" + x.First.Value<string>().Length);

                        System.Diagnostics.Stopwatch stop = new System.Diagnostics.Stopwatch();
                        stop.Start();
                        var rec = ServerPack.TestRec2(10000);
                        stop.Stop();

                        Console.WriteLine("Rec:{0} time:{1} MS", rec, stop.ElapsedMilliseconds);

                    }
                }

                Console.ReadLine();
            }

        }

        private static void LogAction_LogOut(string msg, LogType type)
        {
            Console.WriteLine(msg);
        }

        private static void Client_Disconnect(string obj)
        {
            Console.WriteLine(obj);
        }
    }
}