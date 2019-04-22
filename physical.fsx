open System.Collections.Generic
#load "holon.fsx"
#load "platform.fsx"
open Holon
open Platform

//************************* Misc *********************************/
let refillResources inst r = 
    inst.Resources <- inst.Resources + r
    printfn "inst has refilled its resources by %i to %i" r inst.Resources

//************************* Principle 1 *********************************/
/// Gatekeeper checks for applications
let gatekeepChecksInclude gatekeep inst agents = 
    let getInfoAndInclude mem =
        let memHolon = getHolon agents mem
        match memHolon with
        | Some h -> includeToInst gatekeep h inst
        | None -> printfn "Holon not found"
    let doesInclude msg =
        match msg with
        | Applied(mem,ins) when ins=inst.ID -> getInfoAndInclude mem  
        | _ -> ()
    inst.MessageQueue
    |> List.map doesInclude 
    |> ignore


/// Gatekeeper checks if anyone should be sanctioned -> how often should this happen?
let gatekeepChecksExclude gatekeep inst agents = 
    let checkSanction agent = 
        match agent.SanctionLevel, inst.SanctionLimit with
        | agentSanction, instLimit when agentSanction>=instLimit -> excludeFromInst gatekeep agent inst
        | _ -> ()

    agents
    |> List.filter (fun a -> isAgentInInst a inst)
    |> List.map checkSanction
    |> ignore


//************************* Principle 2 *********************************/
/// Sends Allocated message to inst's MessageQueue
let allocateResources head inst agent =
    let demandedR = powToAllocate head inst agent

    match demandedR with
    | 0 -> printfn "head %s cannot allocate resources to %s in inst %s" head.Name agent.Name inst.Name
    | x -> sendMessage (Some (Allocated(agent.ID,x,inst.ID))) inst

/// Move r resources from inst to agent, if available
/// Sends Appropriated message to inst's MessageQueue
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
/// Sets IssueStatus of inst to true
let openIssue head inst = 
    if head.RoleOf=Some (Head(inst.ID)) then
        inst.IssueStatus <- true
        printfn "head %s of %s has opened an issue for voting" head.Name inst.Name
    else
        printfn "agent %s does not have the authority to open issues for voting in %s" head.Name inst.Name

/// Sets IssueStatus of inst to false
let closeIssue head inst = 
    if head.RoleOf=Some (Head(inst.ID)) then
        inst.IssueStatus <- false
        printfn "head %s of %s has closed an issue from voting" head.Name inst.Name
    else
        printfn "agent %s does not have the authority to close issues in %s" head.Name inst.Name
          
//************************* Principle 4 *********************************/
/// Monitor goes through MessageQueue of inst to find misbehaving agents
/// Misbehaving agents' OffenceLevel++
/// agents is a list of all agents, including inst itself -> why? Seems unnecessary, fix it TODO
let monitorDoesJob monitor inst agents = 
    let hasBeenAllocated mem ins allocMsg = 
        match allocMsg with
        | Allocated(ag,x,i) -> ag=mem && i=ins
        | _ -> false                
    let checkGreed msg = 
        let reportIfTakeMore mem taken allocRecord = 
            let memHolon = getHolon agents mem
            match allocRecord, memHolon with
            | Some (Allocated(_,given,_)), Some m -> 
                if taken > given then
                    reportGreed monitor m inst
            | None, Some m -> reportGreed monitor m inst // not allocated
            | _, Some m -> printfn "%s is not greedy in %s" m.Name inst.Name
            | _ -> printfn "What is the monitor doing?? %A \n %A" allocRecord memHolon 
        match msg with
        | Appropriated(mem,rTaken,ins) ->
            List.tryFind (fun x -> hasBeenAllocated mem ins x) inst.MessageQueue
            |> reportIfTakeMore mem rTaken  
        | _ -> () 
             
    inst.MessageQueue
    |> List.map checkGreed 
    |> ignore

//************************* Principle 5 *********************************/
/// Head goes through all the agents in inst and corrects SanctionLevel 
/// if there's a mismatch in SanctionLevel and OffenceLevel
/// OffenceLevel can never be lower than SanctionLevel
/// Both can only take values of {0, 1, 2} -> orly? TODO
let headDoesJob head inst agents = 
    let checkOffence agent = 
        match agent.OffenceLevel, agent.SanctionLevel with
        | o, s when o>s -> sanctionMember head agent inst
        | o, s when o=s -> ()
        | o, s -> printfn "%s has offence=%i and sanction=%i, why?" agent.Name o s

        // | 0, 0 -> ()
        // | 1, 1 -> ()
        // | 2, 2 -> ()        
        // | 1, 0 -> sanctionMember head agent inst
        // | 2, 1 -> sanctionMember head agent inst
        // | 2, 0 -> sanctionMember head agent inst
        // | x, y -> printfn "%s has offence=%i and sanction=%i, why?" agent.Name x y

    agents
    |> List.filter (fun a -> isAgentInInst a inst)
    |> List.map checkOffence
    |> ignore

//************************* Principle 6 *********************************/
/// Head goes through MessageQueue of inst to get Appeal messages
/// and decrements the corressp agents' OffenceLevel and SanctionLevel
/// TODO how does head decide? Make it frequency-based for simplicity
let headFeelsForgiving head inst agents sancCeil = 
    let doesUphold msg = 
        let getInfoAndUphold mem x = 
            let memHolon = getHolon agents mem
            match memHolon, x with
            | Some m, s when s>0 && s<=sancCeil -> upholdAppeal head m s inst
            | Some m, _ -> printfn "%s cannot appeal for sanction level %i" m.Name x
            | _ -> printfn "Member %A not found" memHolon
        match msg with
        | Appeal(mem,x,ins) when ins=inst.ID -> getInfoAndUphold mem x 
        | _ -> ()

    inst.MessageQueue
    |> List.map doesUphold
    |> ignore


