using System;
using System.Collections.Generic;
using System.Text;
using Peg;

namespace Cat
{
    public class CatGrammar : Grammar
    {
        public static Rule Token(string s) 
        { 
            return Token(CharSeq(s)); 
        }
        public static Rule Token(Rule r) 
        {
            return Seq(r, WS()); 
        }
        public static Rule Word(string s) 
        {
            return Seq(CharSeq(s), EOW(), WS()); 
        }
        public static Rule LineComment() 
        {
            return Seq(CharSeq("//"), NoFail(WhileNot(AnyChar(), NL()), "expected a new line")); 
        }
        public static Rule FullComment()
        {
            return Seq(CharSeq("/*"), WhileNot(AnyChar(), CharSeq("*/")));
        }
        public static Rule Comment()
        {
            return Choice(FullComment(), LineComment());
        }
        public static Rule WS()
        {
            return Star(Choice(CharSet(" \t\n\r"), Comment()));
        }
        public static Rule Quote() 
        {
            // Note the usage of Delay which breaks circular references in the grammar
            return AstNode("quote", Seq(Token("["), Star(Delay(Expr)), NoFail(Token("]"), "missing ']'"))); 
        }
        public static Rule IntegerLiteral() 
        { 
            return AstNode("int", Seq(Plus(Digit()), Not(CharSet(".")))); 
        }
        public static Rule EscapeChar() 
        { 
            return Seq(SingleChar('\\'), AnyChar()); 
        }
        public static Rule StringCharLiteral() 
        { 
            return Choice(EscapeChar(), NotChar('"')); 
        }
        public static Rule CharLiteral() 
        {
            return AstNode("char", Seq(SingleChar('\''), StringCharLiteral(), SingleChar('\'')));
        }
        public static Rule StringLiteral() 
        { 
            return AstNode("string", Seq(SingleChar('\"'), Star(StringCharLiteral()), SingleChar('\"'))); 
        }
        public static Rule FloatLiteral() 
        { 
            return AstNode("float", Seq(Plus(Digit()), SingleChar('.'), Plus(Digit()))); 
        }
        public static Rule HexLiteral() 
        { 
            return AstNode("hex", Seq(CharSeq("0x"), Star(HexDigit()))); 
        }
        public static Rule Literal() 
        {
            return Choice(StringLiteral(), CharLiteral(), FloatLiteral(), IntegerLiteral()); 
        }        
        public static Rule SingleSymbol() 
        { 
            return CharSet("(),"); 
        }
        public static Rule MultiSymbol() 
        { 
            return CharSet("~`!@#$%^&*-+=|\\:;<>.?/"); 
        }
        public static Rule SymbolGroup() 
        {
            return Choice(SingleSymbol(), Plus(MultiSymbol())); 
        }
        public static Rule Name() 
        {
            return Token(AstNode("name", Choice(Ident(), SymbolGroup()))); 
        }
        public static Rule Expr()
        {
            return Token(Choice(Name(), Literal(), Quote()));
        }
        public static Rule CodeBlock()
        {
            return Seq(Token("{"), Star(Expr()), NoFail(Token("}"), "missing '}'"));
        }
        public static Rule Param()
        {
            return Token(AstNode("param", Ident()));
        }
        public static Rule Params()
        {
            return Seq(Token("("), Star(Param()), NoFail(Token(")"), "missing ')'"));
        }
        public static Rule TypeVar()
        {
            return Seq(LowerCaseLetter(), Star(IdentNextChar()));
        }
        public static Rule StackVar()
        {
            return Seq(UpperCaseLetter(), Star(IdentNextChar()));
        }
        public static Rule TypeOrStackVar()
        {
            return Seq(SingleChar('\''), NoFail(Choice(TypeVar(), StackVar()), "invalid type or stack variable name"), WS());
        }
        public static Rule TypeName()
        {
            return Token(Ident());
        }
        public static Rule TypeAlias()
        {
            return Token(Seq(Ident(), Token("="), NoFail(Choice(Delay(FxnType), TypeName()), "only function types can be labelled")));
        }
        public static Rule TypeComponent()
        {
            return Choice(TypeAlias(), TypeName(), TypeOrStackVar(), Delay(FxnType));
        }
        public static Rule Production()
        {
            return NoFail(Token(Star(TypeComponent())), "production matching should never fail");
        }
        public static Rule Consumption()
        {
            return NoFail(Token(Star(TypeComponent())), "consumption matching should never fail");
        }
        public static Rule Arrow()
        {
            return Choice(Token("->"), Token("~>"));
        }
        public static Rule FxnType()
        {
            return Seq(Token("("), Production(), NoFail(Arrow(), "expected either -> or ~>"), Consumption(), NoFail(Token(")"), "expected closing paranthesis"));
        }
        public static Rule TypeDecl()
        {
            return Seq(Token(":"), NoFail(FxnType(), "expected function type declaration"), WS());
        }
        public static Rule Def()
        {
            return AstNode("def", Seq(Word("define"), NoFail(Name(), "expected name"),
                Opt(Params()), Opt(TypeDecl()), NoFail(CodeBlock(), "expected a code block")));
        }
        public static Rule Line()
        {
            return Seq(WS(), Star(Choice(Def(), Expr())));
        }
        public static Rule Program()
        {
            return Star(Token(Def()));
        }
    }
}
