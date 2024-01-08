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
using System.Xml;

//Parser is used to scan the tokens we have stored from Scanner and process each individually
//uses a List of tokens created from Scanner.cs

namespace Lox{
class Parser{
    private readonly List<Token> tokens = new List<Token>();
    private int current = 0;
    public class ParseError : Exception{
        public ParseError() : base(){

        }
        public ParseError(string msg) : base(msg){}
    }

    public Parser(List<Token> tokens){
        this.tokens = tokens;
    }

    //will eventually add more to handle Lox statement logic
    // public Expr Parse(){
    //     try{
    //         return Expression();
    //     }
    //     catch (ParseError error){
    //         Console.WriteLine(error.Message);
    //         return null;
    //     }

    // }

    //new version of parse() to handle the full version of lox that is constructed out of statements
    //allows us to update the grammar to:
        //program → statement* EOF ;
        // statement → exprStmt
        // | printStmt ;
        // exprStmt → expression ";" ;
        // printStmt → "print" expression ";" ;
    public List<Stmt> Parse(){
        List<Stmt> statements = new List<Stmt>();

        while (!IsAtEnd()){
            statements.Add(Declaration());
        }
        return statements;
    }

    private Expr Expression(){
        return Assignment();
    }



    private Stmt Declaration(){
        try {
            if (Match(TokenType.CLASS)) return ClassDeclaration();
            if (Match(TokenType.FUN)) {
                return Function("function");
            }
            if (Match(TokenType.VAR)) return VarDeclaration();
            return Statement();
        }
        catch (ParseError e){
            Console.WriteLine(e.Message);
            Synchronize();
            return null;
        }
    }

    private Stmt ClassDeclaration(){
        Token name = Consume(TokenType.IDENTIFIER, "Expect class name.");
        Expr.Variable superclass = null;
        if (Match(TokenType.LESS)){
            Consume(TokenType.IDENTIFIER, "Expect superclass name");
            superclass = new Expr.Variable(Previous());
        }
        Consume(TokenType.LEFT_BRACE, "Expect '{' before class body");

        List<Stmt.Function> methods = new List<Stmt.Function>();
        while (!Check(TokenType.RIGHT_BRACE) && !IsAtEnd()){
            methods.Add(Function("method"));
        }

        Consume(TokenType.RIGHT_BRACE, "Expect '}' after class body.");
        return new Stmt.Class(name, superclass, methods);
    }


    //controls the function we return to in order to handle the different lox statements we parse
    private Stmt Statement(){
        if (Match(TokenType.PRINT)) return PrintStatement();
        if (Match(TokenType.RETURN)) return ReturnStatement();
        if (Match(TokenType.LEFT_BRACE)) return new Stmt.Block(Block());
        if (Match(TokenType.IF)) return IfStatement();
        if (Match(TokenType.WHILE)) return WhileStatement();
        if (Match(TokenType.FOR)) return ForStatement();
        else{
            return ExpressionStatement();
        }
    }

    private Stmt IfStatement(){
        Consume(TokenType.LEFT_PAREN, "Expect '(' after 'if'. ");
        Expr condition = Expression();
        Consume(TokenType.RIGHT_PAREN, "Expect ')' after 'if' condition. ");
       

        Stmt thenBranch = Statement();
        //do not always need an else with every if
        Stmt elseBranch = null;
        if (Match(TokenType.ELSE)){
            elseBranch = Statement();
        }

        return new Stmt.If(condition, thenBranch, elseBranch);
    }

    private Stmt PrintStatement(){
        Expr value = Expression();
        Consume(TokenType.SEMICOLON, "Expect ';' after value ");
        return new Stmt.Print(value);
    }

    private Stmt ReturnStatement(){
        Token keyword = Previous();
        Expr value = null;
        if (!Check(TokenType.SEMICOLON)){
            value = Expression();
        }

        Consume(TokenType.SEMICOLON, "Expect a ';' after return statement");
        return new Stmt.Return(keyword, value);
    }

    private Stmt VarDeclaration(){
        Token name = Consume(TokenType.IDENTIFIER, "Expecting a variable name.");
       
        Expr init = null;
        if (Match(TokenType.EQUAL)){
            init = Expression();
        }

        Consume(TokenType.SEMICOLON, "Expect ';' after variable declaration.");
        return new Stmt.Var(name, init);
    }

    private Stmt WhileStatement(){
        Consume(TokenType.LEFT_PAREN, "Expect '(' after 'while'. ");
        Expr condition = Expression();
        Consume(TokenType.RIGHT_PAREN, "Expect ')' after 'while' condition. ");
        Stmt whileBody = Statement();

        return new Stmt.While(condition, whileBody);
    }

