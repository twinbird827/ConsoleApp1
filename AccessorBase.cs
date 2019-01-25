using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    public abstract class AccessorBase : IAccessor
    {
        /** ****************************************************************************************************
         ** ｺﾝｽﾄﾗｸﾀ
         ** ****************************************************************************************************/

        public AccessorBase(IConnector connector)
        {
            Connector = connector;
        }

        /** ****************************************************************************************************
         ** ﾌﾟﾛﾊﾟﾃｨ
         ** ****************************************************************************************************/

        /// <summary>
        /// ｺﾈｸﾀ
        /// </summary>
        public IConnector Connector { get; set; }

        /// <summary>
        /// 伝文の終端文字
        /// </summary>
        public string EndString
        {
            get { return Connector.EndString; }
            set { Connector.EndString = value; }
        }

        /// <summary>
        /// 送受信用文字列ｴﾝｺｰﾄﾞ
        /// </summary>
        public Encoding Encoding
        {
            get { return Connector.Encoding; }
            set { Connector.Encoding = value; }
        }

        /// <summary>
        /// 接続文字列
        /// </summary>
        public string ConnectionString
        {
            get { return Connector.ConnectionString; }
            set { Connector.ConnectionString = value; }
        }

        /// <summary>
        /// ﾀｲﾑｱｳﾄ時間(ﾐﾘ秒)
        /// </summary>
        public int Timeout
        {
            get { return Connector.Timeout; }
            set { Connector.Timeout = value; }
        }

        /// <summary>
        /// 通信中かどうか
        /// </summary>
        public bool IsConnected
        {
            get { return Connector.IsConnected; }
        }

        /** ****************************************************************************************************
         ** ﾒｿｯﾄﾞ
         ** ****************************************************************************************************/

        /// <summary>
        /// 通信を開始します。
        /// </summary>
        /// <returns></returns>
        public virtual async Task ConnectAsync()
        {
            await Connector.ConnectAsync();
        }

        /// <summary>
        /// 通信を終了します。
        /// </summary>
        /// <returns></returns>
        public virtual async Task DisConnectAsync()
        {
            await Connector.DisConnectAsync();
        }

        /// <summary>
        /// ﾒｯｾｰｼﾞを受信します。
        /// </summary>
        /// <returns>受信したﾒｯｾｰｼﾞ</returns>
        public virtual async Task<string> ReadAsync()
        {
            return await Connector.ReadAsync();
        }

        /// <summary>
        /// ﾒｯｾｰｼﾞを送信します。
        /// </summary>
        /// <param name="message">送信するﾒｯｾｰｼﾞ</param>
        /// <returns></returns>
        public virtual async Task WriteAsync(string message)
        {
            await Connector.WriteAsync(message);
        }

        /** ****************************************************************************************************
         ** IDisposable Support
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
                    if (Connector != null)
                    {
                        Connector.Dispose();
                    }
                }

                // TODO: アンマネージド リソース (アンマネージド オブジェクト) を解放し、下のファイナライザーをオーバーライドします。
                // TODO: 大きなフィールドを null に設定します。

                disposedValue = true;
            }
        }

        // TODO: 上の Dispose(bool disposing) にアンマネージド リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします。
        // ~AccessorBase() {
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
