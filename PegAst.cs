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
    public class PegAstNode
    {
        int mnBegin;
        int mnCount;
        string msLabel;
        String msText;
        PegAstNode mpParent;
        List<PegAstNode> mChildren = new List<PegAstNode>();

        public PegAstNode(string label, int n, String text, PegAstNode p)
        {
            msLabel = label;
            msText = text;
            mnBegin = n;
            mnCount = -1;
            mpParent = p;
        }

        public PegAstNode Add(string sLabel, Parser p)
        {
            PegAstNode ret = new PegAstNode(sLabel, p.GetPos(), msText, this);
            mChildren.Add(ret);
            return ret;
        }

        public void Complete(Parser p)
        {
            mnCount = p.GetPos() - mnBegin;
        }

        public PegAstNode GetParent()
        {
            return mpParent;
        }

        public void Remove(PegAstNode x)
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
                foreach (PegAstNode node in mChildren)
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

        public List<PegAstNode> GetChildren()
        {
            return mChildren;
        }

        public int GetNumChildren()
        {
            return mChildren.Count;
        }

        public PegAstNode GetChild(int n)
        {
            return mChildren[n];
        }
    }
}
