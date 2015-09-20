# Optimization #

One of the major design goals of Cat is as an [intermediate language](IntermediateLanguage.md) for optimization. The Cat interpreter provides three tools for optimization: a [sub-expression evaluator](PartialEvaluation.md), [inline function expander](InlineExpansion.md), and a [macro rewriting system](MetaCat.md).

The various optimization tools are controlled by the [meta-commands](MetaCommand.md) `#p` for the [partial evaluator](PartialEvaluation.md), `#i` for [inline expansion](InlineExpansion.md), `#m` for [macro application](MetaCat.md), and `#o` for optimization.

The `#o` optimization meta-command, rewrites [quotations](Quotation.md) on the stack using a combination of different techniques.




