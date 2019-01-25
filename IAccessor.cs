using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    public interface IAccessor : IConnector
    {
        /// <summary>
        /// 接続用ｲﾝｽﾀﾝｽ
        /// </summary>
        IConnector Connector { get; set; }
    }
}
