//class to store and handle any resolving scopes within the lox code
//implements statements and expressions to resolve every aspect of every scope/function

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Lox{

    public class Resolver : Expr.IExprVisitor<object>, Stmt.IStmtVisitor<object>{
        private readonly Interpreter interpreter;
        //stack of dicts to keep track of current scope within lox code
        private readonly Stack<Dictionary<string, bool>> scopes = new Stack<Dictionary<string, bool>>();
        private FunctionType currFunction = FunctionType.NONE;

        public Resolver(Interpreter interpreter){
            this.interpreter = interpreter;
        }

        private enum FunctionType{
            NONE,
            Function,
            INITIALIZER,
            METHOD
        }

        private enum ClassType{
            NONE, 
            CLASS,
            SUBCLASS
        }

        private ClassType currClass = ClassType.NONE;

        public object VisitBlockStmt(Stmt.Block stmt){
            BeginScope();
            Resolve(stmt.statements);
            EndScope();
            return null;
        }

        public object VisitClassStmt(Stmt.Class stmt){
            ClassType enclosingClass = currClass;
            currClass = ClassType.CLASS;
            Declare(stmt.name);
            Define(stmt.name);

            if (stmt.superclass!=null && stmt.name.lexeme.Equals(stmt.superclass.name.lexeme)){
                Lox.TokenError(stmt.superclass.name, "A class cannot inherit from itself");
            }

            if (stmt.superclass!=null){
                currClass = ClassType.SUBCLASS;
                Resolve(stmt.superclass);
                BeginScope();
                scopes.Peek().Add("super", true);
            }
          
            BeginScope();
            scopes.Peek().Add("this", true);

            foreach (Stmt.Function method in stmt.methods){
                FunctionType declaration = FunctionType.METHOD;
                if (method.name.lexeme.Equals("init")){
                    declaration = FunctionType.INITIALIZER;
                }
                ResolveFunction(method, declaration);
            }
            EndScope();
            if (stmt.superclass!=null) EndScope();
            currClass = enclosingClass;
            return null;
        }


        public object VisitExpressionStmt(Stmt.Expression stmt){
            Resolve(stmt.expression);
            return null;
        }


        public object VisitFunctionStmt(Stmt.Function stmt){
            Declare(stmt.name);
            Define(stmt.name);

            ResolveFunction(stmt, FunctionType.Function);
            return null;
        }

        public object VisitIfStmt(Stmt.If stmt){
            Resolve(stmt.condition);
            Resolve(stmt.thenBranch);
            if (stmt.elseBranch!=null) Resolve(stmt.elseBranch);
            return null;
        }

        public object VisitPrintStmt(Stmt.Print stmt){
            Resolve(stmt.expression);
            return null;
        }

        public object VisitThisExpr(Expr.This expr){
            if (currClass == ClassType.NONE){
                Lox.TokenError(expr.keyword, "Can't use 'this' outside of a class");
                return null;
            }
            ResolveLocal(expr, expr.keyword);
            return null;
        }


        public object VisitReturnStmt(Stmt.Return stmt){
            //make sure we dont have any dangling return statements outside of functions
            if (currFunction == FunctionType.NONE){
                Lox.TokenError(stmt.keyword, "Can't return from top level code");
            }
            if (stmt.value!=null){
                if (currFunction == FunctionType.INITIALIZER){
                    Lox.TokenError(stmt.keyword, "Can't return a value from an initializer");
                }
                Resolve(stmt.value);
            }
            return null;
        }

        public object VisitVarStmt(Stmt.Var stmt){
            Declare(stmt.name);
            if (stmt.initializer!=null){
                Resolve(stmt.initializer);
            }
            Define(stmt.name);
            return null;
        }

        public object VisitWhileStmt(Stmt.While stmt){
            Resolve(stmt.condition);
            Resolve(stmt.body);
            return null;
        }

        public object VisitAssignExpr(Expr.Assign expr){
            Resolve(expr.value);
            ResolveLocal(expr, expr.name);
            return null;
        }

        public object VisitBinaryExpr(Expr.Binary expr){
            Resolve(expr.left);
            Resolve(expr.right);
            return null;
        }

        public object VisitCallExpr(Expr.Call expr){
            Resolve(expr.callee);

            foreach (Expr arg in expr.arguments){
                Resolve(arg);
            }

            return null;
        }

        public object VisitGetExpr(Expr.Get expr){
            Resolve(expr.obj);
            return null;
        }

        public object VisitGroupingExpr(Expr.Grouping expr){
            Resolve(expr.expression);
            return null;
        }

        //literals have no vars, subexpressions, no work to do but still have to implement
        public object VisitLiteralExpr(Expr.Literal expr){
            return null;
        }

        public object VisitLogicalExpr(Expr.Logical expr){
            Resolve(expr.left);
            Resolve(expr.right);
            return null;
        }

        public object VisitSetExpr(Expr.Set expr){
            Resolve(expr.value);
            Resolve(expr.obj);
            return null;
        }

        public object VisitSuperExpr(Expr.Super expr){
            if (currClass == ClassType.NONE){
                Lox.TokenError(expr.keyword, "Cannot use 'super' outside of a class");
            }
            else if(currClass!=ClassType.SUBCLASS){
                Lox.TokenError(expr.keyword, "Cannot use 'super' in a class with no superclasses");
            }
            ResolveLocal(expr, expr.keyword);
            return null;
        }

        public object VisitUnaryExpr(Expr.Unary expr){
            Resolve(expr.right);
            return null;
        }

        public object VisitVariableExpr(Expr.Variable expr){
            if (scopes.Count!=0 && scopes.Peek().ContainsKey(expr.name.lexeme) && scopes.Peek()[expr.name.lexeme] == false){
                Lox.TokenError(expr.name, "Can't read local var in its own initializer");
            } 
            ResolveLocal(expr, expr.name);
            return null;
        }

        public void Resolve(List<Stmt> statements){
            foreach (Stmt stmt in statements){
                Resolve(stmt);
            }
        }

        //when function is called, we create a new scope for it
        //then iterate through each of its params and declare+define it
        //once everything is defined for that func scope, we resolve and end scope
        private void ResolveFunction(Stmt.Function function, FunctionType type){
            //make sure we keep track of functions with appropriate return statements
            FunctionType enclosingFunc = currFunction;
            currFunction = type;
            BeginScope();
            foreach (Token param in function.parameters){
                Declare(param);
                Define(param);
            }
            Resolve(function.body);
            EndScope();
            currFunction = enclosingFunc;
        }

        private void Resolve(Stmt stmt){
            stmt.Accept(this);
        }

        private void Resolve(Expr expr){
            expr.Accept(this);
        }

        private void BeginScope(){
            scopes.Push(new Dictionary<string, bool>());
        }

        private void EndScope(){
            scopes.Pop();
        }

        //declare a new scope within our scopes dict
        private void Declare(Token name){
            if (scopes.Count==0) return;
            
            Dictionary<string, bool> scope = scopes.Peek();
            //check if we are trying to re-declare a variable of the same name in same/higher scope
            if (scope.ContainsKey(name.lexeme)){
                Lox.TokenError(name, "Already a var with this name in this scope");
            }
            scope[name.lexeme] = false;
        }

        private void Define(Token name){
            if (scopes.Count==0) return;
            try{
            scopes.Peek()[name.lexeme] = true;
            }
            catch (Exception e){
                Console.WriteLine(e.Message);
            }
        }

        private void ResolveLocal(Expr expr, Token name){
            for (int i=0; i<scopes.Count; i++){
                if (scopes.ToArray()[i].ContainsKey(name.lexeme)){
                    interpreter.Resolve(expr, i);
                    return;
                }
            }
        }

    }
}