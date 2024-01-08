using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Lox{
    class Scanner{
        private readonly string source;
        private readonly List<Token> tokens = new List<Token>();
        private int start = 0;
        private int current = 0;
        private int line = 1;


        //create a map/dict structure to store the keywords used in LOX
        //reads in the keyword, maps it to the corresponding tokentype
        private static readonly Dictionary<string, TokenType> keywords = new Dictionary<string, TokenType>{
            {"and", TokenType.AND},
            {"class", TokenType.CLASS},
            {"else", TokenType.ELSE},
            {"false", TokenType.FALSE},
            {"for", TokenType.FOR},
            {"if", TokenType.IF},
            {"nil", TokenType.NIL},
            {"or", TokenType.OR},
            {"print", TokenType.PRINT},
            {"return", TokenType.RETURN},
            {"super", TokenType.SUPER},
            {"this", TokenType.THIS},
            {"true", TokenType.TRUE},
            {"var", TokenType.VAR},
            {"fun", TokenType.FUN},
            {"while", TokenType.WHILE}
        };

        //basic constructor for scanner class to set the source code string
        public Scanner(string source){
            this.source = source;
        }

       public List<Token> ScanTokens(){
            while(!IsAtEnd()){
                start = current;
                ScanToken();
            }
            tokens.Add(new Token(TokenType.EOF, "", null, line));
            return tokens;
        }


        //creating cases for each type of meaningful token we will scan
        //scans all of the input in one go to get through the input without stopping at each error
        //scans entire input without always trying to execute it
        private void ScanToken(){
            char c = Advance();
            switch (c) {
                case '(': AddToken(TokenType.LEFT_PAREN); break;
                case ')': AddToken(TokenType.RIGHT_PAREN); break;
                case '{': AddToken(TokenType.LEFT_BRACE); break;
                case '}': AddToken(TokenType.RIGHT_BRACE); break;
                case ',': AddToken(TokenType.COMMA); break;
                case '.': AddToken(TokenType.DOT); break;
                case '-': AddToken(TokenType.MINUS); break;
                case '+': AddToken(TokenType.PLUS); break;
                case ';': AddToken(TokenType.SEMICOLON); break;
                case '*': AddToken(TokenType.STAR); break;
                //if 
                case '!':
                    AddToken(Match('=') ? TokenType.BANG_EQUAL : TokenType.BANG);
                    break;
                case '=':
                    AddToken(Match('=') ? TokenType.EQUAL_EQUAL : TokenType.EQUAL);
                    break;
                case '>':
                    AddToken(Match('=') ? TokenType.GREATER_EQUAL : TokenType.GREATER);
                    break;
                case '<':
                    AddToken(Match('=') ? TokenType.LESS_EQUAL : TokenType.LESS);
                    break;
                //check if we have // for comments or just '/'
                case '/':
                    if (Match('/')){
                        while (Peek() != '\n' && !IsAtEnd()) Advance();
                    }
                    else{
                        AddToken(TokenType.SLASH);
                    }
                    break;
                //handle whitespace, tabs, newlines, etc
                //increment line number if we see \n to keep track of line number for error handling/messaging
                case ' ':
                case '\r':
                case '\t':
                    break;
                case '\n':
                    line++;
                    break;
                //token is a string
                case '"': Str(); break;
                //if we see an o, and the next character is an r, add the OR token
                case 'o':
                    if (Match('r')){
                        AddToken(TokenType.OR);
                    }
                    break;


                default:
                    //token is a number, integer/double
                    if (IsDigit(c)){
                        Number();
                    }
                    else if (IsAlpha(c)){
                        Identifier();
                    }
                    else{
                    Lox.Error(line, "Unexpected character. Not a known token!");
                    }
                    //Console.WriteLine(c);
                    break;
            }
        }

        //advance through the text as long as we keep seeing digits or alpha chars
        //once done advancing, take the substring we have advanced through and get its tokentype
        //access the dict of tokentypes, add the corresponding token to our list of tokens
        private void Identifier(){
            while (IsAlphaNumeric(Peek())){
                Advance();
            }
            //MAY NEED TO REMOVE THE -1 IN THE SUBSTR INDEX
            string text = source.Substring(start, current-start);
            //if token is one of our keywords, get its type and add it
            //if not in our keywords dict, set the type to IDENTIFIER and add it
            TokenType type = keywords.TryGetValue(text, out TokenType tokenType) ? tokenType : TokenType.IDENTIFIER;
            AddToken(type);
            //if token exists in map, add it with that tokens type
            //else, add the token with type 'identifier'
            // TokenType type = keywords[text];
            // if (IsInDict(type)){
            //     AddToken(type);
            // }
            // else{
            //     AddToken(TokenType.IDENTIFIER);
            // }

            // if (type) type = TokenType.IDENTIFIER;
            // AddToken(type);
            //AddToken(TokenType.IDENTIFIER);
        }

        //helper function to determine if the recognized token is one of our tokens in the dictionary
        //if it is, we return true which will add the token of that type
        //if it is not in the dictionary, the token will be added as a regular identifier
        // private bool IsInDict(TokenType type){
        //     return keywords.ContainsKey(type.ToString().ToLower());
        // }

        private void Number(){
            //while the next character(s) are digits, continue to move through the lox input
            while (IsDigit(Peek())) Advance();

            if (Peek() == '.' && IsDigit(PeekNext())) {
                //process the decimal character
                Advance();
                //continue to process the numbers after the decimal
                while (IsDigit(Peek())) Advance();
            }
            //once we are done parsing through the digits, add a token with type number
            AddToken(TokenType.NUMBER, double.Parse(source.Substring(start, current-start)));
        }

        //check if we are scanning a string literal
        //always begin with the " character, so we know when a string is beginning
        private void Str(){
            while (Peek() != '"' && !IsAtEnd()){
                if (Peek() == '\n') line++;
                Advance();
            }

            if (IsAtEnd()){
                Lox.Error(line, "Unterminated string.");
                return;
            }

            Advance();
            //once we are at correct index, add token of type string with proper index in source code
            string value = source.Substring(start+1, current-start-1);
            AddToken(TokenType.STRING, value);
        }

        //check to see if we have an '=' character after our current index
        //allows us to create new tokens such as !=, <=, etc
        private bool Match(char expected){
            if (IsAtEnd()) return false;
            if (source[current] != expected) return false;
            //else
            current++;
            return true; 
        }

        //similar to match() but doesnt consume character by incrementing current
        //just 'peeks' at next character in source to make sure we don't have newlines, etc
        //"lookahead" function
        private char Peek(){
            if (IsAtEnd()) return '\0';
            return source[current];
        }

        //looking at the character past the next
        //one character farther than peek()
        private char PeekNext(){
            if (current+1 >= source.Length) return '\0';
            return source[current+1];
        }

        private bool IsAlpha(char c){
            return (c >= 'a' && c < 'z') || (c >= 'A' && c <= 'Z') || c =='_';
        }

        private bool IsAlphaNumeric(char c){
            return IsAlpha(c) || IsDigit(c);
        }

        private bool IsDigit(char c){
            return c >= '0' && c <= '9';
        }

        //function to check if the current index is at the end of our input Lox string or not
        private bool IsAtEnd(){
            return current >= source.Length;
        }

        //used to increment the index of our string scanner
        private char Advance(){
            return source[current++];
        }


        //takes in token at current input, adds a token of corresponding type to our tokens list
        private void AddToken(TokenType type){
            AddToken(type, null);
        }

        private void AddToken(TokenType type, object literal){
            string text = source.Substring(start, current-start);
            tokens.Add(new Token(type, text, literal, line));
        }
    }
}