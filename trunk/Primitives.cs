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

            public override void Eval(CatStack stk)
            {                
            }
        }


        public class True : Function
        {
            public True()
                : base("true", "( -> bool)")
            { }

            public override void Eval(CatStack stk)
            {
                stk.Push(true);
            }
        }

        public class False : Function
        {
            public False()
                : base("false", "( -> bool)")
            { }

            public override void Eval(CatStack stk)
            {
                stk.Push(false);
            }
        }

        public class Dup : Function
        {
            public Dup()
                : base("dup", "('a -> 'a 'a)", "duplicate the top item on the stack")
            { }

            public override void Eval(CatStack stk)
            {
                stk.Push(stk.Peek());
            }
        }

        public class Pop : Function
        {
            public Pop()
                : base("pop", "('a -> )", "removes the top item from the stack")
            { }

            public override void Eval(CatStack stk)
            {
                stk.Pop();
            }
        }

        public class Swap : Function
        {
            public Swap()
                : base("swap", "('a 'b -> 'b 'a)", "swap the top two items on the stack")
            { }

            public override void Eval(CatStack stk)
            {
                Object o1 = stk.Pop();
                Object o2 = stk.Pop();
                stk.Push(o1);
                stk.Push(o2);
            }
        }

        public class EvalFxn : Function
        {
            public EvalFxn()
                : base("eval", "('A ('A -> 'B) -> 'B)", "evaluates a function")
            { }

            public override void Eval(CatStack stk)
            {
                Function f = stk.Pop() as Function;
                f.Eval(stk);
            }
        }

        public class Dip : Function
        {
            public Dip()
                : base("dip", "('A 'b ('A -> 'C) -> 'C 'b)", "evaluates function, temporarily removing second item")
            { }

            public override void Eval(CatStack stk)
            {
                Function f = stk.Pop() as Function;
                Object o = stk.Pop();
                f.Eval(stk);
                stk.Push(o);
            }
        }

        public class List : Function
        {
            public List()
                : base("list", "(( -> 'A) -> list)", "creates a list from a function")
            { }

            public override void Eval(CatStack stk)
            {
                CatStack tmp = new CatStack();
                Function f = stk.Pop() as Function;
                f.Eval(tmp);
                stk.Push(new StackToList(tmp));
            }
        }

        public class Gen : Function
        {
            public Gen()
                : base("gen", "(init='a next=('a -> 'a) cond=('a -> 'a bool) -> list)", 
                    "creates a lazily evaluated list from an initial value, applying a successor function until the predicate is satisfied")
            { }

            public override void Eval(CatStack stk)
            {
                Function term = stk.Pop() as Function;
                Function next = stk.Pop() as Function;
                Object init = stk.Pop();
                stk.Push(new LazyList(init, next, term));
            }
        }

        public class Compose : Function
        {
            public Compose()
                : base("compose", "(('A -> 'B) ('B -> 'C) -> ('A -> 'C))", 
                    "creates a function by composing (concatenating) two existing functions")
            { }

            public override void Eval(CatStack stk)
            {
                Function right = stk.Pop() as Function;
                Function left = stk.Pop() as Function;
                ComposedFunction f = new ComposedFunction(left, right);
                stk.Push(f);
            }
        }

        public class Quote : Function
        {
            public Quote()
                : base("quote", "('a -> ( -> 'a))", 
                    "creates a constant generating function from the top value on the stack")
            { }

            public override void Eval(CatStack stk)
            {
                Object o = stk.Pop();
                QuoteValue q = new QuoteValue(o);
                stk.Push(q);
            }
        }

        public class Clr : Function
        {
            public Clr()
                : base("clr", "('A) -> ()", "removes all items from the stack")
            { }

            public override void Eval(CatStack stk)
            {
                stk.Clear();
            }
        }

        public class While : Function
        {
            public While()
                : base("while", "('A body=('A -> 'A) condition=('A -> 'A bool) -> 'A)",
                    "executes a block of code repeatedly until the condition is true")
            { }

            public override void Eval(CatStack stk)
            {
                Function cond = stk.Pop() as Function;
                Function body = stk.Pop() as Function;

                cond.Eval(stk);
                while (!(bool)stk.Pop())
                {
                    body.Eval(stk);
                    cond.Eval(stk);
                }
            }
        }

        public class If : Function
        {
            public If()
                : base("if", "('A bool ontrue=('A -> 'B) onfalse=('A -> 'B) -> 'B)",
                    "executes one predicate or another whether the condition is true")
            { }

            public override void Eval(CatStack stk)
            {
                Function onfalse = stk.Pop() as Function;
                Function ontrue = stk.Pop() as Function;

                if ((bool)stk.Pop())
                {
                    ontrue.Eval(stk);
                }
                else
                {
                    onfalse.Eval(stk);
                }
            }
        }
        #endregion 

        #region boolean functions
        public static bool and(bool x, bool y) { return x && y; }
        public static bool or(bool x, bool y) { return x || y; }
        public static bool not(bool x) { return !x; }
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

        #endregion
    }
}
