/// Public domain code by Christopher Diggins
/// http://www.cat-language.com

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;
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
        bool mbLeaf;

        public CatAstNode(AstNode node)
        {
            mbLeaf = node.IsLeaf();
            if (mbLeaf)
                msText = node.ToString();
            else
                msText = "";
            msLabel = node.GetLabel();
        }

        public CatAstNode(string sLabel, string sText, bool bLeaf)
        {
            mbLeaf = bLeaf;
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

        public void CheckIsLeaf()
        {
            if (!mbLeaf)
            {
                throw new Exception("Expected leaf node");
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

        public bool IsLeaf()
        {
            return mbLeaf;
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

        public abstract void Output(TextWriter writer, int nIndent);
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
        public AstExpr(string sLabel, string sText, bool bLeaf) : base(sLabel, sText, bLeaf) { }

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
        public string Name;
        public List<AstParam> Params = new List<AstParam>();
        public List<AstExpr> Terms = new List<AstExpr>();

        public AstDef(AstNode node) : base(node)
        {
            CheckLabel("def");

            if (node.GetNumChildren() == 0)
                throw new Exception("invalid function definition node");

            AstName name = new AstName(node.GetChild(0));
            Name = name.ToString();

            int n = 1;
            while (n < node.GetNumChildren())
            {
                AstNode child = node.GetChild(n);
                
                if (child.GetLabel() != "param")
                    break;

                Params.Add(new AstParam(child));
                n++;
            }

            while (n < node.GetNumChildren())
            {
                AstNode child = node.GetChild(n);
                CatAstNode expr = Create(child);

                if (!(expr is AstExpr))
                    throw new Exception("expected expression node");
                
                Terms.Add(expr as AstExpr);
                n++;
            }
        }

        public override void Output(TextWriter writer, int nIndent)
        {
            if (HasComment())
                writer.WriteLine(IndentedString(nIndent, " // " + GetComment()));


            string s = "define " + Name + " // ( ";
            foreach (AstParam p in Params)
                s += p.ToString() + " ";
            s += ")";            
            writer.WriteLine(IndentedString(nIndent, s));
            
            writer.WriteLine(IndentedString(nIndent, "{"));
            foreach (AstExpr x in Terms)
            {
                x.Output(writer, nIndent + 1);
            }
            writer.WriteLine(IndentedString(nIndent, "}"));
        }
    }


    public class AstName : AstExpr
    {
        public AstName(AstNode node) : base(node)
        {
            CheckLabel("name");
            CheckIsLeaf();
        }        
        
        public AstName(string sOp, string sComment)
            : base("name", sOp, true)
        {
            SetComment(sComment);
        }
    }

    public class AstParam : CatAstNode
    {
        public AstParam(AstNode node) : base(node)
        {
            CheckLabel("param");
            CheckIsLeaf();
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
            : base("quote", "", false)
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
            CheckIsLeaf();
        }

        public AstInt(int n)
            : base("int", n.ToString(), true)
        {
        }

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
            CheckIsLeaf();
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
            CheckIsLeaf();
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
            CheckIsLeaf();
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
            CheckIsLeaf();
        }

        public string Getvalue()
        {
            return ToString();
        }
    }
}