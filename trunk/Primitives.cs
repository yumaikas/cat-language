/// Public domain code by Christopher Diggins
/// http://www.cat-language.com

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Cat
{
    public class Primitives
    {
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
                : base("dup", "('a -> 'a 'a)", "duplicate the top item on the stack")
            { }

            public override void Eval(Executor exec)
            {
                exec.Push(exec.Peek());
            }
        }

        public class Pop : Function
        {
            public Pop()
                : base("pop", "('a -> )", "removes the top item from the stack")
            { }

            public override void Eval(Executor exec)
            {
                exec.Pop();
            }
        }

        public class Swap : Function
        {
            public Swap()
                : base("swap", "('a 'b -> 'b 'a)", "swap the top two items on the stack")
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
                : base("compose", "(('A -> 'B) ('B -> 'C) -> ('A -> 'C))", 
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
                : base("qv", "('a -> ( -> 'a))", 
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
        public static bool xor(bool x, bool y) { return x ^ y; }
        public static int add(int x, int y) { return x + y; }
        public static int sub(int x, int y) { return x - y; }
        public static int div(int x, int y) { return x / y; }
        public static int mul(int x, int y) { return x * y; }
        public static int mod(int x, int y) { return x % y; }
        public static int neg(int x) { return -x; }
        public static int shl(int x, int y) { return x << y; }
        public static int shr(int x, int y) { return x >> y; }
        public static bool gt(int x, int y) { return x > y; }
        public static bool lt(int x, int y) { return x < y; }
        public static bool gteq(int x, int y) { return x >= y; }
        public static bool lteq(int x, int y) { return x <= y; }
        public static bool eq(int x, int y) { return x == y; }
        public static bool neq(int x, int y) { return x != y; }
        public static int min(int x, int y) { return Math.Min(x, y); }
        public static int max(int x, int y) { return Math.Max(x, y); }
        public static int abs(int x) { return Math.Abs(x); }
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
        public static bool eq(double x, double y) { return x == y; }
        public static bool neq(double x, double y) { return x != y; }
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
        #endregion

        #region string functions
        public static string cat_str(string x, string y) { return x + y; }
        public static string sub_str(string x, int i) { return x.Substring(i); }
        public static string sub_str(string x, int i, int n) { return x.Substring(i, n); }
        public static string new_str(char c, int n) { return new string(c, n); }
        public static int index_of(string x, string y) { return x.IndexOf(y); }
        public static string replace_str(string x, string y, string z) { return x.Replace(y, z); }
        #endregion

        #region console functions
        public static void write(Object o) { MainClass.Write(o.ToString()); }
        public static void writeln(Object o) { MainClass.WriteLine(o.ToString()); }
        public static string readln() { return Console.ReadLine(); }
        public static char readkey() { return Console.ReadKey().KeyChar; }
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
                exec.Push(new StackToList(Executor.Aux.GetStack()));
            }
        }

        public class GenFilterMap : Function
        {
            public GenFilterMap()
                : base("gen_filter_map", "(init='a next=('a -> 'a) cond=('a -> bool) filterf=('a -> bool) mapf=('a -> 'b) -> list)",
                    "creates a lazily evaluated list comprehension")
            { }

            public override void Eval(Executor exec)
            {
                Function mapf = exec.Pop() as Function;
                Function filterf = exec.Pop() as Function;
                Function term = exec.Pop() as Function;
                Function next = exec.Pop() as Function;
                Object init = exec.Pop();
                exec.Push(new LazyList(init, next, term, filterf, mapf));
            }
        }
        #endregion

        #region other function
        public static void error(string s)
        {
            throw new Exception("s");
        }
        #endregion
    }
}
