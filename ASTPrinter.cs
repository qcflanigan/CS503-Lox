using System.Text;


//printer class to print out Lox expressions in correct format+order
class ASTPrinter : Expr.IExprVisitor<string>{
    public string Print(Expr expr){
        return expr.Accept(this);
    }

    public string VisitBinaryExpr(Expr.Binary expr){
        return Parenthesize(expr.op.lexeme, expr.left, expr.right);
    }  

    public string VisitVariableExpr(Expr.Variable expr){
        return expr.ToString();
    }  

    public string VisitGroupingExpr(Expr.Grouping expr){
        return Parenthesize("group", expr.expression);
    }

    public string VisitLiteralExpr(Expr.Literal expr){
        if (expr.value == null) return "null";
        return expr.value.ToString();
    }

    public string VisitLogicalExpr(Expr.Logical expr){
        return expr.ToString();
    }

    //check if the accept() is right, not sure
    public string VisitCallExpr(Expr.Call expr){
        return expr.Accept(this);
    }

    //check if the accept() is right, not sure
    public string VisitGetExpr(Expr.Get expr){
        return expr.Accept(this);
    }
    
    //check tostring()
    public string VisitSetExpr(Expr.Set expr){
        return expr.ToString();
    }

    public string VisitSuperExpr(Expr.Super expr){
        return expr.ToString();
    }

    public string VisitThisExpr(Expr.This expr){
        return expr.ToString();
    }

    public string VisitUnaryExpr(Expr.Unary expr){
        return Parenthesize(expr.op.lexeme, expr.right);
    }

    public string VisitAssignExpr(Expr.Assign expr){
        return expr.value.ToString();
    }

    private string Parenthesize(string name, params Expr[] exprs){
        StringBuilder builder = new StringBuilder();
        builder.Append("(").Append(name);
        foreach (Expr expr in exprs){
            builder.Append(" ");
            builder.Append(expr.Accept(this));
        }

        builder.Append(")");
        return builder.ToString();
    }

    //main function to create expression and test the printing ability
// public static void Main(string[] args){
//     Expr expr = new Expr.Binary(
//         new Expr.Unary(
//             new Token(TokenType.MINUS, "-", null, 1),
//             new Expr.Literal(123)),
//         new Token(TokenType.STAR, "*", null, 1),
//         new Expr.Grouping(
//             new Expr.Literal(45.67)));
    
//     Console.WriteLine(new ASTPrinter().Print(expr));
// }
}