using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    public class MCProtcolAccesso2r : IDisposable
    {
        /// <summary>
        /// ﾛｯｸ用ｵﾌﾞｼﾞｪｸﾄ
        /// </summary>
        private readonly object LockObject = new object();

        public MCProtcolAccesso2r(string ipaddress, int port, string station, string pc)
        {
            StationNumber = station;
            PCNumber = pc;

            Socket = new TcpSocket();
            Socket.EndChar = "\r\n";
            Socket.OnReceiveData += OnReceiveData;
            Socket.OnConnected += OnConnected;
            Socket.OnDisconnected += OnDisconnected;
            Socket.Connect(ipaddress, port);
        }

        private TcpSocket Socket { get; set; }

        /// <summary>
        /// 局番号
        /// </summary>
        private string StationNumber { get; set; }

        /// <summary>
        /// PC番号
        /// </summary>
        private string PCNumber { get; set; }

        private bool IsConnected { get; set; }

        private async Task WaitConnected()
        {
            while (!IsConnected)
            {
                await Task.Delay(100);
            }
        }

        private string CreateMessage(string message)
        {
            var cc = (char)05;
            var chksum = Convert.ToString(
                    Socket.Encoding.GetBytes(message)
                        .Select(b => (int)b)
                        .Sum(),
                    16
                )
                .Right(2, '0');

            return $"{cc}{message}{chksum}";
        }

        public async Task SendQM(IEnumerable<int> addresses)
        {
            await WaitConnected();

            Console.WriteLine("{0} SendQM", DateTime.Now.ToString("HH:mm:ss.fff"));
            var count = addresses.Count();
            var devices = addresses
                .Select(i => i.ToString().PadLeft(6, '0'))
                .Select(s => $"D{s}");

            var ac = $"{StationNumber}{PCNumber}";
            var cm = "QM";
            var dl = '0';
            var dc = Convert.ToString(count, 16).Right(2, '0').ToUpper();
            var ds = string.Join("", devices);

            // ｺﾏﾝﾄﾞ送信
            Socket.Send(CreateMessage($"{ac}{cm}{dl}{dc}{ds}"));
        }

        public async Task SendMQ()
        {
            await WaitConnected();

            Console.WriteLine("{0} SendMQ", DateTime.Now.ToString("HH:mm:ss.fff"));
            var ac = $"{StationNumber}{PCNumber}";
            var cm = "MQ";
            var dl = '0';

            // ｺﾏﾝﾄﾞ送信
            Socket.Send(CreateMessage($"{ac}{cm}{dl}"));
        }

        private void OnReceiveData(object sender, string message)
        {
            Console.WriteLine("{0} OnReceiveData", DateTime.Now.ToString("HH:mm:ss.fff"));
            if (string.IsNullOrEmpty(message))
            {
                return;
            }
            else if (message[0] == (char)06)
            {
                // 正常応答：応答データ無し
                Console.WriteLine("正常応答");
            }
            else if (message[0] == (char)15)
            {
                // 異常応答：応答ﾃﾞｰﾀ無し
                Console.WriteLine("異常応答");
            }
            else
            {
                Console.WriteLine("応答有り **************************************************");
                var rxs = Enumerable.Range(0, (message.Length - 10) / 4)
                    .Select(i => message.Mid(i * 4, 4))
                    .Select(s => { /*Console.WriteLine(s); */return s; });
                Console.WriteLine("総件数 {0}", rxs.Count());
            }
        }

        private void OnDisconnected(object sender, EventArgs e)
        {
            Console.WriteLine("{0} OnDisconnected", DateTime.Now.ToString("HH:mm:ss.fff"));
            IsConnected = false;
        }

        private void OnConnected(EventArgs e)
        {
            Console.WriteLine("{0} OnConnected", DateTime.Now.ToString("HH:mm:ss.fff"));
            IsConnected = true;
        }


        #region IDisposable Support
        private bool disposedValue = false; // 重複する呼び出しを検出するには

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: マネージド状態を破棄します (マネージド オブジェクト)。
                    Socket.Close();
                }

                // TODO: アンマネージド リソース (アンマネージド オブジェクト) を解放し、下のファイナライザーをオーバーライドします。
                // TODO: 大きなフィールドを null に設定します。

                disposedValue = true;
            }
        }

        // TODO: 上の Dispose(bool disposing) にアンマネージド リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします。
        // ~MCProtcolAccessor() {
        //   // このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
        //   Dispose(false);
        // }

        // このコードは、破棄可能なパターンを正しく実装できるように追加されました。
        public void Dispose()
        {
            // このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
            Dispose(true);
            // TODO: 上のファイナライザーがオーバーライドされる場合は、次の行のコメントを解除してください。
            // GC.SuppressFinalize(this);
        }
        #endregion


    }
}
