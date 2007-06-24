/// Dedicated to the public domain by Christopher Diggins
/// http://creativecommons.org/licenses/publicdomain/

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Globalization;
using Peg;

namespace Cat
{
    /// <summary>
    /// A AstNode is used as a base class for a typed abstract syntax tree for Cat programs.
    /// CatAstNodes are created from a Peg.Ast. Apart from being typed, the big difference
    /// is the a AstNode can be modified. This makes rewriting algorithms much easier. 
    /// </summary>
    public abstract class CatAstNode
    {
        string msText;
        string msLabel;
        string msComment;

        public CatAstNode(PegAstNode node)
        {
            if (node.GetNumChildren() == 0)
                msText = node.ToString();
            else
                msText = "";
            msLabel = node.GetLabel();
        }

        public CatAstNode(string sLabel, string sText)
        {
            msLabel = sLabel;
            msText = sText;
        }

        public void SetComment(string s)
        {
            msComment = s;
        }

        public static CatAstNode Create(PegAstNode node)
        {
            switch (node.GetLabel())
            {
                case "program":
                    return new AstProgram(node);
                case "def":
                    return new AstDefNode(node);
                case "name":
                    return new AstNameNode(node);
                case "param":
                    return new AstParamNode(node);
                case "lambda":
                    return new AstLambdaNode(node);
                case "quote":
                    return new AstQuoteNode(node);
                case "char":
                    return new AstCharNode(node);
                case "string":
                    return new AstStringNode(node);
                case "float":
                    return new AstFloatNode(node);
                case "int":
                    return new AstIntNode(node);
                case "bin":
                    return new AstBinNode(node);
                case "hex":
                    return new AstHexNode(node);
                case "stack":
                    return new AstStackNode(node);
                case "type_fxn":
                    return new AstFxnTypeNode(node);
                case "type_var":
                    return new AstTypeVarNode(node);
                case "type_name":
                    return new AstSimpleTypeNode(node);
                case "stack_var":
                    return new AstStackVarNode(node);
                case "macro":
                    return new AstMacroNode(node);
                case "macro_pattern":
                    return new AstMacroPattern(node);
                case "macro_quote":
                    return new AstMacroQuote(node);
                case "macro_type_var":
                    return new AstMacroTypeVar(node);
                case "macro_stack_var":
                    return new AstMacroStackVar(node);
                case "macro_name":
                    return new AstMacroName(node);
                case "meta_data_content":
                    return new AstMetaDataContent(node);
                case "meta_data_label":
                    return new AstMetaDataLabel(node);
                case "meta_data_block":
                    return new AstMetaDataBlock(node);
                default:
                    throw new Exception("unrecognized node type in AST tree: " + node.GetLabel());
            }
        }   

        public void CheckLabel(string s)
        {
            if (msLabel != s)
            {
                throw new Exception("Expected '" + s + "' node, but instead found '" + msLabel + "' node");
            }
        }

        public void CheckIsLeaf(PegAstNode node)
        {
            CheckChildCount(node, 0);
        }

        public void CheckChildCount(PegAstNode node, int n)
        {
            if (node.GetNumChildren() != n)
            {
                throw new Exception("expected " + n.ToString() + " children, instead found " + node.GetNumChildren().ToString());
            }
        }

        public string GetLabel()
        {
            return msLabel;
        }

        public override string ToString()
        {
            return msText;
        }

        public void SetText(string sText)
        {
            msText = sText;
        }

        public string GetComment()
        {
            return msComment;
        }

        public bool HasComment()
        {
            return msComment != null;
        }

        public string IndentedString(int nIndent, string s)
        {
            if (nIndent > 0)
                return new String('\t', nIndent) + s;
            else
                return s;
        }

        public virtual void Output(TextWriter writer, int nIndent)
        {
            writer.Write(ToString());
        }
    }

    public class AstProgram : CatAstNode
    {
        public List<AstDefNode> Defs = new List<AstDefNode>();

        public AstProgram(PegAstNode node) : base(node)
        {
            CheckLabel("ast");
            foreach (PegAstNode child in node.GetChildren())
                Defs.Add(new AstDefNode(child));
        }

        public override void Output(TextWriter writer, int nIndent)
        {
            foreach (AstDefNode d in Defs)
                d.Output(writer, nIndent);
        }
    }

    public class AstExprNode : CatAstNode
    {
        public AstExprNode(PegAstNode node) : base(node) { }
        public AstExprNode(string sLabel, string sText) : base(sLabel, sText) { }

        public override void Output(TextWriter writer, int nIndent)
        {
            string sLine = ToString();
            if (HasComment())
                sLine += " // " + GetComment();
            writer.WriteLine(IndentedString(nIndent, sLine));
        }
    }

