using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Lox{


    interface ILoxCallable{
        public int Arity();
        public object Call(Interpreter interpreter, List<object> arguments);
    }
}