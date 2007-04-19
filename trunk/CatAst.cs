/// Public domain code by Christopher Diggins
/// http://www.cat-language.com

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
    /// A CatAstNode is used as a base class for a typed abstract syntax tree for Cat programs.
    /// CatAstNodes are created from a Peg.AstNode. Apart from being typed, the big difference
    /// is the a CatAstNode can be modified. This makes rewriting algorithms much easier. 
    /// </summary>
    public abstract class CatAstNode
    {
        string msText;
        string msLabel;
        string msComment;

        public CatAstNode(AstNode node)
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

        public static CatAstNode Create(AstNode node)
        {
            switch (node.GetLabel())
            {
                case "program":
                    return new AstProgram(node);
                case "def":
                    return new AstDef(node);
                case "name":
                    return new AstName(node);
                case "param":
                    return new AstParam(node);
                case "quote":
                    return new AstQuote(node);
                case "char":
                    return new AstChar(node);
                case "string":
                    return new AstString(node);
                case "float":
                    return new AstFloat(node);
                case "int":
                    return new AstInt(node);
                case "hex":
                    return new AstHex(node);
                case "stack":
                    return new AstStack(node);
                case "type_fxn":
                    return new AstFxnType(node);
                case "type_var":
                    return new AstType(node);
                case "type_name":
                    return new AstSimpleType(node);
                case "stack_var":
                    return new AstStackVar(node);
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

        public void CheckIsLeaf(AstNode node)
        {
            CheckChildCount(node, 0);
        }

        public void CheckChildCount(AstNode node, int n)
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
        public List<AstDef> Defs = new List<AstDef>();

        public AstProgram(AstNode node) : base(node)
        {
            CheckLabel("ast");
            foreach (AstNode child in node.GetChildren())
                Defs.Add(new AstDef(child));
        }

        public override void Output(TextWriter writer, int nIndent)
        {
            foreach (AstDef d in Defs)
                d.Output(writer, nIndent);
        }
    }

    public class AstExpr : CatAstNode
    {
        public AstExpr(AstNode node) : base(node) { }
        public AstExpr(string sLabel, string sText) : base(sLabel, sText) { }

        public override void Output(TextWriter writer, int nIndent)
        {
            string sLine = ToString();
            if (HasComment())
                sLine += " // " + GetComment();
            writer.WriteLine(IndentedString(nIndent, sLine));
        }
    }

    public class AstDef : CatAstNode
    {
        public string mName;
        public AstFxnType mType;
        public List<AstParam> mParams = new List<AstParam>();
        public List<AstExpr> mTerms = new List<AstExpr>();

        public AstDef(AstNode node) : base(node)
        {
            CheckLabel("def");

            if (node.GetNumChildren() == 0)
                throw new Exception("invalid function definition node");

            AstName name = new AstName(node.GetChild(0));
            mName = name.ToString();

            int n = 1;

            // Look to see if a type is defined
            if ((node.GetNumChildren() >= 2) && (node.GetChild(1).GetLabel() == "type_fxn"))
            {
                mType = new AstFxnType(node.GetChild(1));
                ++n;
            }

            while (n < node.GetNumChildren())
            {
                AstNode child = node.GetChild(n);
                
                if (child.GetLabel() != "param")
                    break;

                mParams.Add(new AstParam(child));
                n++;
            }

            while (n < node.GetNumChildren())
            {
                AstNode child = node.GetChild(n);
                CatAstNode expr = Create(child);

                if (!(expr is AstExpr))
                    throw new Exception("expected expression node");
                
                mTerms.Add(expr as AstExpr);
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
                foreach (AstParam p in mParams)
                    s += p.ToString() + " ";
                s += ")";
            }
            writer.WriteLine(IndentedString(nIndent, s));            
            writer.WriteLine(IndentedString(nIndent, "{"));
            foreach (AstExpr x in mTerms)
                x.Output(writer, nIndent + 1);
            writer.WriteLine(IndentedString(nIndent, "}"));
        }
    }

    public class AstName : AstExpr
    {
        public AstName(AstNode node) : base(node)
        {
            CheckLabel("name");
            CheckIsLeaf(node);
        }        
        
        public AstName(string sOp, string sComment)
            : base("name", sOp)
        {
            SetComment(sComment);
        }
    }

    public class AstParam : CatAstNode
    {
        public AstParam(AstNode node) : base(node)
        {
            CheckLabel("param");
            CheckIsLeaf(node);
        }

        public override void Output(TextWriter writer, int nIndent)
        {
            throw new Exception("The method or operation is not implemented.");
        }
    }

    public class AstQuote : AstExpr
    {
        public List<AstExpr> Terms = new List<AstExpr>();

        public AstQuote(AstNode node) : base(node)
        {
            CheckLabel("quote");
            foreach (AstNode child in node.GetChildren())
            {
                CatAstNode tmp = CatAstNode.Create(child);
                if (!(tmp is AstExpr))
                    throw new Exception("invalid child node " + child.ToString() + ", expected an expression node");
                Terms.Add(tmp as AstExpr);
            }
        }

        public AstQuote(AstExpr expr)
            : base("quote", "")
        {
            Terms.Add(expr);
        }

        public override string ToString()
        {
            string result = "[ ";
            foreach (AstExpr x in Terms)
                result += x.ToString() + " ";
            result += "]";
            return result;
        }

        public override void Output(TextWriter writer, int nIndent)
        {
            if (HasComment())
                writer.WriteLine(IndentedString(nIndent, " // " + GetComment()));
            writer.WriteLine(IndentedString(nIndent, "["));
            foreach (AstExpr x in Terms)
                x.Output(writer, nIndent + 1);
            writer.WriteLine(IndentedString(nIndent, "]"));
        }
    }

    public class AstInt : AstExpr
    {
        public AstInt(AstNode node) : base(node)
        {
            CheckLabel("int");
            CheckIsLeaf(node);
        }

        public AstInt(int n)
            : base("int", n.ToString())
        { }

        public int GetValue()
        {
            return int.Parse(ToString());
        }
    }

    public class AstChar : AstExpr
    {
        public AstChar(AstNode node) : base(node)
        {
            CheckLabel("char");
            CheckIsLeaf(node);
        }

        public char GetValue()
        {
            return char.Parse(ToString());
        }
    }

    public class AstString : AstExpr
    {
        public AstString(AstNode node) : base(node)
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

    public class AstFloat : AstExpr
    {
        public AstFloat(AstNode node) : base(node)
        {
            CheckLabel("float");
            CheckIsLeaf(node);
        }

        public double GetValue()
        {
            return double.Parse(ToString());
        }
    }

    public class AstHex : AstExpr
    {
        public AstHex(AstNode node) : base(node)
        {
            CheckLabel("hex");
            CheckIsLeaf(node);
        }

        public int GetValue()
        {
            return int.Parse(ToString(), NumberStyles.HexNumber);
        }
    }

    public class AstType : CatAstNode
    {
        public AstType(AstNode node)
            : base(node)
        {
        }
    }

    public class AstStack : CatAstNode
    {
        public List<AstType> mTypes = new List<AstType>();

        public AstStack(AstNode node)
            : base(node)
        {
            CheckLabel("stack");
            foreach (AstNode child in node.GetChildren())
            {
                CatAstNode tmp = Create(child);
                if (!(tmp is AstType))
                    throw new Exception("stack AST node should only have type AST nodes as children");
                mTypes.Add(tmp as AstType);
            }
        }

        public override string ToString()
        {
            string result = "";
            foreach (AstType x in mTypes)
                result += x.ToString() + " ";
            return result;
        }
    }

    public class AstTypeVar : AstType
    {
        public AstTypeVar(AstNode node)
            : base(node)
        {
            CheckLabel("type_var");
            CheckIsLeaf(node);
        }
    }

    public class AstSimpleType : AstType
    {
        public AstSimpleType(AstNode node)
            : base(node)
        {
            CheckLabel("type_name");
            CheckIsLeaf(node);
        }
    }

    public class AstStackVar : AstType
    {
        public AstStackVar(AstNode node)
            : base(node)
        {
            CheckLabel("stack_var");
            CheckIsLeaf(node);
        }
    }

    public class AstFxnType : AstType
    {
        public AstStack mProd;
        public AstStack mCons;

        public AstFxnType(AstNode node)
            : base(node)
        {
            CheckChildCount(node, 2);
            mCons = new AstStack(node.GetChild(0));
            mProd = new AstStack(node.GetChild(1));
        }

        public override string ToString()
        {
            return "( " + mCons.ToString() + "-> " + mProd.ToString() + ")";
        }

    }


}