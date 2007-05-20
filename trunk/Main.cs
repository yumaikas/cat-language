/// Public domain code by Christopher Diggins
/// http://www.cat-language.com

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
        static StringWriter gpTranscript = new StringWriter();
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
                {
                    WriteLine("");
                    WriteLine("Cat Interpreter");
                    WriteLine("version 0.14.0 May 12th, 2007");
                    WriteLine("by Christopher Diggins");
                    WriteLine("this software is released under the MIT license");
                    WriteLine("the source code is public domain and available at");
                    WriteLine("http://www.cat-language.com");
                    WriteLine("");
                    WriteLine("Type in #help for help and #exit to exit.");
                    WriteLine("");
                }

                // Load primitive operations 
                RegisterPrimitives(Executor.Main.GetGlobalScope());
                
                // Load all files on the command line                
                foreach (string sFile in MainClass.gsInputFiles)
                    Executor.Main.LoadModule(sFile);

                if (gsInputFiles.Count == 0)
                {
                    Console.WriteLine("warning: no files were passed as command line arguments."); 
                    Console.WriteLine("This means the standard library hasn't been loaded.");
                    Console.WriteLine("You can load the standard library by writing: \"path\\standard.cat\" #load");
                    Console.WriteLine("");
                }

                // main execution loop
                while (true)
                {
                    Prompt();
                    string s = Console.ReadLine();
                    if (s.Equals("#exit"))
                        break;
                    gpTranscript.WriteLine(s);
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
                Write(f.GetName() + "\t");
            }
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
        #endregion

        #region console/loggging output function
        public static void Write(object o)
        {
            if (o is String)
                Write(o as String);
            else 
                Write(ObjectToString(o));
        }
        public static void WriteLine(object o)
        {
            if (o is String)
                WriteLine(o as String);
            else
                WriteLine(ObjectToString(o));
        }

        public static void Write(string s)
        {
            Console.Write(s);
            gpTranscript.Write(s);
        }

        public static void WriteLine(string s)
        {
            Console.WriteLine(s);
            gpTranscript.WriteLine(s);
        }

        public static string ForEachToString(FList x)
        {
            string result = ")";

            int i = 0;
            Accessor acc = delegate(Object o)
            {
                if (i++ > 0)
                    result = ", " + result;
                // we don't print the last one
                // this way we know there is one more
                // and that the ellipsis (...) is appropriate
                if (i <= 4)
                    result = ObjectToString(o) + result;
            };
            x.TakeN(5).ForEach(acc);

            if (i == 5)
                result = "..." + result;

            result = "(" + result;

            return result;
        }

        public static string ObjectToString(object o)
        {
            if (o is string)
            {
                return "\"" + ((string)o) + "\"";
            }
            else if (o is char)
            {
                return "'" + ((char)o) + "'";
            }
            else if (o is FList)
            {
                return ForEachToString(o as FList);
            }
            else if (o is Byte)
            {
                byte b = (byte)o;
                return "0x" + b.ToString("x2");
            }
            else if (o is Double)
            {
                double d = (double)o;
                return d.ToString("F");
            }
            else
            {
                return o.ToString();
            }
        }
        public static void Prompt()
        {
            Write(">> ");
        }
        public static void SaveTranscript(string sTranscript)
        {
            try
            {
                StreamWriter sw = new StreamWriter(sTranscript);
                gpTranscript.Flush();
                sw.Write(gpTranscript.ToString());
                sw.Close();
                WriteLine("A transcript of your session has been saved to the file " + sTranscript);
            }
            catch (Exception e)
            {
                WriteLine("Error occured while attempting to write transcript: " + e.Message);
            }
        }
       #endregion

        #region register primitives
        public static void RegisterPrimitives(Scope scope)
        {
            scope.RegisterType(typeof(MetaCommands));
            scope.RegisterType(typeof(Primitives));
            scope.RegisterType(typeof(Drawer));
        }
        #endregion
    }
}