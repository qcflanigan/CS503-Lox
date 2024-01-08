using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.IO.Compression;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Xml;

namespace Lox{

    public class Environ {
        public Environ enclosing;
        private readonly Dictionary<string, object> values = new Dictionary<string, object>();

        public Environ(){
            enclosing=null;
        }

        public Environ(Environ enclosing){
            this.enclosing=enclosing;
        }

        //retrive a variable's value from the dict of vars
        public object Get(Token name){
            if (values.ContainsKey(name.lexeme)){
                return values[name.lexeme];
            }

            if (enclosing!=null){
                return enclosing.Get(name);
            }

            throw new RuntimeError(name, "Undefined Variable '" + name.lexeme + "'.");
        }

        //function to assign an existing variable to a new value
        public void Assign(Token name, object value){
            if (values.ContainsKey(name.lexeme)){
                values[name.lexeme]=value;
                return;
            }

            if (enclosing!=null){
                enclosing.Assign(name, value);
                return;
            }

            throw new RuntimeError(name, "Undefined variable '" + name.lexeme + "'.");
        }

        //create a variable entry with corresponding value (may be null) within the dict of vars
        public void Define(string name, object value){
            values[name]=value;
    }

        public object GetAt(int distance, string name){
            try{
            return Ancestor(distance).values[name];
            }
            catch (Exception e){
                Console.WriteLine(e.Message);
                return null;
            }
        }

        public void AssignAt(int distance, Token name, object value){
            Ancestor(distance).values[name.lexeme] = value;
        }

        public Environ Ancestor(int distance){
            Environ environment = this;
            for (int i=0; i<distance; i++){
                environment = environment.enclosing;
            }
            return environment;
        }
}
}