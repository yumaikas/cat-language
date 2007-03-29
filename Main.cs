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

namespace Cat
{
    public class MainClass
    {
        public static List<string> gsInputFiles = new List<string>();
        public static StreamWriter gpLogFile = null;

        static void Main(string[] a)
        {
            try
            {
                foreach (string s in a)
                    gsInputFiles.Add(s);

                try
                {
                    if (Config.gbLogSession)
                        gpLogFile = new StreamWriter("log.txt");
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed to open log file {0}", e);
                    Config.gbLogSession = false;
                }

                // Splash screen 
                if (Config.gbShowLogo)
                {
                    WriteLine("");
                    WriteLine("Cat Interpreter");
                    WriteLine("version 0.10.0 March 28, 2006");
                    WriteLine("by Christopher Diggins");
                    WriteLine("this software is public domain");
                    WriteLine("http://www.cat-language.com");
                    WriteLine("");
                }

                // Load primitive operations 
                RegisterPrimitives(Scope.Global());

                // Quietly load the standard library 
                bool bTmp = Config.gbQuietImport;
                Config.gbQuietImport = true;
                string sLibrary = "lib\\standard.cat";
                if (!File.Exists(sLibrary))
                    Console.WriteLine("Could not find the standard library: " + sLibrary);
                Executor.Main.LoadModule(sLibrary);
                Config.gbQuietImport = bTmp;
                
                // Load all files on the command line                
                foreach (string sFile in MainClass.gsInputFiles)
                    Executor.Main.LoadModule(sFile);

                // main execution loop
                while (true)
                {
                    Prompt();
                    try
                    {
                        string s = Console.ReadLine();
                        if (Config.gbLogSession)
                            gpLogFile.WriteLine(s);
                        DateTime begin = DateTime.Now;
                        Executor.Main.Execute(s + '\n');
                        TimeSpan elapsed = DateTime.Now - begin;
                        if (Config.gbOutputTimeElapsed)
                            WriteLine("Time elapsed : {0:F} msec", elapsed.TotalMilliseconds);
                        if (Config.gbOutputStack)
                            Executor.Main.OutputStack();
                    }
                    catch (Exception e)
                    {
                        MainClass.WriteLine("Exception caught: {0}.", e.Message);
                    }
                }
            }
            catch (Exception e)
            {
                WriteLine("Untrapped exception occurred: {0}", e.Message);
            }

            if (Config.gbLogSession)
            {
                gpLogFile.Flush();
                gpLogFile.Close();
            }
        }

        #region console/loggging output function 
        public static void WriteArrayList(ArrayList a)
        {
            Write("(");
            for (int i = 0; i < a.Count; ++i)
            {
                if (i > 0) {
                    Write(",");
                }
                Write(a[i]);
            }
            Write(")");
        }
        public static void Write(object o)
        {
            if (o is ArrayList) WriteArrayList(o as ArrayList);
            else Write(o.ToString());
        }
        public static void WriteLine(object o)
        {
            if (o is ArrayList)
            {
                WriteArrayList(o as ArrayList);
                WriteLine("");
            }
            else
            {
                WriteLine(o.ToString());
            }
        }
        public static void Write(string s, object o)
        {
            Console.Write(s, o);
            if (Config.gbLogSession) gpLogFile.Write(s, o);
        }

        public static void WriteLine(string s, object o)
        {
            Console.WriteLine(s, o);
            if (Config.gbLogSession) gpLogFile.WriteLine(s, o);
        }

        public static void Write(string s)
        {
            Console.Write(s);
            if (Config.gbLogSession) gpLogFile.Write(s);
        }

        public static void WriteLine(string s)
        {
            Console.WriteLine(s);
            if (Config.gbLogSession) gpLogFile.WriteLine(s);
        }

        public static void Prompt()
        {
            Write(">> ");
            if (Config.gbLogSession)
                gpLogFile.Flush();
        }
        #endregion

        #region register primitives
        public static void RegisterPrimitives(Scope scope)
        {
            scope.Register(typeof(Primitives));
            scope.Register(typeof(CatList));
            scope.Register(typeof(Window));
        }

        #endregion
    }
}