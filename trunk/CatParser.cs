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
        public static List<Function> TermsToFxns(List<AstExprNode> terms, DefinedFunction def)
        {
            List<Function> fxns = new List<Function>();
            foreach (AstExprNode child in terms)
            {
                Function f = ExprToFunction(child, def);
                fxns.Add(f);                
            }
            return fxns;
        }

        // The following is old-code that I no longer use. 
        // TODO: remove, once I am sure that I won't be using it anymore.
        /*
        private static int IndexOfNamedTerm(List<AstExprNode> terms, string sName)
        {
            for (int i = 0; i < terms.Count; ++i)
            {
                if (terms[i] is AstNameNode)
                    if (terms[i].ToString().Equals(sName))
                        return i;
            }
            return -1;
        }

        private static void LiftRecursiveCalls(List<AstExprNode> terms, string sName)
        {
            int i = terms.Count - 1;
            while (i >= 0)
            {
                if (terms[i] is AstQuoteNode)
                {
                    AstQuoteNode q = terms[i] as AstQuoteNode;
                    
                    // Recursively lift recursive calls
                    LiftRecursiveCalls(q.mTerms, sName);
                    
                    int n = IndexOfNamedTerm(q.mTerms, sName);
                    if (n >= 0)
                    {
                        AstNameNode name = q.mTerms[n] as AstNameNode;
                        if (name == null)
                            throw new Exception("internall error: expected name node");

                        List<AstExprNode> left = new List<AstExprNode>(q.mTerms.GetRange(0, n));
                        List<AstExprNode> right = new List<AstExprNode>(q.mTerms.GetRange(n + 1, q.mTerms.Count - n - 1));
                        terms.RemoveAt(i);

                        // [... self ...] => [...] self compose [...] compose
                        terms.Insert(i, new AstNameNode("compose"));
                        terms.Insert(i, new AstQuoteNode(right));
                        terms.Insert(i, new AstNameNode("compose"));
                        terms.Insert(i, name);
                        terms.Insert(i, new AstQuoteNode(left));

                        i += 5;
                    }
                }

                --i;
            }
        }

        private static void RewriteRecursiveCalls(List<AstExprNode> terms, DefinedFunction def)
        {
            foreach (AstExprNode node in terms)
                if (node is AstNameNode)
                    if (node.ToString().Equals(def.GetName()))
                        throw new Exception("you can not make a recursive call at the top-level of a function");

            LiftRecursiveCalls(terms, def.GetName());

            // "self" will only occur at top level now.
            foreach (AstExprNode node in terms)
                if (node is AstNameNode)
                    if (node.ToString().Equals(def.GetName()))
                        node.SetText("self");
        }
         */

        private static Quotation MakeQuoteFunction(AstQuoteNode node, DefinedFunction def)
        {
            return new Quotation(TermsToFxns(node.mTerms, def));
        }

        private static Quotation MakeQuoteFunction(AstLambdaNode node, DefinedFunction def)
        {
            CatPointFreeForm.Convert(node);
            return new Quotation(TermsToFxns(node.mTerms, def));
        }

        private static Function ExprToFunction(AstExprNode node, DefinedFunction def)
        {
            if (node is AstIntNode)
                return new PushInt((node as AstIntNode).GetValue());
            else if (node is AstBinNode)
                return new PushInt((node as AstBinNode).GetValue());
            else if (node is AstHexNode)
                return new PushInt((node as AstHexNode).GetValue());
            else if (node is AstFloatNode)
                return new PushValue<double>((node as AstFloatNode).GetValue());
            else if (node is AstStringNode)
                return new PushValue<string>((node as AstStringNode).GetValue());
            else if (node is AstCharNode)
                return new PushValue<char>((node as AstCharNode).GetValue());
            else if (node is AstNameNode)
            {
                string s = node.ToString();
                Function f = Executor.Main.GetGlobalContext().Lookup(s);
                if (s.Equals("self") || (def != null && s.Equals(def.GetName())))
                    return new SelfFunction(def);
                if (f == null)
                    throw new Exception("could not find function " + s);
                return f;
            }
            else if (node is AstQuoteNode)
                return MakeQuoteFunction(node as AstQuoteNode,def );
            else if (node is AstLambdaNode)
                return MakeQuoteFunction(node as AstLambdaNode, def);
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
            Executor.Main.GetGlobalContext().AddFunction(def);

            // NOTE: this is a broken algorithm. I'm leaving it here for reference
            // in case I want to do something similar later on.
            //RewriteRecursiveCalls(node.mTerms, def);

            def.AddFunctions(TermsToFxns(node.mTerms, def));            
            
            // Construct a representation of the meta data if neccessary
            if (node.mpMetaData != null)
                def.SetMetaData(new CatMetaDataBlock(node.mpMetaData));

            // Compare the inferred type with the declared type
            // This is a crtical part of the type checker.
            if (Config.gbTypeChecking && (node.mType != null))
            {
                CatFxnType declaredType = new CatFxnType(node.mType);
                def.SetTypeExplicit();

                if (!CatFxnType.CompareFxnTypes(def.mpFxnType, declaredType))
                {
                    Output.WriteLine("type error in function " + def.GetName());
                    Output.WriteLine("inferred type " + def.GetFxnType().ToPrettyString());
                    Output.WriteLine("declared type " + declaredType.ToPrettyString());
                    bool bTmp = CatFxnType.CompareFxnTypes(def.mpFxnType, declaredType);
                    def.SetTypeError();
                    
                    // avoid an extra output of the type
                    return;
                }
            }
            if (Config.gbShowInferredType)
            {
                if (def != null && def.GetFxnType() != null)
                    Output.WriteLine(def.GetName() + " : " + def.GetFxnType().ToPrettyString());
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
                Function f = ExprToFunction(node as AstExprNode, null);
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
            else if (node is AstMetaDataBlock)
            {
                throw new Exception("meta-data blocks can only occur in definitions");
            }
            else
            {
                throw new Exception("Unhandled AST node type " + node.GetLabel());
            }
        }

        public static void Parse(string s, Executor exec)
        {
            Peg.Parser parser = new Peg.Parser(s);

            try
            {
                bool bResult = parser.Parse(CatGrammar.CatProgram());
                if (!bResult)
                    throw new Exception("failed to parse input");
            }
            catch (Exception e)
            {
                Output.WriteLine("Parsing error occured with message: " + e.Message);
                Output.WriteLine(parser.ParserPosition);
                throw e;
            }

            Peg.PegAstNode node = parser.GetAst();

            foreach (Peg.PegAstNode child in node.GetChildren())
                ProcessNode(CatAstNode.Create(child), exec);
        }

        public static List<AstExprNode> ParseExpr(string s)
        {
            Peg.Parser parser = new Peg.Parser(s);

            try
            {
                bool bResult = parser.Parse(CatGrammar.CatProgram());
                if (!bResult)
                    throw new Exception("failed to parse input");
            }
            catch (Exception e)
            {
                Output.WriteLine("Parsing error occured with message: " + e.Message);
                Output.WriteLine(parser.ParserPosition);
                throw e;
            }

            AstExprListNode tmp = new AstExprListNode(parser.GetAst());

            return tmp.mTerms;
        }
        #endregion
    }
}
