//class used to throw errors when we have invalid operations within our lox expressions during evaluation

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.IO.Compression;
using System.Linq.Expressions;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Lox{

    public class RuntimeError : Exception{
        public Token tokenT;

        public RuntimeError(Token token, string msg) : base(msg){
            tokenT = token;
        }
    }





}