# Referential Transparency #

Refential transparency is the property of a term A that it may be replaced by any other term B if term B accepts precisely the same set of inputs as term A and given any input B will produce the precisely same output as A. Refential transparency can be thought of as a functional or behavioral equivalence.

For example `[1 2 + +]` is referentially transparent and can be replaced with `[3 +]`.

Only [functions](Function.md) or [expressions](Expression.md) with no [side-effects](SideEffect.md) (in other words are [pure](Purity.md)) are referentially transparent.

Refential transparency is an important property to identify for [optimization](Optimization.md).
