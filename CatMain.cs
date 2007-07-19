/// Dedicated to the public domain by Christopher Diggins
/// http://creativecommons.org/licenses/publicdomain/

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Timers;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using System.Drawing;

namespace Cat
{
    public class MainClass
    {
        static List<string> gsInputFiles = new List<string>();
        static CatHelpMaker gpHelp;

        static void Main(string[] a)
        {
            gpHelp = CatHelpMaker.CreateHelp("help.txt");
            if (gpHelp == null)
                WriteLine("failed to load help file");                

            try
            {
                foreach (string s in a)
                    gsInputFiles.Add(s);

                // Splash screen 
                if (Config.gbShowLogo)
                    Output.ShowLogo();

                // Load primitive operations 
                Context context  = Executor.Main.GetGlobalScope();
                context.RegisterType(typeof(MetaCommands));
                context.RegisterType(typeof(Primitives));
                 
                // Load all files on the command line                
                foreach (string sFile in MainClass.gsInputFiles)
                    Executor.Main.LoadModule(sFile);

                if (gsInputFiles.Count == 0)
                {
                    Console.WriteLine("warning: no files were passed as command line arguments,"); 
                    Console.WriteLine("this means the standard library hasn't been loaded.");
                    Console.WriteLine("You can load the standard library by writing: \"everything.cat\" #load");
                    Console.WriteLine("");
                }

                // main execution loop
                while (true)
                {
                    Prompt();
                    string s = Console.ReadLine();
                    if (s.Equals("#exit"))
                        break;
                    Output.LogLine(s);
                    if (s.Length > 0)
                    {
                        DateTime begin = DateTime.Now;
                        Executor.Main.Execute(s + '\n');
                        TimeSpan elapsed = DateTime.Now - begin;
                        if (Config.gbOutputTimeElapsed)
                            WriteLine("Time elapsed in msec " + elapsed.TotalMilliseconds.ToString("F"));
                        if (Config.gbOutputStack)
                            Executor.Main.OutputStack();
                    }
                }
            }
            catch (Exception e)
            {
                WriteLine("uncaught exception: " + e.Message);
            }

            SaveTranscript(Path.GetTempFileName());

            WriteLine("Press any key to exit ...");
            Console.ReadKey();
        }

        #region meta-commands (commands intended for the interpreter)
        public static void OutputDefs(Executor exec)
        {
            Function[] fxns = new Function[exec.GetGlobalScope().GetAllFunctions().Count];
            exec.GetGlobalScope().GetAllFunctions().CopyTo(fxns, 0);
            Comparison<Function> comp = delegate(Function x, Function y) { return x.GetName().CompareTo(y.GetName()); };
            
            Array.Sort(fxns, comp);                            

            foreach (Function f in fxns)
            {
                Write(f.GetName() + " \t");
            }
        }

        public static void OutputHelp(QuotedFunction q)
        {
            foreach (Function f in q.GetChildren())
                f.OutputDetails();
        }

        public static void OutputHelp(string s)
        {
            if (gpHelp == null)
            {
                MainClass.WriteLine("no help has been loaded");
                return;
            }

            FxnDoc f = gpHelp.GetFxnDoc(s);
            if (f == null)
            {
                MainClass.WriteLine("no help available for " + s);
            }
            else
            {
                MainClass.WriteLine("name      : " + f.msName);
                MainClass.WriteLine("category  : " + f.msCategory);
                MainClass.WriteLine("level     : " + f.mnLevel.ToString());
                MainClass.WriteLine("type      : " + f.msType);
                MainClass.WriteLine("semantics : " + f.msSemantics);
                MainClass.WriteLine("remarks   : " + f.msNotes);
            }
        }

        public static void MakeHtmlHelp()
        {
            if (gpHelp == null)
            {
                MainClass.WriteLine("no help has been loaded");
                return;
            }

            try
            {
                string s = "help.html";
                gpHelp.SaveHtmlFile(s);
                MainClass.WriteLine("help file has been saved to " + s);
            }
            catch
            {
                MainClass.WriteLine("failed to create html help file");
            }
        }

        public static void MakeLibrary()
        {
            if (gpHelp == null)
            {
                MainClass.WriteLine("no help has been loaded");
                return;
            }

            try
            {
                string s = "library.cat";
                gpHelp.SaveLibrary(s);
                MainClass.WriteLine("library file has been saved to " + s);
            }
            catch
            {
                MainClass.WriteLine("failed to create library");
            }
        }
        #endregion

        // These functions were added after the fact. If I am bored some day I should 
        // remove them and redirect all calls directly to "Output"
        #region console/loggging output function        
        public static void Write(object o)
        {
            Output.Write(o);
        }
        public static void WriteLine(object o)
        {
            Output.WriteLine(o);
        }
        public static void Write(string s)
        {
            Output.Write(s);
        }
        public static void WriteLine(string s)
        {
            Output.WriteLine(s);
        }
        public static void Prompt()
        {
            Output.Write(">> ");
        }
        public static void SaveTranscript(string sTranscript)
        {
            Output.SaveTranscript(sTranscript);
        }
        #endregion
   }
}