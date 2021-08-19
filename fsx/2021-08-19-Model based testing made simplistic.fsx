(**
Property based testing is an effective way to verify the input/output relation of pure functions,
leveraging predicates (properties) to declaratively describe such relation.

_Model_ based testing extends the scope to stateful systems,
whose behavior is described in terms of an abstract model,
leveraging state machines.

This is an introduction aimed at providing a basic understanding of the idea.
It is yet another [poor man's approach](https://porg.es/model-based-testing/)
which may also work in practice but, since it is deliberately simplistic,
you may also want to invest some time learning about the APIs
provided by [libraries](https://fscheck.github.io/FsCheck//StatefulTestingNew.html).

## A simple example
Our system under test is the `Queue` class from the .NET base library.
We model its state as a list and we focus on two actions only: enqueue and dequeue.
*)
    type Sut = System.Collections.Generic.Queue<int>

    type State = list<int>

    type Action =
        | Enqueue of int
        | Dequeue

(**
The whole point of the approach is to perform an action both on the abstract model and on the actual system,
and then check that the resulting state is the same.

### The system and its model
The functions `fromModel` and `toModel` define the correspondance between the actual system and its abstract model.
They are easy to implement in this example but may be challenging in more realistic scenarios.
Such challenges may arise from the system hiding its internal state or persisting it in a distributed
environment. Most libraries for model based testing in fact take a different route (as we will see later)
but for now we naively assume that:

- `fromModel` puts the system under test in the given state.
- `toModel` retrieves the state of the system under test.
*)
    let fromModel (state: State) =
        Sut(state)

    let toModel (sut: Sut) =
        List.ofSeq sut

(**
### State transitions
The essence of our model is captured by the following function defining state transitions:
*)
    let nextState (action: Action) (state: State) =
        match action with
        | Enqueue x -> List.append state [x]
        | Dequeue -> state.Tail

(**
The effect of enqueing an item corresponds (in the abstract model) to appending it to the
list representing the state of the system.

The abstract effect of a dequeue is to remove the head of the list representing the state.

The `run` function is the actual counterpart to the `nextState` function above.
This time the action is executed on the system under test.
*)
    let run (action: Action) (sut: Sut) =
        match action with
        | Enqueue x -> sut.Enqueue x
        | Dequeue -> sut.Dequeue() |> ignore
        toModel sut

(**

### Invariant and Precondition
Next we define two predicates: an invariant (that should hold in every state)
and a precondition (that should hold in order to perform an action):
*)
    let invariant (_: State) = true

    let precondition (action: Action) (state: State) =
        match (action, state) with
        | Dequeue, [] -> false
        | _ -> true

(**
In this case the invariant always holds, we made illegal states unrepresentable! (but it was an easy win,
let me suggest a nice [article](https://www.hillelwayne.com/post/constructive/) about this topic).

With the precondition we express that a dequeue action may not be performed on an empty queue.

### Running a test
The `test` function initializes the system under test with the given state,
then runs the given action on it and finally checks that its resulting state
conforms to the model.
*)
    let test (state, action) =
        let check error x = if not x then failwith error

        invariant state
        |> check "Input Invariant"

        precondition action state
        |> check "Precondition"

        let sut = fromModel state

        toModel sut = state
        |> check "Initial State"

        let expected = nextState action state

        invariant expected
        |> check "Expected Invariant"

        let actual = run action sut

        actual = expected
        |> check "Final State"

(**
Let's examine the code in more detail.

First we check that the input satisfies the invariant and the precondition.

Then we initialize the system under test, establishing the given state
(and we verify that the initialization succeded by checking the state).

Next we use the model to get the state expected after the execution of the action
(and we verify that this next state produced by the model still satisfies the invariant).

Then we run the action on the actual system, and we verify that, after the execution,
the state of the system under test is the same as the expected one according to the model.

### Random input
To run `test` with many random inputs we need a generator of state-action pairs satisfying invariant and precondition:
*)
(*** define-output:test ***)
    #r "nuget: FsCheck"

    open FsCheck

    let actionGenerator (state: State) =
        let enq = Gen.map Enqueue Arb.generate<int>
        let deq = Gen.constant Dequeue
        if state.IsEmpty
        then enq
        else Gen.oneof [enq; deq]

    let arbitraryStep =
        let stepGenerator =
            gen {
                let! state = Gen.listOf Arb.generate<int>
                let! action = actionGenerator state
                return (state, action)
            }
        Arb.fromGen stepGenerator

    Prop.forAll arbitraryStep test
    |> Check.Quick
(*** include-output:test ***)

(**
## FsCheck state machines
As anticipated, initializing a real system to an arbitrary state may be hard.
But we can explore many states executing a _sequence_ of actions starting from a state that's easy to establish.

FsCheck state machines allow this. It requires a bit of boilerplate,
but we can reuse `actionGenerator`, `nextState` and `run`:
*)
(*** define-output:machine ***)
    open FsCheck.Experimental

    let machine = {
        new Machine<Sut, State>() with
            member __.Setup =
                { new Setup<Sut, State>() with
                    member __.Actual() = Sut()
                    member __.Model() = [] }
                |> Gen.constant
                |> Arb.fromGen

            member __.Next state =
                actionGenerator state
                |> Gen.map (fun action -> {
                    new Operation<Sut, State>() with
                        member __.Run state = nextState action state

                        member __.Check (sut, state) =
                            let actual = run action sut
                            state = actual
                            |@ sprintf "model = %A, actual = %A" state actual

                        override __.ToString() = sprintf "%A" action })
    }

    StateMachine.toProperty machine
    |> Check.Quick

(*** include-output:machine ***)

(**
Among the benefits of using a real library, we get shrinking and useful error messages in
case of test failures.
Notice also the added flexibility given by the `Check` method: if the state is hard to retrieve,
instead of calling `run` and checking the whole state, we are free to focus only on the relevant part of the system.

## Model View Update
Instead of discussing how effective the approach is for verification,
let me point out that modeling is valuable at least for documentation:
the `nextState` function expresses our understanding of the system.

If you're familiar with the [ELM Architecture](https://guide.elm-lang.org/architecture/),
looking at `nextState` should ring a bell: just add a view function and you have the MVU pattern!
And the nice thing is that [F#](https://zaid-ajaj.github.io/the-elmish-book/#/) allows to use
the same code, both for verification with .NET and for visualization with JavaScript.

*)

    #r "nuget: Fable.Elmish.React"

    open Elmish
    open Elmish.React
    open Fable.React.Props
    open Fable.React.Helpers
    open Fable.React.Standard

    let init () = []

    let rnd = System.Random()

    let colors = [| "DarkSlateGray"; "DarkSlateBlue"; "CadetBlue"; "DarkCyan"; "DarkSeaGreen"; "Olive"; "Pink" |]

    let item n =
        let color = colors.[n % colors.Length]
        td [Style [Color color; Border "solid" ]] [str (string n)]

    let view (state: State) (dispatch: Action -> unit) =
        div [] [
            h2 [] [str "What a wonderful queue!"]
            button [ OnClick (fun _ -> rnd.Next(100) |> Enqueue |> dispatch) ] [ str "Enqueue -> " ]
            state |> List.rev |> List.map item |> table []
            if not state.IsEmpty then button [ OnClick (fun _ -> Dequeue |> dispatch) ] [ str " -> Dequeue" ]
        ]

    Program.mkSimple init nextState view
    |> Program.withReactSynchronous "elmish-app"
    |> Program.run

(**
I think this [live documentation](/content/model-based-testing/index.html) aspect is worth exploring.

## Final remark

The provided example was not meant to precisely capture the essence of the queue concept.
It may be improved and refined to this aim, but I just wanted to describe a small stateful system,
with no attempt to define a proper abstraction.

I often praise abstract data types but, quoting Leslie Lamport
(via [Ron Pressler](https://pron.github.io/posts/tlaplus_part4#model-based-specifications-and-information-hiding)):

> The lesson I learned from the specification work of the early ’80s is that axiomatic specifications don’t work.
> The idea of specifying a system by writing down all the properties it satisfies seems perfect.
> We just list what the system must and must not do, and we have a completely abstract specification.
> It sounds wonderful; it just doesn’t work in practice.

I have to admit we rarely encounter formal ADT specifications besides simple examples
and [stacks](https://giacomociti.github.io/2018/05/26/The-lost-art-of-data-abstraction.html)
(a quip of Djikstra, according to Bertrand Meyer, is that the purpose of ADT theory is to define stacks).

So, if we leave aside philosophy, we can lower a bit our theoretical pretenses
and understand our systems better, embracing the operational paradigm of state machines.

*)





