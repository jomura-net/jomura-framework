using System;
using System.Web.UI;

namespace Jomura.Web.UI
{
    /// <summary>
    /// System.Web.UI.Controlをサポートする。
    /// </summary>
    public static class ControlHelper
    {
        /// <summary>
        /// 引数で指定したコントロールのControlsプロパティを再帰的に検索し、
        /// IDが一致した最初のコントロールオブジェクトを返す。
        /// </summary>
        /// <param name="id">検索のキーとなるコントロールID</param>
        /// <param name="control">被検索対象の親コントロールオブジェクト</param>
        /// <returns>
        /// 最初にIDが一致したコントロールオブジェクト。
        /// 全て一致しなかった場合はnull。
        /// </returns>
        public static Control FindControl(string id, Control control)
        {
            // control内のすべてのコントロールを列挙する
            foreach (Control childControl in control.Controls)
            {
                // コントロール名が合致した場合はそのコントロールのインスタンスを返す
                if (childControl.ID == id)
                {
                    return childControl;
                }

                // 列挙したコントロールにコントロールが含まれている場合は再帰呼び出しする
                if (childControl.HasControls())
                {
                    Control grandChildControl = FindControl(id, childControl);
                    // 再帰呼び出し先でコントロールが見つかった場合はそのまま返す
                    if (grandChildControl != null)
                    {
                        return grandChildControl;
                    }
                }
            }

            //最終的に発見できなかった場合
            return null;
        }

        /// <summary>
        /// 引数で指定したコントロールのControlsプロパティを再帰的に検索し、
        /// 型が一致した最初のコントロールオブジェクトを返す。
        /// </summary>
        /// <param name="type">検索のキーとなるコントロール型</param>
        /// <param name="control">被検索対象の親コントロールオブジェクト</param>
        /// <returns>
        /// 最初に型が一致したコントロールオブジェクト。
        /// 全て一致しなかった場合はnull。
        /// </returns>
        public static Control FindControl(Type type, Control control)
        {
            // control内のすべてのコントロールを列挙する
            foreach (Control childControl in control.Controls)
            {
                // コントロール名が合致した場合はそのコントロールのインスタンスを返す
                if (childControl.GetType() == type)
                {
                    return childControl;
                }

                // 列挙したコントロールにコントロールが含まれている場合は再帰呼び出しする
                if (childControl.HasControls())
                {
                    Control grandChildControl = FindControl(type, childControl);
                    // 再帰呼び出し先でコントロールが見つかった場合はそのまま返す
                    if (grandChildControl != null)
                    {
                        return grandChildControl;
                    }
                }
            }

            //最終的に発見できなかった場合
            return null;
        }
    }
}
