/// Released into the public domain by 
/// Christopher Diggins

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace Cat
{
    public class CatDocMaker
    {
        FxnDocList mTable = new FxnDocList();

        class FxnDocList : List<FxnDoc>
        {
            public Dictionary<string, List<FxnDoc>> mCats = new Dictionary<string, List<FxnDoc>>();
            public Dictionary<string, FxnDoc> mFxns = new Dictionary<string, FxnDoc>();

            public void Initialize()
            {
                foreach (FxnDoc fxn in this)
                {
                    string sCat = fxn.msCategory;
                    if (!mCats.ContainsKey(sCat))
                        mCats.Add(sCat, new List<FxnDoc>());
                    mCats[sCat].Add(fxn);
                    mFxns.Add(fxn.msName, fxn);
                }
            }

            public void OutputHtml(StreamWriter sw)
            {
                foreach (KeyValuePair<string, List<FxnDoc>> cat in mCats)
                {
                    sw.WriteLine("<h3>" + cat.Key + "</h3>");

                    foreach (FxnDoc fxn in cat.Value)
                    {
                        sw.WriteLine(fxn.ToHtml(this));
                    }
                }
            }

            public void OutputTocHtml(StreamWriter sw)
            {
                foreach (KeyValuePair<string, List<FxnDoc>> kvp in mCats)
                {
                    sw.WriteLine("<h3>" + kvp.Key + "</h3>");

                    foreach (FxnDoc f in kvp.Value)
                    {
                        sw.WriteLine("<a href='#" + f.GetIdString() + "'><span class='primitive-toc-link'>" + f.msName + "</span></a>, ");
                    }
                }
            }

            public string HyperLinkWord(string s)
            {
                if (!mFxns.ContainsKey(s))
                    return s;
                else
                    return mFxns[s].GetHyperLink();
            }
        }

        class FxnDoc
        {
            public int mnLevel;
            public string msName;
            public string msType;
            public string msSemantics;
            public string msImpl;
            public string msCategory;
            public string msNotes;

            public FxnDoc(string[] cells)
            {
                mnLevel = Int32.Parse(cells[0]);
                msName = cells[1].Trim();
                msType = cells[2].Trim();
                msSemantics = cells[3].Trim();
                msImpl = cells[4].Trim();
                msCategory = cells[5].Trim();
                msNotes = cells[6].Trim();

                if (msNotes.Length > 2)
                {
                    if (msNotes[0] == '"')
                    {
                        msNotes = msNotes.Substring(1, msNotes.Length - 2);
                    }
                }
            }

            public string GetIdString()
            {
                return "primitive-" + msName;
            }

            public string HyperLinkCode(string s, FxnDocList fxns)
            {
                Regex r = new Regex("\\b");
                string[] a = r.Split(s);                
                string ret = "";
                foreach (string tmp in a)
                    ret += fxns.HyperLinkWord(tmp);
                return ret;
            }

            public string GetHyperLink()
            {
                return "<a class='prim-link' href='#" + GetIdString() + "'>" + msName + "</a>";
            }

            public string ToHtml(FxnDocList fxns)
            {
                string ret = "<a name='" + GetIdString() + "' href='#" + GetIdString() + "'><span class='prim_word_head'>" + msName + "</span></a>\n";
                ret += "<table class='prim_def_table'>\n";
                ret += "<tr valign='top'><td><span class='prim_label'>Type</span></td><td><tt><span class='prim_type'>" + msType + "</span></tt></td></tr>\n";
                ret += "<tr valign='top'><td><span class='prim_label'>Semantics</span></td><td><tt><span class='prim_sem'>" + msSemantics + "</span></tt></td></tr>\n";
                
                if (msImpl.Length > 1)
                    ret += "<tr valign='top'><td><span class='prim_label'>Implementation</span></td><td><tt><span class='prim_imp'>" + HyperLinkCode(msImpl, fxns) + "</span></tt></td></tr>\n";

                if (msNotes.Length > 1)
                    ret += "<tr valign='top'><td><span class='prim_label'>Remarks</span></td><td><span class='value'>" + msNotes + "</span></td></tr>\n";
                
                ret += "</table>\n";
                return ret;
            }
        }


        public CatDocMaker(string sInFile, string sOutFile)
        {
            // Load a tab delimited file.
            // split at the tab character 
            FileStream fIn = File.OpenRead(sInFile);
            try
            {
                StreamReader sr = new StreamReader(fIn);
                string sLine;
                while ((sLine = sr.ReadLine()) != null)
                {
                    string[] sCols = sLine.Split(new Char[] { '\t' });
                    mTable.Add(new FxnDoc(sCols));
                }
                mTable.Initialize();
            }
            finally
            {
                fIn.Close();
            }

            FileStream fOut = File.OpenWrite(sOutFile);
            try
            {
                StreamWriter sw = new StreamWriter(fOut);
                sw.WriteLine("<html><body>");
                mTable.OutputTocHtml(sw);
                mTable.OutputHtml(sw);
                sw.WriteLine("</body></html>");
                sw.Flush();
            }
            finally 
            {
                fOut.Close();
            }
            
        }
    }
}
