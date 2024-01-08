using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Lox{

    public class LoxInstance{
        private LoxClass loxClass;
        private readonly Dictionary<string, object> fields = new Dictionary<string, object>();

    public LoxInstance(LoxClass loxClass){
        this.loxClass = loxClass;
    }

    public object Get(Token name){
        if (fields.ContainsKey(name.lexeme)){
            return fields[name.lexeme];
        }

        LoxFunction method = loxClass.FindMethod(name.lexeme);
        if (method!=null) return method.Bind(this);

        throw new RuntimeError(name, "Undefined property '" + name.lexeme + "'.");
    }

    public void Set(Token name, object value){
        fields[name.lexeme]=value;
    }
    public override string ToString(){
        return loxClass.name + " instance";

    }

    }
}