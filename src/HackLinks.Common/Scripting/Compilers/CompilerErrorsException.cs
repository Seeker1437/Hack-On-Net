using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HackLinks.Common.Scripting.Compilers
{
    public class CompilerErrorsException : Exception
    {
        public List<CompilerError> Errors { get; protected set; }

        public CompilerErrorsException()
        {
            Errors = new List<CompilerError>();
        }
    }
}
