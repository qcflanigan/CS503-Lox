using System;
using System.Collections.Generic;

public abstract class Expr
{
public interface IExprVisitor<R>
{
   R VisitAssignExpr(Assign expr);
   R VisitBinaryExpr(Binary expr);
   R VisitCallExpr(Call expr);
   R VisitGetExpr(Get expr);
   R VisitGroupingExpr(Grouping expr);
   R VisitLiteralExpr(Literal expr);
   R VisitLogicalExpr(Logical expr);
   R VisitSetExpr(Set expr);
   R VisitSuperExpr(Super expr);
   R VisitThisExpr(This expr);
   R VisitUnaryExpr(Unary expr);
   R VisitVariableExpr(Variable expr);
}
  public class Assign  : Expr 
  {
      public Assign(Token name, Expr value)
      {
             this.name = name;
             this.value = value;
      }

  public override R Accept<R>(IExprVisitor<R> visitor)
  {
     return visitor.VisitAssignExpr(this);
      }
      public readonly Token name;
      public readonly Expr value;
      }

  public class Binary  : Expr 
  {
      public Binary(Expr left, Token op, Expr right)
      {
             this.left = left;
             this.op = op;
             this.right = right;
      }

  public override R Accept<R>(IExprVisitor<R> visitor)
  {
     return visitor.VisitBinaryExpr(this);
      }
      public readonly Expr left;
      public readonly Token op;
      public readonly Expr right;
      }

  public class Call  : Expr 
  {
      public Call(Expr callee, Token paren, List<Expr> arguments)
      {
             this.callee = callee;
             this.paren = paren;
             this.arguments = arguments;
      }

  public override R Accept<R>(IExprVisitor<R> visitor)
  {
     return visitor.VisitCallExpr(this);
      }
      public readonly Expr callee;
      public readonly Token paren;
      public readonly List<Expr> arguments;
      }

  public class Get  : Expr 
  {
      public Get(Expr obj, Token name)
      {
             this.obj = obj;
             this.name = name;
      }

  public override R Accept<R>(IExprVisitor<R> visitor)
  {
     return visitor.VisitGetExpr(this);
      }
      public readonly Expr obj;
      public readonly Token name;
      }

  public class Grouping  : Expr 
  {
      public Grouping(Expr expression)
      {
             this.expression = expression;
      }

  public override R Accept<R>(IExprVisitor<R> visitor)
  {
     return visitor.VisitGroupingExpr(this);
      }
      public readonly Expr expression;
      }

  public class Literal  : Expr 
  {
      public Literal(Object value)
      {
             this.value = value;
      }

  public override R Accept<R>(IExprVisitor<R> visitor)
  {
     return visitor.VisitLiteralExpr(this);
      }
      public readonly Object value;
      }

  public class Logical  : Expr 
  {
      public Logical(Expr left, Token op, Expr right)
      {
             this.left = left;
             this.op = op;
             this.right = right;
      }

  public override R Accept<R>(IExprVisitor<R> visitor)
  {
     return visitor.VisitLogicalExpr(this);
      }
      public readonly Expr left;
      public readonly Token op;
      public readonly Expr right;
      }

  public class Set  : Expr 
  {
      public Set(Expr obj, Token name, Expr value)
      {
             this.obj = obj;
             this.name = name;
             this.value = value;
      }

  public override R Accept<R>(IExprVisitor<R> visitor)
  {
     return visitor.VisitSetExpr(this);
      }
      public readonly Expr obj;
      public readonly Token name;
      public readonly Expr value;
      }

  public class Super  : Expr 
  {
      public Super(Token keyword, Token method)
      {
             this.keyword = keyword;
             this.method = method;
      }

  public override R Accept<R>(IExprVisitor<R> visitor)
  {
     return visitor.VisitSuperExpr(this);
      }
      public readonly Token keyword;
      public readonly Token method;
      }

  public class This  : Expr 
  {
      public This(Token keyword)
      {
             this.keyword = keyword;
      }

  public override R Accept<R>(IExprVisitor<R> visitor)
  {
     return visitor.VisitThisExpr(this);
      }
      public readonly Token keyword;
      }

  public class Unary  : Expr 
  {
      public Unary(Token op, Expr right)
      {
             this.op = op;
             this.right = right;
      }

  public override R Accept<R>(IExprVisitor<R> visitor)
  {
     return visitor.VisitUnaryExpr(this);
      }
      public readonly Token op;
      public readonly Expr right;
      }

  public class Variable  : Expr 
  {
      public Variable(Token name)
      {
             this.name = name;
      }

  public override R Accept<R>(IExprVisitor<R> visitor)
  {
     return visitor.VisitVariableExpr(this);
      }
      public readonly Token name;
      }


     public abstract R Accept<R>(IExprVisitor<R> visitor);
}
