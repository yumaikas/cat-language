/// Dedicated to the public domain by Christopher Diggins
/// http://creativecommons.org/licenses/publicdomain/

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

    /// <summary>
    /// This is used to as a base class for the various dynamic function dispatch functions
    /// It can not be placed in with the Primitives class, due to some design flaw.
    /// </summary>
    public class Dispatch : PrimitiveFunction
    {
        public Dispatch(string sName, string sType)
            : base(sName, sType, "dispatches a function based on the type of the top stack item")
        { }

        public override void Eval(Executor exec)
        {
            FList list = exec.TypedPop<FList>();
            Type t = exec.Peek().GetType();
            FList iter = list.GetIter();
            while (!iter.IsEmpty())
            {
                Pair p = iter.GetHead() as Pair;
                if (p == null)
                    throw new Exception("dispatch requires a list of pairs; types and functions");
                Type u = p.Second() as Type;
                if (u == null)
                    throw new Exception("dispatch requires a list of pairs; types and functions");
                if (u.IsAssignableFrom(t))
                {
                    Function f = p.First() as Function;
                    if (f == null)
                        throw new Exception("dispatch requires a list of pairs; types and functiosn");
                    f.Eval(exec);
                    return;
                }
                iter = iter.GotoNext();
            }
            throw new Exception("could not find appropriate function to dispatch to");
        }
    }

    public class MetaCommands
    {
        public class Load : PrimitiveFunction
        {
            public Load()
                : base("#load", "(string ~> )", "loads and executes a source code file")
            { }

            public override void Eval(Executor exec)
            {
                exec.LoadModule(exec.PopString());
            }
        }

        public class Save : PrimitiveFunction
        {
            public Save()
                : base("#save", "(string ~> )", "saves a transcript of the session so far")
            { }

            public override void Eval(Executor exec)
            {
                MainClass.SaveTranscript(exec.PopString());
            }
        }

        public class Defs : PrimitiveFunction
        {
            public Defs()
                : base("#defs", "( ~> )", "lists all documented definitions")
            { }

            public override void Eval(Executor exec)
            {
                MainClass.OutputDefs(exec);
            }
        }

        public class TypeOf : PrimitiveFunction
        {
            public TypeOf()
                : base("#t", "(function -> )", "experimental")
            { }

            public override void Eval(Executor exec)
            {
                QuotedFunction f = exec.TypedPop<QuotedFunction>();
                bool bVerbose = Config.gbVerboseInference;
                bool bInfer = Config.gbTypeChecking;
                Config.gbVerboseInference = true;
                Config.gbTypeChecking = true;
                try
                {
                    CatFxnType ft = CatTypeReconstructor.Infer(f.GetChildren());
                    if (ft == null)
                        Output.WriteLine("type could not be inferred");
                }
                finally
                {
                    Config.gbVerboseInference = bVerbose;
                    Config.gbTypeChecking = bInfer;
                }
            }
        }

        public class Help : PrimitiveFunction
        {
            public Help()
                : base("#help", "( ~> )", "prints some helpful tips")
            { }

            public override void Eval(Executor exec)
            {
                Output.WriteLine("The following are some useful commands:");
                Output.WriteLine("  #clr - clears the stack");
                Output.WriteLine("  #defs - lists all defined functions");
                Output.WriteLine("  #exit - exits the interpreter");
                Output.WriteLine("  \"filename\" #load - loads and executes a Cat file");
                Output.WriteLine("  \"filename\" #save - saves a transcript of session");
                Output.WriteLine("  [...] #h - provides documentation on an instruction");
                Output.WriteLine("  [...] #t - infers the type of a quotation");
                Output.WriteLine("  [...] #i - performs inline expansion within a quotation");
                Output.WriteLine("  [...] #p - partially evaluates a quotation");
                Output.WriteLine("  [...] #m - applies macros to a quotation");
                Output.WriteLine("  [...] #o - optimizes a quotation");
                Output.WriteLine("  [...] #c - compiles a quotation into an assembly");
                Output.WriteLine("  #run - runs a compiled assembly");
                Output.WriteLine("  [...] #test - tests all instruction in a quotation");
                Output.WriteLine("  #ta - tests all defined instruction");
                Output.WriteLine("  [...] #e - shows the instruction editor");
            }
        }

        public class CommandHelp : PrimitiveFunction
        {
            public CommandHelp()
                : base("#h", "(function ~> )", "prints help about a command")
            { }

            public override void Eval(Executor exec)
            {
                MainClass.OutputHelp(exec.PopFunction());
            }
        }

        public class Expand : PrimitiveFunction
        {
            public Expand()
                : base("#i", "('A -> 'B) ~> ('A -> 'B)", "performs inline expansion")
            { }

            public override void Eval(Executor exec)
            {
                QuotedFunction qf = exec.PopFunction();
                exec.Push(Optimizer.Expand(qf, 5));
            }
        }

        public class Expand1 : PrimitiveFunction
        {
            public Expand1()
                : base("#i0", "('A -> 'B) ~> ('A -> 'B)", "performs inline expansion")
            { }

            public override void Eval(Executor exec)
            {
                QuotedFunction qf = exec.PopFunction();
                exec.Push(Optimizer.Expand(qf, 1));
            }
        }

        public class ApplyMacros : PrimitiveFunction
        {
            public ApplyMacros()
                : base("#m", "('A -> 'B) ~> ('A -> 'B)", "applies macros to a function")
            { }

            public override void Eval(Executor exec)
            {
                QuotedFunction qf = exec.PopFunction();
                exec.Push(Optimizer.ApplyMacros(qf));
            }
        }

        public class Compile : PrimitiveFunction
        {
            public Compile()
                : base("#c", "(('A -> 'B) -> Compilation)", "compiles a function")
            { 
            }

            public override void Eval(Executor exec)
            {
                QuotedFunction f = exec.TypedPop<QuotedFunction>();
                List<Function> list = new List<Function>(f.GetChildren().ToArray());
                Compilation c = new Compilation();
                c.Compile(list);
                exec.Push(c);
            }
        }

        public class Execute : PrimitiveFunction
        {
            public Execute()
                : base("#run", "(Compilation ~> 'B)", "executes a compiled function")
            {
            }

            public override void Eval(Executor exec)
            {
                Compilation c = exec.TypedPop<Compilation>();
                c.InvokeDefault(exec);
            }
        }

        public class PartialEval : PrimitiveFunction
        {
            public PartialEval()
                : base("#p", "('A -> 'B) -> ('A -> 'B)", "reduces a function through partial evaluation")
            {
            }

            public override void Eval(Executor exec)
            {
                QuotedFunction qf = exec.TypedPop<QuotedFunction>();
                exec.Push(Optimizer.PartialEval(qf));
            }
        }

        public class Optimize : PrimitiveFunction
        {
            public Optimize()
                : base("#o", "('A -> 'B) -> ('A -> 'B)", "optimizes a function using a combination of techniques")
            {
            }

            public override void Eval(Executor exec)
            {
                QuotedFunction qf = exec.TypedPop<QuotedFunction>();
                exec.Push(Optimizer.Optimize(qf));
            }
        }

        public class MakeHtmlHelp : PrimitiveFunction
        {
            public MakeHtmlHelp()
                : base("#html", "( ~> )", "outputs html documentation (deprecated)")
            {
            }

            public override void Eval(Executor exec)
            {
                MainClass.MakeHtmlHelp();
            }
        }

        public class MakeLibrary : PrimitiveFunction
        {
            public MakeLibrary()
                : base("#lib", "( ~> )", "outputs a kernel library with tests (deprecated)")
            {
            }

            public override void Eval(Executor exec)
            {
                MainClass.MakeLibrary();
            }
        }

        public class Clr : PrimitiveFunction
        {
            public Clr()
                : base("#clr", "('A ~> )", "removes all items from the stack")
            { }

            public override void Eval(Executor exec)
            {
                exec.GetStack().Clear();
            }
        }

        public class Test : PrimitiveFunction
        {
            public Test()
                : base("#test", "(function ~> )", "runs tests associated with the function")
            { }

            public override void Eval(Executor exec)
            {
                QuotedFunction q = exec.PopFunction();
                foreach (Function f in q.GetChildren())
                    f.RunTests();
            }
        }

        public class TestAll : PrimitiveFunction
        {
            public TestAll()
                : base("#ta", "( ~> )", "runs tests associated with all loaded functions")
            { }

            public override void Eval(Executor exec)
            {
                foreach (Function f in exec.GetGlobalContext().GetAllFunctions())
                    f.RunTests();
            }
        }

        public class Edit : PrimitiveFunction
        {
            public Edit()
                : base("#edit", "(function ~> )", "shows a function editor dialog box for a given function")
            { }

            public override void Eval(Executor exec)
            {
                QuotedFunction qf = exec.PopFunction();
                if ((qf.GetChildren().Count != 1) || !(qf.GetChildren()[0] is DefinedFunction))
                    throw new Exception("You can only edit single functions that have been defined");
                DefinedFunction f = EditDefForm.EditFunction(qf.GetChildren()[0] as DefinedFunction);
                if (f == null)
                {
                    Output.WriteLine("no function was defined");
                }
                else
                {
                    Output.WriteLine(f.GetName() + " was redefined");
                    Executor.Main.GetGlobalContext().AddFunction(f);
                }
            }
        }

        public class Def : PrimitiveFunction
        {
            public Def()
                : base("#def", "( ~> )", "shows a function editor dialog box for a given function")
            { }

            public override void Eval(Executor exec)
            {
                Function f = EditDefForm.DefineNewFunction();
                if (f == null)
                {
                    Output.WriteLine("no function was defined");
                }
                else
                {
                    Output.WriteLine(f.GetName() + " was defined");
                    Executor.Main.GetGlobalContext().AddFunction(f);
                }
            }
        }

        public class View : PrimitiveFunction
        {
            public View()
                : base("#v", "( ~> )", "displays all of the loaded functions visually")
            { }

            public override void Eval(Executor exec)
            {
                CodeViewForm.Show("");
            }
        }
    }

    public class Primitives
    {
        #region conversion functions
        public class Str : PrimitiveFunction
        {
            public Str()
                : base("str", "(any -> string)", "converts any value into a string representation.")
            { }

            public override void Eval(Executor exec)
            {
                exec.Push(Output.ObjectToString(exec.Pop()));
            }
        }

        public class MakeByte : PrimitiveFunction
        {
            public MakeByte()
                : base("to_byte", "(int -> byte)", "converts an integer into a byte, throwing away sign and ignoring higher bits")
            { }

            public override void Eval(Executor exec)
            {
                int n = exec.PopInt();
                byte b = (byte)n;
                exec.Push(b);
            }
        }
        public class BinStr : PrimitiveFunction
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

        public class HexStr : PrimitiveFunction
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

        #endregion 

        #region primitive function classes
        /// <summary>
        /// HACK: this is a big hack. But it works
        /// </summary>
        public class RecursiveCast : PrimitiveFunction
        {
            public RecursiveCast()
                : base("self_call", "('A ('A -> 'B) -> 'B)", "used to make self-calls type safe")
            { }

            public override void Eval(Executor exec)
            {
                // does nothing
            }
        }

        public class Halt : PrimitiveFunction
        {
            public Halt()
                : base("halt", "(int ~> )", "halts the program with an error code")
            { }

            public override void Eval(Executor exec)
            {
                int n = exec.PopInt();
                throw new Exception("Program halted with error code " + n.ToString());
            }
                
        }

        public class Id : PrimitiveFunction
        {
            public Id()
                : base("id", "('a -> 'a)", "does nothing, but requires one item on the stack.")
            { }

            public override void Eval(Executor exec)
            {                
            }
        }

        public class Eq : PrimitiveFunction
        {
            public Eq()
                : base("eq", "('a 'a -> bool)", "returns true if both items on stack have the same value")
            { }

            public override void Eval(Executor exec)
            {
                Object x = exec.Pop();
                Object y = exec.Pop();
                exec.Push(x.Equals(y));
            }
        }

        public class Dup : PrimitiveFunction
        {
            public Dup()
                : base("dup", "('a -> 'a 'a)", "duplicate the top item on the stack")
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

        public class Pop : PrimitiveFunction
        {
            public Pop()
                : base("pop", "('a -> )", "removes the top item from the stack")
            { }

            public override void Eval(Executor exec)
            {
                exec.Pop();
            }
        }

        public class Swap : PrimitiveFunction
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
        #endregion

        #region function functions
        public class EvalFxn : PrimitiveFunction
        {
            public EvalFxn()
                : base("apply", "('A ('A -> 'B) -> 'B)", "evaluates a function")
            { }

            public override void Eval(Executor exec)
            {
                Function f = exec.TypedPop<Function>();
                f.Eval(exec);
            }
        }

        public class ApplyOneFxn : PrimitiveFunction
        {
            public ApplyOneFxn()
                : base("A", "('a ('a -> 'b) -> 'b)", "applies a unary function to its argument")
            { }

            public override void Eval(Executor exec)
            {
                Function f = exec.TypedPop<Function>();
                f.Eval(exec);
            }
        }

        public class PartialEvalFxn : PrimitiveFunction
        {
            public PartialEvalFxn()
                : base("bind", "('a ('A 'a -> 'B) -> ('A -> 'B))", "binds the top argument to the top value in the stack, also know as partial-application")
            { }

            public override void Eval(Executor exec)
            {
                exec.Execute("swap quote swap compose");
            }
        }

        public class Dip : PrimitiveFunction
        {
            public Dip()
                : base("dip", "('A 'b ('A -> 'C) -> 'C 'b)", "evaluates a function, temporarily removing second item")
            { }

            public override void Eval(Executor exec)
            {
                Function f = exec.TypedPop<Function>();
                Object o = exec.Pop();
                f.Eval(exec);
                exec.Push(o);
            }
        }

        public class Compose : PrimitiveFunction
        {
            public Compose()
                : base("compose", "(('A -> 'B) ('B -> 'C) -> ('A -> 'C))",
                    "creates a function by composing (concatenating) two existing functions")
            { }

            public override void Eval(Executor exec)
            {
                QuotedFunction right = exec.TypedPop<QuotedFunction>();
                QuotedFunction left = exec.TypedPop<QuotedFunction>();
                QuotedFunction f = new QuotedFunction(left, right);
                exec.Push(f);
            }
        }

        public class Quote : PrimitiveFunction
        {
            public Quote()
                : base("quote", "('a -> ( -> 'a))",
                    "creates a constant generating function from the top value on the stack")
            { }

            public override void Eval(Executor exec)
            {
                Object o = exec.Pop();
                QuotedValue q = new QuotedValue(o);
                exec.Push(q);
            }
        }

        public class Dispatch3 : Dispatch
        {
            public Dispatch3()
                : base("dispatch3", "(any any any list -> any)")
            { }
        }

        public class Dispatch2 : Dispatch
        {
            public Dispatch2()
                : base("dispatch2", "(any any list -> any)")
            { }
        }

        public class Dispatch1 : Dispatch
        {
            public Dispatch1()
                : base("dispatch1", "(any list -> any)")
            { }
        }

        #endregion

        #region control flow primitives 
        public class CallCC : PrimitiveFunction
        {
            public CallCC()
                : base("callcc", "('A ('A ('B -> 'C) -> 'B) ~> 'B)", "calls a function with the current continuation")
            { }

            public override void Eval(Executor exec)            
            {
                throw new Exception("unimplemented");
                // TODO: make a copy of the stack, and a pointer to the current instruction. 
                // this implies that I need to make a copy of the index stream.
            }
        }

        public class While : PrimitiveFunction
        {
            public While()
                : base("while", "('A ('A -> 'A) ('A -> 'A bool) -> 'A)",
                    "executes a block of code repeatedly until the condition returns true")
            { }

            public override void Eval(Executor exec)
            {
                Function cond = exec.TypedPop<Function>();
                Function body = exec.TypedPop<Function>();

                cond.Eval(exec);
                while ((bool)exec.Pop())
                {
                    body.Eval(exec);
                    cond.Eval(exec);
                }
            }
        }

        public class If : PrimitiveFunction
        {
            public If()
                : base("if", "('A bool ('A -> 'B) ('A -> 'B) -> 'B)",
                    "executes one predicate or another whether the condition is true")
            { }

            public override void Eval(Executor exec)
            {
                Function onfalse = exec.TypedPop<Function>();
                Function ontrue = exec.TypedPop<Function>();

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

        public class BinRec : PrimitiveFunction
        {
            // The fact that it takes 'b instead of 'B is a minor optimization for untyped implementations
            // I may ignore it later on.
            public BinRec()
                : base("bin_rec", "('a ('a -> 'a bool) ('a -> 'b) ('a -> 'C 'a 'a) ('C 'b 'b -> 'b) -> 'b)",
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

        public class Throw : PrimitiveFunction
        {
            public Throw()
                : base("throw", "(any -> )", "throws an exception")
            { }

            public override void Eval(Executor exec)
            {
                object o = exec.Pop();
                throw new CatException(o);
            }
        }

        public class TryCatch : PrimitiveFunction
        {
            public TryCatch()
                : base("try_catch", "('A ('A -> 'B) ('A any -> 'B) -> 'B)", "evaluates a function, and catches any exceptions")
            { }

            public override void Eval(Executor exec)
            {
                Function c = exec.TypedPop<Function>();
                Function t = exec.TypedPop<Function>();
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

                    Output.WriteLine("exception caught");

                    exec.Push(e.GetObject());
                    c.Eval(exec);
                }
            }
        }
        #endregion 

        #region boolean functions
        public class True : PrimitiveFunction
        {
            public True()
                : base("true", "( -> bool)", "pushes the boolean value true on the stack")
            { }

            public override void Eval(Executor exec)
            {
                exec.Push(true);
            }
        }

        public class False : PrimitiveFunction
        {
            public False()
                : base("false", "( -> bool)", "pushes the boolean value false on the stack")
            { }

            public override void Eval(Executor exec)
            {
                exec.Push(false);
            }
        }

        public class And : PrimitiveFunction
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

        public class Or : PrimitiveFunction
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

        public class Not : PrimitiveFunction
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

        #region type functions
        public class TypeId : PrimitiveFunction
        {
            public TypeId()
                : base("type_of", "(any -> type)", "returns a type tag for an object")
            { }

            public override void Eval(Executor exec)
            {
                Object o = exec.Pop();
                if (o is FList)
                {
                    // HACK: this is not the correct type! 
                    exec.Push(typeof(FList));
                }
                else if (o is Function)
                {
                    exec.Push((o as Function).GetFxnType());
                }
                else
                {
                    // HACK: this is not the correct type! 
                    exec.Push(o.GetType());
                }
            }
        }
        public class TypeType : PrimitiveFunction
        {
            public TypeType()
                : base("type", "( -> type)", "")
            { }

            public override void Eval(Executor exec)
            {
                exec.Push(typeof(Type));
            }
        }
        public class IntType : PrimitiveFunction
        {
            public IntType()
                : base("int", "( -> type)", "")
            { }

            public override void Eval(Executor exec)
            {
                exec.Push(typeof(int));
            }
        }
        public class StrType : PrimitiveFunction
        {
            public StrType()
                : base("string", "( -> type)", "")
            { }

            public override void Eval(Executor exec)
            {
                exec.Push(typeof(string));
            }
        }
        public class DblType : PrimitiveFunction
        {
            public DblType()
                : base("double", "( -> type)", "")
            { }

            public override void Eval(Executor exec)
            {
                exec.Push(typeof(double));
            }
        }
        public class ByteType : PrimitiveFunction
        {
            public ByteType()
                : base("byte", "( -> type)", "")
            { }

            public override void Eval(Executor exec)
            {
                exec.Push(typeof(byte));
            }
        }
        public class BitType : PrimitiveFunction
        {
            public BitType()
                : base("bit", "( -> type)", "")
            { }

            public override void Eval(Executor exec)
            {
                exec.Push(typeof(Bit));
            }
        }
        public class BoolType : PrimitiveFunction
        {
            public BoolType()
                : base("bool", "( -> type)", "")
            { }

            public override void Eval(Executor exec)
            {
                exec.Push(typeof(Bit));
            }
        }
        public class TypeEq : PrimitiveFunction
        {
            public TypeEq()
                : base("type_eq", "(type type -> bool)", "returns true if either type is assignable to the other")
            { }

            public override void Eval(Executor exec)
            {
                Type t = exec.TypedPop<Type>();
                Type u = exec.TypedPop<Type>();
                exec.Push(t.Equals(u) || u.Equals(t));
            }
        }
        #endregion 

        #region int functions
        public class AddInt : PrimitiveFunction
        {
            public AddInt() : base("add_int", "(int int -> int)", "") { }            
            public override void Eval(Executor exec) { exec.Push(exec.PopInt() + exec.PopInt()); }
        }
        public class MulInt : PrimitiveFunction
        {
            public MulInt() : base("mul_int", "(int int -> int)", "") { }
            public override void Eval(Executor exec) { exec.Push(exec.PopInt() * exec.PopInt()); }
        }
        public class DivInt : PrimitiveFunction
        {
            public DivInt() : base("div_int", "(int int -> int)", "") { }
            public override void Eval(Executor exec) { exec.Swap(); exec.Push(exec.PopInt() / exec.PopInt()); }
        }
        public class SubInt : PrimitiveFunction
        {
            public SubInt() : base("sub_int", "(int int -> int)", "") { }
            public override void Eval(Executor exec) { exec.Swap();  exec.Push(exec.PopInt() - exec.PopInt()); }
        }
        public class ModInt : PrimitiveFunction
        {
            public ModInt() : base("mod_int", "(int int -> int)", "") { }
            public override void Eval(Executor exec) { exec.Swap();  exec.Push(exec.PopInt() % exec.PopInt()); }
        }
        public class NegInt : PrimitiveFunction
        {
            public NegInt() : base("neg_int", "(int -> int)", "") { }
            public override void Eval(Executor exec) { exec.Push(-exec.PopInt()); }
        }
        public class ComplInt : PrimitiveFunction
        {
            public ComplInt() : base("compl_int", "(int -> int)", "") { }
            public override void Eval(Executor exec) { exec.Push(~exec.PopInt()); }
        }
        public class ShlInt : PrimitiveFunction
        {
            public ShlInt() : base("shl_int", "(int int -> int)", "") { }
            public override void Eval(Executor exec) { exec.Swap(); exec.Push(exec.PopInt() << exec.PopInt()); }
        }
        public class ShrInt : PrimitiveFunction
        {
            public ShrInt() : base("shr_int", "(int int -> int)", "") { }
            public override void Eval(Executor exec) { exec.Swap(); exec.Push(exec.PopInt() >> exec.PopInt()); }
        }
        public class GtInt : PrimitiveFunction
        {
            public GtInt() : base("gt_int", "(int int -> bool)", "") { }
            public override void Eval(Executor exec) { exec.Swap(); exec.Push(exec.PopInt() > exec.PopInt()); }
        }
        public class LtInt : PrimitiveFunction
        {
            public LtInt() : base("lt_int", "(int int -> bool)", "") { }
            public override void Eval(Executor exec) { exec.Swap(); exec.Push(exec.PopInt() < exec.PopInt()); }
        }
        public class GtEqInt : PrimitiveFunction
        {
            public GtEqInt() : base("gteq_int", "(int int -> bool)", "") { }
            public override void Eval(Executor exec) { exec.Swap(); exec.Push(exec.PopInt() >= exec.PopInt()); }
        }
        public class LtEqInt : PrimitiveFunction
        {
            public LtEqInt() : base("lteq_int", "(int int -> bool)", "") { }
            public override void Eval(Executor exec) { exec.Swap();  exec.Push(exec.PopInt() <= exec.PopInt()); }
        }
        #endregion

        #region byte functions
        public static byte add_byte(byte x, byte y) { return (byte)(x + y); }
        public static byte sub_byte(byte x, byte y) { return (byte)(x - y); }
        public static byte div_byte(byte x, byte y) { return (byte)(x / y); }
        public static byte mul_byte(byte x, byte y) { return (byte)(x * y); }
        public static byte mod_byte(byte x, byte y) { return (byte)(x % y); }
        public static byte compl_byte(byte x) { return (byte)(~x); }
        public static byte shl_byte(byte x, byte y) { return (byte)(x << y); }
        public static byte shr_byte(byte x, byte y) { return (byte)(x >> y); }
        public static bool gt_byte(byte x, byte y) { return x > y; }
        public static bool lt_byte(byte x, byte y) { return x < y; }
        public static bool gteq_byte(byte x, byte y) { return x >= y; }
        public static bool lteq_byte(byte x, byte y) { return x <= y; }
        #endregion

        #region char functions
        public static int char_to_int(char c) { return (int)c; }
        public static char int_to_char(int n) { return (char)n; }
        public static string char_to_str(char c) { return c.ToString(); }
        #endregion

        #region bit functions
        public struct Bit
        {
            public bool m;
            public Bit(int n) { m = n != 0; }
            public Bit(bool x) { m = x; }
            public Bit add(Bit x) { return new Bit(m ^ x.m); }
            public Bit sub(Bit x) { return new Bit(m && !x.m); }
            public Bit mul(Bit x) { return new Bit(m && !x.m); }
            public Bit div(Bit x) { return new Bit(m && !x.m); }
            public Bit mod(Bit x) { return new Bit(m && !x.m); }
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
        public static Bit add_bit(Bit x, Bit y) { return x.add(y); }
        public static Bit sub_bit(Bit x, Bit y) { return x.sub(y); }
        public static Bit mul_bit(Bit x, Bit y) { return x.mul(y); }
        public static Bit div_bit(Bit x, Bit y) { return x.div(y); }
        public static Bit mod_bit(Bit x, Bit y) { return x.mod(y); }
        public static Bit compl_bit(Bit x) { return new Bit(!x.m); }
        public static bool neq_bit(Bit x, Bit y) { return !x.eq(y); }
        public static bool gt_bit(Bit x, Bit y) { return !x.lteq(y); }
        public static bool lt_bit(Bit x, Bit y) { return !x.eq(y) && x.lteq(y); }
        public static bool gteq_bit(Bit x, Bit y) { return x.eq(y) || !x.lteq(y); }
        public static bool lteq_bit(Bit x, Bit y) { return x.lteq(y); }
        public static Bit min_bit(Bit x, Bit y) { return new Bit(x.m && y.m); }
        public static Bit max_bit(Bit x, Bit y) { return new Bit(x.m || y.m); }
        #endregion

        #region double functions
        public static double add_dbl(double x, double y) { return x + y; }
        public static double sub_dbl(double x, double y) { return x - y; }
        public static double div_dbl(double x, double y) { return x / y; }
        public static double mul_dbl(double x, double y) { return x * y; }
        public static double mod_dbl(double x, double y) { return x % y; }
        public static double inc_dbl(double x) { return x + 1; }
        public static double dec_dbl(double x) { return x - 1; }
        public static double neg_dbl(double x) { return -x; }
        public static bool gt_dbl(double x, double y) { return x > y; }
        public static bool lt_dbl(double x, double y) { return x < y; }
        public static bool gteq_dbl(double x, double y) { return x >= y; }
        public static bool lteq_dbl(double x, double y) { return x <= y; }
        public static double min_dbl(double x, double y) { return Math.Min(x, y); }
        public static double max_dbl(double x, double y) { return Math.Max(x, y); }
        public static double abs_dbl(double x) { return Math.Abs(x); }
        public static double pow_dbl(double x, double y) { return Math.Pow(x, y); }
        public static double sqr_dbl(double x) { return x * x; }
        public static double sin_dbl(double x) { return Math.Sin(x); }
        public static double cos_dbl(double x) { return Math.Cos(x); }
        public static double tan_dbl(double x) { return Math.Tan(x); }
        public static double asin_dbl(double x) { return Math.Asin(x); }
        public static double acos_dbl(double x) { return Math.Acos(x); }
        public static double atan_dbl(double x) { return Math.Atan(x); }
        public static double atan2_dbl(double x, double y) { return Math.Atan2(x, y); }
        public static double sinh_dbl(double x) { return Math.Sinh(x); }
        public static double cosh_dbl(double x) { return Math.Cosh(x); }
        public static double tanh_dbl(double x) { return Math.Tanh(x); }
        public static double sqrt_dbl(double x) { return Math.Sqrt(x); }
        public static double trunc_dbl(double x) { return Math.Truncate(x); }
        public static double round_dbl(double x) { return Math.Round(x); }
        public static double ceil_dbl(double x) { return Math.Ceiling(x); }
        public static double floor_dbl(double x) { return Math.Floor(x); }
        public static double log_dbl(double x, double y) { return Math.Log(x, y); }
        public static double log10_dbl(double x) { return Math.Log10(x); }
        public static double ln_dbl(double x) { return Math.Log(x); }
        public static double e_dbl() { return Math.E; }
        public static double pi_dbl() { return Math.PI; }
        public static string format_scientific(double x) { return x.ToString("E"); }
        public static string format_currency(double x) { return x.ToString("C"); }
        #endregion

        #region string functions
        public static bool gt_str(string x, string y) { return x.CompareTo(y) > 0; }
        public static bool lt_str(string x, string y) { return x.CompareTo(y) < 0; }
        public static bool gteq_str(string x, string y) { return x.CompareTo(y) >= 0; }
        public static bool lteq_str(string x, string y) { return x.CompareTo(y) <= 0; }
        public static string min_str(string x, string y) { return lteq_str(x, y) ? x : y; }
        public static string max_str(string x, string y) { return gteq_str(x, y) ? x : y; }
        public static string add_str(string x, string y) { return x + y; }
        public static string sub_str(string x, int i, int n) { return x.Substring(i, n); }
        public static string new_str(char c, int n) { return new string(c, n); }
        public static int index_of(string x, string y) { return x.IndexOf(y); }
        public static string replace_str(string x, string y, string z) { return x.Replace(y, z); }
        public static FList str_to_array(string x) { return new FArray<char>(x.ToCharArray()); }
        #endregion

        #region console functions
        public class Write : PrimitiveFunction
        {
            public Write()
                : base("write", "('a ~> )", "outputs the text representation of a value to the console")
            { }

            public override void Eval(Executor exec)
            {
                Output.Write(exec.Pop());
            }
        }

        public class WriteLn : PrimitiveFunction
        {
            public WriteLn()
                : base("writeln", "('a ~> )", "outputs the text representation of a value to the console followed by a newline character")
            { }

            public override void Eval(Executor exec)
            {
                Output.WriteLine(exec.Pop());
            }
        }

        public class ReadLn : PrimitiveFunction
        {
            public ReadLn()
                : base("readln", "( ~> string)", "inputs a string from the user (or console)")
            { }

            public override void Eval(Executor exec)
            {
                exec.Push(Console.ReadLine());
            }
        }

        public class ReadKey : PrimitiveFunction
        {
            public ReadKey()
                : base("readch", "( ~> char)", "inputs a single character from the user (or console)")
            { }

            public override void Eval(Executor exec)
            {
                exec.Push(Console.ReadKey().KeyChar);
            }
        }
        #endregion

        #region byte block functions
        public class MakeByteBlock : PrimitiveFunction
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
        public class OpenFileReader : PrimitiveFunction
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

        public class OpenWriter : PrimitiveFunction
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

        public class FileExists : PrimitiveFunction
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

        public class TmpFileName : PrimitiveFunction
        {
            public TmpFileName()
                : base("temp_file", "( -> string)", "creates a unique temporary file")
            { }

            public override void Eval(Executor exec)
            {
                exec.Push(Path.GetTempFileName());
            }
        }

        public class ReadBytes : PrimitiveFunction
        {
            public ReadBytes()
                : base("read_bytes", "(istream int -> istream byte_block)", "reads a number of bytes into an array from an input stream")
            { }

            public override void Eval(Executor exec)
            {
                int n = exec.PopInt();
                Stream f = exec.TypedPeek<Stream>();
                byte[] ab = new byte[n];
                f.Read(ab, 0, n);
                exec.Push(new MArray<byte>(ab)); 
            }
        }

        public class WriteBytes : PrimitiveFunction
        {
            public WriteBytes()
                : base("write_bytes", "(ostream byte_block -> ostream)", "writes a byte array to an output stream")
            { }

            public override void Eval(Executor exec)
            {
                MArray<byte> mb = exec.TypedPop<MArray<byte>>();
                Stream f = exec.TypedPeek<Stream>();
                f.Write(mb.m, 0, mb.Count());
            }
        }

        public class CloseStream : PrimitiveFunction
        {
            public CloseStream()
                : base("close_stream", "(stream ~> )", "closes a stream")
            { }

            public override void Eval(Executor exec)
            {
                Stream f = exec.TypedPop<Stream>();
                f.Flush();
                f.Close();
                f.Dispose();
            }
        }
        #endregion

        #region hash functions
        public class MakeHashList : PrimitiveFunction
        {
            public MakeHashList()
                : base("hash_list", "( -> hash_list)", "makes an empty hash list")
            { }

            public override void Eval(Executor exec)
            {
                exec.Push(new HashList());
            }
        }

        public class HashGet : PrimitiveFunction
        {
            public HashGet()
                : base("hash_get", "(hash_list any -> hash_list any)", "gets a value from a hash list using a key")
            { }

            public override void Eval(Executor exec)
            {
                Object key = exec.Pop();
                HashList hash = exec.TypedPeek<HashList>();
                Object value = hash.Get(key);
                exec.Push(value);
            }
        }

        public class HashSet : PrimitiveFunction
        {
            public HashSet()
                : base("hash_set", "(hash_list any any -> hash_list)", "associates a value with a key in a hash list")
            { }

            public override void Eval(Executor exec)
            {
                Object value = exec.Pop();
                Object key = exec.Pop();
                HashList hash = exec.TypedPop<HashList>();
                exec.Push(hash.Set(key, value));
            }
        }

        public class HashAdd : PrimitiveFunction
        {
            public HashAdd()
                : base("hash_add", "(hash_list any any -> hash_list)", "associates a value with a key in a hash list")
            { }

            public override void Eval(Executor exec)
            {
                Object value = exec.Pop();
                Object key = exec.Pop();
                HashList hash = exec.TypedPop<HashList>();
                exec.Push(hash.Add(key, value));
            }
        }

        public class HashContains : PrimitiveFunction
        {
            public HashContains()
                : base("hash_contains", "(hash_list any -> hash_list bool)", "returns true if hash list contains key")
            { }

            public override void Eval(Executor exec)
            {
                Object key = exec.Pop();
                HashList hash = exec.TypedPeek<HashList>();
                exec.Push(hash.ContainsKey(key));
            }
        }

        public class HashToList : PrimitiveFunction
        {
            public HashToList()
                : base("hash_to_list", "(hash_list -> list)", "converts a hash_list to a list of pairs")
            { }

            public override void Eval(Executor exec)
            {
                HashList hash = exec.TypedPop<HashList>();
                exec.Push(hash.ToArray());
            }
        }
        #endregion 

        #region list functions
        public class List : PrimitiveFunction
        {
            public List()
                : base("@", "(( -> 'A) -> list)", "creates a list from a function")
            { }

            public override void Eval(Executor exec)
            {
                Function f = exec.TypedPop<Function>();
                CatStack stk = exec.GetStack();
                exec.SetStack(new CatStack());
                f.Eval(exec);
                FList list = exec.GetStack().ToList();
                exec.SetStack(stk);
                exec.Push(list);
            }
        }

        public class IsEmpty : PrimitiveFunction
        {
            public IsEmpty()
                : base("empty", "(list -> list bool)", "returns true if the list is empty")
            { }

            public override void Eval(Executor exec)
            {
                FList list = exec.TypedPeek<FList>();
                exec.Push(list.IsEmpty());
            }
        }

        public class Count : PrimitiveFunction
        {
            public Count()
                : base("count", "(list -> list int)", "returns the number of items in a list")
            { }

            public override void Eval(Executor exec)
            {
                FList list = exec.TypedPeek<FList>();
                exec.Push(list.Count());
            }
        }

        public class Nth : PrimitiveFunction
        {
            public Nth()
                : base("nth", "(list int -> list any)", "returns the nth item in a list")
            { }

            public override void Eval(Executor exec)
            {
                int n = exec.PopInt();
                FList list = exec.TypedPeek<FList>();
                exec.Push(list.Nth(n));
            }
        }

        public class Gen : PrimitiveFunction
        {
            public Gen()
                : base("gen", "('a ('a -> 'a) ('a -> bool) -> list)",
                    "creates a lazily evaluated list")
            { }

            public override void Eval(Executor exec)
            {
                Function term = exec.TypedPop<Function>();
                Function next = exec.TypedPop<Function>();
                Object init = exec.Pop();
                exec.Push(new Generator(init, next.ToMapFxn(), term.ToFilterFxn()));
            }
        }

        public class Nil : PrimitiveFunction
        {
            public Nil()
                : base("nil", "( -> list)", "creates an empty list")
            { }

            public override void  Eval(Executor exec)
            {
 	            exec.Push(FList.Nil());
            }
        }

        public class Unit : PrimitiveFunction
        {
            public Unit()
                : base("unit", "('a -> list)", "creates a list of one item")
            { }

            public override void  Eval(Executor exec)
            {
 	            exec.Push(FList.MakeUnit(exec.Pop()));
            }
        }

        public class MakePair : PrimitiveFunction
        {
            public MakePair()
                : base("pair", "('a 'b -> list)", "creates a list from two items")
            { }

            public override void Eval(Executor exec)
            {
                Object x = exec.Pop();
                Object y = exec.Pop();
 	            exec.Push(FList.MakePair(x, y));
            }
        }

        public class Cons : PrimitiveFunction
        {
            public Cons()
                : base("cons", "(list 'a -> list)", "prepends an item to a list")
            { }

            public override void Eval(Executor exec)
            {
                object x = exec.Pop();
                FList list = exec.TypedPop<FList>();
 	            exec.Push(FList.Cons(x, list));
            }
        }

        public class Head : PrimitiveFunction
        {
            public Head()
                : base("head", "(list -> any)", "replaces a list with the first item")
            { }

            public override void Eval(Executor exec)
            {
                FList list = exec.TypedPop<FList>();
 	            exec.Push(list.GetHead());
            }
        }

        public class First : PrimitiveFunction
        {
            public First()
                : base("first", "(list -> list any)", "gets the first item from a list")
            { }

            public override void Eval(Executor exec)
            {
                FList list = exec.TypedPeek<FList>();
                exec.Push(list.GetHead());
            }
        }

        public class Last : PrimitiveFunction
        {
            public Last()
                : base("last", "(list -> list any)", "gets the last item from a list")
            { }

            public override void Eval(Executor exec)
            {
                FList list = exec.TypedPeek<FList>();
 	            exec.Push(list.Last());
            }
        }

        public class Tail : PrimitiveFunction
        {
            public Tail()
                : base("tail", "(list -> list)", "removes first item from a list")
            { }

            public override void Eval(Executor exec)
            {
                FList list = exec.TypedPop<FList>();
                exec.Push(list.Tail());
            }
        }

        public class Rest : PrimitiveFunction
        {
            public Rest()
                : base("rest", "(list -> list list)", "gets a copy of the list with one item")
            { }

            public override void Eval(Executor exec)
            {
                FList list = exec.TypedPeek<FList>();
                exec.Push(list.Tail());
            }
        }

        public class Uncons : PrimitiveFunction
        {
            public Uncons()
                : base("uncons", "(list -> list any)", "returns the top of the list, and the rest of a list")
            {}

            public override void Eval(Executor exec)
            {
                FList list = exec.TypedPop<FList>();
                exec.Push(list.Tail());
                exec.Push(list.GetHead());
            }
        }

        public class Map : PrimitiveFunction
        {
            public Map()
                : base("map", "(list ('a -> 'b) -> list)", "creates a new list by modifying an existing list")
            { }

            public override void Eval(Executor exec)
            {
                Function f = exec.TypedPop<Function>();
                FList list = exec.TypedPop<FList>();
                exec.Push(list.Map(f.ToMapFxn()));
            }
        }

        public class MapFilter : PrimitiveFunction
        {
            public MapFilter()
                : base("map_filter", "(list ('a -> 'b) ('b -> bool) -> list)", "creates a new list by applying map then filter")
            { }

            public override void Eval(Executor exec)
            {
                Function fFilter = exec.TypedPop<Function>();
                Function fMap = exec.TypedPop<Function>();
                FList list = exec.TypedPop<FList>();
                exec.Push(list.Map(fMap.ToMapFxn()).Filter(fFilter.ToFilterFxn()));
            }
        }

        public class Filter : PrimitiveFunction
        {
            public Filter()
                : base("filter", "(list ('a -> bool) -> list)", "creates a new list containing elements that pass the condition")
            { }

            public override void Eval(Executor exec)
            {
                Function f = exec.TypedPop<Function>();
                FList list = exec.TypedPop<FList>();
                exec.Push(list.Filter(f.ToFilterFxn()));
            }
        }
        public class Fold : PrimitiveFunction
        {
            public Fold()
                : base("gfold", "('A list ('A 'b -> 'A) -> 'A)", "recursively applies a function to each element in a list")
            { }

            public override void Eval(Executor exec)
            {
                Function f = exec.TypedPop<Function>();
                FList list = exec.TypedPop<FList>();
                FList iter = list.GetIter();
                while (!iter.IsEmpty())
                {
                    exec.Push(iter.GetHead());
                    f.Eval(exec);
                    iter = iter.GotoNext();
                }
            }
        }

        public class Cat : PrimitiveFunction
        {
            public Cat()
                : base("cat", "(list list -> list)", "concatenates two lists")
            { }

            public override void Eval(Executor exec)
            {
                FList first = exec.TypedPop<FList>();
                FList second = exec.TypedPop<FList>();
                exec.Push(FList.Concat(first, second));
            }
        }

        public class TakeN : PrimitiveFunction
        {
            public TakeN()
                : base("take", "(list int -> list)", "creates a new list from the first n items")
            { }

            public override void Eval(Executor exec)
            {
                int n = exec.PopInt();
                FList list = exec.TypedPop<FList>();
                exec.Push(list.TakeN(n));
            }
        }

        public class DropN : PrimitiveFunction
        {
            public DropN()
                : base("drop", "(list int -> list)", "creates a new list without the first n items")
            { }

            public override void Eval(Executor exec)
            {
                int n = exec.PopInt();
                FList list = exec.TypedPop<FList>();
                exec.Push(list.DropN(n));
            }
        }

        public class TakeRange : PrimitiveFunction
        {
            public TakeRange()
                : base("take_range", "(list int int -> list)", "creates a new list which is a sub-range of the original")
            { }

            public override void Eval(Executor exec)
            {
                int count = exec.PopInt();
                int n = exec.PopInt();
                FList list = exec.TypedPop<FList>();
                exec.Push(list.TakeRange(n, count));
            }
        }

        public class TakeWhile : PrimitiveFunction
        {
            public TakeWhile()
                : base("take_while", "(list ('a -> bool) -> list)", "creates a new list by taking items while the predicate is true")
            { }

            public override void Eval(Executor exec)
            {
                Function f = exec.TypedPop<Function>();
                FList list = exec.TypedPop<FList>();
                exec.Push(list.TakeWhile(f.ToFilterFxn()));
            }
        }

        public class DropWhile : PrimitiveFunction
        {
            public DropWhile()
                : base("drop_while", "(list ('a -> bool) -> list)", "creates a new list by dropping items while the predicate is true")
            { }

            public override void Eval(Executor exec)
            {
                Function f = exec.TypedPop<Function>();
                FList list = exec.TypedPop<FList>();
                exec.Push(list.DropWhile(f.ToFilterFxn()));
            }
        }

        public class CountWhile : PrimitiveFunction
        {
            public CountWhile()
                : base("count_while", "(list ('a -> bool) -> list int)", "creates a new list by dropping items while the predicate is true")
            { }

            public override void Eval(Executor exec)
            {
                Function f = exec.TypedPop<Function>();
                FList list = exec.TypedPeek<FList>();
                exec.Push(list.CountWhile(f.ToFilterFxn()));
            }
        }

        public class RangeGen : PrimitiveFunction
        {
            public RangeGen()
                : base("range_gen", "(int int (int -> 'a) -> list)", 
                    "creates a lazy list from a range of numbers and a generating function")
            { }

            public override void Eval(Executor exec)
            {
                Function f = exec.TypedPop<Function>();
                int count = exec.PopInt();
                int n = exec.PopInt();
                exec.Push(FList.RangeGen(f.ToRangeGenFxn(), n, count));
            }
        }

        public class Repeater : PrimitiveFunction
        {
            public Repeater()
                : base("repeater", "('a -> list)", 
                    "creates a lazy list by repeating a value over and over again")
            { }

            public override void Eval(Executor exec)
            {
                Object o = exec.Pop();
                exec.Push(FList.MakeRepeater(o));
            }
        }

        public class Flatten : PrimitiveFunction
        {
            public Flatten()
                : base("flatten", "(list -> list)", "concatenates all sub-lists in a list of lists")
            { }

            public override void Eval(Executor exec)
            {
                FList list = exec.TypedPop<FList>();
                exec.Push(list.Flatten());
            }
        }
        #endregion

        #region mutable list instructions
        public class SetAt : PrimitiveFunction
        {
            public SetAt()
                : base("set_at", "(list 'a int -> list)", "sets an item in a list")
            { }

            public override void Eval(Executor exec)
            {
                int n = exec.PopInt();
                Object o = exec.Pop();
                if (exec.Peek() is FMutableList)
                {
                    FMutableList list = exec.TypedPeek<FMutableList>();
                    list.Set(n, o);
                }
                else
                {
                    FList list = exec.TypedPop<FList>();
                    FMutableList mut = new MArray<Object>(list);
                }
            }
        }
        #endregion 

        #region misc functions
        public class RandomInt : PrimitiveFunction
        {
            static Random mGen = new Random();

            public RandomInt()
                : base("rnd_int", "(int ~> int)", "creates a random integer between zero and some maximum value")
            { }

            public override void Eval(Executor exec)
            {
                exec.Push(mGen.Next(exec.PopInt()));
            }
        }

        public class RandomDbl : PrimitiveFunction
        {
            static Random mGen = new Random();

            public RandomDbl()
                : base("rnd_dbl", "( ~> double)", "creates a random floating point number between zero and 1.0")
            { }

            public override void Eval(Executor exec)
            {
                exec.Push(mGen.NextDouble());
            }
        }

        public class Null : PrimitiveFunction
        {
            public Null()
                : base("null", "( -> " + CatClass.GetNullType() + ")", "returns the default object with no fields")
            { }

            public override void Eval(Executor exec)
            {
                exec.Push(CatObject.GetNullObject());
            }
        }
        #endregion 

        #region casting functions
        public class AsVar : PrimitiveFunction
        {
            public AsVar()
                : base("as_var", "('a -> var)", "converts anything into a variant")
            { }

            public override void Eval(Executor exec)
            {
                // does nothing.
            }
        }

        public class AsBool : PrimitiveFunction
        {
            public AsBool()
                : base("as_bool", "(any -> bool)", "converts a variant to a bool")
            { }

            public override void Eval(Executor exec)
            {
                exec.Push(exec.TypedPop<bool>());
            }
        }

        public class AsInt : PrimitiveFunction
        {
            public AsInt()
                : base("as_int", "(any -> int)", "converts a variant to an int")
            { }

            public override void Eval(Executor exec)
            {
                exec.Push(exec.TypedPop<int>());
            }
        }

        public class AsList : PrimitiveFunction
        {
            public AsList()
                : base("as_list", "(any -> list)", "converts a variant to a list")
            { }

            public override void Eval(Executor exec)
            {
                exec.Push(exec.TypedPop<FList>());
            }
        }

        public class AsString : PrimitiveFunction
        {
            public AsString()
                : base("as_string", "(any -> string)", "converts a variant to a char")
            { }

            public override void Eval(Executor exec)
            {
                exec.Push(exec.TypedPop<string>());
            }
        }

        public class AsDbl : PrimitiveFunction
        {
            public AsDbl()
                : base("as_dbl", "(any -> double)", "converts a variant to a double")
            { }

            public override void Eval(Executor exec)
            {
                exec.Push(exec.TypedPop<double>());
            }
        }

        public class AsChar : PrimitiveFunction
        {
            public AsChar()
                : base("as_char", "(any -> char)", "converts a variant to a double")
            { }

            public override void Eval(Executor exec)
            {
                exec.Push(exec.TypedPop<double>());
            }
        }        
        #endregion

        #region graphics primitives 
        public class OpenWindow : PrimitiveFunction
        {
            public OpenWindow()
                : base("open_window", "( ~> )", "opens a drawing window")
            { }

            public override void Eval(Executor exec)
            {
                WindowGDI.OpenWindow();
            }
        }

        public class SaveWindow  : PrimitiveFunction
        {
            public SaveWindow()
                : base("save_window", "(string ~> )", "saves a bitmap of the viewport")
            { }

            public override void Eval(Executor exec)
            {
                WindowGDI.SaveToFile(exec.TypedPop<string>());
            }
        }

        public class CloseWindow : PrimitiveFunction
        {
            public CloseWindow()
                : base("close_window", "( ~> )", "close a drawing window")
            { }

            public override void Eval(Executor exec)
            {
                WindowGDI.CloseWindow();
            }
        }

        public class ClearWindow : PrimitiveFunction
        {
            public ClearWindow()
                : base("clear", "( ~> )", "clears the drawing window")
            { }

            public override void Eval(Executor exec)
            {
                WindowGDI.ClearWindow();
            }
        }
        
        /**
         *  Not currently used. 
         * 
        public class GetGdi : PrimitiveFunction
        {
            public GetGdi()
                : base("get_window", "( ~> gdi)", "gets the active graphics device interface")
            { }

            public override void Eval(Executor exec)
            {
                // Note: this is a placemark for later, when an actual gdi is supported
                exec.Push("gdi");
            }            
        }
         */

        public class Render : PrimitiveFunction
        {
            public Render()
                : base("render", "(list string ~> )", "sends a drawing instruction to the graphics device")
            { }

            public override void Eval(Executor exec)
            {
                string s = exec.TypedPop<string>();
                FList f = exec.TypedPop<FList>();
                Object[] args = f.GetObjectArray();
                GraphicCommand c = new GraphicCommand(s, args);
                WindowGDI.Render(c);
            }
        }

        #endregion

        #region .NET (CLR) reflection API

        public class Invoke : PrimitiveFunction
        {
            public Invoke()
                : base("clr_invoke", "(any list string -> any any)", "calls a method on a .NET object")
            { }

            public override void Eval(Executor exec)
            {
                string s = exec.TypedPop<string>();
                FList a = exec.TypedPop<FList>();
                Object self = exec.Peek();
                MethodInfo m = self.GetType().GetMethod(s, a.GetTypeArray());
                if (m == null)
                    throw new Exception("could not find method " + s + " on object of type " + self.GetType().ToString() + " with matching types");
                Object o = m.Invoke(self, a.GetObjectArray());
                exec.Push(o);
            }
        }

        public class SetField : PrimitiveFunction
        {
            public SetField()
                : base("clr_set_field", "(any any string -> any)", "assigns a value to a field of a .NET object")
            { }

            public override void Eval(Executor exec)
            {
                string s = exec.TypedPop<string>();
                Object val = exec.Pop();
                Object self = exec.Peek();
                FieldInfo fi = self.GetType().GetField(s);
                if (fi == null)
                    throw new Exception("could not find field " + s + " on object of type " + self.GetType().ToString());
                fi.SetValue(self, val);
            }
        }

        public class GetField : PrimitiveFunction
        {
            public GetField()
                : base("clr_get_field", "(any string -> any any)", "retrieves the value of a field from a .NET object")
            { }

            public override void Eval(Executor exec)
            {
                string s = exec.TypedPop<string>();
                Object self = exec.Peek();
                FieldInfo fi = self.GetType().GetField(s);
                if (fi == null)
                    throw new Exception("could not find field " + s + " on object of type " + self.GetType().ToString());
                exec.Push(fi.GetValue(self));
            }
        }

        public class ListFields : PrimitiveFunction
        {
            public ListFields()
                : base("clr_list_fields", "(any -> any list)", "retrieves a list of field names from a .NET object")
            { }

            public override void Eval(Executor exec)
            {
                Object self = exec.Peek();
                List<string> list = new List<string>();
                FieldInfo[] fis = self.GetType().GetFields();
                foreach (FieldInfo fi in fis)
                    list.Add(fi.Name);
                string[] a = list.ToArray();
                exec.Push(new FArray<string>(a));
            }
        }

        public class ListMethods : PrimitiveFunction
        {
            public ListMethods()
                : base("clr_list_methods", "(any -> any list)", "retrieves a list of field names from a .NET object")
            { }

            public override void Eval(Executor exec)
            {
                Object self = exec.Peek();
                List<string> list = new List<string>();
                FieldInfo[] fis = self.GetType().GetFields();
                foreach (FieldInfo fi in fis)
                    list.Add(fi.Name);
                string[] a = list.ToArray();
                exec.Push(new FArray<string>(a));
            }
        }

        public class New : PrimitiveFunction
        {
            public New()
                : base("clr_new", "(list string -> any)", "constructs a new .NET object")
            { }

            public override void Eval(Executor exec)
            {
                string s = exec.TypedPop<string>();
                FList a = exec.TypedPop<FList>();
                Type t = Type.GetType(s);
                if (t == null)
                    throw new Exception("could not find type " + s);
                ConstructorInfo c = t.GetConstructor(a.GetTypeArray());
                if (c == null)
                    throw new Exception("could not find constructor for object of type " + t.ToString() + " with matching types");
                Object o = c.Invoke(a.GetObjectArray());
                if (o == null)
                    throw new Exception(s + " object could not be constructed");
                exec.Push(o);
            }
        }
        #endregion
    }
}
