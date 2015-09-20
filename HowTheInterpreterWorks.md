# Introduction #

The Cat interpreter is written in C# and is completely public domain. One of the intentions of the Cat project is to help other people develop their own compilers and interpreters, by providing a simple open-source and non-toy implementation of a programming language interpreter to use and play with.

This document provides the so-called "ten thousand foot view" of how the interpreter works.

## Cat Interpreter ##

This is pseudo-code that shows how the interpreter works:

```
Interpreter()
  RegisterPrimitivesInLookupTable()
  ParsingExpressionGrammar g = MakeGrammar()
  ParsingExpressionGrammarParser p = MakeParser(g)
  loop 
    string s = GetInput()
    try
      GenericAbstractSyntaxTree ast = p.Parse(s)
      CatAbstractSyntaxTree cat_ast = MakeCatAst(ast);           
      // only process the top-level nodes
      foreach TopLevelNode node in cat_ast do
        ProcessNode(node)
    except
      ReportError

ProcessNode(CatAstNode node)
  if (node is Definition)
    AddDefintion(node as Definition)
  else if (node is Expression)
    ProcessExpression(node is Expression)  

ProcessDefinition(CatDefinitionNode def)
  AddToFxnLookup(def.GetName(), def.GetExpressions())

ProcessExpression(CatExpressionNode expr)
  if (expr is Number)
    stack.Push(expr.GetNumber)
  else if (expr as String)
    stack.Push(expr.GetString)
  else if (expr as FunctionName)
    CatExpressionNode[] expr_list = FxnLookup(expr.GetFunctionName())
    foreach (node in expr_list)
      ProcessExpression(expr)
  else if (expr is QuotedFxn)
    stack.Push(expr.GetQuotedFxn)   
```

## From Psuedo-Code to Real Code ##

