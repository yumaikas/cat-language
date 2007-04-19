/// Public domain code by Christopher Diggins
/// http://www.cat-language.com

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Peg
{
    /// <summary>
    /// Abstract Syntax Tree node.  
    /// </summary>
    public class AstNode
    {
        int mnBegin;
        int mnCount;
        string msLabel;
        String msText;
        AstNode mpParent;
        List<AstNode> mChildren = new List<AstNode>();

        public AstNode(string label, int n, String text, AstNode p)
        {
            msLabel = label;
            msText = text;
            mnBegin = n;
            mnCount = -1;
            mpParent = p;
        }

        public AstNode Add(string sLabel, Parser p)
        {
            AstNode ret = new AstNode(sLabel, p.GetPos(), msText, this);
            mChildren.Add(ret);
            return ret;
        }

        public void Complete(Parser p)
        {
            mnCount = p.GetPos() - mnBegin;
        }

        public AstNode GetParent()
        {
            return mpParent;
        }

        public void Remove(AstNode x)
        {
            mChildren.Remove(x);
        }

        public string GetXmlDoc()
        {
            string s = "<?xml version='1.0'?>";
            s += GetXmlText();
            return s;
        }

        public override string ToString()
        {
            return msText.Substring(mnBegin, mnCount);
        }

        public string GetXmlText()
        {
            string s = "<" + msLabel + ">\n";

            if (GetNumChildren() == 0)
            {
                s += ToString();
            }
            else
            {
                foreach (AstNode node in mChildren)
                {
                    s += node.GetXmlText();
                }
            }
            s += "</" + msLabel + ">\n";
            return s;
        }

        public string GetLabel()
        {
            return msLabel;
        }

        public List<AstNode> GetChildren()
        {
            return mChildren;
        }

        public int GetNumChildren()
        {
            return mChildren.Count;
        }

        public AstNode GetChild(int n)
        {
            return mChildren[n];
        }
    }
}
