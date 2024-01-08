using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Lox{

    public class Clock : ILoxCallable{
        public int Arity(){
            return 0;
        }

        public object Call(Interpreter interpreter, List<object> arguments){
            return (DateTime.UtcNow - DateTime.UnixEpoch).TotalMilliseconds / 1000.0;
        }

        public override string ToString()
        {
            return "<native fn>";
        }
    }
}