using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HackLinks.Common.Util.Commands
{
    public delegate CommandResult ConsoleCommandFunc(string command, IList<string> args);
}
