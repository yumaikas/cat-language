/// Released into the public domain by 
/// Christopher Diggins

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Cat
{
    public class CatDocMaker
    {
        FxnDocList mTable = new FxnDocList();

        class FxnDocList : List<FxnDoc>
        {
            public Dictionary<string, List<FxnDoc>> mCats = new Dictionary<string, List<FxnDoc>>();

            public void Initialize()
            {
                foreach (FxnDoc fxn in this)
                {
                    string sCat = fxn.msCategory;
                    if (!mCats.ContainsKey(sCat))
                        mCats.Add(sCat, new List<FxnDoc>());
                    mCats[sCat].Add(fxn);
                }
            }

            public void OutputHtml()
            {
                foreach (FxnDoc fxn in this)
                    MainClass.WriteLine(fxn.ToHtml(this));
            }

            public void OutputTocHtml()
            {
                foreach (KeyValuePair<string, List<FxnDoc>> kvp in mCats)
                {
                    MainClass.WriteLine("<span class='primitive-category-label'>" + kvp.Key + "</span>");

                    foreach (FxnDoc f in kvp.Value)
                    {
                        MainClass.WriteLine("<a href='#" + f.GetId() + "'><span class='primitive-toc-link'>" + f.msName + "</span></a>, ");
                    }
                }
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
                msName = cells[1];
                msType = cells[2];
                msSemantics = cells[3];
                msImpl = cells[4];
                msCategory = cells[5];
                msNotes = ""; // cells[6]; 
            }

            public string GetId()
            {
                return "primitive-" + msName;
            }

            public string ToHtml(FxnDocList fxns)
            {
                string ret = "<a name='" + GetId() + "' href='#" + GetId() + "'><span class='prim_word_head'>" + msName + "</span></a>\n";
                ret += "<table class='prim_def_table'>\n";
                ret += "<tr valign='top'><td><span class='prim_label'>Type</span></td><td><span class='prim_type'><tt>" + msType + "</span></tt></td></tr>\n";
                ret += "<tr valign='top'><td><span class='prim_label'>Semantics</span></td><td><span class='prim_sem'><tt>" + msSemantics + "</span></tt></td></tr>\n";
                ret += "<tr valign='top'><td><span class='prim_label'>Implementation</span></td><td><span class='prim_imp'><tt>" + msImpl + "</span></tt></td></tr>\n";
                // ret += "<tr valign='top'><td><span class='label'>Notes</span></td><td><span class='value'>" + msNotes + "</span></td></tr>\n";-->
                ret += "</table>\n";
                return ret;
            }
        }


        public CatDocMaker(string sFile)
        {
            // Load a tab delimited file.
            // split at the tab character 
            FileStream f = File.OpenRead(sFile);
            try
            {
                StreamReader sr = new StreamReader(f);
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
                f.Close();
            }
            mTable.OutputTocHtml();
            mTable.OutputHtml();
        }
    }
}
