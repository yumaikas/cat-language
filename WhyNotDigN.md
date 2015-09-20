# Why isn't there a digN or buryN instruction? #

Many people ask why there isn't a dig or bury instruction that can take an integer argument. The answer is that it wouldn't be type-safe.

If the type-system did allow such a construction we wouldn't be able to detect programs that resulted in [stack underflow](StackUnderflow.md) at compile-time.

You can construct a dig or bury instruction to any specific depth using the [method described here](ShufflingBasis.md).