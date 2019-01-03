(**
### Sets are not functors
I was a bit surprised to learn from Mark Seeman's blog that 
[sets aren't functors](http://blog.ploeh.dk/2018/12/03/set-is-not-a-functor/).
But, after all, I have almost no knowledge of category theory and, perhaps like
[the fox of the fairy tale](https://fairytalez.com/the-fox-and-the-grapes/),
little interest in getting more acquainted with it. 
I get reassured by authors like [Tomas Petricek](http://tomasp.net/academic/papers/monads/)
who certainly _do_ understand the formal laws, yet deemphasize their prominence in programming. 

### _Gen_ is not a monad
In fact, also _QuickCheck_ generators violate monad laws,
as acknowledged by the authors themselves in
[the original paper](http://www.eecs.northwestern.edu/~robby/courses/395-495-2009-fall/quick.pdf).
The details are not relevant for this post, except maybe the following quote:

"We cannot fix the problem just by reinterpreting equality for the _Gen_ type
claiming the two sides are just different representations of the same
abstract generator. This isn't good enough because we can actually observe
the difference at other types ..."

This post is about equality and abstraction.
The main point is that a proper abstraction should not
let us observe any difference between two values considered equal.

### _DateTimeOffset_ breaks substitution

What I find really surprising and interesting in Mark Seeman's post is the example
used to break the functor laws: `DateTimeOffset` overrides equality and,
quite reasonably, conflates values with different offsets but representing the same
point in time. The problem is that the offset remains visible, hence violating the
_substitution property_ of equality.

I've always been aware that any override of `Equals` should be reflexive, symmetric
and transitive (the properties of an equivalence relation), but only now I realize
the importance of this even more fundamental property of
[mathematical equality](https://en.m.wikipedia.org/wiki/Equality_(mathematics)),
substitution: `x = y => f(x) = f(y)`

`DateTimeOffset` clearly violates this property since we can observe different offsets
on equal values.

This hampers equational reasoning, the hallmark of functional programming.

My gut reaction is that we should put such poor abstractions in the same league
as of impure stuff. At least, side effects, mutable state and randomness can be useful
to get things done, and there's growing awareness on how to avoid or limit their use.

### Abstract types to the rescue
This way of messing up things, choosing the
[easy](https://www.infoq.com/presentations/Simple-Made-Easy) route,
is reminiscent of implementation inheritance: it's tempting to derive 
from a class instead of composing it, except that once you specialize a class you are
committed to that abstraction, which is not limited to a signature but includes semantics
(invariants, pre and post conditions);  otherwise you end up breaking the
[Liskov substitution principle](https://en.m.wikipedia.org/wiki/Liskov_substitution_principle).
I digress, but Barbara Liskov reminds us of abstract data types. Maybe I'm obsessed with
this concept but it's a real pity that our industry care so little about it or at least
some lightweight variant like
[Design by Contract](https://en.m.wikipedia.org/wiki/Design_by_contract).
And, yes, certainly `DateTimeOffset` is not a proper ADT, as substitution
is an essential property to qualify as such.

Laws (called axioms) defining the formal semantics of an abstract data type are expressed with
[equational logic](https://en.wikipedia.org/wiki/Equational_logic),
which is based on substitution; they determine a congruence on ADT expressions (terms).
In [my attempt](https://giacomociti.github.io/2018/05/26/The-lost-art-of-data-abstraction.html)
at explaining a bit of this _initial algebra_ approach I refer to it as an equivalence
relation, but actually it's a [_congruence_](https://en.wikipedia.org/wiki/Congruence_relation),
that is a stronger notion encompassing substitution.
In the [_final semantics_](http://prl.ccs.neu.edu/blog/2017/09/27/final-algebra-semantics-is-observational-equivalence/)
approach, two terms denote the same value if they can be substituted for each other and no difference can be observed.
The last sentence in
[this nice summary](https://www.cs.scranton.edu/~mccloske/courses/se507/alg_specs_lec.html)
of the above concepts clearly states that ADT consistency requires the substitution property.

### What about Haskell?
Armed with this theoretical evidence I turned to Haskell and, to my bigger surprise,
I discovered that there's no
[agreement](https://www.google.com/amp/s/amp.reddit.com/r/haskell/comments/1njlqr/laws_for_the_eq_class/)
on the need for the `Eq` type class to satisfy the substitution property.
I understand `=` and `==` are not the same but I do not dare to even start any philosophical
nor technical discussion about it. Fortunately 
[I'm not alone](https://stackoverflow.com/questions/19177125/sets-functors-and-eq-confusion)
in being confused. 

What seems uncontroversial is that the substitution property is
compromised by lack of encapsulation.

[This article](https://github.com/jafingerhut/thalia/blob/master/doc/other-topics/referential-transparency.md)
explains pretty well why also _HashSet_ is a leaky abstraction, in Haskell too:
it may yield elements in different orders for equal instances.

A draconian fix would be to not expose any enumeration of elements: a set should be used
only to check if it contains an element. 
Such a minimalist kind of set should satisfy the functor's laws when the element type is a
proper ADT.
Some evidence is provided in the following [academic paper](http://babel.ls.fi.upm.es/~pablo/Papers/adt-functors.pdf)
which explores the relationship between functors and ADTs (not an easy read, at least for me).

### Conclusion
Maybe I gave the impression of dismissing abstractions like functor and monad.
Actually I don't. I just don't get (and I'm more than happy to read comments about it)
why wrapping our heads around non trivial abstractions while caring so little about
simpler ones like dates and sets.

*)