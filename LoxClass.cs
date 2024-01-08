using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;


namespace Lox{
    

    public class LoxClass : ILoxCallable{
        public readonly string name;
        public readonly LoxClass superclass;
        private readonly Dictionary<string, LoxFunction> methods = new Dictionary<string, LoxFunction>();

        public LoxClass(string name, LoxClass superclass, Dictionary<string, LoxFunction> methods){
            this.name = name;
            this.methods = methods;
            this.superclass = superclass;    
        
        }

        public LoxFunction FindMethod(string name){
            if (methods.ContainsKey(name)){
                return methods[name];
            }
            if (superclass!=null){
                return superclass.FindMethod(name);
            }
            return null;
        }

        public override string ToString(){
            return name;
        }

        public object Call(Interpreter interpreter, List<object> arguments){
            LoxInstance instance = new LoxInstance(this);
            LoxFunction init = FindMethod("init");
            if (init != null){
                init.Bind(instance).Call(interpreter, arguments);
            }
            return instance;
        }

        public int Arity(){
            LoxFunction init = FindMethod("init");
            if (init==null){
                return 0;
            }
            return init.Arity();
        }
    }
}