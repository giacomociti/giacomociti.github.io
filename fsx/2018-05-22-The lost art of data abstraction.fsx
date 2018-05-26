(**
### Two types of ADT
Algebraic Data Types and Abstract Data Types share the same acronym
but are very different concepts. Both are clever ideas but with
opposite aims. In this post I will use F# to show abstract data types
hinting at old and fascinating techniques for their algebraic
specification.

### Algebraic Data Types
Algebraic Data Types are the workhorses of statically typed 
functional languages like F#. They are also known as *sum types* or 
*discriminated unions*, so I will refer to them as DUs here,
reserving  the ADT acronym for abstract data types which is the main
subject of the post.

There's no need to introduce DUs because I assume they are well known
since they're often praised as a great tool for [domain modeling][WlaschinDDD]
and [making illegal states unrepresentable][WlaschinIllegalStates].
In this post we will see that they are not completely unrelated to
abstract data types: in fact DUs are useful as internal representation
of ADT values and they also play a role in the algebraic specification
of ADTs (as the *algebraic* adjective may suggest).

### Abstract Data Types
While DUs are concrete data structures, abstraction is (unsurprisingly)
the defining trait of abstract data types. ADTs are the ultimate tool
for encapsulation and information hiding: their behavior can be
described only implicitly, relating values obtained applying the ADT
operations without referring to their internal representation.

### Objects and ADTs
There are important [differences between objects and ADTs][OOsvADT]
but even [Bertrand Meyer][MeyerOOSC] advocates conflating the two
concepts and I sympathize a lot with this view because for me the
similarities outweigh the differences.

It's a fact of (computing) history that [objects won over ADTs][Martini]
and even a functional first language like [F# embraces objects][Syme].
That's why I'm using F# objects to show ADTs (modules are used in the 
ML tradition). This is also an occasion to show that _immutable_
objects are very nice and close to the mathematical notion of
abstract data type.
*)

(*** hide ***)
module Module1 =

(**
### The obligatory stack example
The standard pedagogical example of ADT is the stack, whose signature
features the operations `New`, `Push`, `Pop` and `Top`:
*)

    type IStack<'a> =
        // abstract static New: unit -> IStack<'a>
        abstract Push: 'a -> IStack<'a>
        abstract Pop: unit -> IStack<'a>
        abstract Top: 'a Option

