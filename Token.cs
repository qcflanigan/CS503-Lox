//main class to represent each token we process during Lox code execution
//gives each token a type, lexeme, literal, and line number
public class Token{
    public TokenType type;
    public string lexeme;
    public Object literal;
    public int line;

    //constructor for each token, assigning it its corresponding values
    public Token(TokenType type, string lexeme, Object literal, int line){
        this.type = type;
        this.lexeme = lexeme;
        this.literal = literal;
        this.line = line;
    }

    //converts the member info of each token into a string 
    public override string ToString(){
        return type + " " + lexeme + " " + literal;
    }
}