    //will follow C structure - initializer, condition, increment (int i=0; i<val; i++)
    private Stmt ForStatement(){
        Consume(TokenType.LEFT_PAREN, "Expect '(' after 'for'. ");

        Stmt initializer;
        //if there is no initializing statement
        if (Match(TokenType.SEMICOLON)){
            initializer=null;
        }
        //if init stmt is just a single variable
        else if (Match(TokenType.VAR)){
            initializer=VarDeclaration();
        }
        //init stmt is an entire expression
        else{
            initializer=ExpressionStatement();
        }

        Expr condition = null;
        if (!Check(TokenType.SEMICOLON)){
            condition = Expression();
        }

        Consume(TokenType.SEMICOLON, "Expect ';' after loop condition");

        Expr increment = null;
        if (!Check(TokenType.RIGHT_PAREN)){
            increment = Expression();
        }

        Consume(TokenType.RIGHT_PAREN, "Expect ')' after for loop clauses");

        Stmt forBody = Statement();
        
        if (increment != null){
            forBody = new Stmt.Block(
                new List<Stmt>{
                    forBody, new Stmt.Expression(increment)
        });
    }

        if (condition==null) condition=new Expr.Literal(true);
        forBody = new Stmt.While(condition, forBody);

        if (initializer!=null){
            forBody = new Stmt.Block(
                new List<Stmt>{
                    initializer, forBody});
    }

        return forBody;

    }

    private Stmt ExpressionStatement(){
        Expr expr = Expression();
        //Console.WriteLine("in exp stmt");
        Consume(TokenType.SEMICOLON, "Expect ';' after expression ");
        return new Stmt.Expression(expr);
    }

    private Stmt.Function Function(string kind){
        Token name = Consume(TokenType.IDENTIFIER, "Expect " + kind + " name.");

        Consume(TokenType.LEFT_PAREN, "Expect '(' after )" + kind + " name.");
        List<Token> parameters = new List<Token>();
        if (!Check(TokenType.RIGHT_PAREN)){
            do{
                if (parameters.Count >= 255){
                    Error(Peek(), "Can't have more than 255 parameters");
                }

                parameters.Add(Consume(TokenType.IDENTIFIER, "Expect Parameter name"));

            }
            //add the parameters of the function as long as we keep consuming commas within the func args list
            while (Match(TokenType.COMMA));
        }
        Consume(TokenType.RIGHT_PAREN, "Expect ')' after parameters");

        Consume(TokenType.LEFT_BRACE, "Expect '{' before " + kind + " body.");
        List<Stmt> body = Block();
        return new Stmt.Function(name, parameters, body);
    }

    private List<Stmt> Block(){
        List<Stmt> statements = new List<Stmt>();
        
        while (!Check(TokenType.RIGHT_BRACE) && !IsAtEnd()){
            statements.Add(Declaration());
        }

        Consume(TokenType.RIGHT_BRACE, "Expect a } after block.");
        return statements;
    }

    private Expr Assignment(){
        Expr expr = Or();

        if (Match(TokenType.EQUAL)){
            Token eq = Previous();
            Expr value = Assignment();

            if (expr is Expr.Variable){
                Token name = ((Expr.Variable)expr).name;
                return new Expr.Assign(name, value);
            }
            else if (expr is Expr.Get){
                Expr.Get get = (Expr.Get)expr;
                return new Expr.Set(get.obj, get.name, value);
            }

            Error(eq, "Invalid assignment target");
        }
        return expr;
    }

    private Expr Or(){
        Expr expr = And();

        while (Match(TokenType.OR)){
            Token op = Previous();
            Expr right = And();
            expr = new Expr.Logical(expr, op, right);
        }

        return expr;
    }

    private Expr And(){
        Expr expr = Equality();

        while (Match(TokenType.AND)){
            Token op = Previous();
            Expr right = Equality();
            expr = new Expr.Logical(expr, op, right);
        }

        return expr;
    }

    //function to handle equality tokens such as != and ==
    private Expr Equality(){
        Expr expr = Comparison();

        while (Match(TokenType.BANG_EQUAL, TokenType.EQUAL_EQUAL)){
            Token op = Previous();
            Expr right = Comparison();
            expr = new Expr.Binary(expr, op, right);
        }

        return expr;
    }

    //handles any tokens dealing with comparison logic (<, >, etc)
    //creates a new expression to handle the comparators with the previous token (operator) and the 
    private Expr Comparison(){
        Expr expr = Term();
        while (Match(TokenType.GREATER, TokenType.GREATER_EQUAL, TokenType.LESS, TokenType.LESS_EQUAL)){
            Token op = Previous();
            Expr right = Term();
            expr = new Expr.Binary(expr, op, right);
        }

        return expr;
    }

    //handles simple +/- arithmetic expressions
    private Expr Term(){
        Expr expr = Factor();

        while (Match(TokenType.MINUS, TokenType.PLUS)){
            Token op = Previous();
            Expr right = Factor();
            expr = new Expr.Binary(expr, op, right);
        }

        return expr;
    }