    public class AstDefNode : CatAstNode
    {
        public string mName;
        public AstFxnTypeNode mType;
        public List<AstParamNode> mParams = new List<AstParamNode>();
        public List<AstExprNode> mTerms = new List<AstExprNode>();

        public AstDefNode(PegAstNode node) : base(node)
        {
            CheckLabel("def");

            if (node.GetNumChildren() == 0)
                throw new Exception("invalid function definition node");

            AstNameNode name = new AstNameNode(node.GetChild(0));
            mName = name.ToString();

            int n = 1;

            // Look to see if a type is defined
            if ((node.GetNumChildren() >= 2) && (node.GetChild(1).GetLabel() == "type_fxn"))
            {
                mType = new AstFxnTypeNode(node.GetChild(1));
                ++n;
            }

            while (n < node.GetNumChildren())
            {
                PegAstNode child = node.GetChild(n);
                
                if (child.GetLabel() != "param")
                    break;

                mParams.Add(new AstParamNode(child));
                n++;
            }

            while (n < node.GetNumChildren())
            {
                PegAstNode child = node.GetChild(n);

                if (child.GetLabel() != "param")
                    break;

                mParams.Add(new AstParamNode(child));
                n++;
            }

            while (n < node.GetNumChildren())
            {
                PegAstNode child = node.GetChild(n);

                if (child.GetLabel() != "meta_data_block")
                    break;

                // TODO: store meta data.

                n++;
            }

            while (n < node.GetNumChildren())
            {
                PegAstNode child = node.GetChild(n);
                CatAstNode expr = Create(child);

                if (!(expr is AstExprNode))
                    throw new Exception("expected expression node");
                
                mTerms.Add(expr as AstExprNode);
                n++;
            }
        }

        public override void Output(TextWriter writer, int nIndent)
        {
            if (HasComment())
                writer.WriteLine(IndentedString(nIndent, " // " + GetComment()));
            string s = "define " + mName;
            if (mType != null)
            {
                s += " : " + mType.ToString();
            }
            if (mParams.Count > 0)
            {
                s += " // ( ";
                foreach (AstParamNode p in mParams)
                    s += p.ToString() + " ";
                s += ")";
            }
            writer.WriteLine(IndentedString(nIndent, s));            
            writer.WriteLine(IndentedString(nIndent, "{"));
            foreach (AstExprNode x in mTerms)
                x.Output(writer, nIndent + 1);
            writer.WriteLine(IndentedString(nIndent, "}"));
        }
    }

    public class AstNameNode : AstExprNode
    {
        public AstNameNode(PegAstNode node) : base(node)
        {
            CheckLabel("name");
            CheckIsLeaf(node);
        }        
        
        public AstNameNode(string sOp, string sComment)
            : base("name", sOp)
        {
            SetComment(sComment);
        }

        public AstNameNode(string sOp)
            : base("name", sOp)
        {
        }
    }

    public class AstParamNode : CatAstNode
    {
        public AstParamNode(PegAstNode node) : base(node)
        {
            CheckLabel("param");
            CheckIsLeaf(node);
        }

        public override void Output(TextWriter writer, int nIndent)
        {
            throw new Exception("The method or operation is not implemented.");
        }
    }

    public class AstLambdaNode : AstExprNode
    {
        public List<string> mIdentifiers = new List<string>();
        public List<AstExprNode> mTerms = new List<AstExprNode>();

        public AstLambdaNode(PegAstNode node)
            : base(node)
        {
            CheckLabel("lambda");
            CheckChildCount(node, 2);
            
            AstParamNode name = new AstParamNode(node.GetChild(0));
            mIdentifiers.Add(name.ToString());
            CatAstNode tmp = Create(node.GetChild(1));

            // lambda nodes either contain quotes or other lambda nodes
            if (!(tmp is AstQuoteNode))
            {
                if (!(tmp is AstLambdaNode))
                    throw new Exception("expected lambda expression or quotation");
                AstLambdaNode lambda = tmp as AstLambdaNode;
                mIdentifiers.AddRange(lambda.mIdentifiers);

                // Take ownership of the terms from the child lambda expression
                mTerms = lambda.mTerms;
            }
            else
            {
                AstQuoteNode q = tmp as AstQuoteNode;

                // Take ownership of the terms from the quote
                mTerms = q.mTerms;
            }
        }
    }

    public class AstQuoteNode : AstExprNode
    {
        public List<AstExprNode> mTerms = new List<AstExprNode>();

        public AstQuoteNode(PegAstNode node) : base(node)
        {
            CheckLabel("quote");
            foreach (PegAstNode child in node.GetChildren())
            {
                CatAstNode tmp = CatAstNode.Create(child);
                if (!(tmp is AstExprNode))
                    throw new Exception("invalid child node " + child.ToString() + ", expected an expression node");
                mTerms.Add(tmp as AstExprNode);
            }
        }

