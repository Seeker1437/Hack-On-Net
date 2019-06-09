using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HackLinks.Common.Util
{
    public class FileReaderLine
    {
        /// <summary>
        /// Current line.
        /// </summary>
        public string Value { get; private set; }

        /// <summary>
        /// Full path to the file the value was read from.
        /// </summary>
        public string File { get; private set; }

        /// <summary>
        /// New FileReaderLine.
        /// </summary>
        /// <param name="line"></param>
        /// <param name="file"></param>
        public FileReaderLine(string line, string file)
        {
            this.Value = line;
            this.File = Path.GetFullPath(file);
        }
    }
}
