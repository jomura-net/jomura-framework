using System;

namespace Jomura.Web.UI.WebControls
{
    /// <summary>
    /// LabelEx コントロールの内容を表示する方法を指定します。
    /// </summary>
    public enum LabelExMode
    {
        /// <summary>
        /// LabelEx コントロールの内容は変更されません。
        /// </summary>
        PassThrough = 0,

        /// <summary>
        /// LabelEx コントロールの内容は HTML エンコードされます。
        /// </summary>
        Encode = 1,
    }
}