        public AstQuoteNode(AstExprNode expr)
            : base("quote", "")
        {
            mTerms.Add(expr);
        }

        public AstQuoteNode(List<AstExprNode> expr)
            : base("quote", "")
        {
            mTerms.AddRange(expr);
        }

        public override string ToString()
        {
            string result = "[ ";
            foreach (AstExprNode x in mTerms)
                result += x.ToString() + " ";
            result += "]";
            return result;
        }

        public override void Output(TextWriter writer, int nIndent)
        {
            if (HasComment())
                writer.WriteLine(IndentedString(nIndent, " // " + GetComment()));
            writer.WriteLine(IndentedString(nIndent, "["));
            foreach (AstExprNode x in mTerms)
                x.Output(writer, nIndent + 1);
            writer.WriteLine(IndentedString(nIndent, "]"));
        }
    }

    public class AstIntNode : AstExprNode
    {
        public AstIntNode(PegAstNode node) : base(node)
        {
            CheckLabel("int");
            CheckIsLeaf(node);
        }

        public AstIntNode(int n)
            : base("int", n.ToString())
        { }

        public int GetValue()
        {
            return int.Parse(ToString());
        }
    }

    public class AstBinNode : AstExprNode
    {
        public AstBinNode(PegAstNode node)
            : base(node)
        {
            CheckLabel("bin");
            CheckIsLeaf(node);
        }

        public int GetValue()
        {
            string s = ToString();
            int n = 0;
            int place = 1;
            for (int i = s.Length; i > 0; --i)
            {
                if (s[i - 1] == '1')
                {
                    n += place;
                }
                else
                {
                    if (s[i - 1] != '0')
                        throw new Exception("Invalid binary number");
                }
                place *= 2;
            }
            return n;
        }
    }

    public class AstHexNode : AstExprNode
    {
        public AstHexNode(PegAstNode node)
            : base(node)
        {
            CheckLabel("hex");
            CheckIsLeaf(node);
        }

        public int GetValue()
        {
            return int.Parse(ToString(), NumberStyles.AllowHexSpecifier);
        }
    }

    public class AstCharNode : AstExprNode
    {
        public AstCharNode(PegAstNode node) : base(node)
        {
            CheckLabel("char");
            CheckIsLeaf(node);
        }

        public char GetValue()
        {
            string s = ToString();
            // strip quotes
            return char.Parse(s.Substring(1, s.Length - 2));
        }
    }

    public class AstStringNode : AstExprNode
    {
        public AstStringNode(PegAstNode node) : base(node)
        {
            CheckLabel("string");
            CheckIsLeaf(node);
        }

        public string GetValue()
        {            
            string s = ToString();
            // strip quotes
            return s.Substring(1, s.Length - 2);
        }
    }

    public class AstFloatNode : AstExprNode
    {
        public AstFloatNode(PegAstNode node) : base(node)
        {
            CheckLabel("float");
            CheckIsLeaf(node);
        }

        public double GetValue()
        {
            return double.Parse(ToString());
        }
    }

    public class AstTypeNode : CatAstNode
    {
        public AstTypeNode(PegAstNode node)
            : base(node)
        {
        }
    }

    public class AstStackNode : CatAstNode
    {
        public List<AstTypeNode> mTypes = new List<AstTypeNode>();

        public AstStackNode(PegAstNode node)
            : base(node)
        {
            CheckLabel("stack");
            foreach (PegAstNode child in node.GetChildren())
            {
                CatAstNode tmp = Create(child);
                if (!(tmp is AstTypeNode))
                    throw new Exception("stack AST node should only have type AST nodes as children");
                mTypes.Add(tmp as AstTypeNode);
            }
        }

        public override string ToString()
        {
            string result = "";
            foreach (AstTypeNode x in mTypes)
                result += x.ToString() + " ";
            return result;
        }
    }

    public class AstTypeVarNode : AstTypeNode
    {
        public AstTypeVarNode(PegAstNode node)
            : base(node)
        {
            CheckLabel("type_var");
            CheckIsLeaf(node);
        }
    }

    public class AstSimpleTypeNode : AstTypeNode
    {
        public AstSimpleTypeNode(PegAstNode node)
            : base(node)
        {
            CheckLabel("type_name");
            CheckIsLeaf(node);
        }
    }

    public class AstStackVarNode : AstTypeNode
    {
        public AstStackVarNode(PegAstNode node)
            : base(node)
        {
            CheckLabel("stack_var");
            CheckIsLeaf(node);
        }
    }

    public class AstFxnTypeNode : AstTypeNode
    {
        public AstStackNode mProd;
        public AstStackNode mCons;
        bool mbSideEffects;

