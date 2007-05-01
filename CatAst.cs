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
    /// A AstNode is used as a base class for a typed abstract syntax tree for Cat programs.
    /// CatAstNodes are created from a Peg.Ast. Apart from being typed, the big difference
    /// is the a AstNode can be modified. This makes rewriting algorithms much easier. 
    /// </summary>
    public abstract class AstNode
    {
        string msText;
        string msLabel;
        string msComment;

        public AstNode(Ast node)
        {
            if (node.GetNumChildren() == 0)
                msText = node.ToString();
            else
                msText = "";
            msLabel = node.GetLabel();
        }

        public AstNode(string sLabel, string sText)
        {
            msLabel = sLabel;
            msText = sText;
        }

        public void SetComment(string s)
        {
            msComment = s;
        }

        public static AstNode Create(Ast node)
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

        public void CheckIsLeaf(Ast node)
        {
            CheckChildCount(node, 0);
        }

        public void CheckChildCount(Ast node, int n)
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

    public class AstProgram : AstNode
    {
        public List<AstDefNode> Defs = new List<AstDefNode>();

        public AstProgram(Ast node) : base(node)
        {
            CheckLabel("ast");
            foreach (Ast child in node.GetChildren())
                Defs.Add(new AstDefNode(child));
        }

        public override void Output(TextWriter writer, int nIndent)
        {
            foreach (AstDefNode d in Defs)
                d.Output(writer, nIndent);
        }
    }

    public class AstExprNode : AstNode
    {
        public AstExprNode(Ast node) : base(node) { }
        public AstExprNode(string sLabel, string sText) : base(sLabel, sText) { }

        public override void Output(TextWriter writer, int nIndent)
        {
            string sLine = ToString();
            if (HasComment())
                sLine += " // " + GetComment();
            writer.WriteLine(IndentedString(nIndent, sLine));
        }
    }

    public class AstDefNode : AstNode
    {
        public string mName;
        public AstFxnTypeNode mType;
        public List<AstParamNode> mParams = new List<AstParamNode>();
        public List<AstExprNode> mTerms = new List<AstExprNode>();

        public AstDefNode(Ast node) : base(node)
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
                Ast child = node.GetChild(n);
                
                if (child.GetLabel() != "param")
                    break;

                mParams.Add(new AstParamNode(child));
                n++;
            }

            while (n < node.GetNumChildren())
            {
                Ast child = node.GetChild(n);
                AstNode expr = Create(child);

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
        public AstNameNode(Ast node) : base(node)
        {
            CheckLabel("name");
            CheckIsLeaf(node);
        }        
        
        public AstNameNode(string sOp, string sComment)
            : base("name", sOp)
        {
            SetComment(sComment);
        }
    }

    public class AstParamNode : AstNode
    {
        public AstParamNode(Ast node) : base(node)
        {
            CheckLabel("param");
            CheckIsLeaf(node);
        }

        public override void Output(TextWriter writer, int nIndent)
        {
            throw new Exception("The method or operation is not implemented.");
        }
    }

    public class AstQuoteNode : AstExprNode
    {
        public List<AstExprNode> Terms = new List<AstExprNode>();

        public AstQuoteNode(Ast node) : base(node)
        {
            CheckLabel("quote");
            foreach (Ast child in node.GetChildren())
            {
                AstNode tmp = AstNode.Create(child);
                if (!(tmp is AstExprNode))
                    throw new Exception("invalid child node " + child.ToString() + ", expected an expression node");
                Terms.Add(tmp as AstExprNode);
            }
        }

        public AstQuoteNode(AstExprNode expr)
            : base("quote", "")
        {
            Terms.Add(expr);
        }

        public override string ToString()
        {
            string result = "[ ";
            foreach (AstExprNode x in Terms)
                result += x.ToString() + " ";
            result += "]";
            return result;
        }

        public override void Output(TextWriter writer, int nIndent)
        {
            if (HasComment())
                writer.WriteLine(IndentedString(nIndent, " // " + GetComment()));
            writer.WriteLine(IndentedString(nIndent, "["));
            foreach (AstExprNode x in Terms)
                x.Output(writer, nIndent + 1);
            writer.WriteLine(IndentedString(nIndent, "]"));
        }
    }

    public class AstIntNode : AstExprNode
    {
        public AstIntNode(Ast node) : base(node)
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
        public AstBinNode(Ast node)
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
        public AstHexNode(Ast node)
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
        public AstCharNode(Ast node) : base(node)
        {
            CheckLabel("char");
            CheckIsLeaf(node);
        }

        public char GetValue()
        {
            return char.Parse(ToString());
        }
    }

    public class AstStringNode : AstExprNode
    {
        public AstStringNode(Ast node) : base(node)
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
        public AstFloatNode(Ast node) : base(node)
        {
            CheckLabel("float");
            CheckIsLeaf(node);
        }

        public double GetValue()
        {
            return double.Parse(ToString());
        }
    }

    public class AstTypeNode : AstNode
    {
        public AstTypeNode(Ast node)
            : base(node)
        {
        }
    }

    public class AstStackNode : AstNode
    {
        public List<AstTypeNode> mTypes = new List<AstTypeNode>();

        public AstStackNode(Ast node)
            : base(node)
        {
            CheckLabel("stack");
            foreach (Ast child in node.GetChildren())
            {
                AstNode tmp = Create(child);
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
        public AstTypeVarNode(Ast node)
            : base(node)
        {
            CheckLabel("type_var");
            CheckIsLeaf(node);
        }
    }

    public class AstSimpleTypeNode : AstTypeNode
    {
        public AstSimpleTypeNode(Ast node)
            : base(node)
        {
            CheckLabel("type_name");
            CheckIsLeaf(node);
        }
    }

    public class AstStackVarNode : AstTypeNode
    {
        public AstStackVarNode(Ast node)
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

        public AstFxnTypeNode(Ast node)
            : base(node)
        {
            CheckChildCount(node, 2);
            mCons = new AstStackNode(node.GetChild(0));
            mProd = new AstStackNode(node.GetChild(1));
        }

        public override string ToString()
        {
            return "( " + mCons.ToString() + "-> " + mProd.ToString() + ")";
        }

    }


}