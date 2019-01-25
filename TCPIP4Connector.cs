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
    public class TCPIP4Connector : IConnector
    {
        /** ****************************************************************************************************
         ** ｺﾝｽﾄﾗｸﾀ
         ** ****************************************************************************************************/

        /// <summary>
        /// ｺﾝｽﾄﾗｸﾀ
        /// </summary>
        /// <param name="connectionString">接続文字列。これは、「ipaddress:port」の形式である必要があります。</param>
        /// <example>192.168.1.100:9001</example>
        public TCPIP4Connector(string connectionString)
        {
            ConnectionString = connectionString;

            var buf = ConnectionString.Split(':');

            if (buf.Length != 2)
            {
                throw new FormatException("接続文字列の書式が[ipaddress:port]ではありません。");
            }

            IP = new IPEndPoint(IPAddress.Parse(buf[0]), int.Parse(buf[1]));
        }

        /// <summary>
        /// ｺﾝｽﾄﾗｸﾀ
        /// </summary>
        /// <param name="ipaddress">IPｱﾄﾞﾚｽ</param>
        /// <param name="port">ﾎﾟｰﾄ</param>
        public TCPIP4Connector(string ipaddress, int port) : this($"{ipaddress}:{port}")
        {

        }

        /** ****************************************************************************************************
         ** ﾌﾟﾛﾊﾟﾃｨ
         ** ****************************************************************************************************/

        /// <summary>
        /// 伝文の終端文字
        /// </summary>
        public string EndString { get; set; } = "\r\n";

        /// <summary>
        /// 送受信用文字列ｴﾝｺｰﾄﾞ
        /// </summary>
        public Encoding Encoding { get; set; } = Encoding.GetEncoding("Shift_JIS");

        /// <summary>
        /// 接続文字列
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// ﾀｲﾑｱｳﾄ時間
        /// </summary>
        public int Timeout { get; set; } = 3000;

        /// <summary>
        /// 接続が確立しているかどうか
        /// </summary>
        public bool IsConnected
        {
            get
            {
                if (TCP == null)
                {
                    return false;
                }
                if (!TCP.Connected)
                {
                    return false;
                }

                return true;
            }
        }

        /// <summary>
        /// ｴﾝﾄﾞﾎﾟｲﾝﾄ
        /// </summary>
        private IPEndPoint IP { get; set; }

        /// <summary>
        /// TCPｸﾗｲｱﾝﾄ
        /// </summary>
        private TcpClient TCP { get; set; }

        /// <summary>
        /// 送受信用ｽﾄﾘｰﾑ
        /// </summary>
        private NetworkStream Stream { get; set; }

        /// <summary>
        /// ﾛｯｸ用ｾﾏﾌｫ
        /// </summary>
        private SemaphoreSlim Semaphore = new SemaphoreSlim(1, 1);

        /** ****************************************************************************************************
         ** 公開ﾒｿｯﾄﾞ
         ** ****************************************************************************************************/

        /// <summary>
        /// 通信を確立します。
        /// </summary>
        public async Task ConnectAsync()
        {
            Program.Info("1");

            if (IsConnected)
            {
                // 既に通信している場合は中断
                return;
            }
            else if (TCP != null)
            {
                // ｺﾞﾐ掃除
                await DisConnectAsync();
            }

            Program.Info("2");

            // ｲﾝｽﾀﾝｽ生成
            TCP = new TcpClient();
            TCP.ReceiveTimeout = Timeout;
            TCP.SendTimeout = Timeout;

            Program.Info("3");

            // 通信開始
            await TCP.ConnectAsync(IP.Address, IP.Port);

            // 通信用ｽﾄﾘｰﾑ作成
            Stream = TCP.GetStream();
            Stream.ReadTimeout = Timeout;
            Stream.WriteTimeout = Timeout;
        }

        /// <summary>
        /// 通信を切断します。
        /// </summary>
        public async Task DisConnectAsync()
        {
            Program.Info("1");

            await Semaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                Program.Info("2");

                // 通信を安全に切断する。
                DisConnect();
            }
            finally
            {
                Semaphore.Release();
            }
        }

        /// <summary>
        /// 通信を切断します。
        /// </summary>
        private void DisConnect()
        {
            if (Stream != null)
            {
                Stream.Dispose();
                Stream = null;
            }
            if (TCP != null)
            {
                TCP.Close();
                TCP = null;
            }
        }

        /// <summary>
        /// 非同期受信を開始して、結果を返却します。
        /// </summary>
        public async Task<string> ReadAsync()
        {
            Program.Info("1");

            await Semaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                Program.Info("2");

                await ConnectAsync();

                using (var buffer = new MemoryStream())
                {
                    var bytes = new byte[256];
                    var size = 0;

                    Program.Info("3");

                    // 読取ﾃﾞｰﾀがある間繰り返す。
                    while ((size = await Stream.ReadAsync(bytes, 0, bytes.Length)) != 0)
                    {
                        await buffer.WriteAsync(bytes, 0, size);

                        if (buffer.Length < EndString.Length)
                        {
                            // 終端文字の数だけ読み取っていない場合は次ﾙｰﾌﾟへ
                            continue;
                        }

                        // 終端文字の数だけｼｰｸ位置を戻す。
                        buffer.Seek(EndString.Length * -1, SeekOrigin.End);

                        if (EndString.All(c => c == buffer.ReadByte()))
                        {
                            // 読み取った最後の文字がEndStringと同値なら終了
                            break;
                        }
                        else
                        {
                            // ｼｰｸ位置を戻す。
                            buffer.Seek(0, SeekOrigin.End);
                        }
                    }

                    Program.Info("4");

                    // ﾒｯｾｰｼﾞ取得
                    var message = buffer.ToArray().ToString(Encoding).ToUpper();

                    // 終端文字を除外して返却
                    return message.Left(message.Length - EndString.Length);
                }
            }
            finally
            {
                Semaphore.Release();
            }
        }

        /// <summary>
        /// 非同期送信を実行します。
        /// </summary>
        public async Task WriteAsync(string message)
        {
            Program.Info("1");

            await Semaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                Program.Info("2");

                await WriteAsyncNoLock(message);
            }
            catch (Exception ex)
            {
                Program.Info("3");

                Console.WriteLine(ex);

                // 送信失敗時は再接続して再送する。
                DisConnect();
                await WriteAsyncNoLock(message);
                
            }
            finally
            {
                Semaphore.Release();
            }
        }

        /// <summary>
        /// 非同期送信をｱﾝｾｰﾌで実行します。
        /// </summary>
        private async Task WriteAsyncNoLock(string message)
        {
            await ConnectAsync();

            if (message.EndsWith(EndString))
            {
                // 送信文字作成+ﾊﾞｲﾄ変換
                var messageBytes = message.ToUpper().ToBytes(Encoding);

                // 送信処理
                await Stream.WriteAsync(messageBytes, 0, messageBytes.Length);
            }
            else
            {
                // 終端文字を追加して再帰
                await WriteAsyncNoLock($"{message}{EndString}");
            }
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
                    DisConnect();
                }

                // TODO: アンマネージド リソース (アンマネージド オブジェクト) を解放し、下のファイナライザーをオーバーライドします。
                // TODO: 大きなフィールドを null に設定します。

                disposedValue = true;
            }
        }

        // TODO: 上の Dispose(bool disposing) にアンマネージド リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします。
        // ~TCPIP4Connector() {
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
