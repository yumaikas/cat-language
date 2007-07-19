using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Cat
{
    public class CatMetaData : List<CatMetaData>
    {
        public string msName = "";
        public string msContent = "";
        public CatMetaData mpParent;
        public CatMetaData(string sName, CatMetaData pParent)
        {
            msName = sName;
            mpParent = pParent;
        }
        public void AddContent(string s)
        {
            msContent = msContent.Trim() + " " + s.Trim();
        }
        public CatMetaData NewChild(string sName)
        {
            CatMetaData ret = new CatMetaData(sName, this);
            Add(ret);
            return ret;
        }
        public CatMetaData GetParent()
        {
            return mpParent;
        }
        public CatMetaData Find(string s)
        {
            foreach (CatMetaData child in this)
                if (child.msName.Equals(s))
                    return child;
            return null;
        }
        public List<CatMetaData> FindAll(string s)
        {
            List<CatMetaData> ret = new List<CatMetaData>();
            foreach (CatMetaData child in this)
                if (child.msName.Equals(s))
                    ret.Add(child);
            return ret;
        }

        public string GetContent()
        {
            return msContent;
        }
    }

    public class CatMetaDataBlock : CatMetaData 
    {
        private void SplitLabel(string sIn, out int nIndent, out string sName)
        {
            nIndent = 0;
            for (int i = 0; i < sIn.Length; ++i)
            {
                if (sIn[i] == ' ' || sIn[i] == '\t')
                    nIndent++;
            }
            sName = sIn.Substring(nIndent);
            // check validity
            if ((sName.Length < 2) || (sName[sName.Length - 1] != ':'))
                throw new Exception("invalid meta-data label: " + sName);
            // strip the trailing ':'
            sName = sName.Substring(0, sName.Length - 1);
        }

        public CatMetaDataBlock(AstMetaDataBlock node)
            : base("root", null)
        {
            CatMetaData cur = this;
            int nCurIndent = -1;
            
            for (int i=0; i < node.children.Count; ++i)
            {
                AstMetaDataNode tmp = node.children[i];
                if (tmp is AstMetaDataLabel)
                {
                    int nIndent;
                    string sName; 
                    SplitLabel(tmp.ToString(), out nIndent, out sName);
                    
                    if (nIndent > nCurIndent)
                    {
                        cur = cur.NewChild(sName);
                    }
                    else if (nIndent == nCurIndent)
                    {
                        cur = cur.GetParent();
                        Trace.Assert(cur != null);
                        cur = cur.NewChild(sName);
                    }
                    else
                    {
                        cur = cur.GetParent();
                        Trace.Assert(cur != null);
                        cur = cur.GetParent();
                        Trace.Assert(cur != null);
                        cur = cur.NewChild(sName);
                    }
                    nCurIndent = nIndent;
                }
                else if (tmp is AstMetaDataContent)
                {
                    cur.AddContent(tmp.ToString());
                }
                else
                {
                    throw new Exception("invalid AstMetaDataBlock");
                }
            }
        }
    }
}
