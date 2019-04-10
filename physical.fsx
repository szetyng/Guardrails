open System.Collections.Generic
#load "holon.fsx"
#load "platform.fsx"
open Holon
open Platform

//************************* Principle 2 *********************************/
let allocateResources head inst agent =
    let a = agent.ID
    let i = inst.ID
    let demandedR = powToAllocate head inst agent

    match demandedR with
    | 0 -> printfn "head %s cannot allocate resources to %s in inst %s" head.Name agent.Name inst.Name
    | x -> sendMessage (Some (Allocated(agent.ID,x,inst.ID))) inst