using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace Cat
{
    /// <summary>
    /// Constructs a dynamic assembly from a Cat function.
    /// </summary>
    public class Compilation
    {
        public class CompiledFunction : Function
        {
            MethodBase mMethod;

            public CompiledFunction(MethodBase mi) 
                : base(mi.Name)
            {
                mMethod = mi;
            }

            public override void Eval(Executor exec)
            {
                mMethod.Invoke(null, new object[] { exec });
            }
        }

        public static int gnId = 0;
        public static Type[] gParamTypes = new Type[] { typeof(Executor) };
        
        AssemblyName mAsmName = new AssemblyName();
        AssemblyBuilder mAssembly;
        ModuleBuilder mModule;
        TypeBuilder mTypeBldr;
        MethodBuilder mMainBldr;
        MethodBuilder mDefaultBldr;
        Dictionary<string, MethodBuilder> mpBuiltFxns = new Dictionary<string, MethodBuilder>();
        Type mType;

        public static void Test1(Executor exec)
        {
            exec.Push(42);
        }

        public static void Test2(Executor exec)
        {
            Console.WriteLine(exec.PopInt());
        }

        public static void Test3(Executor exec)
        {           
            int n = exec.PopInt();
            exec.Push(exec.PopInt() + n);
        }        
        
        public static string GetNewAnonName()
        {
            return "_anon" + (gnId++).ToString();
        }

        private MethodBuilder NewAnonMethodBldr()
        {
            return mTypeBldr.DefineMethod(GetNewAnonName(), MethodAttributes.Public | MethodAttributes.Static, typeof(void), gParamTypes);
        }

        public void InvokeMain()
        {
            // Is it really neccessary to use "InvokeMember"? 
            mType.InvokeMember("Main", BindingFlags.InvokeMethod, null, null, new object[] { } );
        }

        public void InvokeDefault(Executor exec)
        {
            mType.InvokeMember("Default", BindingFlags.InvokeMethod, null, null, new object[] { exec } );
        }

        public void EmitCall<T>(ILGenerator ilg, string s)
        {
            MethodInfo mi = typeof(T).GetMethod(s);
            if (mi == null) 
                throw new Exception("Could not find method Executor." + s);
            ilg.EmitCall(OpCodes.Call, mi, null);
        }

        public MethodBuilder EmitFunction(List<Function> fxns)
        {
            MethodBuilder anon = NewAnonMethodBldr();
            ILGenerator ilg = anon.GetILGenerator();
            foreach (Function f in fxns)
                EmitCatCall(ilg, f);
            ilg.Emit(OpCodes.Ret);
            return anon;
        }

        public static Function FunctionFromMethodHandle(RuntimeMethodHandle mh)
        {
            return new CompiledFunction(MethodBase.GetMethodFromHandle(mh));   
        }

        public void EmitCatCall(ILGenerator ilg, Function f)
        {
            if (f == null)
                throw new Exception("null pointer error");

            if (f is PushValue<int>)
            {
                // Push the executor
                ilg.Emit(OpCodes.Ldarg_0);
                int n = (f as PushValue<int>).GetValue();

                // Push a constant
                ilg.Emit(OpCodes.Ldc_I4, n);
                EmitCall<Executor>(ilg, "PushInt");
            }
            else if (f is Quotation)
            {
                // Push the executor
                ilg.Emit(OpCodes.Ldarg_0);

                MethodBuilder mb = EmitFunction((f as Quotation).GetChildren());
                ilg.Emit(OpCodes.Ldtoken, mb);

                MethodInfo mh_to_fxn = typeof(Compilation).GetMethod("FunctionFromMethodHandle");
                if (mh_to_fxn == null) 
                    throw new Exception("Could not find FunctionFromMethodHandle");
                ilg.EmitCall(OpCodes.Call, mh_to_fxn, null);

                MethodInfo push = typeof(Executor).GetMethod("Push");
                if (push == null)
                    throw new Exception("Could not find Push");
                ilg.EmitCall(OpCodes.Call, typeof(Executor).GetMethod("Push"), null);
            }
            else if (f is DefinedFunction)
            {
                // Push the executor
                ilg.Emit(OpCodes.Ldarg_0);

                // get the name and lookup everything
                string sName = f.GetName();
                MethodBuilder mb;

                // Look to see if the function is already emitted
                if (!mpBuiltFxns.ContainsKey(sName))
                {
                    // Emit the code for the function
                    mb = EmitFunction((f as DefinedFunction).GetChildren());

                    // Add it for later look-up
                    mpBuiltFxns.Add(sName, mb);
                }
                else
                {
                    // Get a pointer to the previously emitted function
                    mb = mpBuiltFxns[sName];                
                }

                ilg.EmitCall(OpCodes.Call, mb, new Type[] { });
            }
            else if (f is PrimitiveFunction)
            {
                // Every Cat function has one argument: a stack. So every function call
                // takes the function as an argument places it on the stack, and calls the next function.
                Type t = f.GetType();

                // The constructor here is awful.
                // I could memoize it or something else.
                ConstructorInfo ci = t.GetConstructor(new Type[] { });
                if (ci == null)
                    throw new Exception("could not find default constructor for function " + f.ToString());
                ilg.Emit(OpCodes.Newobj, ci);

                // Push the exector 
                ilg.Emit(OpCodes.Ldarg_0);

                // Call the "Eval" method of a function
                MethodInfo mi = t.GetMethod("Eval");
                if (mi == null)
                    throw new Exception("Could not find evaluate function");
                ilg.EmitCall(OpCodes.Call, mi, null);
            }
            else
            {
                throw new Exception("function type not handled " + f.GetType().ToString() + " for function " + f.ToString());
            }
        }

        public Type Compile(List<Function> fxns)
        {
            // Compute the various names
            string sFileName = "out.exe";
            string sAsmName = "generated_assembly";
            string sNameSpace = "Cat";
            string sTypeName = "generated_type";
            string sModuleName = "generated_module";
            string sFullName = sNameSpace + "." + sTypeName;

            // Create a dynamic wrapper assembly around a module around a type.
            mAsmName.Name = sAsmName;
            mAssembly = AppDomain.CurrentDomain.DefineDynamicAssembly(mAsmName, AssemblyBuilderAccess.RunAndSave);
            mModule = mAssembly.DefineDynamicModule(sModuleName, sFileName);
            mTypeBldr = mModule.DefineType(sTypeName);
            mMainBldr = mTypeBldr.DefineMethod("Main", MethodAttributes.Public | MethodAttributes.Static, typeof(void), null);
            mDefaultBldr = mTypeBldr.DefineMethod("Default", MethodAttributes.Public | MethodAttributes.Static, typeof(void), gParamTypes);

            // Construct the entry point function
            //
            //   public static void Main() { 
            //      Default(new Executor()); 
            //   }
            //
            ILGenerator ilg = mMainBldr.GetILGenerator();
            LocalBuilder exec = ilg.DeclareLocal(typeof(Executor));
            ilg.Emit(OpCodes.Newobj, typeof(Executor).GetConstructor(new Type[] { }));
            ilg.EmitCall(OpCodes.Call, mDefaultBldr, null);
            ilg.Emit(OpCodes.Ret);
                        
            ilg = mDefaultBldr.GetILGenerator();

            foreach (Function f in fxns) {
                EmitCatCall(ilg, f);
            }

            // Add final return opcode
            ilg.Emit(OpCodes.Ret);
            
            // Finalize the construction of global functions
            mType = mTypeBldr.CreateType();

            // set the entrypoint (thereby declaring it an EXE)
            //ab.SetEntryPoint(fxb, PEFileKinds.ConsoleApplication);            
            mAssembly.SetEntryPoint(mMainBldr);

            // Save the compilation
            mAssembly.Save(sFileName);
            //MainClass.WriteLine("Saved compiled target to " + mAsmName.FullName);

            return mType;
        }
    }

    
}
