using System;
using System.Collections.Generic;
using System.Text;

namespace Jomura.IO
{
    /// <summary>
    /// ファイルパス、ファイル名等に関する静的メソッドを提供する。
    /// </summary>
    public class Path
    {
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
