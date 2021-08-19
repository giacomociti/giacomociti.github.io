import { Union } from "./.fable/fable-library.3.2.10/Types.js";
import { union_type, int32_type } from "./.fable/fable-library.3.2.10/Reflection.js";
import { isEmpty, reverse, map, empty, singleton, append, tail } from "./.fable/fable-library.3.2.10/List.js";
import * as react from "react";
import { randomNext, int32ToString } from "./.fable/fable-library.3.2.10/Util.js";
import { empty as empty_1, singleton as singleton_1, append as append_1, delay, toList } from "./.fable/fable-library.3.2.10/Seq.js";
import { ProgramModule_mkSimple, ProgramModule_run } from "./.fable/Fable.Elmish.3.0.0/program.fs.js";
import { Program_withReactSynchronous } from "./.fable/Fable.Elmish.React.3.0.1/react.fs.js";

export class Action extends Union {
    constructor(tag, ...fields) {
        super();
        this.tag = (tag | 0);
        this.fields = fields;
    }
    cases() {
        return ["Enqueue", "Dequeue"];
    }
}

export function Action$reflection() {
    return union_type("App.Action", [], Action, () => [[["Item", int32_type]], []]);
}

export function nextState(action, state) {
    if (action.tag === 1) {
        return tail(state);
    }
    else {
        return append(state, singleton(action.fields[0]));
    }
}

export function init() {
    return empty();
}

export const rnd = {};

export const colors = ["DarkSlateGray", "DarkSlateBlue", "CadetBlue", "DarkCyan", "DarkSeaGreen", "Olive", "Pink"];

export function item(n) {
    const color = colors[n % colors.length];
    return react.createElement("td", {
        style: {
            color: color,
            border: "solid",
        },
    }, int32ToString(n));
}

export function view(state, dispatch) {
    return react.createElement("div", {}, ...toList(delay(() => append_1(singleton_1(react.createElement("h2", {}, "What a wonderful queue!")), delay(() => append_1(singleton_1(react.createElement("button", {
        onClick: (_arg1) => {
            dispatch(new Action(0, randomNext(0, 100)));
        },
    }, "Enqueue -\u003e ")), delay(() => append_1(singleton_1(react.createElement("table", {}, ...map((n) => item(n), reverse(state)))), delay(() => ((!isEmpty(state)) ? singleton_1(react.createElement("button", {
        onClick: (_arg2) => {
            dispatch(new Action(1));
        },
    }, " -\u003e Dequeue")) : empty_1()))))))))));
}

ProgramModule_run(Program_withReactSynchronous("elmish-app", ProgramModule_mkSimple(init, (action, state) => nextState(action, state), (state_1, dispatch) => view(state_1, dispatch))));

