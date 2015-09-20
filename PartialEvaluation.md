# Partial Evaluation #

Partial evaluation refers to the evaluation of sub-expressions. Given the [expression](Expression.md) `[3 4 * +] #p` a compiler will replace the sub-expression `3 4 *` with the value `12` so that the result is: `[12 +]`.

Only [pure](Purity.md) expressions can pre-evaluated.

The [meta-command](MetaCommand.md) for performing partial-evaluation is `#p`.
