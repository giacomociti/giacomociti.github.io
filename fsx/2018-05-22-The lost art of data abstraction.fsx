(**
## Two types of ADT
Algebraic Data Types and Abstract Data Types share the same acronym 
but are very different concepts. Both are clever ideas, but with opposite aims. 
In this post I will use F# to show abstract data types and hint at some beautiful 
and old techniques for their algebraic specification.

## Algebraic Data Types
Algebraic Data Types are the workhorses of statically typed functional languages like F#.
They are also known as *sum types* or *discriminated unions*, so I will refer to them as DUs
here, reserving the ADT acronym for abstract data types which is the main subject of the post. 
There's no need to introduce DUs because I assume they are well known since they're ofter praised
as a great tool for [domain modeling](https://fsharpforfunandprofit.com/ddd/) 
and [making illegal states unrepresentable](https://fsharpforfunandprofit.com/posts/designing-with-types-making-illegal-states-unrepresentable/).
In this post we will see that they are not completely unrelated to abstract data types:
in fact DUs are useful as internal representation of ADT values and they also
play a role in the algebraic specification of ADTs (as the *algebraic* adjective may suggest).

## Abstract Data Types
While DUs are concrete data structures, abstraction is (unsurprisingly) the defining trait of abstract data types.
ADTs are the ultimate tool for encapsulation and information hiding: their behavior can be described only implicitly, relating values obtained applying the ADT operations without referencing their internal representation.

### Objects and ADTs
There are important [differences between objects and ADTs](https://medium.com/JosephJnk/abstract-data-types-and-objects-17828bd4abdc)
but even [Bertrand Meyer](https://en.wikipedia.org/wiki/Object-Oriented_Software_Construction)
advocates conflating the two concepts and I sympathize a lot with this view because for me 
the similarities outweight the differences.
It's a fact of (computing) history that [objects won over ADTs](https://hal.inria.fr/hal-01335657/document)
and even a functional first language like [F# embraces objects](https://skillsmatter.com/skillscasts/11439-keynote-f-sharp-code-i-love).
That's why I'm using F# objects to show ADTs (modules are used in the ML tradition).
This is also an occasion to show that _immutable_ objects are very nice and close to the
mathematical notion of abstract data types.

### The obligatory stack example
The standard pedagogical example of ADT is the stack, whose signature features the operations
`New`, `Push`, `Pop` and `Top`:

*)

    type IStack<'a> =
        // abstract static New: unit -> IStack<'a>
        abstract Push: 'a -> IStack<'a>
        abstract Pop: unit -> IStack<'a>
        abstract Top: 'a Option


(**
Unfortunately neither constructors nor static members are allowed in .NET interfaces 
(in Java 8 they are allowed) so we cannot express the `New` operation in the above signature definition.
Let's then move to a proper class, although with a dummy implementation for now:
*)

    type private DummyRepresentation = TODO

    type Stack<'a> private(repr) =

        static member New() = Stack(TODO)
        member __.Push(x: 'a) = Stack(TODO)
        member __.Pop() = Stack(TODO)
        member __.Top: 'a Option = None


(** 
The class constructor is private and takes a private representation object.
Since for now this representation has a single dummy value (`TODO`) all the ADT constructors
(`New`, `Push` and `Pop`) use the same dummy value.

A side note on terminology: in ADT parlance a _constructor_ is any operation returning a value of the ADT type.
That's why in our stack all operations (except `Top`) qualify as constructors.

The above implementation is clearly wrong but, to claim that, we first need to _specify_ what is the correct behavior and we can do this with _axioms_ stating the properties of the ADT operations.

We can use [FsCheck](https://fscheck.github.io/FsCheck/) to check the correctness of our implementation (hence we
won't prove it but at least test it thoroughly with many random values).

### Stack Axioms
Here are the stack axioms (we use `int` for the generic parameter but any type will do):
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
The meaning of the axioms should be fairly readable:

1. popping an empty stack should leave it empty
1. there's no top of an empty stack
1. pop *undo* the effect of a push
1. pushing an item make it the top

Different API's are possible, but this one has the advantage of defining
total functions: `Top` is defined (albeit with value `None`) also on the empty stack,
and popping an empty stack is possible although useless because it will leave it empty.


### Equality
Before running FsCheck we need to fix a couple of things: the first one is that since axioms
are written as _equations_ comparing ADT values, in order to compare stacks we have to override `Equals` so that stack values with the same internal representation (whatever it is) are considered equivalent:

*)


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

    //and private Representation = TODO

(**    
### Term Algebra
Now we are equipped with stack equality but the remaining issue is that FsCheck can't generate 
instances of the `Stack` type (becuase it is not a concrete data type).
The trick is to define the so called _term algebra_ as a DU: 
*)
    type StackTerm<'a> =
        | New
        | Push of 'a * StackTerm<'a>
        | Pop of StackTerm<'a>
(**
Notice how _data constructors_ in `StackTerm` correspond to ADT constructors in the `Stack` API
and how simple is to create stack instances from terms:
*)
    let rec stack term =
        match term with
        | New -> Stack.New()
        | Push(x, s) -> (stack s).Push(x)
        | Pop(s) -> (stack s).Pop()
(**
The `stack` function highlight the correspondence between the stack API and the term algebra. 
Terms are just stack expressions, for example `term1` correspond to `stack1`:
*)
    let term1 = Push(5, Pop(Push(42, New)))
    let stack1 = Stack.New().Push(42).Pop().Push(5)
(**
and applyng the `stack` function to `term1` we expect to obtain a stack instance equivalent to `stack1`.

Random terms can now be generated by FsCheck, we just need to adapt our properties a little:
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
We added also a new axiom for equality (`axiom0`). This is not specific for stacks,
in every ADT we expect two values to be equivalent if they are constructed with
the same sequence of operations.

We can finally run FsCheck and see our tests fail.
Now it's time to get back to the implementation and make it compliant with the axioms.

### Initial Algebra
There are of course multiple implementation options, but first we will pursue one which 
is of theoretical interest only.
We use terms also as the internal representation:

*)

    type Stack2<'a when 'a : equality> private(repr: StackTerm<'a>) =
    
        let rec reduce term =
            match term with
            | New -> New
            | Push(x, s) -> Push(x, reduce s)
            | Pop(New) -> New
            | Pop(Push(_, s)) -> reduce s
            | Pop(s) -> Pop(reduce s)

        static member New() = New |> Stack2

        member __.Push(x: 'a) = Push(x, repr) |> Stack2

        member __.Pop() = Pop(repr) |> Stack2

        member __.Top: 'a Option = 
            match reduce repr with
            | Push(x, _) -> Some x
            | _ -> None

        member private __.repr = repr
        override __.Equals(obj) =
            match obj with
            | :? Stack2<'a> as x -> (reduce x.repr).Equals(reduce repr)
            | _ -> false
        override __.GetHashCode() = (reduce repr).GetHashCode()


(**
Clearly this implementation is not performant: the representation object
always keeps growing, even when `Pop` is called, and a lot of processing is needed
(the `reduce` function) for `Top`, `Equals` and `GetHashCode`.
But it works, and it's derived almost mechanically from the axioms.

There are [specification languages](https://cseweb.ucsd.edu/~goguen/sys/obj.html) in 
which this kind of implementation can often be automatically derived from axioms.

Axioms determine an equivalence relation on terms, and the ADT values are conceptually
the equivalence classes of this relation (quotient term algebra).
This theoretical model is a sort of reference implementation, and this approach (initial algebra)
is deeply rooted in category theory (so it has to be fancy!).

### Canonical Constructors
A more pragmatic [approach](http://www.cs.unc.edu/techreports/02-012.pdf) is to identify _canonical_ constructors,
the ones intuitively sufficient to construct all values. In our example `New` and `Push` are enough because every term
involving `Pop` can be expressed in a simpler form using only `New` and `Push`. For example `term1` is intended to be equivalent to:

*)
    Push(5, New)

(**
Adopting this approach we have a precise guideline to specify axioms: one axiom is needed for each 
combination of a non-canonical operation applied to the result of a canonical operation.
Our stack axioms happen to follow this pattern:

1. Pop(New) = ...
1. Top(New) = ...
1. Pop(Push(x, s)) = ...
1. Top(Push(x, s)) = ...

Since only `New` and `Push` are needed to build stacks, we can simplify our representation type:

*)

    type Stack3<'a when 'a : equality> private(repr: Repr<'a>) =

        static member New() = Repr<'a>.New |> Stack3

        member __.Push(x: 'a) = Push(x, repr) |> Stack3

        member __.Pop() =
            match repr with
            | New -> New
            | Push(_, s) -> s
            |> Stack3

        member __.Top: 'a Option = 
            match repr with
            | New -> None
            | Push(x, _) -> Some x
            
        member private __.repr = repr
        override __.Equals(obj) =
            match obj with
            | :? Stack3<'a> as x -> x.repr.Equals(repr)
            | _ -> false
        override __.GetHashCode() = repr.GetHashCode()

(**
Also this implementation just mirrors the axioms, but this is a realistic one.
If you squint a bit you notice that our final representation type is isomorphic to
the good old F# (linked) list, which is a reasonable internal representation for a stack.

### Conclusion
to do hint at sufficient completeness, final algebra
consistency congruence term rewriting equational logic.
*)