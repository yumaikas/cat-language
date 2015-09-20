# Purity #

A pure [expression](Expression.md) is an expression that has no [side-effects](SideEffect.md). An expression is pure if and only if all sub-expressions are pure. [Quotations](Quotation.md) and [literals](Literal.md) are always pure. A quotation may be impure if evaluated.

A pure function is a function which consists of only pure expressions.

A pure expression is [referentially transparent](ReferentialTransparency.md).

Pure functions have a type expressed as: `(Consumption -> Production)` where as impure functions have a type expressed as `(Consumption ~> Production)`.

A language is pure if only pure expressions can be expressed using it. The subset of Cat consisting of only [Level0](Level.md) and [Level1](Level.md) [primitives](Primitives.md) is pure.


