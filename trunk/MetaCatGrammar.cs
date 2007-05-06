/// Public domain code by Christopher Diggins
/// http://www.cat-language.com

using System;
using System.Collections.Generic;
using System.Text;
using Peg;

namespace Cat
{
    /// <summary>
    /// MetaCat is a superset of ordinary Cat which includes the usage of macros.
    /// </summary>
    public class MetaCatGrammar : CatGrammar
    {
        public static Rule MacroTypeVar()
        {
            return Seq(SingleChar('$'), TypeVar());
        }
        public static Rule MacroStackVar()
        {
            return Seq(SingleChar('$'), StackVar());
        }
        public static Rule MacroTerm()
        {
            return AstNode("macro_term", Choice(MacroQuote(), MacroTypeVar(), MacroStackVar(), Name())); 
        }
        public static Rule MacroQuote()
        {
            return AstNode("macro_quote", Seq(Token("["), Delay(MacroTerm), NoFail(Token("]"), "missing ']'"))); 
        }
        public static Rule MacroSrcPattern()
        {
            return AstNode("macro_src", Seq(Token("{"), Star(MacroTerm()), NoFail(Token("}"), "missing '}'")));
        }
        public static Rule MacroDestPattern()
        {
            return AstNode("macro_dest", Seq(Token("{"), Star(MacroTerm()), NoFail(Token("}"), "missing '}'")));
        }
        public static Rule MacroDef()
        {
            return Seq(MacroSrcPattern(), Token("=>"), MacroDestPattern());
        }
        public static Rule Macro()
        {
            return AstNode("mac", Seq(Word("macro"), NoFail(MacroDef(), "expected macro defintiion")));
        }
        public static Rule MetaCatProgram()
        {
            return Star(Token(Macro()));
        }
    }
}