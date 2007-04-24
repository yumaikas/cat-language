/// Public domain code by Christopher Diggins
/// http://www.cat-language.com

using System;
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
        public static int min(int x, int y) { return Math.Min(x, y); }
        public static int max(int x, int y) { return Math.Max(x, y); }
        public static int abs(int x) { return Math.Abs(x); }
        public static int sqr(int x) { return x * x; }
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
 	            exec.Push(list.Head());
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
                exec.Push(list.Head());
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
                exec.Push(list.Head());
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
                : base("fold", "(list 'a ('a 'b -> 'a) -> 'a)", "recursively applies a function to each an accumlator")
            { }

            public override void Eval(Executor exec)
            {
                Function f = exec.Pop() as Function;
                Object o = exec.Pop();
                FList list = exec.Pop() as FList;
                exec.Push(list.Fold(o, f.ToFoldFxn()));
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
    }
}
