using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HackLinks.Common.Scripting.Compilers
{
    public interface IPreCompiler
    {
        string PreCompile(string script);
    }
}
