/// Dedicated to the public domain by Christopher Diggins
/// http://creativecommons.org/licenses/publicdomain/

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace Cat
{
    public class FxnDoc
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
            return "<a class='prim-link' href='#" + msName + "'>" + msName + "</a>";
        }

        public string ToHtml(FxnDocList fxns)
        {
            string ret = "<a name='" + msName + "' href='#" + msName + "'><h4>" + msName + "</h4></a>\n";
            ret += "<table class='prim_def_table'>\n";

            if (msType.Length > 1)
                ret += "<tr valign='top'><td><span class='prim_label'>Type</span></td><td><tt><span class='prim_type'>" + msType + "</span></tt></td></tr>\n";

            if (msSemantics.Length > 1)
                ret += "<tr valign='top'><td><span class='prim_label'>Semantics</span></td><td><tt><span class='prim_sem'>" + msSemantics + "</span></tt></td></tr>\n";

            if (msImpl.Length > 1)
                ret += "<tr valign='top'><td><span class='prim_label'>Implementation</span></td><td><tt><span class='prim_imp'>" + HyperLinkCode(msImpl, fxns) + "</span></tt></td></tr>\n";

            if (msNotes.Length > 1)
                ret += "<tr valign='top'><td><span class='prim_label'>Remarks</span></td><td><span class='value'>" + msNotes + "</span></td></tr>\n";

            ret += "</table>\n";
            return ret;
        }
    }

    public class FxnDocList : List<FxnDoc>
    {
        Dictionary<string, List<FxnDoc>>[] mLevels = new Dictionary<string, List<FxnDoc>>[6];
        public Dictionary<string, FxnDoc> mFxns = new Dictionary<string, FxnDoc>();

        public void Initialize()
        {
            for (int i = 0; i < 6; ++i)
            {
                Dictionary<string, List<FxnDoc>> cats = new Dictionary<string, List<FxnDoc>>();
                mLevels[i] = cats;
            }

            foreach (FxnDoc fxn in this)
            {
                int nLevel = fxn.mnLevel;
                Trace.Assert((nLevel >= 0) && (nLevel <= 5));

                Dictionary<string, List<FxnDoc>> cats = mLevels[nLevel];

                string sCat = fxn.msCategory;
                if (!cats.ContainsKey(sCat))
                    cats.Add(sCat, new List<FxnDoc>());
                cats[sCat].Add(fxn);
                mFxns.Add(fxn.msName, fxn);
            }
        }

        public void OutputHtml(StreamWriter sw)
        {
            for (int i = 0; i < 6; ++i)
            {
                string sLevel = "level" + i.ToString();
                //sw.WriteLine("<a href='#" + sLevel + "' name='" + sLevel + "'><h2>Level " + i.ToString() + " Primitives</h2></a>");
                sw.WriteLine("<h2>Level " + i.ToString() + " Primitives</h2>");
                Dictionary<string, List<FxnDoc>> cats = mLevels[i];

                foreach (KeyValuePair<string, List<FxnDoc>> cat in cats)
                {
                    sw.WriteLine("<h3>" + cat.Key + "</h3>");

                    foreach (FxnDoc fxn in cat.Value)
                    {
                        sw.WriteLine(fxn.ToHtml(this));
                        sw.WriteLine("<a href='#top' class='top-link'>top</a>");
                    }
                }
            }
        }

        public void OutputTocHtml(StreamWriter sw)
        {
            for (int i = 0; i < 6; ++i)
            {
                string sLevel = "level" + i.ToString();
                //sw.WriteLine("<a href='#" + sLevel + "'><h2>Level " + i.ToString() + " Primitives</h2></a>");
                sw.WriteLine("<h2>Level " + i.ToString() + " Primitives</h2>");
                Dictionary<string, List<FxnDoc>> cats = mLevels[i];

                foreach (KeyValuePair<string, List<FxnDoc>> kvp in cats)
                {
                    sw.WriteLine("<h3>" + kvp.Key + "</h3>");

                    for (int j=0; j < kvp.Value.Count; ++j)
                    {
                        FxnDoc f = kvp.Value[j];
                        if (j > 0) sw.Write(", ");
                        sw.WriteLine("<a href='#" + f.msName + "'><span class='primitive-toc-link'><tt>" + f.msName + "</tt></span></a>");
                    }
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

        public FxnDoc GetFxnDoc(string s)
        {            
            FxnDoc ret = null;
            mFxns.TryGetValue(s, out ret);
            return ret;
        }
    }

    public class CatHelpMaker
    {
        FxnDocList mTable = new FxnDocList();

        public FxnDoc GetFxnDoc(string s)
        {
            return mTable.GetFxnDoc(s);
        }

        public CatHelpMaker(string sInFile)
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
        }

        public void SaveHtmlFile(string sOutFile)
        {
            FileStream fOut = File.OpenWrite(sOutFile);
            try
            {
                StreamWriter sw = new StreamWriter(fOut);
                sw.WriteLine("<html><body>");
                sw.WriteLine("<a name='#top'><h1>Cat Primitives</h1></a>");
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

        public static CatHelpMaker CreateHelp(string s)
        {
            try
            {
                return new CatHelpMaker(s);
            }
            catch
            {
                return null;
            }
        }
    }
}
