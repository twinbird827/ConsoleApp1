using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    /// <summary>
    /// [A 互換1C ﾌﾚｰﾑ/形式4] のMCﾌﾟﾛﾄｺﾙに対してｱｸｾｽするためのｸﾗｽです。
    /// </summary>
    public class MCProtocolA1CF4Accessor : AccessorBase
    {
        /** ****************************************************************************************************
         ** ｺﾝｽﾄﾗｸﾀ
         ** ****************************************************************************************************/

        public MCProtocolA1CF4Accessor(IConnector connector, string station = "01", string pc = "FF") : base(connector)
        {
            StationNumber = station;
            PCNumber = pc;
        }

        /** ****************************************************************************************************
         ** ﾌﾟﾛﾊﾟﾃｨ
         ** ****************************************************************************************************/

        /// <summary>
        /// 要求伝文の先頭文字
        /// </summary>
        public char ReqBeginChar { get; set; } = (char)05;

        /// <summary>
        /// 応答伝文の先頭文字 (応答ﾃﾞｰﾀあり)
        /// </summary>
        public char ResBeginInDataChar { get; set; } = (char)02;

        /// <summary>
        /// 応答伝文の先頭文字 (応答ﾃﾞｰﾀなし)
        /// </summary>
        public char ResBeginNoDataChar { get; set; } = (char)06;

        /// <summary>
        /// 応答伝文の先頭文字 (異常終了)
        /// </summary>
        public char ResBeginErrorChar { get; set; } = (char)15;

        /// <summary>
        /// 局番号
        /// </summary>
        public string StationNumber { get; set; }

        /// <summary>
        /// PC番号
        /// </summary>
        public string PCNumber { get; set; }

        /// <summary>
        /// ﾒｯｾｰｼﾞ送信時に全て大文字に変換するかどうか
        /// </summary>
        public bool IsUpper { get; set; } = true;

        /// <summary>
        /// ｱｸｾｽ経路
        /// </summary>
        public string AccessRoute
        {
            get { return $"{StationNumber}{PCNumber}"; }
        }

        /// <summary>
        /// ﾓﾆﾀ登録しているﾃﾞﾊﾞｲｽ位置の配列
        /// </summary>
        public int[] Monitoes { get; private set; }

        /** ****************************************************************************************************
         ** 内部ﾒｿｯﾄﾞ
         ** ****************************************************************************************************/

        /// <summary>
        /// ﾁｪｯｸｻﾑ対象ﾒｯｾｰｼﾞのﾁｪｯｸｻﾑを計算します。
        /// </summary>
        /// <param name="chksumMessage">ﾁｪｯｸｻﾑ対象ﾒｯｾｰｼﾞ</param>
        private string CalcCheckSum(string chksumMessage)
        {
            return chksumMessage.ToBytes(Encoding).Sum(b => (int)b).ToHex();
        }

        /// <summary>
        /// 応答ﾒｯｾｰｼﾞを読取ﾃﾞｰﾀ単位の配列に変換します。
        /// </summary>
        /// <param name="responseMessage">応答ﾒｯｾｰｼﾞ</param>
        /// <returns></returns>
        private string[] GetInData(string responseMessage)
        {
            if (string.IsNullOrEmpty(responseMessage))
            {
                // 応答ﾒｯｾｰｼﾞがない
                return default(string[]);
            }
            else if (responseMessage.Length % 4 != 0)
            {
                // 4文字区切りではない
                return default(string[]);
            }
            else
            {
                // 4ﾊﾞｲﾄ単位で配列にして返却
                return Enumerable.Range(0, responseMessage.Length / 4)
                    .Select(i => responseMessage.Mid(i * 4, 4))
                    .ToArray();
            }
        }

        /// <summary>
        /// 要求ﾒｯｾｰｼﾞを作成します。
        /// </summary>
        /// <param name="message">元となるﾒｯｾｰｼﾞ</param>
        /// <returns></returns>
        private string CreateRequestMessage(string message)
        {
            if (IsUpper)
            {
                return message.ToUpper();
            }
            else
            {
                return message.ToLower();
            }
        }

        /** ****************************************************************************************************
         ** 公開ﾒｿｯﾄﾞ(override)
         ** ****************************************************************************************************/

        /// <summary>
        /// 要求ﾒｯｾｰｼﾞを「ｺﾝﾄﾛｰﾙｺｰﾄﾞ+ｱｸｾｽ経路+要求ﾒｯｾｰｼﾞ+ﾁｪｯｸｻﾑ+終端文字」に整形して送信します。
        /// </summary>
        /// <param name="requestMessage">要求ﾒｯｾｰｼﾞ</param>
        public override async Task WriteAsync(string requestMessage)
        {
            // ﾁｪｯｸｻﾑ対象の文字列
            var chksumString = CreateRequestMessage($"{AccessRoute}{requestMessage}");

            // ﾁｪｯｸｻﾑ結果(対象文字全ﾊﾞｲﾄの合計の下2桁)
            var chksum = CalcCheckSum(chksumString);

            // 送信文字作成
            var message = CreateRequestMessage($"{ReqBeginChar}{chksumString}{chksum}");

            // 送信処理
            await base.WriteAsync(message);
        }

        /// <summary>
        /// 受信ﾒｯｾｰｼﾞから応答ﾒｯｾｰｼﾞのみ抽出して取得します。
        /// 戻り値がnullの場合、異常伝文を表します。
        /// </summary>
        /// <returns></returns>
        public async override Task<string> ReadAsync()
        {
            var message = await base.ReadAsync();

            if (string.IsNullOrEmpty(message))
            {
                return string.Empty;
            }

            // ｺﾝﾄﾛｰﾙｺｰﾄﾞを取得
            var first = message.First();

            if (first == ResBeginErrorChar)
            {
                // 異常伝文
                return null;
            }
            else if (first == ResBeginNoDataChar)
            {
                // 正常伝文(応答ﾒｯｾｰｼﾞなし)
                return string.Empty;
            }
            else if (first == ResBeginInDataChar)
            {
                // 正常伝文(応答ﾒｯｾｰｼﾞあり)
                // 応答ﾒｯｾｰｼﾞ(ｱｸｾｽ経路+応答ﾃﾞｰﾀ+ｺﾝﾄﾛｰﾙｺｰﾄﾞ)
                var responseMessage = message.Mid(1, message.Length - 2 - 1);

                // ﾁｪｯｸｻﾑ(計算値と戻り値)
                var calChksum = CalcCheckSum(responseMessage).ToUpper();
                var retChksum = message.Mid(message.Length - 2, 2).ToUpper();
                if (calChksum.Equals(retChksum))
                {
                    // ﾁｪｯｸｻﾑが一致=応答ﾒｯｾｰｼﾞ単体で返却(応答ﾒｯｾｰｼﾞから接頭辞のｱｸｾｽ経路と末尾のｺﾝﾄﾛｰﾙｺｰﾄﾞを除外)
                    return responseMessage.Mid(AccessRoute.Length, responseMessage.Length - AccessRoute.Length - 1);
                }
                else
                {
                    // ﾁｪｯｸｻﾑが不一致(異常)
                    return null;
                }
            }
            else
            {
                // 認識外のｺﾝﾄﾛｰﾙｺｰﾄﾞ(異常)
                return null;
            }
        }

        /** ****************************************************************************************************
         ** 公開ﾒｿｯﾄﾞ(not override)
         ** ****************************************************************************************************/

        /// <summary>
        /// WRｺﾏﾝﾄﾞ(一括読み出し)を実行します。
        /// </summary>
        /// <param name="identifer">ﾃﾞﾊﾞｲｽ識別子</param>
        /// <param name="device">先頭ﾃﾞﾊﾞｲｽ</param>
        /// <param name="count">ﾃﾞﾊﾞｲｽ点数</param>
        public async Task<string[]> WRAsync(string identifer, int device, int count)
        {
            Program.Info("1");
            return await WRQRAsync("WR", identifer, device.ToHex(4), count);
        }

        /// <summary>
        /// QRｺﾏﾝﾄﾞ(一括読み出し)を実行します。
        /// </summary>
        /// <param name="identifer">ﾃﾞﾊﾞｲｽ識別子</param>
        /// <param name="device">先頭ﾃﾞﾊﾞｲｽ</param>
        /// <param name="count">ﾃﾞﾊﾞｲｽ点数</param>
        public async Task<string[]> QRAsync(string identifer, int device, int count)
        {
            Program.Info("1");
            return await WRQRAsync("QR", identifer, device.ToHex(6), count);
        }

        /// <summary>
        /// WR/QRｺﾏﾝﾄﾞ(一括読み出し)を実行します。
        /// </summary>
        /// <param name="command">ｺﾏﾝﾄﾞ</param>
        /// <param name="identifer">ﾃﾞﾊﾞｲｽ識別子</param>
        /// <param name="device">先頭ﾃﾞﾊﾞｲｽ</param>
        /// <param name="count">ﾃﾞﾊﾞｲｽ点数</param>
        /// <returns></returns>
        private async Task<string[]> WRQRAsync(string command, string identifer, string device, int count)
        {
            var wait = "0";
            var countString = count.ToHex();
            var requestMessage = $"{command}{wait}{identifer}{device}{countString}";

            // 要求ﾒｯｾｰｼﾞ送信
            await WriteAsync(requestMessage);

            // 応答ﾒｯｾｰｼﾞ受信
            var responseMessage = await ReadAsync();

            if (string.IsNullOrEmpty(responseMessage))
            {
                // 応答ﾒｯｾｰｼﾞがない
                return default(string[]);
            }
            else if (responseMessage.Length % 4 != 0)
            {
                // 4文字区切りではない
                return default(string[]);
            }
            else
            {
                return Enumerable.Range(0, responseMessage.Length / 4)
                    .Select(i => responseMessage.Mid(i * 4, 4))
                    .ToArray();
            }
        }

        /// <summary>
        /// WWｺﾏﾝﾄﾞ(一括書き込み)を実行します。
        /// </summary>
        /// <param name="identifer">識別子</param>
        /// <param name="device">先頭ﾃﾞﾊﾞｲｽ</param>
        /// <param name="devices">ﾃﾞﾊﾞｲｽ点数分の書き込みﾃﾞｰﾀ</param>
        /// <returns></returns>
        public async Task<bool> WWAsync(string identifer, int device, string[] devices)
        {
            Program.Info("1");
            return await WWQWAsync("WW", identifer, device.ToHex(4), devices);
        }

        /// <summary>
        /// QWｺﾏﾝﾄﾞ(一括書き込み)を実行します。
        /// </summary>
        /// <param name="identifer">識別子</param>
        /// <param name="device">先頭ﾃﾞﾊﾞｲｽ</param>
        /// <param name="devices">ﾃﾞﾊﾞｲｽ点数分の書き込みﾃﾞｰﾀ</param>
        /// <returns></returns>
        public async Task<bool> QWAsync(string identifer, int device, string[] devices)
        {
            Program.Info("1");
            return await WWQWAsync("QW", identifer, device.ToHex(6), devices);
        }

        /// <summary>
        /// WW/QWｺﾏﾝﾄﾞ(一括書き込み)を実行します。
        /// </summary>
        /// <param name="command">ｺﾏﾝﾄﾞ</param>
        /// <param name="identifer">識別子</param>
        /// <param name="device">先頭ﾃﾞﾊﾞｲｽ</param>
        /// <param name="devices">ﾃﾞﾊﾞｲｽ点数分の書き込みﾃﾞｰﾀ</param>
        /// <returns></returns>
        private async Task<bool> WWQWAsync(string command, string identifer, string device, string[] devices)
        {
            var wait = "0";
            var countString = devices.Length.ToHex();
            var deviceString = string.Join("", devices);
            var requestMessage = $"{command}{wait}{identifer}{deviceString}{countString}";

            // 要求ﾒｯｾｰｼﾞ送信
            await WriteAsync(requestMessage);

            // 応答ﾒｯｾｰｼﾞ受信
            var responseMessage = await ReadAsync();

            // 結果(正常応答ならtrue)
            return string.Empty.Equals(responseMessage);
        }

        /// <summary>
        /// WTｺﾏﾝﾄﾞ(ﾗﾝﾀﾞﾑ書き込み)を実行します。
        /// </summary>
        /// <param name="identifer">識別子</param>
        /// <param name="devices">「ﾃﾞﾊﾞｲｽ位置(10進):書き込みﾃﾞｰﾀ(16進)」の配列</param>
        /// <returns></returns>
        public async Task<bool> WTAsync(string identifer, string[] devices)
        {
            Program.Info("1");
            return await WTQTAsync("WT", identifer, devices, 4);
        }

        /// <summary>
        /// QTｺﾏﾝﾄﾞ(ﾗﾝﾀﾞﾑ書き込み)を実行します。
        /// </summary>
        /// <param name="identifer">識別子</param>
        /// <param name="devices">「ﾃﾞﾊﾞｲｽ位置(10進):書き込みﾃﾞｰﾀ(16進)」の配列</param>
        /// <returns></returns>
        public async Task<bool> QTAsync(string identifer, string[] devices)
        {
            Program.Info("1");
            return await WTQTAsync("QT", identifer, devices, 6);
        }

        /// <summary>
        /// WT/QTｺﾏﾝﾄﾞ(ﾗﾝﾀﾞﾑ書き込み)を実行します。
        /// </summary>
        /// <param name="command">ｺﾏﾝﾄﾞ</param>
        /// <param name="identifer">識別子</param>
        /// <param name="devices">「ﾃﾞﾊﾞｲｽ位置(10進):書き込みﾃﾞｰﾀ(16進)」の配列</param>
        /// <param name="length">ﾃﾞﾊﾞｲｽ位置の16進長さ</param>
        /// <returns></returns>
        private async Task<bool> WTQTAsync(string command, string identifer, string[] devices, int length)
        {
            var wait = "0";
            var countString = devices.Length.ToHex();
            var deviceString = string.Join("", devices
                .Select(d =>
                {
                    var split = d.Split(':');
                    var c = int.Parse(split[0]).ToHex(length);
                    var s = split[1];
                    return $"{identifer}{c}{s}";
                }));
            var requestMessage = $"{command}{wait}{countString}{deviceString}";

            // 要求ﾒｯｾｰｼﾞ送信
            await WriteAsync(requestMessage);

            // 応答ﾒｯｾｰｼﾞ受信
            var responseMessage = await ReadAsync();

            // 結果(正常応答ならtrue)
            return string.Empty.Equals(responseMessage);
        }

        /// <summary>
        /// WMｺﾏﾝﾄﾞ(ﾓﾆﾀ登録)を実行します。
        /// </summary>
        /// <param name="identifer">識別子</param>
        /// <param name="devices">ﾓﾆﾀ登録するﾃﾞﾊﾞｲｽ位置(10進)の配列</param>
        /// <returns></returns>
        public async Task<bool> WMAsync(string identifer, int[] devices)
        {
            Program.Info("1");
            return await WMQMAsync("WM", identifer, devices, 4);
        }

        /// <summary>
        /// QMｺﾏﾝﾄﾞ(ﾓﾆﾀ登録)を実行します。
        /// </summary>
        /// <param name="identifer">識別子</param>
        /// <param name="devices">ﾓﾆﾀ登録するﾃﾞﾊﾞｲｽ位置(10進)の配列</param>
        /// <returns></returns>
        public async Task<bool> QMAsync(string identifer, int[] devices)
        {
            Program.Info("1");
            return await WMQMAsync("QM", identifer, devices, 6);
        }

        /// <summary>
        /// WM/QMｺﾏﾝﾄﾞ(ﾓﾆﾀ登録)を実行します。
        /// </summary>
        /// <param name="command">ｺﾏﾝﾄﾞ</param>
        /// <param name="identifer">識別子</param>
        /// <param name="devices">ﾓﾆﾀ登録するﾃﾞﾊﾞｲｽ位置(10進)の配列</param>
        /// <param name="length">ﾃﾞﾊﾞｲｽ位置の16進長さ</param>
        /// <returns></returns>
        private async Task<bool> WMQMAsync(string command, string identifer, int[] devices, int length)
        {
            var wait = "0";
            var countString = devices.Length.ToHex();
            var deviceString = string.Join("", devices.Select(i => identifer + i.ToString().PadLeft(length, '0')));
            var requestMessage = $"{command}{wait}{countString}{deviceString}";

            // 要求ﾒｯｾｰｼﾞ送信
            await WriteAsync(requestMessage);

            // 応答ﾒｯｾｰｼﾞ受信
            var responseMessage = await ReadAsync();

            // 結果(正常応答ならtrue)
            if (string.Empty.Equals(responseMessage))
            {
                // ﾌﾟﾛﾊﾟﾃｨに登録したﾃﾞﾊﾞｲｽ位置を保存
                Monitoes = devices;

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// MNｺﾏﾝﾄﾞ(ﾓﾆﾀ取得)を実行します。
        /// </summary>
        /// <returns></returns>
        public async Task<string[]> MNAsync()
        {
            Program.Info("1");
            return await MNMQAsync("MN");
        }

        /// <summary>
        /// MQｺﾏﾝﾄﾞ(ﾓﾆﾀ取得)を実行します。
        /// </summary>
        /// <returns></returns>
        public async Task<string[]> MQAsync()
        {
            Program.Info("1");
            return await MNMQAsync("MQ");
        }

        /// <summary>
        /// MN/MQｺﾏﾝﾄﾞ(ﾓﾆﾀ取得)を実行します。
        /// </summary>
        /// <param name="command">ｺﾏﾝﾄﾞ</param>
        /// <returns></returns>
        private async Task<string[]> MNMQAsync(string command)
        {
            var wait = "0";
            var requestMessage = $"{command}{wait}";

            // 要求ﾒｯｾｰｼﾞ送信
            await WriteAsync(requestMessage);

            // 応答ﾒｯｾｰｼﾞ受信
            var responseMessages = GetInData(await ReadAsync());

            if (Monitoes.Length == responseMessages.Length)
            {
                // ﾃﾞﾊﾞｲｽ位置(10進):応答ﾃﾞｰﾀの配列形式で返却
                return Monitoes
                    .Zip(responseMessages, (m, r) => m + ":" + r)
                    .ToArray();
            }
            else
            {
                return default(string[]);
            }
        }
    }

}
