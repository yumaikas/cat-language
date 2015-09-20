# Quadratic #

Here is the quadratic function in Python:

```
def quadratic(x a b c):
    return a * x * x + b * x + c
```

Here is the quadratic function in Cat using named parameters:

```
define quadratic(x a b c) 
{ x sqr a * x b * + c + }
```

Here is the quadratic function in Cat, but in [point-free form](PointFreeForm.md)
and using a [type annotation](TypeAnnotation.md):

```
define quadratic : (x=float a=float b=float c=float -> float) 
{ 
  [over] dip2       // x a x b c
  [*] dip +         // x a ((x*b)+c)
  [swap sqr *] dip  // (a*(x^2)) ((x*b)+c)
  +
}
```