The following list describes how the pseudo-code maps to an actual implementation:

  * The main interpreter loop can be found in the [MainClass.Main](http://cat-language.googlecode.com/svn/trunk/Main.cs) method.

  * Registration refers to adding [functions](http://cat-language.googlecode.com/svn/trunk/Functions.cs) to a lookup table which is stored in a [scope object](http://cat-language.googlecode.com/svn/trunk/Scope.cs). This is done in the function MainClass.RegisterPrimitive in the file [Main.cs](http://cat-language.googlecode.com/svn/trunk/Main.cs).

  * [Primitives](http://cat-language.googlecode.com/svn/trunk/Primitives.cs) are defined as either a class inheriting from [Function class](http://cat-language.googlecode.com/svn/trunk/Functions.cs). The interpreter also uses the Reflection API (in [Scope.RegisterType](http://cat-language.googlecode.com/svn/trunk/Scope.cs)) to iterate through all the methods of a class and create primitive objects from them. This makes it easy to extend the Cat language with new functions and types.

  * Primitive instructions that are defined as [methods](http://cat-language.googlecode.com/svn/trunk/Method.cs) of a type registered through the [Scope.RegisterType](http://cat-language.googlecode.com/svn/trunk/Scope.cs) function are invoked by popping values from the stack and treating them as arguments (in reverse order), and the result of invocation is pushed onto the execution stack. If the method is non-static then an implicit "this" pointer is popped from the stack last, and pushed onto the stack, before the result. Methods can not return multiple values, so not all primitives can be expressed as methods.

  * Scope is a bit of a misnomer, because Cat doesn't actually support scoping. I used Scope objects as a contingency plan in case scoping is added to a later version, and as a way of managing lookup tables in a reasonable manner.

  * The [Cat grammar](http://cat-language.googlecode.com/svn/trunk/CatGrammar.cs) is a [parsing expression grammar](http://cat-language.googlecode.com/svn/trunk/PegBaseGrammar.cs), or PEG grammar, that precisely describes the syntax of the language. A Grammar object contains a set of production rules that combined describe sytactically valid phrases in the language. Production rules are in reality functions that return a pattern matching object called a [Rule](http://cat-language.googlecode.com/svn/trunk/PegBaseGrammar.cs).

  * [Rule objects](http://cat-language.googlecode.com/svn/trunk/PegBaseGrammar.cs) have a single abstract function called Match that take a [parser](http://cat-language.googlecode.com/svn/trunk/PegParser.cs) and returns true or false depending on whether the pattern matches or not. On failure, a rule will also tell the parser to back-up to its previous position.

  * Rules can be combined using special rule operators, to create new rules. The common rule operators are:
    * ChoiceRule - Succeeds only if any rule of an associated group of rules matches
    * SeqRule - Succeeds only if each rule in a list of rules matches in order
    * OptRule - Tries to match a rule, but returns false even if it fails
    * StarRule - Tries to match a rule as many times as possible, but always returns true
    * NotRule - Tries to match a rule but returns true only if that rule fails. It never advances the parser
    * PlusRule - Tries to match a rule as many times as possible and returns true if it succeeds at least once.

  * There are a couple of special rule operators which do other things than simply match rules
    * The special rule object [AstNodeRule](http://cat-language.googlecode.com/svn/trunk/PegBaseGrammar.cs) is associated with another rule to describe the construction of a node in the abstract syntax tree. If the associated pattern is matched successfully then  a node is created.
    * The special rule object [NoFailRule](http://cat-language.googlecode.com/svn/trunk/PegBaseGrammar.cs) prevents back-tracking from a previous point. For example you can use it to say I know that we are trying to match a function because we passed the words "define", so if it fails to match a function, don't bother trying to match anything else. This is essentially a bailing out strategy for the interpreter.

  * A [parsing object](http://cat-language.googlecode.com/svn/trunk/PegParser.cs) stores the current state of the parser. It contains a pointer to the input, a position indicator, and it contains the [abstract syntax tree](http://cat-language.googlecode.com/svn/trunk/PegAst.cs). It also contains a string of text with the next several characters in input, so during debugging, you have a snapshot view of the input context.

  * Traditionally parsing was done by compilers in two passes (known as the tokenization phase, and the lexing phase), but in Cat is done in a single pass. This is an advantage gained by using a recursive-descent parser and a PEG grammar.

  * During parsing (i.e. while rules are being matched) the [AstNodeRule](http://cat-language.googlecode.com/svn/trunk/PegBaseGrammar.cs), if successfully matched, will add nodes to a tree called the AST or Abstract Syntax Tree. The node will contain pointers to other nodes that are created within any sub-rules, and will contain a string that corresponds to the precise section of text that was matched. Many rules in a grammar correspond to things that an interpreter doesn't care about (e.g. white space, comments, code boundaries), so the AST when completed should contain just enough information about the input to be able to interpret or compile the code successfully.

  * The generated abstract syntax tree has no useful type information. The nodes are all instances of [AstNode](http://cat-language.googlecode.com/svn/trunk/CatAst.cs). This is in part so that the parsing library can be separated and used elsewhere. What is more useful for compiling or interpreting is to convert the tree into a tree with specific domain knowledge about the language. This is done in the file [CatAst.cs](http://cat-language.googlecode.com/svn/trunk/CatAst.cs). This typed AST also serves as a way to catch errors in the grammar, by double-checking the structure of the generic AST while constructing the typed AST.

  * The resulting typed AST now contains very specific and useful information about the structure of the input. Nodes correspond to things like function definitions, and make it easy to get the name of the defined function, the expressions in the defined function

  * The final step is to execute the contents of the AST. This is done by visiting nodes in the tree, and either adding function definitions to the lookup table (e.g. through [Scope.AddFunction](http://cat-language.googlecode.com/svn/trunk/Scope.cs)) or to actually execute expressions, in the case of an interpreter, or to convert to an output format, in the case of a compiler.

  * The execution of a function is performed from an [Executor object](http://cat-language.googlecode.com/svn/trunk/Executor.cs) that calls the [Function.Eval](http://cat-language.googlecode.com/svn/trunk/Functions.cs) method passing a [CatStack object](http://cat-language.googlecode.com/svn/trunk/CatStack.cs).

  * Cat expressions are either literals (i.e. integers, floating point numbers, strings, characters), function names (note: in Cat symbols like + or >= are also considered function names), or a quotation (i.e. a set of expressions enclosed with "[" and "]").
    * Literals are pushed onto the executing stack immediately
    * Function names are looked up (according to the types on the stack) and their bodies (list of expressions) are executed
    * Quotations (anonymous functions) are pushed onto the stack