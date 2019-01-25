using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    public interface IConnector : IDisposable
    {
        /// <summary>
        /// 伝文の終端文字
        /// </summary>
        string EndString { get; set; }

        /// <summary>
        /// 送受信用文字列ｴﾝｺｰﾄﾞ
        /// </summary>
        Encoding Encoding { get; set; }

        /// <summary>
        /// 接続文字列
        /// </summary>
        string ConnectionString { get; set; }

        /// <summary>
        /// ﾀｲﾑｱｳﾄ時間(ﾐﾘ秒)
        /// </summary>
        int Timeout { get; set; }

        /// <summary>
        /// 通信中かどうか
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// 通信を開始します。
        /// </summary>
        /// <returns></returns>
        Task ConnectAsync();

        /// <summary>
        /// ﾒｯｾｰｼﾞを送信します。
        /// </summary>
        /// <param name="message">送信するﾒｯｾｰｼﾞ</param>
        /// <returns></returns>
        Task WriteAsync(string message);

        /// <summary>
        /// ﾒｯｾｰｼﾞを受信します。
        /// </summary>
        /// <returns>受信したﾒｯｾｰｼﾞ</returns>
        Task<string> ReadAsync();

        /// <summary>
        /// 通信を終了します。
        /// </summary>
        /// <returns></returns>
        Task DisConnectAsync();

    }
}
