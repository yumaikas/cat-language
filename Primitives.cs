/// Public domain code by Christopher Diggins
/// http://www.cat-language.com

using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Cat
{
    public class CatException : Exception
    {
        object data;

        public CatException(object o)
        {
            data = o;
        }

        public object GetObject()
        {
            return data;
        }
    }

    public class MetaCommands
    {
        public class Load : Function
        {
            public Load()
                : base("#load", "(string ~> )", "loads and executes a source code file")
            { }

            public override void Eval(Executor exec)
            {
                exec.LoadModule(exec.PopString());
            }
        }

        public class Defs : Function
        {
            public Defs()
                : base("#defs", "( ~> )", "lists all loaded definitions")
            { }

            public override void Eval(Executor exec)
            {
                MainClass.OutputDefs(exec);
            }
        }

        public class TypeOf : Function
        {
            public TypeOf()
                : base("#t", "(function -> )", "experimental")
            { }

            public override void Eval(Executor exec)
            {
                string sName = exec.PopString();
                Function f = exec.GetGlobalScope().Lookup(sName);
                if (f == null) throw new Exception("could not find function " + sName);
                string sType = f.GetTypeString();
                MainClass.WriteLine(f.GetName() + " : " + sType);
                CatFxnType t = CatFxnType.CreateFxnType(sType);
                MainClass.WriteLine(f.GetName() + " : " + t.ToString());
            }
        }

        public class AllTypes : Function
        {
            public AllTypes()
                : base("#at", "(function -> )", "experimental")
            { }

            public override void Eval(Executor exec)
            {
                foreach (Function f in exec.GetGlobalScope().GetAllFunctions())
                {
                    string s = f.GetTypeString();
                    if (!s.Equals("untyped"))
                    {
                        try
                        {
                            CatFxnType t = CatFxnType.CreateFxnType(s);
                            Console.WriteLine(f.GetName() + "\t" + s + "\t" + t.ToString());
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(f.GetName() + "\t" + s + "\t" + "error:" + e.Message);
                        }
                    }
                }
            }
        }

        public class ComposedType : Function
        {
            public ComposedType()
                : base("#ct", "(function function -> ", "experimental")
            { }

            public override void Eval(Executor exec)
            {
                String gs = exec.PopString();
                String fs = exec.PopString();
                Function g = exec.GetGlobalScope().Lookup(gs);
                Function f = exec.GetGlobalScope().Lookup(fs);
                CatFxnType ft = CatFxnType.CreateFxnType(f.GetTypeString());
                CatFxnType gt = CatFxnType.CreateFxnType(g.GetTypeString());
                Constraints c = new Constraints();
                CatComposedFxnType t = new CatComposedFxnType(null, ft, gt, c);
                Console.WriteLine(f.GetName() + " : " + ft.ToString());
                Console.WriteLine(g.GetName() + " : " + gt.ToString());
                Console.WriteLine("composed : " + t.ToString());                
                c.OutputConstraints();
                c.ResolveConstraints();
                c.OutputConstraints();
            }
        }

        public class Help : Function
        {
            public Help()
                : base("#help", "( ~> )", "prints some helpful tips")
            { }

            public override void Eval(Executor exec)
            {
                MainClass.WriteLine("The following are some useful meta-commands for the interpreter.");
                MainClass.WriteLine("  #exit - exits the interpreter.");
                MainClass.WriteLine("  #defs - lists available functions.");
                MainClass.WriteLine("  \"command\" #h  - provides more information about a command.");
                MainClass.WriteLine("  \"filename\" #load - load and execute a code file");
            }
        }

        public class CommandHelp : Function
        {
            public CommandHelp()
                : base("#h", "(string ~> )", "prints help about a command")
            { }

            public override void Eval(Executor exec)
            {
                Function f = exec.GetGlobalScope().Lookup(exec.PopString());
                if (f != null)
                {
                    Console.WriteLine(f.GetName() + "\t" + f.GetTypeString() + "\t" + f.GetDesc());
                }
                else
                {
                    Console.WriteLine(exec.PopString() + " is not defined");
                }
            }
        }

    }

    public class Primitives
    {
        #region conversion functions
        public class Str : Function
        {
            public Str()
                : base("str", "(var -> string)", "converts any value into a string representation.")
            { }

            public override void Eval(Executor exec)
            {
                exec.Push(MainClass.ObjectToString(exec.Pop()));
            }
        }

        public class MakeByte : Function
        {
            public MakeByte()
                : base("byte", "(int -> byte)", "converts an integer into a byte, throwing away sign and ignoring higher bits")
            { }

            public override void Eval(Executor exec)
            {
                int n = exec.PopInt();
                byte b = (byte)n;
                exec.Push(b);
            }
        }
        #endregion 

        #region primitive function classes
        public class Id : Function
        {
            public Id()
                : base("id", "('a -> 'a)", "does nothing, but requires one item on the stack.")
            { }

            public override void Eval(Executor exec)
            {                
            }
        }

        public class BinStr : Function
        {
            public BinStr()
                : base("bin_str", "(int -> string)", "converts a number into a binary string representation.")
            { }

            public override void Eval(Executor exec)
            {
                int n = exec.PopInt();
                string s = "";

                if (n == 0) s = "0";
                while (n > 0)
                {
                    if (n % 2 == 1)
                    {
                        s = "1" + s;
                    }
                    else
                    {
                        s = "0" + s;
                    }
                    n /= 2;
                }
                exec.Push(n.ToString(s));
            }
        }

        public class HexStr : Function
        {
            public HexStr()
                : base("hex_str", "(int -> string)", "converts a number into a hexadecimal string representation.")
            { }

            public override void Eval(Executor exec)
            {
                int n = exec.PopInt();
                exec.Push(n.ToString("x"));
            }
        }

        public class Eq : Function
        {
            public Eq()
                : base("eq", "(var var -> bool)", "returns true if both items on stack are the same type, and have same value")
            { }

            public override void Eval(Executor exec)
            {
                Object x = exec.Pop();
                Object y = exec.Pop();
                exec.Push(x.Equals(y));
            }
        }

        public class True : Function
        {
            public True()
                : base("true", "( -> bool)")
            { }

            public override void Eval(Executor exec)
            {
                exec.Push(true);
            }
        }

        public class False : Function
        {
            public False()
                : base("false", "( -> bool)")
            { }

            public override void Eval(Executor exec)
            {
                exec.Push(false);
            }
        }

        public class Dup : Function
        {
            public Dup()
                : base("dup", "('R 'a -> 'R 'a 'a)", "duplicate the top item on the stack")
            { }

            public override void Eval(Executor exec)
            {
                if (exec.Peek() is FMutableList)
                {
                    exec.Push((exec.Peek() as FMutableList).Clone());
                }
                else
                {
                    exec.Push(exec.Peek());
                }
            }
        }

        public class Pop : Function
        {
            public Pop()
                : base("pop", "('R 'a -> 'R)", "removes the top item from the stack")
            { }

            public override void Eval(Executor exec)
            {
                exec.Pop();
            }
        }

        public class Swap : Function
        {
            public Swap()
                : base("swap", "('R 'a 'b -> 'R 'b 'a)", "swap the top two items on the stack")
            { }

            public override void Eval(Executor exec)
            {
                Object o1 = exec.Pop();
                Object o2 = exec.Pop();
                exec.Push(o1);
                exec.Push(o2);
            }
        }

        public class EvalFxn : Function
        {
            public EvalFxn()
                : base("eval", "('A ('A -> 'B) -> 'B)", "evaluates a function")
            { }

            public override void Eval(Executor exec)
            {
                Function f = exec.Pop() as Function;
                f.Eval(exec);
            }
        }

        public class Throw : Function
        {
            public Throw()
                : base("throw", "(var -> )", "throws an exception")
            { }

            public override void Eval(Executor exec)
            {
                object o = exec.Pop();
                     throw new CatException(o);
            }
        }

        public class TryCatch : Function
        {
            public TryCatch()
                : base("try_catch", "('A ('A -> 'B) ('A var -> 'B) -> 'B)", "evaluates a function, and catches any exceptions")
            { }

            public override void Eval(Executor exec)
            {
                Function c = exec.Pop() as Function;
                Function t = exec.Pop() as Function;
                object[] stkCopy = new object[exec.GetStack().Count];
                exec.GetStack().CopyTo(stkCopy);
                try
                {
                    t.Eval(exec);
                }
                catch (CatException e)
                {
                    exec.GetStack().RemoveRange(stkCopy.Length, stkCopy.Length);
                    exec.GetStack().SetRange(0, stkCopy);

                    MainClass.WriteLine("exception caught");

                    exec.Push(e.GetObject());
                    c.Eval(exec);
                }
            }
        }

        public class Dip : Function
        {
            public Dip()
                : base("dip", "('A 'b ('A -> 'C) -> 'C 'b)", "evaluates function, temporarily removing second item")
            { }

            public override void Eval(Executor exec)
            {
                Function f = exec.Pop() as Function;
                Object o = exec.Pop();
                f.Eval(exec);
                exec.Push(o);
            }
        }

        public class Compose : Function
        {
            public Compose()
                : base("compose", "('R ('A -> 'B) ('B -> 'C) -> 'R ('A -> 'C))", 
                    "creates a function by composing (concatenating) two existing functions")
            { }

            public override void Eval(Executor exec)
            {
                Function right = exec.Pop() as Function;
                Function left = exec.Pop() as Function;
                ComposedFunction f = new ComposedFunction(left, right);
                exec.Push(f);
            }
        }

        public class Quote : Function
        {
            public Quote()
                : base("qv", "('R 'a -> 'R ('S -> 'S 'a))", 
                    "short for 'quote value', creates a constant generating function from the top value on the stack")
            { }

            public override void Eval(Executor exec)
            {
                Object o = exec.Pop();
                QuoteValue q = new QuoteValue(o);
                exec.Push(q);
            }
        }

        public class Clr : Function
        {
            public Clr()
                : base("clear", "('A) -> ()", "removes all items from the stack")
            { }

            public override void Eval(Executor exec)
            {
                exec.GetStack().Clear();
            }
        }
        #endregion

        #region control flow primitives 
        public class While : Function
        {
            public While()
                : base("while", "(input='A body=('A -> 'A) condition=('A -> 'A bool) -> 'A)",
                    "executes a block of code repeatedly until the condition returns true")
            { }

            public override void Eval(Executor exec)
            {
                Function cond = exec.Pop() as Function;
                Function body = exec.Pop() as Function;

                cond.Eval(exec);
                while ((bool)exec.Pop())
                {
                    body.Eval(exec);
                    cond.Eval(exec);
                }
            }
        }

        public class If : Function
        {
            public If()
                : base("if", "('A bool ontrue=('A -> 'B) onfalse=('A -> 'B) -> 'B)",
                    "executes one predicate or another whether the condition is true")
            { }

            public override void Eval(Executor exec)
            {
                Function onfalse = exec.Pop() as Function;
                Function ontrue = exec.Pop() as Function;

                if ((bool)exec.Pop())
                {
                    ontrue.Eval(exec);
                }
                else
                {
                    onfalse.Eval(exec);
                }
            }
        }

        public class BinRec : Function
        {
            // The fact that it takes 'b instead of 'B is a minor optimization for untyped implementations
            // I may ignore it later on.
            public BinRec()
                : base("bin_rec", "('A cond=('A -> 'A bool) base=('A -> 'b) arg_rel=('A -> 'C 'A 'A) result_rel('C 'b 'b -> 'b) -> 'b)",
                    "execute a binary recursion process")
            { }

            public void Helper(Executor exec, Function fResultRelation, Function fArgRelation, Function fBaseCase, Function fCondition)
            {
                fCondition.Eval(exec);
                if (exec.PopBool())
                {
                    fBaseCase.Eval(exec);
                }
                else
                {
                    fArgRelation.Eval(exec);
                    Helper(exec, fResultRelation, fArgRelation, fBaseCase, fCondition);
                    Object o = exec.Pop();
                    Helper(exec, fResultRelation, fArgRelation, fBaseCase, fCondition);
                    exec.Push(o);
                    fResultRelation.Eval(exec);
                }
            }

            public override void Eval(Executor exec)
            {
                Helper(exec, exec.PopFunction(), exec.PopFunction(), exec.PopFunction(), exec.PopFunction());
            }
        }
        #endregion 

        #region boolean functions
        public class And : Function
        {
            public And()
                : base("and", "(bool bool -> bool)", "returns true if both of the top two values on the stack are true")
            { }

            public override void Eval(Executor exec)
            {
                bool x = (bool)exec.Pop();
                bool y = (bool)exec.Pop();
                exec.Push(x && y);
            }
        }

        public class Or : Function
        {
            public Or()
                : base("or", "(bool bool -> bool)", "returns true if either of the top two values on the stack are true")
            { }

            public override void Eval(Executor exec)
            {
                bool x = (bool)exec.Pop();
                bool y = (bool)exec.Pop();
                exec.Push(x || y);
            }
        }

        public class Not : Function
        {
            public Not()
                : base("not", "(bool -> bool)", "returns true if the top value on the stack is false")
            { }

            public override void Eval(Executor exec)
            {
                exec.Push(!(bool)exec.Pop());
            }
        }
        #endregion

        #region int functions
        public static int add(int x, int y) { return x + y; }
        public static int sub(int x, int y) { return x - y; }
        public static int div(int x, int y) { return x / y; }
        public static int mul(int x, int y) { return x * y; }
        public static int mod(int x, int y) { return x % y; }
        public static int neg(int x) { return -x; }
        public static int compl(int x) { return ~x; } 
        public static int shl(int x, int y) { return x << y; }
        public static int shr(int x, int y) { return x >> y; }
        public static bool gt(int x, int y) { return x > y; }
        public static bool lt(int x, int y) { return x < y; }
        public static bool gteq(int x, int y) { return x >= y; }
        public static bool lteq(int x, int y) { return x <= y; }
        public static int min(int x, int y) { return Math.Min(x, y); }
        public static int max(int x, int y) { return Math.Max(x, y); }
        public static int abs(int x) { return Math.Abs(x); }
        public static int sqr(int x) { return x * x; }
        #endregion

        #region byte functions
        public static byte add(byte x, byte y) { return (byte)(x + y); }
        public static byte sub(byte x, byte y) { return (byte)(x - y); }
        public static byte div(byte x, byte y) { return (byte)(x / y); }
        public static byte mul(byte x, byte y) { return (byte)(x * y); }
        public static byte mod(byte x, byte y) { return (byte)(x % y); }
        public static byte compl(byte x) { return (byte)(~x); } 
        public static byte shl(byte x, byte y) { return (byte)(x << y); }
        public static byte shr(byte x, byte y) { return (byte)(x >> y); }
        public static bool gt(byte x, byte y) { return x > y; }
        public static bool lt(byte x, byte y) { return x < y; }
        public static bool gteq(byte x, byte y) { return x >= y; }
        public static bool lteq(byte x, byte y) { return x <= y; }
        public static byte min(byte x, byte y) { return Math.Min(x, y); }
        public static byte max(byte x, byte y) { return Math.Max(x, y); }
        public static byte abs(byte x) { return (byte)Math.Abs(x); }
        public static byte sqr(byte x) { return (byte)(x * x); }
        #endregion

        #region bit functions
        public struct Bit
        {
            public bool m;
            public Bit(int n) { m = n != 0; }
            public Bit(bool x) { m = x; }
            public Bit add(Bit x) { return bit(m ^ x.m); }
            public Bit sub(Bit x) { return bit(m && !x.m); }
            public Bit mul(Bit x) { return bit(m && !x.m); }
            public Bit div(Bit x) { return bit(m && !x.m); }
            public Bit mod(Bit x) { return bit(m && !x.m); }
            public bool lteq(Bit x) { return !m || x.m; }
            public bool eq(Bit x) { return m == x.m; }
            public override bool Equals(object obj)
            {
                return (obj is Bit) && (((Bit)obj).m == m);
            }
            public override int GetHashCode()
            {
                return m.GetHashCode();
            }
            public override string ToString()
            {
                return m ? "0b1" : "0b0";
            }
        }
        public static Bit bit(int x) { return new Bit(x); }
        public static Bit bit(bool x) { return new Bit(x); }
        public static Bit add(Bit x, Bit y) { return x.add(y); }
        public static Bit sub(Bit x, Bit y) { return x.sub(y); }
        public static Bit mul(Bit x, Bit y) { return x.mul(y); }
        public static Bit div(Bit x, Bit y) { return x.div(y); }
        public static Bit mod(Bit x, Bit y) { return x.mod(y); }
        public static Bit compl(Bit x) { return bit(!x.m); }
        public static bool neq(Bit x, Bit y) { return !x.eq(y); }
        public static bool gt(Bit x, Bit y) { return !x.lteq(y); }
        public static bool lt(Bit x, Bit y) { return !x.eq(y) && x.lteq(y); }
        public static bool gteq(Bit x, Bit y) { return x.eq(y) || !x.lteq(y); }
        public static bool lteq(Bit x, Bit y) { return x.lteq(y); }
        public static Bit min(Bit x, Bit y) { return bit(x.m && y.m); }
        public static Bit max(Bit x, Bit y) { return bit(x.m || y.m); }
        #endregion

        #region double functions
        public static double add(double x, double y) { return x + y; }
        public static double sub(double x, double y) { return x - y; }
        public static double div(double x, double y) { return x / y; }
        public static double mul(double x, double y) { return x * y; }
        public static double mod(double x, double y) { return x % y; }
        public static double inc(double x) { return x + 1; }
        public static double dec(double x) { return x - 1; }
        public static double neg(double x) { return -x; }
        public static bool gt(double x, double y) { return x > y; }
        public static bool lt(double x, double y) { return x < y; }
        public static bool gteq(double x, double y) { return x >= y; }
        public static bool lteq(double x, double y) { return x <= y; }
        public static double min(double x, double y) { return Math.Min(x, y); }
        public static double max(double x, double y) { return Math.Max(x, y); }
        public static double sin(double x) { return Math.Sin(x); }
        public static double cos(double x) { return Math.Cos(x); }
        public static double tan(double x) { return Math.Tan(x); }
        public static double asin(double x) { return Math.Asin(x); }
        public static double acos(double x) { return Math.Acos(x); }
        public static double atan(double x) { return Math.Atan(x); }
        public static double atan2(double x, double y) { return Math.Atan2(x, y); }
        public static double sinh(double x) { return Math.Sinh(x); }
        public static double cosh(double x) { return Math.Cosh(x); }
        public static double tanh(double x) { return Math.Tanh(x); }
        public static double abs(double x) { return Math.Abs(x); }
        public static double pow(double x, double y) { return Math.Pow(x, y); }
        public static double sqr(double x) { return x * x; }
        public static double sqrt(double x) { return Math.Sqrt(x); }
        public static double trunc(double x) { return Math.Truncate(x); }
        public static double round(double x) { return Math.Round(x); }
        public static double ceil(double x) { return Math.Ceiling(x); }
        public static double floor(double x) { return Math.Floor(x); }
        public static double log(double x, double y) { return Math.Log(x, y); }
        public static double log10(double x) { return Math.Log10(x); }
        public static double ln(double x) { return Math.Log(x); }
        public static double e() { return Math.E; }
        public static double pi() { return Math.PI; }
        public static string format_scientific(double x) { return x.ToString("E"); }
        public static string format_currency(double x) { return x.ToString("C"); }
        #endregion

        #region string functions
        public static bool gt(string x, string y) { return x.CompareTo(y) > 0; }
        public static bool lt(string x, string y) { return x.CompareTo(y) < 0; }
        public static bool gteq(string x, string y) { return x.CompareTo(y) >= 0; }
        public static bool lteq(string x, string y) { return x.CompareTo(y) <= 0; }
        public static string min(string x, string y) { return lteq(x, y) ? x : y; }
        public static string max(string x, string y) { return gteq(x, y) ? x : y; }
        public static string add(string x, string y) { return x + y; }
        public static string sub_str(string x, int i) { return x.Substring(i); }
        public static string sub_str(string x, int i, int n) { return x.Substring(i, n); }
        public static string new_str(char c, int n) { return new string(c, n); }
        public static int index_of(string x, string y) { return x.IndexOf(y); }
        public static string replace_str(string x, string y, string z) { return x.Replace(y, z); }
        #endregion

        #region console functions
        public static void write(Object o) { MainClass.Write(MainClass.ObjectToString(o)); }
        public static void writeln(Object o) { MainClass.WriteLine(MainClass.ObjectToString(o)); }
        public static string readln() { return Console.ReadLine(); }
        public static char readkey() { return Console.ReadKey().KeyChar; }
        #endregion

        #region byte block functions
        public class MakeByteBlock : Function
        {
            public MakeByteBlock()
                : base("byte_block", "(int -> byte_block)", "creates a mutable array of bytes")
            { }

            public override void Eval(Executor exec)
            {
                int n = exec.PopInt();
                ByteBlock bb = new ByteBlock(n);
                bb.ZeroMemory();
                exec.Push(bb);
            }
        }
        #endregion 

        #region i/o functions
        public class OpenFileReader : Function
        {
            public OpenFileReader()
                : base("file_reader", "(string -> istream)", "creates an input stream from a file name")
            { }

            public override void Eval(Executor exec)
            {
                string s = exec.PopString();
                exec.Push(File.OpenRead(s));
            }
        }

        public class OpenWriter : Function
        {
            public OpenWriter()
                : base("file_writer", "(string -> ostream)", "creates an output stream from a file name")
            { }

            public override void Eval(Executor exec)
            {
                string s = exec.PopString();
                exec.Push(File.Create(s));
            }
        }

        public class FileExists : Function
        {
            public FileExists()
                : base("file_exists", "(string -> string bool)", "returns a boolean value indicating whether a file or directory exists")
            { }

            public override void Eval(Executor exec)
            {
                string s = exec.PeekString();
                exec.Push(Directory.Exists(s));
            }
        }

        public class TmpFileName : Function
        {
            public TmpFileName()
                : base("temp_file", "( -> string)", "creates a unique temporary file")
            { }

            public override void Eval(Executor exec)
            {
                exec.Push(Path.GetTempFileName());
            }
        }

        public class ReadBytes : Function
        {
            public ReadBytes()
                : base("read_bytes", "(istream int -> istream bytes)", "reads a number of bytes into an array from an input stream")
            { }

            public override void Eval(Executor exec)
            {
                int n = exec.PopInt();
                Stream f = exec.Peek() as Stream;
                byte[] ab = new byte[n];
                f.Read(ab, 0, n);
                exec.Push(new MArray<byte>(ab)); 
            }
        }

        public class WriteBytes : Function
        {
            public WriteBytes()
                : base("write_bytes", "(ostream bytes -> ostream)", "writes a byte array to an output stream")
            { }

            public override void Eval(Executor exec)
            {                
                MArray<byte> mb = exec.Pop() as MArray<byte>;
                Stream f = exec.Peek() as Stream;
                f.Write(mb.m, 0, mb.Count());
            }
        }

        public class CloseStream : Function
        {
            public CloseStream()
                : base("close_stream", "(stream -> )", "closes a stream")
            { }

            public override void Eval(Executor exec)
            {
                Stream f = exec.Pop() as Stream;
                f.Close();
                f.Dispose();
            }
        }
        #endregion

        #region hash functions
        public class MakeHashList : Function
        {
            public MakeHashList()
                : base("hash_list", "( -> hash_list)", "makes an empty hash list")
            { }

            public override void Eval(Executor exec)
            {
                exec.Push(new HashList());
            }
        }

        public class HashGet : Function
        {
            public HashGet()
                : base("hash_get", "(hash_list var -> hash_list var)", "gets a value from a hash list using a key")
            { }

            public override void Eval(Executor exec)
            {
                Object key = exec.Pop();
                HashList hash = exec.Peek() as HashList;
                Object value = hash.Get(key);
                exec.Push(value);
            }
        }

        public class HashSet : Function
        {
            public HashSet()
                : base("hash_set", "(hash_list key=var value=var -> hash_list)", "associates a value with a key in a hash list")
            { }

            public override void Eval(Executor exec)
            {
                Object value = exec.Pop();
                Object key = exec.Pop();
                HashList hash = exec.Pop() as HashList;
                exec.Push(hash.Set(key, value));
            }
        }

        public class HashAdd : Function
        {
            public HashAdd()
                : base("hash_add", "(hash_list key=var value=var -> hash_list)", "associates a value with a key in a hash list")
            { }

            public override void Eval(Executor exec)
            {
                Object value = exec.Pop();
                Object key = exec.Pop();
                HashList hash = exec.Pop() as HashList;
                exec.Push(hash.Add(key, value));
            }
        }

        public class HashContains : Function
        {
            public HashContains()
                : base("hash_contains", "(hash_list key=var -> hash_list bool)", "returns true if hash list contains key")
            { }

            public override void Eval(Executor exec)
            {
                Object key = exec.Pop();
                HashList hash = exec.Peek() as HashList;
                exec.Push(hash.ContainsKey(key));
            }
        }

        public class HashToList : Function
        {
            public HashToList()
                : base("hash_to_list", "(hash_list -> list)", "converts a hash_list to a list of pairs")
            { }

            public override void Eval(Executor exec)
            {
                HashList hash = exec.Pop() as HashList;
                exec.Push(hash.ToArray());
            }
        }
        #endregion 

        #region list functions
        public class List : Function
        {
            public List()
                : base("list", "(( -> 'A) -> list)", "creates a list from a function")
            { }

            public override void Eval(Executor exec)
            {
                Function f = exec.Pop() as Function;
                f.Eval(Executor.Aux);
                exec.Push(Executor.Aux.GetStack().ToList());
                Executor.Aux.GetStack().Clear();
            }
        }

        public class IsEmpty : Function
        {
            public IsEmpty()
                : base("empty", "(list -> list bool)", "returns true if the list is empty")
            { }

            public override void Eval(Executor exec)
            {
                FList list = exec.Peek() as FList;
                exec.Push(list.IsEmpty());
            }
        }

        public class Count : Function
        {
            public Count()
                : base("count", "(list -> list int)", "returns the number of items in a list")
            { }

            public override void Eval(Executor exec)
            {
                FList list = exec.Peek() as FList;
                exec.Push(list.Count());
            }
        }

        public class Nth : Function
        {
            public Nth()
                : base("nth", "(list int -> list var)", "returns the nth item in a list")
            { }

            public override void Eval(Executor exec)
            {
                int n = (int)exec.Pop();
                FList list = exec.Peek() as FList;
                exec.Push(list.Nth(n));
            }
        }

        public class Gen : Function
        {
            public Gen()
                : base("gen", "(init='a next=('a -> 'a) cond=('a -> bool) -> list)",
                    "creates a lazily evaluated list")
            { }

            public override void Eval(Executor exec)
            {
                Function term = exec.Pop() as Function;
                Function next = exec.Pop() as Function;
                Object init = exec.Pop();
                exec.Push(new Generator(init, next.ToMapFxn(), term.ToFilterFxn()));
            }
        }

        public class Nil : Function
        {
            public Nil()
                : base("nil", "( -> list)", "creates an empty list")
            { }

            public override void  Eval(Executor exec)
            {
 	            exec.Push(FList.Nil());
            }
        }

        public class Unit : Function
        {
            public Unit()
                : base("unit", "('a -> list)", "creates a list of one item")
            { }

            public override void  Eval(Executor exec)
            {
 	            exec.Push(FList.MakeUnit(exec.Pop()));
            }
        }

        public class Pair : Function
        {
            public Pair()
                : base("pair", "('second 'first -> list)", "creates a list from two items")
            { }

            public override void Eval(Executor exec)
            {
                Object x = exec.Pop();
                Object y = exec.Pop();
 	            exec.Push(FList.MakePair(x, y));
            }
        }

        public class Cons : Function
        {
            public Cons()
                : base("cons", "(list 'a -> list)", "prepends an item to a list")
            { }

            public override void Eval(Executor exec)
            {
                object x = exec.Pop();
                FList list = exec.Pop() as FList;
 	            exec.Push(FList.Cons(x, list));
            }
        }

        public class Head : Function
        {
            public Head()
                : base("head", "(list -> var)", "replaces a list with the first item")
            { }

            public override void Eval(Executor exec)
            {
                FList list = exec.Pop() as FList;
 	            exec.Push(list.GetHead());
            }
        }

        public class First : Function
        {
            public First()
                : base("first", "(list -> list var)", "gets the first item from a list")
            { }

            public override void Eval(Executor exec)
            {
                FList list = exec.Peek() as FList;
                exec.Push(list.GetHead());
            }
        }

        public class Last : Function
        {
            public Last()
                : base("last", "(list -> list var)", "gets the last item from a list")
            { }

            public override void Eval(Executor exec)
            {
                FList list = exec.Peek() as FList;
 	            exec.Push(list.Last());
            }
        }

        public class Tail : Function
        {
            public Tail()
                : base("tail", "(list -> list)", "removes first item from a list")
            { }

            public override void Eval(Executor exec)
            {
                FList list = exec.Pop() as FList;
                exec.Push(list.Tail());
            }
        }

        public class Rest : Function
        {
            public Rest()
                : base("rest", "(list -> list list)", "gets a copy of the list with one item")
            { }

            public override void Eval(Executor exec)
            {
                FList list = exec.Peek() as FList;
                exec.Push(list.Tail());
            }
        }

        public class Uncons : Function
        {
            public Uncons()
                : base("uncons", "(list -> list var)", "returns the top of the list, and the rest of a list")
            {}

            public override void Eval(Executor exec)
            {
                FList list = exec.Pop() as FList;
                exec.Push(list.Tail());
                exec.Push(list.GetHead());
            }
        }

        public class Map : Function
        {
            public Map()
                : base("map", "(list ('a -> 'b) -> list)", "creates a new list by modifying an existing list")
            { }

            public override void Eval(Executor exec)
            {
                Function f = exec.Pop() as Function;
                FList list = exec.Pop() as FList;
                exec.Push(list.Map(f.ToMapFxn()));
            }
        }

        public class Filter : Function
        {
            public Filter()
                : base("filter", "(list ('a -> bool) -> list)", "creates a new list containing elements that pass the condition")
            { }

            public override void Eval(Executor exec)
            {
                Function f = exec.Pop() as Function;
                FList list = exec.Pop() as FList;
                exec.Push(list.Filter(f.ToFilterFxn()));
            }
        }
        public class Fold : Function
        {
            public Fold()
                : base("gfold", "('A list ('A var -> 'A) -> 'A)", "recursively applies a function to each element in a list")
            { }

            public override void Eval(Executor exec)
            {
                Function f = exec.Pop() as Function;
                FList list = exec.Pop() as FList;
                FList iter = list.GetIter();
                while (!iter.IsEmpty())
                {
                    exec.Push(iter.GetHead());
                    f.Eval(exec);
                    iter = iter.GotoNext();
                }
            }
        }

        public class Cat : Function
        {
            public Cat()
                : base("cat", "(list list -> list)", "concatenates two lists")
            { }

            public override void Eval(Executor exec)
            {
                FList first = exec.Pop() as FList;
                FList second = exec.Pop() as FList;
                exec.Push(FList.Concat(first, second));
            }
        }

        public class TakeN : Function
        {
            public TakeN()
                : base("take", "(list int -> list)", "creates a new list from the first n items")
            { }

            public override void Eval(Executor exec)
            {
                int n = (int)exec.Pop();
                FList list = exec.Pop() as FList;
                exec.Push(list.TakeN(n));
            }
        }

        public class DropN : Function
        {
            public DropN()
                : base("drop", "(list int -> list)", "creates a new list without the first n items")
            { }

            public override void Eval(Executor exec)
            {
                int n = (int)exec.Pop();
                FList list = exec.Pop() as FList;
                exec.Push(list.DropN(n));
            }
        }

        public class TakeRange : Function
        {
            public TakeRange()
                : base("take_range", "(list first=int count=int -> list)", "creates a new list which is a sub-range of the original")
            { }

            public override void Eval(Executor exec)
            {
                int count = (int)exec.Pop();
                int n = (int)exec.Pop();
                FList list = exec.Pop() as FList;
                exec.Push(list.TakeRange(n, count));
            }
        }

        public class TakeWhile : Function
        {
            public TakeWhile()
                : base("take_while", "(list ('a -> bool) -> list)", "creates a new list by taking items while the predicate is true")
            { }

            public override void Eval(Executor exec)
            {
                Function f = exec.Pop() as Function;
                FList list = exec.Pop() as FList;
                exec.Push(list.TakeWhile(f.ToFilterFxn()));
            }
        }

        public class DropWhile : Function
        {
            public DropWhile()
                : base("drop_while", "(list ('a -> bool) -> list)", "creates a new list by dropping items while the predicate is true")
            { }

            public override void Eval(Executor exec)
            {
                Function f = exec.Pop() as Function;
                FList list = exec.Pop() as FList;
                exec.Push(list.DropWhile(f.ToFilterFxn()));
            }
        }

        public class CountWhile : Function
        {
            public CountWhile()
                : base("count_while", "(list ('a -> bool) -> list count)", "creates a new list by dropping items while the predicate is true")
            { }

            public override void Eval(Executor exec)
            {
                Function f = exec.Pop() as Function;
                FList list = exec.Peek() as FList;
                exec.Push(list.CountWhile(f.ToFilterFxn()));
            }
        }

        public class RangeGen : Function
        {
            public RangeGen()
                : base("range_gen", "(int int (int -> 'a) -> list)", 
                    "creates a lazy list from a range of numbers and a generating function")
            { }

            public override void Eval(Executor exec)
            {
                Function f = exec.Pop() as Function;
                int count = (int)exec.Pop();
                int n = (int)exec.Pop();
                exec.Push(FList.RangeGen(f.ToRangeGenFxn(), n, count));
            }
        }

        public class Repeater : Function
        {
            public Repeater()
                : base("repeater", "(var -> list)", 
                    "creates a lazy list by repeating a value over and over again")
            { }

            public override void Eval(Executor exec)
            {
                Object o = exec.Pop();
                exec.Push(FList.MakeRepeater(o));
            }
        }

        public class Flatten : Function
        {
            public Flatten()
                : base("flatten", "(list -> list)", "concatenates all sub-lists in a list of lists")
            { }

            public override void Eval(Executor exec)
            {
                FList list = exec.Pop() as FList;
                exec.Push(list.Flatten());
            }
        }
        #endregion

        #region mutable list instructions
        public class SetAt : Function
        {
            public SetAt()
                : base("set_at", "(list var int -> list)", "sets an item in a mutable list")
            { }

            public override void Eval(Executor exec)
            {
                int n = (int)exec.Pop();
                Object o = exec.Pop();
                if (exec.Peek() is FMutableList)
                {
                    FMutableList list = exec.Peek() as FMutableList;
                    list.Set(n, o);
                }
                else
                {
                    FList list = exec.Pop() as FList;
                    FMutableList mut = new MArray<Object>(list);
                }
            }
        }
        #endregion 
    }
}
