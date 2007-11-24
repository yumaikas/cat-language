/// Dedicated to the public domain by Christopher Diggins
/// http://creativecommons.org/licenses/publicdomain/

using System;
using System.Collections.Generic;
using System.Text;
using Peg;

namespace Cat
{
    public class CatGrammar : Grammar
    {
        public static Rule UntilEndOfLine()
        {
            return NoFail(WhileNot(AnyChar(), NL()), "expected a new line");
        }
        public static Rule LineComment() 
        { 
            return Seq(CharSeq("//"), UntilEndOfLine()); 
        }
        public static Rule BlockComment()
        {
            return Seq(CharSeq("/*"), NoFail(WhileNot(AnyChar(), CharSeq("*/")), "expected a new line"));
        }
        public static Rule MetaDataContent()
        {
            return AstNode("meta_data_content", UntilEndOfLine());
        }
        public static Rule MetaDataLabel()
        {            
            return AstNode("meta_data_label", Seq(Star(CharSet(" \t")), Ident(), SingleChar(':')));
        }
        public static Rule MetaDataEntry()
        {
            return Seq(Opt(MetaDataLabel()), Star(CharSet(" \t")), MetaDataContent());
        }
        public static Rule StartMetaDataBlock()
        {
            return Seq(WS(), CharSeq("{{"), UntilEndOfLine());
        }
        public static Rule EndMetaDataBlock()
        {
            return Seq(WS(), CharSeq("}}"), UntilEndOfLine());
        }
        public static Rule MetaDataBlock() 
        {
            return Seq(AstNode("meta_data_block", Seq(StartMetaDataBlock(), WhileNot(MetaDataEntry(), EndMetaDataBlock()))), WS()); 
        }
        public static Rule Comment() 
        { 
            return Choice(BlockComment(), LineComment()); 
        }
        public static Rule WS() 
        { 
            return Star(Choice(CharSet(" \t\n\r"), Comment())); 
        }
        public static Rule CatIdentChar()
        {
            return Choice(IdentNextChar(), CharSet("~`!@#$%^&*-+=|:;<>.?/"));
        }
        public static Rule CatIdent()
        {
            return Plus(CatIdentChar()); ;
        }
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
        public static Rule Quote() 
        {
            // Note the usage of Delay which breaks circular references in the grammar
            return AstNode("quote", Seq(Token("["), Star(Delay(Expr)), NoFail(Token("]"), "missing ']'"))); 
        }
        public static Rule IntegerLiteral() 
        { 
            return AstNode("int", Seq(Opt(SingleChar('-')), Plus(Digit()), Not(CharSet(".")))); 
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
            return AstNode("float", Seq(Opt(SingleChar('-')), Plus(Digit()), SingleChar('.'), Plus(Digit()))); 
        }
        public static Rule HexValue()
        {
            return AstNode("hex", Plus(HexDigit()));
        }
        public static Rule HexLiteral()
        {
            return Seq(CharSeq("0x"), NoFail(HexValue(), "expected at least one hexadecimal digit"));
        }
        public static Rule BinaryValue()
        {
            return AstNode("bin", Plus(BinaryDigit()));
        }
        public static Rule BinaryLiteral()
        {
            return Seq(CharSeq("0b"), NoFail(BinaryValue(), "expected at least one binary digit"));
        }
        public static Rule NumLiteral()
        {            
            return Choice(HexLiteral(), BinaryLiteral(), FloatLiteral(), IntegerLiteral());
        }
        public static Rule Literal() 
        {
            return Choice(StringLiteral(), CharLiteral(), NumLiteral()); 
        }        
        public static Rule Symbol() 
        { 
            // The "()" together is treated as a single symbol
            return Choice(CharSeq("()"), CharSet("(),")); 
        }
        public static Rule Name() 
        {
            return Token(AstNode("name", Choice(Symbol(), CatIdent()))); 
        }
        public static Rule Lambda()
        {
            return AstNode("lambda", Seq(CharSeq("\\"), NoFail(Seq(Param(), CharSeq("."), Choice(Delay(Lambda), 
                NoFail(Quote(), "expected a quotation or lambda expression"))), "expected a lambda expression")));
        }
        public static Rule Expr()
        {
            return Token(Choice(Lambda(), Literal(), Quote(), Name()));
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
        public static Rule TypeModifier()
        {
            return AstNode("type_modifier", Opt(SingleChar('*')));
        }
        public static Rule TypeVar()
        {
            return AstNode("type_var", Seq(Opt(CharSeq("$")), LowerCaseLetter(), Star(IdentNextChar()), TypeModifier()));
        }
        public static Rule StackVar()
        {
            return AstNode("stack_var", Seq(Opt(CharSeq("$")), UpperCaseLetter(), Star(IdentNextChar()), TypeModifier()));
        }
        public static Rule TypeOrStackVar()
        {
            return Seq(SingleChar('\''), NoFail(Choice(TypeVar(), StackVar()), "invalid type or stack variable name"), WS());
        }
        public static Rule TypeName()
        {
            return Token(AstNode("type_name", Seq(Ident(), TypeModifier())));        
        }
        public static Rule TypeComponent()
        {
            return Choice(TypeName(), TypeOrStackVar(), Delay(FxnType));
        }
        public static Rule Production()
        {
            return AstNode("stack", Token(Star(TypeComponent())));
        }
        public static Rule Consumption()
        {
            return AstNode("stack", Token(Star(TypeComponent())));
        }
        public static Rule Arrow()
        {
            return AstNode("arrow", Choice(Token("->"), Token("~>")));
        }
        public static Rule FxnType()
        {
            return AstNode("type_fxn", Seq(Token("("), Production(), NoFail(Arrow(), "expected either -> or ~>"), Consumption(), NoFail(Token(")"), "expected closing paranthesis"), TypeModifier()));
        }
        public static Rule TypeDecl()
        {
            return Seq(Token(":"), NoFail(FxnType(), "expected function type declaration"), WS());
        }
        public static Rule FxnDef()
        {
            return AstNode("def", Seq(Word("define"), NoFail(Name(), "expected name"),
                Opt(Params()), Opt(TypeDecl()), Opt(MetaDataBlock()), NoFail(CodeBlock(), "expected a code block")));
        }
        #region macros
        public static Rule MacroTypeVar()
        {
            return AstNode("macro_type_var", Seq(LowerCaseLetter(), Star(IdentNextChar())));
        }
        public static Rule MacroStackVar()
        {
            return AstNode("macro_stack_var", Seq(UpperCaseLetter(), Star(IdentNextChar())));
        }
        public static Rule MacroVar()
        {
            return Seq(SingleChar('$'), NoFail(Choice(MacroTypeVar(), MacroStackVar()), "expected a valid macro type variable or stack variable"));
        }
        public static Rule MacroName()
        {
            return AstNode("macro_name", Choice(Symbol(), CatIdent()));
        }
        public static Rule MacroTerm()
        {
            return Token(Choice(MacroQuote(), MacroVar(), MacroName()));
        }
        public static Rule MacroQuote()
        {
            return AstNode("macro_quote", Seq(Token("["), Star(Delay(MacroTerm)), NoFail(Token("]"), "missing ']'")));
        }
        public static Rule MacroPattern()
        {
            return AstNode("macro_pattern", Seq(Token("{"), Star(MacroTerm()), NoFail(Token("}"), "missing '}'")));
        }
        public static Rule MacroDef()
        {
            return AstNode("macro", Seq(Choice(Word("macro"), Word("rule")), NoFail(Seq(MacroPattern(), Token("=>"), MacroPattern()), "expected macro defintiion")));
        }
        #endregion
        public static Rule CatProgram()
        {
            return Seq(WS(), Star(Choice(MetaDataBlock(), FxnDef(), MacroDef(), Expr())), WS(), NoFail(EndOfInput(), "expected macro or function defintion"));
        }
    }
}
