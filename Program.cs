using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.IO;
using System.IO.Enumeration;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;

namespace Lox
{
    public class Lox{
        private static Interpreter interpreter = new Interpreter();
        static bool hadError = false;
        static bool hadRuntimeError = false;

        public static void Main(string[] args){
            //if we are given a file to execute
            if (args.Length == 1){
                if (args[0]=="test"){
                    LoxTest();
                }
                else{
                ExecuteFile(args[0]);
                }
            }
            else if (args.Length > 1){
                Console.WriteLine("Usage: Lox [script]");
                Environment.Exit(64);   //exit with code 64 signifying invalid input
            }
            //if we are given no file, execute line-by-line
            else{
                ExecutePrompt();
            }
        }

        public static void LoxTest(){
            int testCount=1;
            List<string []> testFiles = new List<string []>();
            string [] directories = Directory.EnumerateDirectories("test").ToArray();
            foreach (string directory in directories){
                testFiles.Add(Directory.GetFiles(directory));
            }
            
            foreach (string [] files in testFiles){
                foreach (string file in files){
                    try{
                    byte[] bytes = File.ReadAllBytes(file);
                    Console.WriteLine("path\\filename: " + file);
                    ExecuteLoxCode(Encoding.Default.GetString(bytes));
                    Console.WriteLine();
                    testCount+=1;
                    TestingReset();
                    //Thread.Sleep(1500);
                    }
                    catch (Exception e){
                        continue;
                        
                    }
                }
            }
            // Console.WriteLine(cleanFiles.Count);
            // Console.WriteLine("-------------------------------------------------------------------");
            // Console.WriteLine("Running Files with no Expected Errors:");
            // foreach (string file in cleanFiles){
            //     Console.WriteLine("path\\filename: " + file);
            //     ExecuteFile(file);
            //     Console.WriteLine();
            // }
            Console.WriteLine("Test Count: " + testCount);

        }

        //function to interpret/execute a given file of Lox code
        //reads Lox code into array of type bytes, and converts the bytes to strings, passing the strings to the Run() function
        public static void ExecuteFile(string path){
            byte[] bytes = File.ReadAllBytes(path);
            ExecuteLoxCode(Encoding.Default.GetString(bytes));
            if (hadError){
                Environment.Exit(65);
            }
            if (hadRuntimeError){
                Environment.Exit(70);
            }
        }

        //function to run Lox code line-by-line within while loop
        //provides a basic terminal-like interface
        private static void ExecutePrompt(){
            StreamReader reader = new StreamReader(Console.OpenStandardInput());
                while (true){
                    Console.Write("Lox> ");
                    string input = reader.ReadLine();
                    if (input == null) break;
                    ExecuteLoxCode(input);
                    hadError = false;
                }
            }


        //main driver function that executes the Lox code by scanning the input for each meaningful token
        //for now, just prints the tokens we are given in our input to see if we are processing input correctly
        private static void ExecuteLoxCode(string input){
            Scanner scanner = new Scanner(input);
            List<Token> tokens = scanner.ScanTokens();

            Parser parser = new Parser(tokens);
            List<Stmt> statements = parser.Parse();
            //Expr expression = parser.Parse();
            if (hadError) return;
            //resolve after error check, will not run code if there are syntax errors
            Resolver resolver = new Resolver(interpreter);
            resolver.Resolve(statements);

            //check again for any resolving errors between scopes
            if (hadError) return;

            interpreter.Interpret(statements);
            //Console.WriteLine(new ASTPrinter().Print(expression));

            // foreach (Token token in tokens){
            //     Console.WriteLine("token text: " + token.lexeme + " token type: " + token.type);
            // }
        }

        //function to notify user of invalid code/errors in code
        //returns the line number of the error and a message explaining there was an error
        public static void Error(int line, string message){
            Report(line, "", message);
        }

        private static void Report(int line, string location, string message){
            Console.Error.WriteLine("[line " + line + "] Error" + location + ": " + message);
            hadError = true;
        }

        public static void TokenError(Token token, string msg){
            if (token.type == TokenType.EOF){
                Report(token.line, " at end", msg);
            }
            else {
                Report(token.line, " at '" + token.lexeme + "'", msg);
            }
        }

        public static void RuntimeError(RuntimeError error){
            Console.WriteLine(error.Message + "Line number: " + error.tokenT.line);
            hadRuntimeError = true;
        }

         public static void TestingReset(){
            interpreter = new Interpreter();
            hadError = false;
            hadRuntimeError = false;
        }

        }
}  
