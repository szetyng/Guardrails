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
          
//************************* Principle 4 *********************************/
/// agents is a list of all agents, including inst itself -> why? Seems unnecessary, fix it TODO
let monitorDoesJob monitor inst agents = 
    let allocatedLst = 
        let rec getAllocated q lst = 
            match q with  
            | Allocated(mem,x,ins)::rest ->
                if ins=inst.ID then getAllocated rest (lst @ [Allocated(mem,x,ins)])  
                else getAllocated rest lst
            | _::rest -> getAllocated rest lst
            | [] -> lst
        getAllocated inst.MessageQueue [] 
    let appropriateLst = 
        let rec getAppropriators q lst = 
            match q with  
            | Appropriated(mem,x,ins)::rest ->
                if ins=inst.ID then getAppropriators rest (lst @ [Appropriated(mem,x,ins)])  
                else getAppropriators rest lst
            | _::rest -> getAppropriators rest lst
            | [] -> lst
        getAppropriators inst.MessageQueue [] 

    let sameMemIns mem ins allocMsg = 
        match allocMsg with
        | Allocated(ag,x,i) -> ag=mem && i=ins
        | _ -> false                
    let checkGreed apprRecord = 
        let reportIfTakeMore mem taken ins allocRecord = 
            let memHolon = getHolon agents mem
            let insHolon = getHolon agents ins
            match allocRecord, memHolon, insHolon with
            | Some (Allocated(_,given,_)), Some m, Some i -> 
                if taken > given then
                    reportGreed monitor m i
            | None, Some m, Some i -> reportGreed monitor m i // not allocated
            | _, Some m, Some i -> printfn "%s is not greedy in %s" m.Name i.Name
            | _ -> printfn "What is the monitor doing?? %A \n %A \n %A \n" allocRecord memHolon insHolon
        match apprRecord with
        | Appropriated(mem,rTaken,ins) ->
            List.tryFind (fun x -> sameMemIns mem ins x) allocatedLst
            |> reportIfTakeMore mem rTaken ins 
        | _ -> printfn "Error with %A" apprRecord   
             
    appropriateLst 
    |> List.map checkGreed 
    |> ignore

//************************* Principle 5 *********************************/
let headDoesJob head inst agents = 
    let checkOffence agent = 
        match agent.OffenceLevel, agent.SanctionLevel with
        | 0, 0 -> ()
        | 1, 1 -> ()
        | 2, 2 -> ()        
        | 1, 0 -> sanctionMember head agent inst
        | 2, 1 -> sanctionMember head agent inst
        | 2, 0 -> sanctionMember head agent inst
        | x, y -> printfn "%s has offence=%i and sanction=%i, why?" agent.Name x y

    agents
    |> List.map checkOffence
    |> ignore

//************************* Principle 6 *********************************/
// how does head decide? Make it frequency-based for simplicity
let headFeelsForgiving head inst agents = 
    let appealLst = 
        let rec getAppeals q lst = 
            match q with    
            | Appeal(mem,x,ins)::rest ->
                if ins=inst.ID then getAppeals rest (lst @ [(mem,x)])
                else getAppeals rest lst
            | _::rest -> getAppeals rest lst
            | [] -> lst
        getAppeals inst.MessageQueue []  

    let upholds mem x =
        let memHolon = getHolon agents mem
        match memHolon, x with
        | Some m, 1 | Some m, 2 -> upholdAppeal head m x inst
        | Some m, _ -> printfn "%s cannot appeal for sanction level %i" m.Name x
        | _ -> printfn "Member %A not found" memHolon


    appealLst
    |> List.map (fun (m,x) -> upholds m x)
    |> ignore