(**
Unfortunately neither constructors nor static members are allowed
in .NET interfaces (Java 8 allows static methods) so we cannot
express the `New` operation in the above signature. Let's then
move to a proper class, although with a dummy implementation:
*)

    type private DummyRepresentation = TODO

    type Stack<'a> private(repr) =

        static member New() = Stack(TODO)
        member __.Push(x: 'a) = Stack(TODO)
        member __.Pop() = Stack(TODO)
        member __.Top: 'a Option = None


(** 
The class constructor is private and takes a private representation
object. For now all the ADT constructors (`New`, `Push` and `Pop`)
necessarily use the same dummy representation value (`TODO`).

A side note on terminology: in ADT parlance a _constructor_ is any
operation returning a value of the ADT type. That's why in our stack
all operations (except `Top`) qualify as constructors.

The above implementation is clearly wrong but, to claim that, we
first need to _specify_ what is the correct behavior and we can do
this with _axioms_ stating the properties of the ADT operations.

We can use [FsCheck][FsCheck] to verify the correctness of our
implementation (hence we won't prove it but at least test it
thoroughly with many random values).

### Stack Axioms
Here are the stack axioms (we use `int` for the generic parameter
but any type will do):
*)

    let axiom1 () = 
        Stack<int>.New().Pop() = Stack<int>.New()

    let axiom2 () = 
        Stack<int>.New().Top = None

    let axiom3 (s: Stack<int>) x =
        s.Push(x).Pop() = s

    let axiom4 (s: Stack<int>) x =
        s.Push(x).Top = Some x

    //#r "FsCheck.dll"
    //open FsCheck

    //Check.Quick axiom1
    //Check.Quick axiom2
    //Check.Quick axiom3
    //Check.Quick axiom4

(**
The meaning of the axioms is fairly readable:

1. popping an empty stack should leave it empty
1. there's no top of an empty stack
1. pop *undo* the effect of a push
1. pushing an item make it the top

Different API's are possible, but this one has the advantage of
defining total functions: `Top` is defined (albeit with value `None`)
also on the empty stack, and popping an empty stack is possible
although useless because it will leave it empty.
*)

(*** hide ***)
module Module2 =

(**
### Equality
Before running FsCheck we need to fix a couple of things: the first
one is that since axioms are expressed as _equations_ comparing ADT
values, to compare stacks we have to override `Equals` so that stack
values with the same internal representation (whatever it is) are
equivalent:
*)
    type private DummyRepresentation = TODO

    type Stack<'a> private(repr) =

        static member New() = Stack(TODO)
        member __.Push(x: 'a) = Stack(TODO)
        member __.Pop() = Stack(TODO)
        member __.Top: 'a Option = None

        member private __.repr = repr
        override __.Equals(obj) =
            match obj with
            | :? Stack<'a> as x -> x.repr.Equals(repr)
            | _ -> false
        override __.GetHashCode() = repr.GetHashCode()

(*** hide ***)
module Module3 =
    open Module2
(**    
### Term Algebra
Now we are equipped with stack equality but the remaining issue is
that FsCheck can't generate instances of the `Stack` type (because
it is not a concrete data type). The trick is to define the so
called _term algebra_ as a DU: 
*)
    type StackTerm<'a> =
        | New
        | Push of 'a * StackTerm<'a>
        | Pop of StackTerm<'a>
(**
Notice how _data constructors_ in `StackTerm` correspond to ADT
constructors in the `Stack` API and how straightforward is to
create stack instances from terms:
*)
    let rec stack term =
        match term with
        | New -> Stack.New()
        | Push(x, s) -> (stack s).Push(x)
        | Pop(s) -> (stack s).Pop()
(**
The `stack` function highlights the correspondence between the
term algebra and the stack API. Terms are just stack expressions,
for example `term1` corresponds to `stack1`:
*)
    let term1 = Push(5, Pop(Push(42, New)))
    let stack1 = Stack.New().Push(42).Pop().Push(5)
(**
and applying the `stack` function to `term1` we expect to get
a stack instance equivalent to `stack1`:
*)
    stack term1 = stack1
(**
Random terms can now be generated by FsCheck, and we just need
to adapt our properties a little:
*)
    let axiom0 (s: StackTerm<int>) =
        stack s = stack s
    
    let axiom1 () = 
        Stack<int>.New().Pop() = Stack<int>.New()

    let axiom2 () = 
        Stack<int>.New().Top = None

    let axiom3 (s: StackTerm<int>) x =
        let s' = stack s
        s'.Push(x).Pop() = s'

    let axiom4 (s: StackTerm<int>) x =
        let s' = stack s
        s'.Push(x).Top = Some x

(**
We added also a new axiom for equality (`axiom0`). This is not
specific for stacks, in every ADT we expect two values to be
equivalent if they are constructed with the same sequence of
operations.

We can finally run FsCheck and see our tests fail. Now it's time to
get back to the implementation and make it compliant with the axioms.
*)

(*** hide ***)
module Module4 =
    open Module3

(**
### Initial Algebra
There are of course multiple implementation options,
but first we will pursue one of theoretical interest only.
We use terms also for the internal representation:
*)

    type Stack<'a when 'a : equality> private(repr: StackTerm<'a>) =
    
        let rec reduce term =
            match term with
            | New -> New
            | Push(x, s) -> Push(x, reduce s)
            | Pop(New) -> New
            | Pop(Push(_, s)) -> reduce s
            | Pop(s) -> Pop(reduce s)

        static member New() = New |> Stack

        member __.Push(x: 'a) = Push(x, repr) |> Stack

        member __.Pop() = Pop(repr) |> Stack

        member __.Top: 'a Option = 
            match reduce repr with
            | Push(x, _) -> Some x
            | _ -> None

        member private __.repr = repr
        override __.Equals(obj) =
            match obj with
            | :? Stack<'a> as x -> (reduce x.repr).Equals(reduce repr)
            | _ -> false
        override __.GetHashCode() = (reduce repr).GetHashCode()


(**
Clearly this implementation is not good from a performance point of view:
the representation object always keeps growing, even when `Pop` is called,
and a lot of processing is needed (the `reduce` function) for `Top`, `Equals`
and `GetHashCode`. But it works, and it's derived almost
mechanically from the axioms.

In fact there are [specification languages][GoguenOBJ] in which
this kind of implementation can often be automatically derived
from axioms.

Axioms determine an equivalence relation on terms (two terms are
equivalent if the axioms allow to reduce them to the same term)
and the ADT values are conceptually the equivalence classes of
this relation. This theoretical model is a sort of reference
implementation (quotient term algebra) and this approach (initial
algebra) has strong connections with category theory (so it has to
be fancy!). There is also the dual approach of final algebra, where
two terms are considered equivalent _unless_ the axioms don't allow
it. There's a lot of beautiful theory behind abstract data types.
*)

(*** hide ***)
module Module5 =
    open Module4

(**
### Canonical Constructors
The key insight to improve the implementation is nicely explained
[here][JAX]. The idea is to identify _canonical_constructors,
the ones intuitively sufficient to construct all values. In our
example `New` and `Push` are enough because every stack involving
`Pop` can be expressed in a simpler form using only `New` and
`Push`. For example:
*)
    Stack<int>.New().Push(42).Pop().Push(5) = Stack<int>.New().Push(5)
(**
This approach provides a precise guideline to specify axioms: 
one axiom is needed for each combination of a non-canonical operation
applied to the result of a canonical operation. Our stack axioms
happen to follow this pattern:

1. Pop(New) = ...
1. Top(New) = ...
1. Pop(Push(x, s)) = ...
1. Top(Push(x, s)) = ...

The first two axioms specify the behavior of the non-canonical
operations (`Pop` and `Top`) when applied to the empty stack;
the other two axioms specify the behavior of the non-canonical
operations when applied to a stack obtained with a push operation.
There are no more cases to cover.

Since only `New` and `Push` are needed to build stacks,
we can simplify our representation type:
*)

    type private Representation<'a> = New | Push of 'a * Representation<'a>

    type Stack<'a when 'a : equality> private(repr: Representation<'a>) =

        static member New() = New |> Stack

        member __.Push(x: 'a) = Push(x, repr) |> Stack

        member __.Pop() =
            match repr with
            | New -> New
            | Push(_, s) -> s
            |> Stack

        member __.Top: 'a Option = 
            match repr with
            | New -> None
            | Push(x, _) -> Some x
            
        member private __.repr = repr
        override __.Equals(obj) =
            match obj with
            | :? Stack<'a> as x -> x.repr.Equals(repr)
            | _ -> false
        override __.GetHashCode() = repr.GetHashCode()

(**
Also this implementation just mirrors the axioms, but this is a
realistic one. If you squint a bit you notice that our final
representation type is isomorphic to the good old F# (linked) list,
which is in fact a reasonable internal representation for a stack.

### Conclusion
The purpose of this post is to present a few old and nice concepts
about data abstraction. I'm not claiming that specifying and
implementing an ADT is always as nice and simple as in the stack
example, nor I'm advocating formal methods; but I think these ideas
may still provide a useful intellectual guideline.


[WlaschinDDD]:https://fsharpforfunandprofit.com/ddd
[WlaschinIllegalStates]:https://fsharpforfunandprofit.com/posts/designing-with-types-making-illegal-states-unrepresentable
[MeyerOOSC]:https://en.wikipedia.org/wiki/Object-Oriented_Software_Construction
[OOsvADT]:https://medium.com/JosephJnk/abstract-data-types-and-objects-17828bd4abdc
[Martini]:https://hal.inria.fr/hal-01335657/document
[Syme]:https://skillsmatter.com/skillscasts/11439-keynote-f-sharp-code-i-love
[FsCheck]:https://fscheck.github.io/FsCheck
[GoguenOBJ]:https://cseweb.ucsd.edu/~goguen/sys/obj.html
[JAX]:http://www.cs.unc.edu/techreports/02-012.pdf
*)