//The main class to evaluate the Lox expressions
//Uses the Expr class to evaluate the Literal, Grouping, Binary and Unary expressions

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
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic;

namespace Lox{
    
    public class Interpreter : Expr.IExprVisitor<object>, Stmt.IStmtVisitor<object>{

        public Environ globals = new Environ();
        private Environ environment;
        private readonly Dictionary<Expr, int> locals = new Dictionary<Expr, int>();

        public Interpreter(){
            environment=globals;
            globals.Define("clock", new Clock());
      
        }
        
        public void Interpret(List<Stmt> statements){

            try{
                foreach (Stmt stmt in statements){
                    Execute(stmt);
                }
            }
            catch (RuntimeError error){
                Lox.RuntimeError(error);
            }
            }

        public object VisitExpressionStmt(Stmt.Expression stmt){
            Evaluate(stmt.expression);
            return null;
        }

        public object VisitFunctionStmt(Stmt.Function stmt){
            LoxFunction function = new LoxFunction(stmt, environment, false);
            environment.Define(stmt.name.lexeme, function);
            return null;
        }

        public object VisitIfStmt(Stmt.If stmt){
            //if the if statement's condition evals to true, execute the block within the if statement
            if (IsTruthy(Evaluate(stmt.condition))){
                Execute(stmt.thenBranch);
            }
            return null;
        }

        public object VisitPrintStmt(Stmt.Print stmt){
            object value = Evaluate(stmt.expression);
            Console.WriteLine(Stringify(value));
            return null;
        }

        public object VisitReturnStmt(Stmt.Return stmt){
            object value = null;
            if (stmt.value != null){
                value = Evaluate(stmt.value);
            }
            //use exceptions to get back to the code that began the executing body of the function we are returning from
            throw new Return(value);
        }

        //allows for "var b = 2;" and just "var b;"
        public object VisitVarStmt(Stmt.Var stmt){
           
            object value = null;
       
            //evaluate the expression on the right hand side of var declaration
            if (stmt.initializer != null){
                value = Evaluate(stmt.initializer);
            }
         
            //add the variable name and the value of its initializing expression to the dict of variables
            try{
            environment.Define(stmt.name.lexeme, value);
            }
            catch (Exception e){
                Console.WriteLine(e.Message);
            }
            return null;
        }

        //if the while condition is true, execute the statement(s) within the body
        //else, return null
        public object VisitWhileStmt(Stmt.While stmt){
            while (IsTruthy(Evaluate(stmt.condition))){
                Execute(stmt.body);
            }

            return null;
        }

        public object VisitAssignExpr(Expr.Assign expr){
            object value = Evaluate(expr.value);
            if (locals.TryGetValue(expr, out int distance)){
                environment.AssignAt(distance, expr.name, value);
            }
            else{
                globals.Assign(expr.name, value);
            }

            return value;

        }
        //just need to extract the value of the literal itself from the expression
        public object VisitLiteralExpr(Expr.Literal expr){
            return expr.value;
        }

        public object VisitLogicalExpr(Expr.Logical expr){
            object left = Evaluate(expr.left);

            if (expr.op.type == TokenType.OR){
                if (IsTruthy(left)) return left;
                else{
                    if (!IsTruthy(left)) return left;
                }
            }
            return Evaluate(expr.right);
        }

        public object VisitSetExpr(Expr.Set expr){
            object obj = Evaluate(expr.obj);

            if (!(obj is LoxInstance)){
                throw new RuntimeError(expr.name, "Only instances have fields.");
            }

            object value = Evaluate(expr.value);
            ((LoxInstance)obj).Set(expr.name, value);
            return value;
        }

        public object VisitSuperExpr(Expr.Super expr){
            int distance = locals[expr];
            LoxClass superclass = (LoxClass)environment.GetAt(distance, "super");

            LoxInstance obj = (LoxInstance)environment.GetAt(distance-1, "this");

            LoxFunction method = superclass.FindMethod(expr.method.lexeme);

            if (method==null){
                throw new RuntimeError(expr.method, "Undefined property '" + expr.method.lexeme + "'.");
            }

            return method.Bind(obj);
        }

        //recursively evaluate the expression itself to 'unfold' the grouping terms (), {}, etc
        public object VisitGroupingExpr(Expr.Grouping expr){
            return Evaluate(expr.expression);
        }

        private object Evaluate(Expr expr){
            return expr.Accept(this);
        }

        private void Execute(Stmt stmt){
            stmt.Accept(this);
        }

        public void Resolve(Expr expr, int depth){
            locals.Add(expr, depth);
        }

        public void ExecuteBlock(List<Stmt> statements, Environ environment){
            Environ prev = this.environment;
            try{
                this.environment = environment;

                foreach (Stmt stmt in statements){
                    Execute(stmt);
                }
            }
            //reset environment back to original after executing the block of statements 
            finally{
                this.environment=prev;
            }
        }

        public object VisitBlockStmt(Stmt.Block stmt){
            ExecuteBlock(stmt.statements, new Environ(environment));
            return null;
        }

        public object VisitClassStmt(Stmt.Class stmt){
            object superclass = null;
            if (stmt.superclass!=null){
                superclass = Evaluate(stmt.superclass);
                if (!(superclass is LoxClass)){
                    throw new RuntimeError(stmt.superclass.name, "Superclass must be a class");
                }
            }

            environment.Define(stmt.name.lexeme, null);

            if (stmt.superclass!=null){
                environment = new Environ(environment);
                environment.Define("super", superclass);
            }


