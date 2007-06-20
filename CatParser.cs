/// Dedicated to the public domain by Christopher Diggins
/// http://creativecommons.org/licenses/publicdomain/

using System;
using System.Collections.Generic;
using System.Text;

namespace Cat
{
    /// <summary>
    /// The CatParser transforms a Peg AST into meaningful data structures.
    /// </summary>
    class CatParser
    {
        #region parsing functions
        private static Quotation MakeQuoteFunction(AstQuoteNode node)
        {
            List<Function> fxns = new List<Function>();
            foreach (AstExprNode child in node.Terms)
                fxns.Add(ExprToFunction(child));
            return new Quotation(fxns);
        }

        private static Function ExprToFunction(AstExprNode node)
        {
            if (node is AstIntNode)
                return new PushValue<int>((node as AstIntNode).GetValue());
            else if (node is AstBinNode)
                return new PushValue<int>((node as AstBinNode).GetValue());
            else if (node is AstHexNode)
                return new PushValue<int>((node as AstHexNode).GetValue());
            else if (node is AstFloatNode)
                return new PushValue<double>((node as AstFloatNode).GetValue());
            else if (node is AstStringNode)
                return new PushValue<string>((node as AstStringNode).GetValue());
            else if (node is AstCharNode)
                return new PushValue<char>((node as AstCharNode).GetValue());
            else if (node is AstNameNode)
            {
                Function f = Executor.Main.GetGlobalScope().Lookup(node.ToString());
                if (f == null)
                    throw new Exception("could not find function " + node.ToString());
                return f;
            }
            else if (node is AstQuoteNode)
                return MakeQuoteFunction(node as AstQuoteNode);
            else
                throw new Exception("node " + node.ToString() + " does not have associated function");
        }

        private static void ProcessDefinition(AstDefNode node)
        {
            // NOTE: should this really be here? 
            if (Config.gbAllowNamedParams)
                CatPointFreeForm.Convert(node);
            else if (node.mParams.Count > 0)
                throw new Exception("named parameters are not enabled");

            DefinedFunction def = new DefinedFunction(node.mName);
            Executor.Main.GetGlobalScope().AddFunction(def);
            List<Function> fxns = new List<Function>();
            foreach (AstExprNode term in node.mTerms)
                fxns.Add(ExprToFunction(term));
            def.AddFunctions(fxns);

            // Compare the inferred type with the declared type
            if (node.mType != null)
            {
                CatFxnType declaredType = new CatFxnType(node.mType);
                if (!CatFxnType.CompareFxnTypes(declaredType, def.mpFxnType))
                {
                    if (!Config.gbVerboseInference)
                        MainClass.WriteLine("inferred type " + def.GetFxnType());
                    MainClass.WriteLine("declared type " + declaredType.ToString());
                    MainClass.WriteLine("type error in function " + def.GetName());
                }
                else if (Config.gbVerboseTypeChecking)
                {
                    MainClass.WriteLine("declared type " + declaredType.ToString());
                    MainClass.WriteLine("type check successful for " + def.GetName());
                }
            }
        }

        private static void ProcessMacro(AstMacroNode node)
        {
            Macros.GetGlobalMacros().AddMacro(node);
        }

        private static void ProcessNode(CatAstNode node, Executor exec)
        {
            if (node is AstExprNode)
            {
                Function f = ExprToFunction(node as AstExprNode);
                f.Eval(exec);
            }
            else if (node is AstDefNode)
            {
                ProcessDefinition(node as AstDefNode);
            }
            else if (node is AstMacroNode)
            {
                ProcessMacro(node as AstMacroNode);
            }
            else
            {
                throw new Exception("Unhandled AST node type " + node.GetLabel());
            }
        }

        public static void Parse(string s, Executor exec)
        {
            Peg.Parser parser = new Peg.Parser(s);
            bool bResult = parser.Parse(CatGrammar.Line());
            if (!bResult)
                throw new Exception("failed to parse: " + s);
            Peg.PegAstNode node = parser.GetAst();

            foreach (Peg.PegAstNode child in node.GetChildren())
                ProcessNode(CatAstNode.Create(child), exec);
        }
        #endregion
    }
}