        public AstFxnTypeNode(PegAstNode node)
            : base(node)
        {
            CheckLabel("type_fxn");
            CheckChildCount(node, 3);
            mCons = new AstStackNode(node.GetChild(0));
            mbSideEffects = node.GetChild(1).ToString().Equals("~>");
            mProd = new AstStackNode(node.GetChild(2));
        }

        public override string ToString()
        {
            if (mbSideEffects)
            {
                return "( " + mCons.ToString() + "~> " + mProd.ToString() + ")";
            }
            else
            {
                return "( " + mCons.ToString() + "-> " + mProd.ToString() + ")";
            }
        }

        public bool HasSideEffects()
        {
            return mbSideEffects;
        }

    }

    public class AstMacroNode : CatAstNode
    {
        public AstMacroPattern mSrc;
        public AstMacroPattern mDest;

        public AstMacroNode(PegAstNode node)
            : base(node)
        {
            CheckChildCount(node, 2);
            CheckLabel("macro");
            mSrc = new AstMacroPattern(node.GetChild(0));
            mDest = new AstMacroPattern(node.GetChild(1));
        }

        public override string ToString()
        {
            return "{" + mSrc.ToString() + "} => {" + mDest.ToString() + "}";
        }
    }

    public class AstMacroPattern : CatAstNode
    {
        public List<AstMacroTerm> mPattern = new List<AstMacroTerm>();

        public AstMacroPattern(PegAstNode node)
            : base(node)
        {
            CheckLabel("macro_pattern");
            foreach (PegAstNode child in node.GetChildren())
            {
                AstMacroTerm tmp = CatAstNode.Create(child) as AstMacroTerm;
                if (tmp == null)
                    throw new Exception("invalid grammar: only macro terms can be children of an ast macro mPattern");
                mPattern.Add(tmp);
            }
        }

        public override string ToString()
        {
            string ret = "";
            foreach (AstMacroTerm t in mPattern)
                ret += " " + t.ToString();
            ret = ret.Substring(1);
            return ret;
        }
    }

    public class AstMacroTerm : CatAstNode
    {
        public AstMacroTerm(PegAstNode node)
            : base(node)
        {
        }
    }

    public class AstMacroQuote : AstMacroTerm
    {
        public List<AstMacroTerm> mTerms = new List<AstMacroTerm>();

        public AstMacroQuote(PegAstNode node)
            : base(node)
        {
            foreach (PegAstNode child in node.GetChildren())
            {
                AstMacroTerm term = Create(child) as AstMacroTerm;
                if (term == null)
                    throw new Exception("internal grammar error: macro quotations can only contain macro terms");
                mTerms.Add(term);
            }
        }

        public override string ToString()
        {
            string ret = "";
            foreach (AstMacroTerm t in mTerms)
                ret += " " + t.ToString();
            ret = ret.Substring(1);
            return "[" + ret + "]";
        }
    }

    public class AstMacroTypeVar : AstMacroTerm
    {
        public AstMacroTypeVar(PegAstNode node)
            : base(node)
        {
            CheckChildCount(node, 0);
            CheckLabel("macro_type_var");
        }
    }

    public class AstMacroStackVar : AstMacroTerm
    {
        public AstMacroStackVar(PegAstNode node)
            : base(node)
        {
            CheckChildCount(node, 0);
            CheckLabel("macro_stack_var");
        }
    }

    public class AstMacroName : AstMacroTerm
    {
        public AstMacroName(PegAstNode node)
            : base(node)
        {
            CheckChildCount(node, 0);
            CheckLabel("macro_name");
        }
    }
    
    #region AST nodes for representing meta data
    public class AstMetaDataNode : CatAstNode
    {
        public List<AstMetaDataNode> children = new List<AstMetaDataNode>();

        public AstMetaDataNode(PegAstNode node)
            : base(node)
        {
            foreach (PegAstNode child in node.GetChildren())
            {
                AstMetaDataNode x = Create(child) as AstMetaDataNode;
                if (x == null)
                    throw new Exception("Meta data-nodes can only have meta-data nodes as children");
                children.Add(x);
            }
        }
    }

    public class AstMetaDataContent : AstMetaDataNode
    {
        public AstMetaDataContent(PegAstNode node)
            : base(node)
        {
            CheckLabel("meta_data_content");
        }
    }

    public class AstMetaDataLabel : AstMetaDataNode
    {
        public AstMetaDataLabel(PegAstNode node)
            : base(node)
        {
            CheckLabel("meta_data_label");
            CheckIsLeaf(node);
            Trace.Assert(children.Count == 0);
        }
    }

    public class AstMetaDataBlock : AstMetaDataNode
    {
        public AstMetaDataBlock(PegAstNode node)
            : base(node)
        {
            CheckLabel("meta_data_block");
        }
    }
    #endregion
}
