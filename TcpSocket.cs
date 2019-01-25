using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    public class TcpSocket : IDisposable
    {
        /** ****************************************************************************************************
         ** ｲﾍﾞﾝﾄ
         ** ****************************************************************************************************/

        //データ受信イベント
        public delegate void ReceiveEventHandler(object sender, string e);
        public event ReceiveEventHandler OnReceiveData;

        //接続断イベント
        public delegate void DisconnectedEventHandler(object sender, EventArgs e);
        public event DisconnectedEventHandler OnDisconnected;

        //接続OKイベント
        public delegate void ConnectedEventHandler(EventArgs e);
        public event ConnectedEventHandler OnConnected;

        /// <summary>
        /// ｺﾝｽﾄﾗｸﾀ
        /// </summary>
        public TcpSocket()
        {
            OnConnected += (e) => { };
            OnDisconnected += (sender, e) => { };
            OnReceiveData += (sender, e) => { };
        }

        /** ****************************************************************************************************
         ** ﾌﾟﾛﾊﾟﾃｨ
         ** ****************************************************************************************************/

        /// <summary>
        /// ｿｹｯﾄ
        /// </summary>
        private Socket MySocket { get; set; }

        /// <summary>
        /// 受信ﾃﾞｰﾀ保存用ｽﾄﾘｰﾑ
        /// </summary>
        private MemoryStream Stream { get; set; }

        /// <summary>
        /// ﾛｯｸ用ｵﾌﾞｼﾞｪｸﾄ
        /// </summary>
        private readonly object LockObject = new object();

        /// <summary>
        /// 送受信用文字列ｴﾝｺｰﾄﾞ
        /// </summary>
        public Encoding Encoding { get; set; } = Encoding.ASCII; //Encoding.GetEncoding("Shift_JIS");

        /// <summary>
        /// 接続が終了しているかどうか
        /// </summary>
        public bool IsClosed { get { return MySocket == null; } }

        /// <summary>
        /// 受信ﾃﾞｰﾀの終端文字
        /// </summary>
        public string EndChar { get; set; }

        /// <summary>
        /// 最後に接続したIPｱﾄﾞﾚｽ
        /// </summary>
        private IPEndPoint IP { get; set; }

        private bool IsReceiving { get; set; }

        private bool IsSending { get; set; }

        /// <summary>
        /// 指定したIPｱﾄﾞﾚｽ、ﾎﾟｰﾄの接続を開始します。
        /// </summary>
        /// <param name="ipaddress">接続先</param>
        /// <param name="port">ポート</param>
        public void Connect(string ipaddress, int port)
        {
            Console.WriteLine("{0} Connect", DateTime.Now.ToString("HH:mm:ss.fff"));
            // IP作成
            IP = new IPEndPoint(IPAddress.Parse(ipaddress), port);

            // 接続開始
            Reconnect();
        }

        public void Reconnect()
        {
            Console.WriteLine("{0} Reconnect", DateTime.Now.ToString("HH:mm:ss.fff"));

            if (!IsClosed)
                Close();

            IsSending = false;
            IsReceiving = false;

            // Socket生成
            MySocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            MySocket.ReceiveTimeout = 10000;
            MySocket.SendTimeout = 10000;

            // Connect to the remote endpoint.
            MySocket.BeginConnect(
                IP, new AsyncCallback(ConnectCallback), MySocket
            );
        }

        /// <summary>
        /// BeginConnect ｺｰﾙﾊﾞｯｸﾒｿｯﾄﾞ
        /// </summary>
        /// <param name="ar">ｿｹｯﾄ</param>
        private void ConnectCallback(IAsyncResult ar)
        {
            Console.WriteLine("{0} ConnectCallback", DateTime.Now.ToString("HH:mm:ss.fff"));

            // Retrieve the socket from the state object.
            var s = ar.AsyncState as Socket;

            if (s == null)
            {
                throw new ArgumentException("引数がSocketではありません");
            }

            // 接続完了通知
            s.EndConnect(ar);

            //接続OKイベント発生
            OnConnected(new EventArgs());

            // 受信ﾃﾞｰﾀ初期化
            InitializeStream();

            // ﾃﾞｰﾀ受信開始
            BeginReceive();
        }

        /// <summary>
        /// ﾃﾞｰﾀ受信開始
        /// </summary>
        public void InitializeStream()
        {
            Console.WriteLine("{0} InitializeStream", DateTime.Now.ToString("HH:mm:ss.fff"));

            if (Stream != null)
            {
                using (Stream) { }
                Stream = null;
            }

            //受信ﾃﾞｰﾀ初期化
            Stream = new MemoryStream();
        }

        private void BeginReceive()
        {
            Console.WriteLine("{0} BeginReceive", DateTime.Now.ToString("HH:mm:ss.fff"));

            lock (LockObject)
            {
                if (IsClosed)
                    throw new IOException("ﾈｯﾄﾜｰｸが切断しています。");

                IsReceiving = true;

                //受信バッファ
                byte[] buffer = new byte[1024];

                //非同期データ受信開始
                MySocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveDataCallback), buffer);
            }
        }

        /// <summary>
        /// 非同期データ受信
        /// </summary>
        /// <param name="ar"></param>
        private void ReceiveDataCallback(IAsyncResult ar)
        {
            Console.WriteLine("{0} ReceiveDataCallback", DateTime.Now.ToString("HH:mm:ss.fff"));

            int len = -1;
            lock (LockObject)
            {
                if (IsClosed)
                    return;

                // ﾃﾞｰﾀ受信終了
                len = MySocket.EndReceive(ar);

                IsReceiving = false;

            }

            // 切断された
            if (len <= 0)
            {
                Close();
                return;
            }

            //受信データ取り出し
            byte[] buffer = (byte[])ar.AsyncState;

            //受信データ保存
            Stream.Write(buffer, 0, len);

            if (buffer.Any(b => b == EndChar.Last()))
            {
                // 受信ﾃﾞｰﾀに終端文字が含まれる
                var messages = Encoding.GetString(Stream.ToArray())
                    .Split(EndChar.ToArray(), StringSplitOptions.None)
                    .ToArray();

                foreach (var message in messages)
                {
                    if (!string.IsNullOrEmpty(message))
                    {
                        // ﾃﾞｰﾀ受信ｲﾍﾞﾝﾄ
                        OnReceiveData(this, $"{message}{EndChar}");
                    }
                }

                // 受信用ｽﾄﾘｰﾑ初期化
                InitializeStream();

                var last = messages.Last();
                if (!string.IsNullOrEmpty(last))
                {
                    // 最後の受信ﾃﾞｰﾀが全て読み込めてない場合

                    // 受信用ｽﾄﾘｰﾑに最後の文字だけ書き込み
                    Stream.Write(Encoding.GetBytes(last), 0, last.Length);
                }
            }
            //// 終端文字ﾁｪｯｸ
            //if (EndChar.Length <= Stream.Length)
            //{
            //    Stream.Seek(EndChar.Length * -1, SeekOrigin.End);

            //    // 今回読み取ったﾃﾞｰﾀの終端文字が指定した文字であるか
            //    if (EndChar.All(c => c == Stream.ReadByte()))
            //    {
            //        // 受信ﾃﾞｰﾀを文字列に変換
            //        string message = Encoding.GetString(Stream.ToArray());

            //        // 受信用ｽﾄﾘｰﾑ初期化
            //        InitializeStream();

            //        // ﾃﾞｰﾀ受信ｲﾍﾞﾝﾄ
            //        OnReceiveData(this, message);
            //    }

            //    Stream.Seek(0, SeekOrigin.End);
            //}

            //非同期受信を再開始
            BeginReceive();
        }

        /// <summary>
        /// ﾒｯｾｰｼﾞを送信する。
        /// </summary>
        /// <param name="message"></param>
        public bool Send(string message)
        {
            Console.WriteLine("{0} Send", DateTime.Now.ToString("HH:mm:ss.fff"));

            lock (LockObject)
            {
                if (IsClosed)
                    return false;

                IsSending = true;

                //文字列をBYTE配列に変換
                byte[] buffer = Encoding.GetBytes($"{message}{EndChar}");

                // 非同期ﾃﾞｰﾀ送信
                MySocket.BeginSend(buffer, 0, buffer.Length, 0, new AsyncCallback(SendCallBack), MySocket);
                return true;
            }
        }

        private void SendCallBack(IAsyncResult ar)
        {
            Console.WriteLine("{0} SendCallBack", DateTime.Now.ToString("HH:mm:ss.fff"));

            lock (LockObject)
            {
                if (IsClosed)
                    return;

                var s = ar.AsyncState as Socket;

                // 送信完了
                var i = s.EndSend(ar);

                IsSending = false;

                Console.WriteLine(i);
            }
        }

        /// <summary>
        /// ｿｹｯﾄ通信終了
        /// </summary>
        public void Close()
        {

            Console.WriteLine("{0} Close", DateTime.Now.ToString("HH:mm:ss.fff"));

            //Socketを停止
            MySocket.Shutdown(SocketShutdown.Both);
            MySocket.Disconnect(false);
            MySocket.Dispose();
            MySocket = null;

            //受信ﾃﾞｰﾀ初期化
            InitializeStream();

            // 接続断ｲﾍﾞﾝﾄ発生
            OnDisconnected(this, new EventArgs());
        }

        /** ****************************************************************************************************
         ** Disposable Support
         ** ****************************************************************************************************/

        #region IDisposable Support
        private bool disposedValue = false; // 重複する呼び出しを検出するには

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: マネージド状態を破棄します (マネージド オブジェクト)。
                    Close();
                }

                // TODO: アンマネージド リソース (アンマネージド オブジェクト) を解放し、下のファイナライザーをオーバーライドします。
                // TODO: 大きなフィールドを null に設定します。

                disposedValue = true;
            }
        }

        // TODO: 上の Dispose(bool disposing) にアンマネージド リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします。
        // ~TcpSocket() {
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
