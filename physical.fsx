open System.Collections.Generic
#load "holon.fsx"
#load "platform.fsx"
open Holon
open Platform

//************************* Misc *********************************/
let refillResources inst r = 
    inst.Resources <- inst.Resources + r
    printfn "inst has refilled its resources by %i to %i" r inst.Resources

//************************* Principle 2 *********************************/
let allocateResources head inst agent =
    let a = agent.ID
    let i = inst.ID
    let demandedR = powToAllocate head inst agent

    match demandedR with
    | 0 -> printfn "head %s cannot allocate resources to %s in inst %s" head.Name agent.Name inst.Name
    | x -> sendMessage (Some (Allocated(agent.ID,x,inst.ID))) inst

/// Move r resources from inst to agent, if available
/// TODO: agent decides what r is, from Allocated or from greed
let appropriateResources agent inst r = 
    let pool = inst.Resources
    let own = agent.Resources
    let appropriatedR = 
        if pool>=r then 
            inst.Resources <- pool - r
            agent.Resources <- own + r
            r
        else
            inst.Resources <- 0
            agent.Resources <- own + pool
            pool
    printfn "inst %s resources went from %i to %i, member %s resources went from %i to %i" inst.Name pool inst.Resources agent.Name own agent.Resources                
    sendMessage (Some (Appropriated(agent.ID, appropriatedR, inst.ID))) inst        

//************************* Principle 3 *********************************/
let openIssue head inst = 
    if head.RoleOf=Some (Head(inst.ID)) then
        inst.IssueStatus <- true
        printfn "head %s of %s has opened an issue for voting" head.Name inst.Name
    else
        printfn "agent %s does not have the authority to open issues for voting in %s" head.Name inst.Name

let closeIssue head inst = 
    if head.RoleOf=Some (Head(inst.ID)) then
        inst.IssueStatus <- false
        printfn "head %s of %s has closed an issue from voting" head.Name inst.Name
    else
        printfn "agent %s does not have the authority to close issues in %s" head.Name inst.Name
          

        