    //handles arithmetic expressions with division (/) and multiplication (*)
    private Expr Factor(){
        Expr expr = Unary();

        while (Match(TokenType.SLASH, TokenType.STAR)){
            Token op = Previous();
            Expr right = Unary();
            expr = new Expr.Binary(expr, op, right);
        }

        return expr;
    }

    //handles the unary operations (!, -) as opposed to binary
    private Expr Unary(){
        if (Match(TokenType.BANG, TokenType.MINUS)){
            Token op = Previous();
            Expr right = Unary();
            return new Expr.Unary(op, right);
    }
        return Call();
    }

    private Expr FinishCall(Expr callee){
        List<Expr> arguments = new List<Expr>();
        if (!Check(TokenType.RIGHT_PAREN)){
            do {
                if (arguments.Count >= 255){
                    Error(Peek(), "Can't have more than 255 args");
                }
                arguments.Add(Expression());
            }
            while(
                Match(TokenType.COMMA));
        }

        Token paren = Consume(TokenType.RIGHT_PAREN, "Expect ')' after args");

        return new Expr.Call(callee, paren, arguments);
    }

    private Expr Call(){
        Expr expr = Primary();

        while (true){
            if (Match(TokenType.LEFT_PAREN)){
                expr = FinishCall(expr);
            }
            //eg. Parser.Call() -- consuming the call method
            else if (Match(TokenType.DOT)){
                Token name = Consume(TokenType.IDENTIFIER, "Expect property name after '.'");
                expr = new Expr.Get(expr, name);
            }
            else{
                break;
            }
        }

        return expr;
    }



    //function to handle adding the T/F, null, (), string and num tokens
    private Expr Primary(){
        if(Match(TokenType.FALSE)) return new Expr.Literal(false);
        if(Match(TokenType.TRUE)) return new Expr.Literal(true);
        if(Match(TokenType.NIL)) return new Expr.Literal(null);

        if (Match(TokenType.NUMBER, TokenType.STRING)){
            return new Expr.Literal(Previous().literal);
        }
        if (Match(TokenType.SUPER)){
            Token keyword = Previous();
            Consume(TokenType.DOT, "Expect '.' after 'super'.");
            Token method = Consume(TokenType.IDENTIFIER, "Expect superclass method name.");
            return new Expr.Super(keyword, method);
        }
        if (Match(TokenType.THIS)) return new Expr.This(Previous());

        if (Match(TokenType.IDENTIFIER)){
            return new Expr.Variable(Previous());
        }

        if (Match(TokenType.LEFT_PAREN)){
            Expr expr = Expression();
            Consume(TokenType.RIGHT_PAREN, "Expect ')' after expression");
            return new Expr.Grouping(expr);
        }
        //handles error when we find a token that cannot start an expression
        Error(Peek(), "Expect expression");
        return null;
    }


    //used to check the next token from our current index and see if it matches the expected token passed to it
    private bool Match(params TokenType[] types){
        foreach (TokenType token in types){
            if (Check(token)){
                Advance();
                return true;
            }
        }
        return false;
    }

    private Token Consume(TokenType type, string message){
        if (Check(type)) {
            return Advance();
        }
        else{
            throw Error(Peek(), message);
        }
    }

    //similar to match, but doesn't "consume"/advance the index
    //only looks at the next token to see if it matches the expected type
    private bool Check(TokenType type){
        if (IsAtEnd()) return false;
        return Peek().type == type;
    }


    //scans the current token and returns it after incrementing the index of our tokens list
    private Token Advance(){
        if (!IsAtEnd()) current++;
        return Previous();
    }

    //used to check if we see an EOF token, signals there is no more source code/tokens to parse
    private bool IsAtEnd(){
        return Peek().type == TokenType.EOF;
    }

    //finds the token at our current index
    private Token Peek(){
        return tokens[current];
    }

    //finds the token before our current index
    private Token Previous(){
        return tokens[current-1];
    }

    private ParseError Error(Token token, string msg){
        Lox.TokenError(token, msg);
        return new ParseError();
    }

    //find a semicolon, means were done with a statement
    //when we see the beginning of a new statement, use a switch/case statement to handle each type of Lox statement
    private void Synchronize(){
        Advance();

        //want to continue through Lox tokens until we find semicolon, meaning we are at end of statement
        while (!IsAtEnd()){
            if (Previous().type == TokenType.SEMICOLON) return;

            //will complete once we have set up the statement handling logic
            switch (Peek().type){
                case TokenType.CLASS:
                case TokenType.FUN:
                case TokenType.VAR:
                case TokenType.FOR:
                case TokenType.IF:
                case TokenType.WHILE:
                case TokenType.PRINT:
                case TokenType.RETURN:
                    return;
            }

            Advance();
        }
    }

}
}