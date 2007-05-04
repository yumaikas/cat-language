/// Public domain code by Christopher Diggins
/// http://www.cat-language.com

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Peg
{
    public class Parser
    {
        int mIndex;
        string mData;
        Ast mTree;
        Ast mCur;

        public Parser(string s)
        {
            mIndex = 0;
            mData = s;
            mTree = new Ast("ast", 0, mData, null);
            mCur = mTree;
        }

        public bool AtEnd()
        {
            return mIndex >= mData.Length;
        }

        public int GetPos()
        {
            return mIndex; 
        }

        public string CurrentLine
        {
            get
            {
                return mData.Substring(mIndex, 20);
            }
        }

        public void SetPos(int pos)
        {
            mIndex = pos;
        }   

        public void GotoNext()
        {
            if (AtEnd())
            {
                throw new Exception("passed the end of input");
            }
            mIndex++;
        }

        public char GetChar()
        {
            if (AtEnd()) 
            { 
                throw new Exception("passed end of input"); 
            }
            return mData[mIndex];
        }

        public Ast CreateNode(string sLabel)
        {
            Trace.Assert(mCur != null);
            mCur = mCur.Add(sLabel, this);
            Trace.Assert(mCur != null);
            return mCur;
        }

        public void AbandonNode()
        {
            Trace.Assert(mCur != null);
            Ast tmp = mCur;
            mCur = mCur.GetParent();
            Trace.Assert(mCur != null);
            mCur.Remove(tmp);
        }

        public void CompleteNode()
        {
            Trace.Assert(mCur != null);
            mCur.Complete(this);
            mCur = mCur.GetParent();
            Trace.Assert(mCur != null);
        }

        public Ast GetAst()
        {
            return mTree;
        }

        public bool Parse(Peg.Grammar.Rule g)
        {
            bool b = g.Match(this);

            if (b)
            {
                if (mCur != mTree)
                    throw new Exception("internal error: parse tree and parse node do not match after parsing");
                mCur.Complete(this);
            }

            return b;
        }
    }
}
