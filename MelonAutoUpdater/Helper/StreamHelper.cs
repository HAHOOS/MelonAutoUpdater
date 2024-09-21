using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MelonAutoUpdater.Helper
{
    /// <summary>
    /// Class with methods helping use <see cref="Stream"/>
    /// </summary>
    public static class StreamHelper
    {
        /// <summary>
        /// Copy contents of a <see cref="Stream"/> to a new one<br/>
        /// </summary>
        /// <param name="input">The <see cref="Stream"/> u want to copy from</param>
        /// <param name="output">The <see cref="Stream"/> u want to copy to</param>
        internal static void CopyTo(this Stream input, Stream output)
        {
            byte[] buffer = new byte[16 * 1024];
            int bytesRead;

            while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, bytesRead);
            }
        }
    }
}