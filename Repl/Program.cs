using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MiniMALRepl {
    class Program {
        static void Main(string[] args)
        {
            MiniMAL.REPL.AbstractSyntaxTreeInterpreterRepl.Run();
        }
    }
}

