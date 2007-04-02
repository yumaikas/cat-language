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

namespace Cat
{
    public class MainClass
    {
        static List<string> gsInputFiles = new List<string>();
        static StringWriter gpTranscript = new StringWriter();

        static void Main(string[] a)
        {
            try
            {
                foreach (string s in a)
                    gsInputFiles.Add(s);

                // Splash screen 
                if (Config.gbShowLogo)
                {
                    WriteLine("");
                    WriteLine("Cat Interpreter");
                    WriteLine("version 0.10.0 March 29, 2006");
                    WriteLine("by Christopher Diggins");
                    WriteLine("this software is public domain");
                    WriteLine("http://www.cat-language.com");
                    WriteLine("");
                    WriteLine("Type in #help for help, and #exit to exit.");
                    WriteLine("");
                }

                // Load primitive operations 
                RegisterPrimitives(Scope.Global());
                
                // Load all files on the command line                
                foreach (string sFile in MainClass.gsInputFiles)
                    Executor.Main.LoadModule(sFile);


                if (gsInputFiles.Count == 0)
                {
                    Console.WriteLine("warning: no files were passed as command line arguments, therefore the standard library hasn't been loaded.");
                    Console.WriteLine("you can load the standard library by writing: #load path\\standard.cat");
                }

                // main execution loop
                while (true)
                {
                    Prompt();
                    try
                    {
                        string s = Console.ReadLine();
                        gpTranscript.WriteLine(s);
                        if (s.Length > 0)
                        {
                            // Is this a meta-command?
                            if (s[0] == '#')
                            {
                                if (s == "#exit")
                                    break;
                                ParseMetaCommand(s);
                            }
                            else
                            {
                                DateTime begin = DateTime.Now;
                                Executor.Main.Execute(s + '\n');
                                TimeSpan elapsed = DateTime.Now - begin;
                                if (Config.gbOutputTimeElapsed)
                                    WriteLine("Time elapsed : {0:F} msec", elapsed.TotalMilliseconds);
                                if (Config.gbOutputStack)
                                    Executor.Main.OutputStack();
                            }
                        }
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

            string sTranscript = "transcript.txt";
            try
            {
                StreamWriter sw = new StreamWriter("transcript.txt");
                gpTranscript.Flush();
                sw.Write(gpTranscript.ToString());
                sw.Close();
                WriteLine("A transcript of your session has been saved in the file " + sTranscript.ToString());
            }
            catch(Exception e)
            {
                WriteLine("Error occured while writing transcript: " + e.Message);
            }

            WriteLine("goodbye!");

        }

        #region meta-commands (commands intended for the interpreter)
        public static void ParseMetaCommand(string s)
        {
            Trace.Assert(s.Length > 0);
            Trace.Assert(s[0] == '#');

            string[] tokens = s.Split(new char[] { ' ' });

            Trace.Assert(tokens.Length > 0);

            switch (tokens[0])
            {
                case "#help":
                    WriteLine("#defs will provide a list of available functions.");
                    WriteLine("#exit will allow you to exit the program.");
                    WriteLine("#load filename loads a file during execution.");
                    WriteLine("More help will be available in later versions.");
                    break;
                case "#load":
                    if (tokens.Length != 2)
                        throw new Exception("The #load meta-command requires an additional argument");
                    Executor.Main.LoadModule(tokens[1]);
                    break;
                case "#defs":
                    OutputDefs();
                    break;
                default:
                    WriteLine("unrecognized meta-command " + tokens[0]);
                    break;
            }
        }

        public static void OutputDefs()
        {
            foreach (Function f in Scope.Global().GetAllFunctions())
            {
                if (f is DefinedFunction)
                {
                    DefinedFunction d = f as DefinedFunction;
                    WriteLine(f.GetName() + "\t== " + d.GetTermsAsString());
                }
                else
                {
                    WriteLine(f.GetName() + "\t" + f.GetTypeString());
                }
            }
            foreach (List<Method> list in Scope.Global().GetAllMethods())
                foreach (Method m in list)
                    WriteLine(m.GetName() + "\t" + m.GetTypeString());
        }
        #endregion

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
            gpTranscript.Write(s, o);
        }

        public static void WriteLine(string s, object o)
        {
            Console.WriteLine(s, o);
            gpTranscript.WriteLine(s, o);
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

        public static void Prompt()
        {
            Write(">> ");
        }
        #endregion

        #region register primitives
        public static void RegisterPrimitives(Scope scope)
        {
            scope.Register(typeof(Primitives));
            scope.Register(typeof(CatList));
            scope.Register(typeof(HashList));
            scope.Register(typeof(Window));
        }

        #endregion
    }
}