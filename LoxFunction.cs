using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Lox{

    public class LoxFunction : ILoxCallable{
        private readonly Stmt.Function declaration;
        private readonly Environ closure;
        private readonly bool isInit;

        public LoxFunction(Stmt.Function declaration, Environ closure, bool isInit){
            this.isInit = isInit;
            this.closure = closure;
            this.declaration = declaration;
        }

        public LoxFunction Bind(LoxInstance instance){
            Environ environment = new Environ(closure);
            environment.Define("this", instance);
            return new LoxFunction(declaration, environment, isInit);
    }


        public override string ToString()
        {
            return "<fn " + declaration.name.lexeme + ">";
        }

        public int Arity(){
            return declaration.parameters.Count;
        }

        public object Call(Interpreter interpreter, List<object> arguments){
            Environ environment = new Environ(closure);
            
            for (int i=0; i<declaration.parameters.Count; i++){
                environment.Define(declaration.parameters[i].lexeme, arguments[i]);
            }

            //interpreter.ExecuteBlock(declaration.body, environment);
            //try to execute the func block, then catch the return exceptions for return statements
            try{
                interpreter.ExecuteBlock(declaration.body, environment);
            }
            catch (Return returnVal){
                if (isInit){
                    return closure.GetAt(0, "this");
                }
                return returnVal.value;
            }
            if (isInit) return closure.GetAt(0, "this");
            return null;
        }


    }
}