using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Main2(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                Console.ReadLine();
            }
        }
        static async void Main2(string[] args)
        {
            //using (var accessor = new MCProtcolAccesso2r("192.168.1.234", 9001, "01", "FF"))
            //{
            //    // ﾓﾆﾀ登録
            //    await accessor.SendQM(Enumerable.Range(0, 255).Select(i => i + 1910));

            //    Console.WriteLine("{0} BeginLoop", DateTime.Now.ToString("HH:mm:ss.fff"));

            //    for (var i = 0; i < 5; i++)
            //    {
            //        // 5回MQ
            //        await accessor.SendMQ();
            //    }

            //    Console.ReadLine();
            //}

            //var s = "aaabbbccc";
            //Console.WriteLine("|{0}|", s.Left(4));
            //Console.WriteLine("|{0}|", s.Mid(4, 3));
            //Console.WriteLine("|{0}|", s.Right(4));
            //Console.WriteLine("|{0}|", s.Left(12));
            //Console.WriteLine("|{0}|", s.Mid(3, 12));
            //Console.WriteLine("|{0}|", s.Right(12));
            //Console.ReadLine();

            // **********************************************************************************
            // 同期バージョン
            //var sp = new Stopwatch();
            //using (var tcp1 = new TcpClient("192.168.1.234", 9001))
            //using (var ns1 = tcp1.GetStream())
            //{
            //    Console.WriteLine(tcp1.Connected);
            //    //ns1.ReadTimeout = 5000;
            //    //ns1.WriteTimeout = 5000;
            //    //ns2.ReadTimeout = 5000;
            //    //ns2.WriteTimeout = 5000;
            //    try
            //    {

            //        // ｱﾄﾞﾚｽ 001910 から連番でいっぱいﾓﾆﾀ登録
            //        await QM(ns1, Enumerable.Range(0, 255)
            //            .Select(i => "D" + (i + 1910).ToString().PadLeft(6, '0'))
            //            .ToArray()
            //        );
            //        //QM(ns2, Enumerable.Range(0, 255)
            //        //        .Select(i => "D" + (i + 1910).ToString().PadLeft(6, '0'))
            //        //        .ToArray()
            //        //);

            //        sp.Start();
            //        for (var index = 0; index < 5; index++)
            //        {
            //            await MQ(ns1);
            //            //MQ(ns2);
            //            Console.WriteLine("{0}", sp.Elapsed.ToString(@"hh\:mm\:ss\.fff"));
            //        }
            //        sp.Reset();
            //    }
            //    catch (Exception ex)
            //    {
            //        Console.WriteLine(ex.ToString());
            //        Console.ReadLine();
            //    }
            //}

            // **********************************************************************************
            // DPC4接続
            using (var connector = new TCPIP4Connector("192.168.1.234", 9001))
            using (var accessor = new MCProtocolA1CF4Accessor(connector))
            {
                Info("1");

                // QM ｺﾏﾝﾄﾞ実行
                await accessor.QMAsync("D", Enumerable.Range(0, 255).Select(i => 1910 + i).ToArray());

                Info("2");

                for (var index = 0; index < 5; index++)
                {
                    Info("3");

                    var devices = await accessor.MQAsync();

                    foreach (var device in devices)
                    {
                        Console.WriteLine("{0,10}", device);
                    }

                    Console.WriteLine("総件数：{0}件", devices.Count());
                }
            }

            //using (var connector = new SerialConnector("192.168.1.234", 9001))
            ////using (var connector = new TCPIP4Connector("192.168.0.1", 9004))
            //using (var accessor = new MCProtocolA1CF4Accessor(connector, "01"))
            //{
            //    Info("1");

            //    // WR ｺﾏﾝﾄﾞ実行
            //    var devices = await accessor.WRAsync("D", 100, 16);

            //    foreach (var device in devices)
            //    {
            //        Console.WriteLine("{0,10}", device);
            //    }

            //    Console.WriteLine("総件数：{0}件", devices.Count());

            //    //// QM ｺﾏﾝﾄﾞ実行
            //    //await accessor.WMAsync("D", Enumerable.Range(0, 64).Select(i => 100 + i).ToArray());

            //    //Info("2");

            //    //for (var index = 0; index < 5; index++)
            //    //{
            //    //    Info("3");

            //    //    var devices = await accessor.MNAsync();

            //    //    foreach (var device in devices)
            //    //    {
            //    //        Console.WriteLine("{0,10}", device);
            //    //    }

            //    //    Console.WriteLine("総件数：{0}件", devices.Count());
            //    //}
            //}


        }

        private static async Task QM(NetworkStream ns, string[] devices)
        {
            Debug.WriteLine("{0} QM", DateTime.Now.ToString("HH:mm:ss.fff"));

            var qm = "QM";
            var ac = "01FF";
            var dc = Convert.ToString(devices.Count(), 16).Right(2, '0').ToUpper();
            var dt = string.Join("", devices);
            var message = $"{ac}{qm}0{dc}{dt}";

            var rx = Encoding.ASCII.GetString(await Send(ns, message));

            if (string.IsNullOrWhiteSpace(rx))
            {
                // 異常応答：応答ﾃﾞｰﾀ無し
                Console.WriteLine("QM: 異常応答→応答データ無し");
            }
            else if (rx[0] == (char)06)
            {
                // 正常応答：応答データ無し
                Console.WriteLine("QM: 正常応答");
            }
            else if (rx[0] == (char)15)
            {
                // 異常応答：応答ﾃﾞｰﾀ無し
                Console.WriteLine("QM: 異常応答");
            }
            else
            {
                Console.WriteLine("QM: 応答有り **************************************************");
                var rxs = Enumerable.Range(0, (rx.Length - 10) / 4)
                    .Select(i => rx.Mid(i * 4, 4))
                    .Select(s => { Console.WriteLine(s); return s; })
                    .ToArray();
                Console.WriteLine("QM: 総件数 {0}", rxs.Length);
            }
        }

        private static async Task MQ(NetworkStream ns)
        {
            Debug.WriteLine("{0} MQ", DateTime.Now.ToString("HH:mm:ss.fff"));

            try
            {
                var rx = Encoding.ASCII.GetString(await Send(ns, "01FFMQ0"));

                Debug.WriteLine("{0} MQ2", DateTime.Now.ToString("HH:mm:ss.fff"));

                if (rx[0] == (char)06)
                {
                    // 正常応答：応答データ無し
                    Console.WriteLine("MQ:正常応答");
                }
                else if (rx[0] == (char)15)
                {
                    // 異常応答：応答ﾃﾞｰﾀ無し
                    Console.WriteLine("MQ:異常応答");
                }
                else
                {
                    Console.WriteLine("MQ:応答有り **************************************************");
                    var rxs = Enumerable.Range(0, (rx.Length - 10) / 4)
                        .Select(i => rx.Mid(i * 4, 4))
                        .Select(s => { Console.WriteLine(s); return s; })
                        .ToArray();
                    Console.WriteLine("MQ: 総件数 {0}", rxs.Length);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        private static async Task<byte[]> Send(NetworkStream ns, string message)
        {
            Debug.WriteLine("{0} Send1", DateTime.Now.ToString("HH:mm:ss.fff"));

            var chksum = Convert.ToString(
                Encoding.ASCII.GetBytes(message)
                    .Sum(b => (int)b), 16)
                    .Right(2, '0');

            var Tx = Encoding.ASCII.GetBytes(
                string.Format("{0}{1}{2}{3}", (char)05, message, chksum, "\r\n")
            );

            Debug.WriteLine("{0} Send2", DateTime.Now.ToString("HH:mm:ss.fff"));

            await ns.WriteAsync(Tx, 0, Tx.Length);

            Debug.WriteLine("{0} Send3", DateTime.Now.ToString("HH:mm:ss.fff"));

            using (var ms = new MemoryStream())
            {
                var Rx = new byte[256];
                var size = 0;
                try
                {
                    Debug.WriteLine("{0} Send4", DateTime.Now.ToString("HH:mm:ss.fff"));

                    while ((size = await ns.ReadAsync(Rx, 0, Rx.Length)) != 0)
                    {
                        Debug.WriteLine("{0} Send5", DateTime.Now.ToString("HH:mm:ss.fff"));

                        await ms.WriteAsync(Rx, 0, size);

                        Debug.WriteLine("{0} Send6", DateTime.Now.ToString("HH:mm:ss.fff"));

                        // 読み取った最後の文字が改行文字なら終了
                        if (Rx[size - 1] == (byte)'\n') break;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }

                Debug.WriteLine("{0} Send7", DateTime.Now.ToString("HH:mm:ss.fff"));
                Debug.WriteLine(ms.ToArray().Count());

                // ﾊﾞｲﾄ配列を取得
                return ms.ToArray();
            }
        }

        public static void Info(string message = "",
            [CallerMemberName] string callerMemberName = "",
            [CallerFilePath]   string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            Console.WriteLine(string.Format("[INFO][{0:yy/MM/dd HH:mm:ss.fff}][{1}][{2}][{3}]\n{4}", DateTime.Now, callerFilePath, callerMemberName, callerLineNumber, message));
        }

    }
}
