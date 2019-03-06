#load "holon.fsx"
open Holon 

/// P1: Applicant applies to inst
let applyToInstitution (applicant:Holon) (inst:Holon) makeCrit = 
    let crit = makeCrit applicant inst
    let rec checkCrit (supraH:Holon) (h:Holon) lst = 
        match lst with 
        | [] -> true
        | true::rest -> checkCrit supraH h rest
        | false::_ -> false
    let applicationResult = 
        match checkCrit inst applicant crit with
        | true ->
            match inst.Gatekeeper with
            | Some gatekeeper ->
                printfn "%A is applying to Institution %A via Gatekeeper %A" applicant.Name inst.Name gatekeeper.Name
                Some (gatekeeper.IncludeMember inst)
            | None -> None
        | false -> Some (false)

    match applicationResult with
    | Some true ->
        printfn "Result of agent %A applying to %A is %A" applicant.Name inst.Name applicationResult
        inst.AddMember(applicant)
        applicant.JoinHolon(inst)
    | Some false -> printfn "Result of agent %A applying to %A is %A" applicant.Name inst.Name applicationResult
    | None -> printfn "%A does not have a gatekeeper, or gatekeeper did not reply - it cannot admit new member %A" inst.Name applicant.Name  

/// P2: indiv mem demands for r amount of resources from inst
let demandResources (mem:Holon) (inst:Holon) r = 
    let q = inst.DemandQ

    // TODO: operate in time slices, can demand if has not demanded in this time slice
    // mem is a member of inst and has no sanctions
    if (List.contains inst mem.MemberOf) && (mem.SanctionLevel=0)
        then 
            mem.Demanded <- r
            inst.DemandQ <- (List.append q [mem])

// let demandResources (mem:Holon) (inst:Holon) r = 
//     if powDemandResources mem inst then
//         let q = inst.DemandQ
//         inst.DemandQ <- q @ [mem]
//         mem.Demanded <-         

// P2: Head allocates resources to the members in the demand queue of the inst
let allocateResources (head:Holon) (inst:Holon)  = 
    // Do not deduct from resources 
    // Allocating, not appropriating yet
    let r = inst.Resources
    if Some head = inst.Head then 
        match inst.RaMethod with
        | Queue -> 
            let rec allocQ (head:Holon) (q:Holon list) (r:int) =
                match (q, r) with
                | mem::rest, res -> 
                    let d = mem.Demanded
                    if res >= d then 
                        // TODO: how to make head allocate resources? Does it matter?
                        mem.Allocated <- d
                        allocQ head rest (r-d)
                    else if res <> 0 && d <> 0 then
                        mem.Allocated <- res // nothing left to give, no need to recurse
                        printfn "Inst has finished allocating resources"
                    else 
                        mem.Allocated <- 0 // safety  
                | [], _ -> printfn "Inst has finished allocating resources"
            allocQ head inst.DemandQ r           
        | Ration ->
            let limit = inst.RationLimit
            let rec allocR (q:Holon list) (r:int) =
                match (q, r) with
                | mem::rest, res ->
                    let d = mem.Demanded
                    // if demand less than equal to ration, allocate demand
                    if d <= limit then
                        mem.Allocated <- d
                        allocR rest (r-d)
                    // if demand more than ration, allocate ration                
                    else if d > limit && res <> 0 then
                        mem.Allocated <- limit
                        allocR rest (r-limit)
                    else 
                        mem.Allocated <- 0
                | [], _ -> printfn "Inst has finished allocating resources"                
            allocR inst.DemandQ r                                              


// let appropriateResources (mem:Holon) (inst:Holon) = 
    // something to do with propensity of compliance here

// P3: indiv mem wants to vote
// TODO: how does it decide on what to vote for?
let voting (mem:Holon) (inst:Holon) vote = 
    // is a member and issue is open
    if (List.contains inst mem.MemberOf) && inst.Issue then 
        inst.AddVote vote
    else
        printfn "%A not allowed to vote" mem.Name    

// get winner using wdm
let winnerDetermination (inst:Holon) = 
    match inst.WdMethod with
    | Plurality -> 
        let w = 
            inst.Votelist
            |> Seq.countBy id
            |> Seq.maxBy snd
            |> fst
        inst.ClearVotes        
        inst.RaMethod <- w         


// P3: TODO: Head should be declaring winner when issue is open
// usind WDM
let declareWinner (head:Holon) (inst:Holon) = 
    if Some head = inst.Head && not inst.Issue then
        winnerDetermination inst
    else
        printfn "Issue is not yet closed"
    
        




// Platform helper functions    
/// name: name of supraholon
/// memberLst: initial members
/// membershipCrit: limit on number of members
let createInstitution (name:string) memberLst membershipCrit = 
    let inst = Holon(name)
    inst.Members <- memberLst
    List.map (fun (h:Holon) -> h.JoinHolon inst) memberLst |> ignore

    inst.Head <- (Some (List.item 0 memberLst))
    inst.Gatekeeper <- (Some (List.item 1 memberLst))
    
    // Principle 1: Membership criteria
    inst.MembershipLimit <- membershipCrit

    // Principle 2: Congruence
    inst.Resources <- 100

    inst
    





