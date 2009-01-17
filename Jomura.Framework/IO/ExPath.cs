using System;
using System.Collections.Generic;
using System.Text;

namespace Jomura.IO
{
    /// <summary>
    /// ファイルパス、ファイル名等に関する静的メソッドを提供する。
    /// </summary>
    public static class ExPath
    {
        /// <summary>
        /// ファイル名として適当でない文字を'-'に置換した文字列を返す。
        /// </summary>
        /// <param name="rawFileName">元のファイル名</param>
        /// <returns>変換後のファイル名</returns>
        public static string ConvertToValidFileName(string rawFileName)
        {
            string validFileName = rawFileName;
            char[] invalidChars = System.IO.Path.GetInvalidFileNameChars();

            foreach (char c in invalidChars)
            {
                validFileName = validFileName.Replace(c, '-');
            }
            return validFileName;
        }
    }
}