            Dictionary<string, LoxFunction> methods = new Dictionary<string, LoxFunction>();
            foreach (Stmt.Function method in stmt.methods){
                LoxFunction function = new LoxFunction(method, environment, method.name.lexeme.Equals("init"));
                methods.Add(method.name.lexeme, function);
            }
            LoxClass loxclass = new LoxClass(stmt.name.lexeme, (LoxClass)superclass, methods);

            if (superclass!=null){
                environment = environment.enclosing;
            }

            environment.Assign(stmt.name, loxclass);
            return null;
        }

        //handles binary expressions such as + (addition), - (subtraction), / (division) and * (multiplication), logical comparison (<, >)
        public object VisitBinaryExpr(Expr.Binary expr){
            object left = Evaluate(expr.left);
            object right = Evaluate(expr.right);

            switch (expr.op.type){
                case TokenType.GREATER:
                    CheckNumberOPs(expr.op, left, right);
                    return (double)left > (double)right;
                case TokenType.GREATER_EQUAL:
                    CheckNumberOPs(expr.op, left, right);
                    return (double)left >= (double)right;
                case TokenType.LESS:
                    CheckNumberOPs(expr.op, left, right);
                    return (double)left < (double)right;
                case TokenType.LESS_EQUAL:
                    CheckNumberOPs(expr.op, left, right);
                    return (double)left <= (double)right;
                case TokenType.BANG_EQUAL: 
                    return !IsEqual(left, right);
                case TokenType.EQUAL_EQUAL:
                    return IsEqual(left, right);
                case TokenType.MINUS:
                    CheckNumberOPs(expr.op, left, right);
                    return (double)left - (double)right;
                //special case to handle for string concatenation as well as arithmetic expression
                case TokenType.PLUS:
                    if (left is double && right is double){
                        return (double)left + (double)right;
                    }
                    if (left is string && right is string){
                        return (string)left + (string)right;
                    }
                    else{
                        throw new RuntimeError(expr.op, "Operands must be two integers or two strings ");
                    }
                case TokenType.SLASH:
                    CheckNumberOPs(expr.op, left, right);
                    return (double)left / (double)right;
                case TokenType.STAR:
                    CheckNumberOPs(expr.op, left, right);
                    return (double)left * (double)right;
            }
            return null;
        }

        public object VisitCallExpr(Expr.Call expr){
            object callee = Evaluate(expr.callee);
            List<object> arguments = new List<object>();

            foreach (Expr arg in expr.arguments){
                arguments.Add(Evaluate(arg));
            }

            if (!(callee is ILoxCallable)){
                throw new RuntimeError(expr.paren, "Can only call functions and classes");
            }

            ILoxCallable func = (ILoxCallable)callee;
            if (arguments.Count != func.Arity()){
                throw new RuntimeError(expr.paren, "Expected " + func.Arity() + " arguments but got " + arguments.Count + ". ");
            }
            return func.Call(this, arguments);
        }

        public object VisitGetExpr(Expr.Get expr){
            object obj = Evaluate(expr.obj);
            if(obj is LoxInstance){
                return ((LoxInstance)obj).Get(expr.name);
            }
            throw new RuntimeError(expr.name, "Only instances have properties");
        }

        public object VisitThisExpr(Expr.This expr){
            return LookUpVariable(expr.keyword, expr);
        }


        //handles the two unary operations of - (negation) and ! (logical not)
        public object VisitUnaryExpr(Expr.Unary expr){
            object right = Evaluate(expr.right);
            switch(expr.op.type){
                case TokenType.BANG:
                    return !IsTruthy(right);
                case TokenType.MINUS:
                    //check if the - sign is being applied to the correct/valid term in an expression
                    CheckNumberOP(expr.op, right);
                    return -(double)right;  
               
            }
            return null;
        }

        public object VisitVariableExpr(Expr.Variable expr){
            return LookUpVariable(expr.name, expr);
        }

        private object LookUpVariable(Token name, Expr expr){
            if (locals.TryGetValue(expr, out int distance)){
                return environment.GetAt(distance, name.lexeme);
            }

            else{
                return globals.Get(name);
            }

            // int distance = locals[expr];
            // if (distance!=null){
            //     return environment.GetAt(distance, name.lexeme);
            // }
            // else{
            //     return globals.Get(name);
            // }
        }

       

        //helper function to make sure the passed operator (-, /, *) is applied to a valid term in an expression
        private void CheckNumberOP(Token op, object operand){
            if (operand is double) return;
            else{
                throw new RuntimeError(op, "Operand must be a number ");
            }
        }

        //similar to above function, checks validity for binary operations 
        private void CheckNumberOPs(Token op, object left, object right){
            if (left is double && right is double) return;
            else{
                throw new RuntimeError(op, "Operands must be numbers ");
            }
        }

        //false & null are "falsey", everything else is "truthy"
        private bool IsTruthy(object ob){
            if (ob==null) return false;
            if (ob is bool) return (bool)ob;
            return true;
        }

        //helper function to determine if two objects in an expression are equal
        //used to help evaluate != and = expressions
        private bool IsEqual(object a, object b){
            if (a==null && b==null) return true;
            if (a==null) return false;
            return a.Equals(b);
        }

        //converts the result of our expression to a string value
        private string Stringify(object ob){
            if (ob==null) return "nil";

            if (ob is double){
                string text = ob.ToString();
                if (text.EndsWith(".0")){
                    text = text.Substring(0, text.Length-2);
                }
                return text;
            }
            return ob.ToString();
        }



    }

}