using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Cat
{
    /// <summary>
    /// A CatSession contains all loaded and defined functions, for saving loading and editing. 
    /// </summary>
    public class CatSession
    {
        static CatSession gpSession = new CatSession(Executor.Main.GetGlobalContext());

        Context mpContext;

        public CatSession(Context pContext)
        {
            mpContext = pContext;
        }

        public static CatSession GetGlobalSession()
        {
            return gpSession;
        }

        public static int CompareFxns(Function f1, Function f2)
        {
            return f1.GetName().CompareTo(f2.GetName());
        }

        public List<DefinedFunction> GetSortedFxns()
        {
            List<DefinedFunction> fxns = new List<DefinedFunction>(mpContext.GetDefinedFunctions());
            fxns.Sort(CompareFxns);
            return fxns;
        }

        public void Output(CatWriter w)
        {
            foreach (DefinedFunction f in GetSortedFxns())
                w.WriteFunction(f);
        }

        public static void SaveToFile(string sFile)
        {
            throw new Exception("unimplemented");
        }

    }
}
