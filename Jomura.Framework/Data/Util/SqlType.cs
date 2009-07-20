using System;
using System.Collections.Generic;
using System.Text;

namespace Jomura.Data.Util
{
    /// <summary>
    /// DDL種別
    /// DatabaseCopyクラスで、処理数をカウントするために利用される。
    /// </summary>
    public enum SqlType
    {
        /// <summary>
        ///  Create Table文
        /// </summary>
        Create,
        /// <summary>
        ///  Drop Table文
        /// </summary>
        Drop,
        /// <summary>
        ///  INSERT文
        /// </summary>
        Insert,
        /// <summary>
        ///  Update文
        /// </summary>
        Update,
        /// <summary>
        ///  Delete文
        /// </summary>
        Delete
    }
}
