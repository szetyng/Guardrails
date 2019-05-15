#load "holon.fsx"
open Holon

//********************** Helper functions *******************************/
/// get holonID of last agent in list of Agents
let getLatestId agents = 
    agents
    |> List.sortBy (fun a -> a.ID) 
    |> List.last 
    |> fun a -> a.ID 

/// check if Agent occupies Role
let checkRole agent role =
    match (role, agent.RoleOf) with
    | "Head", Some (Head (_)) -> true
    | "Gatekeeper", Some (Gatekeeper (_)) -> true
    | "Monitor", Some (Monitor (_)) -> true
    | "Member", Some (Member (_)) -> true
    | "Null", None | "None", None -> true
    | _, _ -> false

/// get holonID of the supra-institution that Agent belongs to
/// TODO: list might not be sorted
let getSupraID agent = 
    match agent.RoleOf with
    | Some (Head supraID) -> Some supraID
    | Some (Gatekeeper supraID) -> Some supraID
    | Some (Monitor supraID) -> Some supraID
    | Some (Member supraID) -> Some supraID
    | None -> None

/// get holon record from the list of Agents based on its HolonID
let getHolon agents holonID = List.tryFind (fun a -> a.ID = holonID) agents

let getSupraHolon agent agents= 
    match agent.RoleOf with
    | Some (Head supraID) -> getHolon agents supraID
    | Some (Gatekeeper supraID) -> getHolon agents supraID
    | Some (Monitor supraID) -> getHolon agents supraID
    | Some (Member supraID) -> getHolon agents supraID
    | None -> None

let findApplicants inst =     
    let getApplicant msg = 
        match msg with
        | Applied(x,i) when i=inst.ID -> Some x
        | _ -> None 

    inst.MessageQueue
    |> List.map getApplicant 
    |> List.choose id                   
    
/// SIDE-EFFECT: agent.MessageQueue
/// if toRemove, only removes first occurence of fact
let checkFromQ agent fact toRemove = 
    match (List.contains fact agent.MessageQueue), toRemove with
    | true, true -> 
        let rec removeFirstX lst x = 
            match lst with
            | h::rest when h=x -> rest // no recursion, won't remove other occurences
            | h::rest -> h::(removeFirstX rest x)
            | _ -> []
        agent.MessageQueue <- removeFirstX agent.MessageQueue fact
        // printfn "%A has been removed from %s inbox" fact agent.Name 
        true
    | true, false -> true
    | false, _ -> false  

let deleteFromQ agent msg = 
    let rec removeFirstMsg lst m =
        match lst with
        | h::rest when h=m -> rest // no recursion, won't remove other occurences
        | h::rest -> h::(removeFirstMsg rest m)
        | _ -> []
    agent.MessageQueue <- removeFirstMsg agent.MessageQueue msg  

let deleteAllocatedAndAppr inst = 
    let rec removeMsg oldQ newQ = 
        match oldQ with
        | Allocated(_)::rest -> removeMsg rest newQ
        | Appropriated(_)::rest -> removeMsg rest newQ
        | h::rest -> removeMsg rest ([h] @ newQ)
        | [] -> newQ
    let update = removeMsg inst.MessageQueue []
    inst.MessageQueue <- update    

let deletedVotedCount inst = 
    let rec removeMsg oldQ newQ  = 
        match oldQ with
        | VotedRaMeth(_)::rest -> removeMsg rest newQ
        | h::rest -> removeMsg rest ([h] @ newQ)
        | [] -> newQ
    let update = removeMsg inst.MessageQueue []    
    inst.MessageQueue <- update
    

let isAgentInInst agent inst = 
    match agent.RoleOf with
    | Some (Member (sID)) | Some (Head (sID)) | Some (Monitor (sID)) | Some (Gatekeeper (sID)) -> sID = inst.ID
    | _ -> false

let hasMembers agents inst = 
    let rec checkForMember agentLst =
        match agentLst with
        | agent::_ when isAgentInInst agent inst -> true
        | _::rest -> checkForMember rest
        | [] -> false
    checkForMember agents  

let hasBoss holons inst =    
    let rec checkForBoss holonLst = 
        match holonLst with
        | holon::_ when isAgentInInst inst holon -> true
        | _::rest -> checkForBoss rest
        | [] -> false
    checkForBoss holons    

let getBaseMembers agents inst = 
    agents
    |> List.filter (fun a -> a.RoleOf=Some(Member(inst.ID))) 

let printNames agents = 
    agents
    |> List.map (fun a -> printf "%s, " a.Name)
    |> ignore

    printfn ""

//************************* Principle 1 *********************************/
/// Sends Applied message from agent to inst
/// if agent qualifies as a member
let applyToInst agent inst = 
    let checkCritLst = [agent.RoleOf = None; agent.SanctionLevel < inst.SanctionLimit]
    match List.contains false checkCritLst with
    | false -> 
        printfn "agent %s applied to inst %s" agent.Name inst.Name
        inst.MessageQueue <- inst.MessageQueue @ [Applied (agent.ID, inst.ID)]
    | true -> 
        printfn "agent %s failed to apply to inst %s" agent.Name inst.Name

/// SIDE-EFFECT: inst.MessageQueue
/// is gatekeep empowered to include agent into inst -> bool
let powToInclude gatekeep agent inst = 
    let didAgentApply a i =
        let application = Applied (a.ID, i.ID)
        checkFromQ i application true

    let checkCritLst = [didAgentApply agent inst ; (gatekeep.RoleOf = Some (Gatekeeper(inst.ID)))]     
    not (List.contains false checkCritLst) 
             
/// SIDE-EFFECT: agent.RoleOf
/// gatekeep includes agent into inst 
let includeToInst gatekeep agent inst = 
    if powToInclude gatekeep agent inst then 
        agent.RoleOf <- Some (Member(inst.ID))
        printfn "%s has included %s as a member of %s" gatekeep.Name agent.Name inst.Name
    else
        printfn "%s is not empowered to include %s as a member of %s" gatekeep.Name agent.Name inst.Name

let powToExclude gatekeep agent inst =
    let checkCritLst = [gatekeep.RoleOf = Some (Gatekeeper(inst.ID)); agent.SanctionLevel >= inst.SanctionLimit]
    not (List.contains false checkCritLst)

let excludeFromInst gatekeep agent inst = 
    if powToExclude gatekeep agent inst then
        agent.RoleOf <- None    
        printfn "%s has excluded %s from %s" gatekeep.Name agent.Name inst.Name
    else
        printfn "%s is not empowered to exclude %s from %s" gatekeep.Name agent.Name inst.Name
//************************* Principle 2 *********************************/

let powToDemand agent inst step = 
    let hasAgentDemanded = 
        let rec checkDemand q = 
            match q with
            | Demanded(a,_,i)::_ when a=agent.ID && i=inst.ID -> true
            | _::rest -> checkDemand rest
            | [] -> false 
        checkDemand inst.MessageQueue              
    let checkCritLst = [isAgentInInst agent inst ; agent.SanctionLevel = 0; not hasAgentDemanded]
    not (List.contains false checkCritLst)

let demandResources agent r inst t = 
    match powToDemand agent inst t with
    | true -> 
        // printfn "member %s demanded %i from inst %s" agent.Name r inst.Name 
        inst.MessageQueue <- inst.MessageQueue @ [Demanded (agent.ID,r,inst.ID)]  
    | false -> printfn "member %s is not empowered to demand %i from inst %s" agent.Name r inst.Name       

// let powToAllocate head inst agent =
//     let a = agent.ID
//     let i = inst.ID
//     let demandLst = 
//         let rec getDemands q dLst= 
//             match q with
//             | Demanded(ag,x,ins)::rest -> getDemands rest (dLst @ [Demanded(ag,x,ins)])
//             | _::rest -> getDemands rest dLst
//             | [] -> dLst
//         getDemands inst.MessageQueue [] 
//     if head.RoleOf = Some (Head(inst.ID)) then
//         match inst.RaMethod, demandLst with
//         | Some Queue, Demanded(aID, r, iID)::_ -> 
//             if aID=a && iID=i then 
//                 checkFromQ inst (Demanded(aID, r, iID)) true |> ignore
//                 if r<=inst.Resources then r
//                 else if r>inst.Resources then inst.Resources
//                 else 0
//             else 0
//         | Some (Ration (Some rPrime)), Demanded(aID, r, iID)::_ ->
//             if aID=a && iID=i then
//                 checkFromQ inst (Demanded(aID, r, iID)) true |> ignore
//                 if r<=rPrime && r<=inst.Resources then r
//                 else if r>rPrime && rPrime<=inst.Resources then rPrime
//                 else if r>rPrime && rPrime>inst.Resources then inst.Resources 
//                 else 0
//             else 0
//         | _ , _ -> 0     
//     else 0       

let powToAllocate head inst agent = 
    let getDemands q =
        let getDemand msg = 
            match msg with
            | Demanded(_,_,i) when i=inst.ID -> Some msg
            | _ -> None
        List.map getDemand q
        |> List.choose id

    match head.RoleOf = Some(Head(inst.ID)) with
    | true -> 
        let demandLst = getDemands inst.MessageQueue
        match inst.RaMethod, demandLst with
        | Some Queue, Demanded(a,r,i)::_ when a=agent.ID ->
            deleteFromQ inst (Demanded(a,r,i))
            match r with
            | demand when demand<=inst.Resources -> demand
            | _ -> inst.Resources
        | Some (Ration(Some ration)), Demanded(a,r,i)::_ when a=agent.ID ->
            deleteFromQ inst (Demanded(a,r,i))    
            match r with
            | demand when demand<=ration && demand<=inst.Resources -> demand
            | demand when demand<=ration && demand>inst.Resources -> inst.Resources
            | demand when demand>ration && ration<=inst.Resources -> ration
            | demand when demand>ration && ration>inst.Resources -> inst.Resources  
            | _ -> 0
        | _, _ -> 0
    | false -> 0               



//************************* Principle 3 *********************************/
let powToVote agent inst = 
    let hasAgentVoted = 
        let rec checkVote q =
            match q with
            | VotedRaMeth(a)::_ when a=agent.ID -> true
            | _::rest -> checkVote rest
            | [] -> false
        checkVote inst.MessageQueue        
      
    let checkCritLst = [isAgentInInst agent inst ; not hasAgentVoted ; inst.IssueStatus]
    not (List.contains false checkCritLst) 

let doVote agent inst vote = 
    match (powToVote agent inst) with
    | true ->
        // printfn "agent %s has voted in inst %s" agent.Name inst.Name
        inst.MessageQueue <- inst.MessageQueue @ [VotedRaMeth(agent.ID); Vote(vote, inst.ID)]
    | false -> printfn "agent %s cannot vote on issue in inst %s" agent.Name inst.Name   
 
let powToDeclare head inst = 
    let checkCritLst = [head.RoleOf = Some(Head(inst.ID)) ; not inst.IssueStatus]
    not (List.contains false checkCritLst)

let countVotesPlurality votelist = 
    votelist
    |> Seq.countBy id
    |> Seq.maxBy snd
    |> fst

let declareWinner head inst = 
    let winner = 
        match powToDeclare head inst with
        | true -> 
            let collectVote msg = 
                match msg with
                | Vote (_, i) when i=inst.ID -> 
                    deleteFromQ inst msg
                    Some msg
                | _ -> None
            let votelist = 
                inst.MessageQueue
                |> List.map collectVote 
                |> List.choose id 
            printfn "votelist for %s: %A" inst.Name votelist 

            match inst.WdMethod, votelist with
            | _, [] ->
                printfn "error: No one voted"
                None
            | Some Plurality, vLst -> Some (countVotesPlurality vLst)
            | _ ->
                printfn "winner declaration method %A is not implemented" inst.WdMethod
                None
        | false -> 
            printfn "%s is not empowered to declare winners in inst %s" head.Name inst.Name
            None  

    deletedVotedCount inst
    match winner with
    | Some (Vote(ra, _)) ->
        printfn "%s has changed raMethod in %s to %A" head.Name inst.Name ra
        inst.RaMethod <- Some ra
    | _ -> printfn "no winner declared in %s" inst.Name

//************************* Principle 4 *********************************/
let powToAssignMonitor head monitor inst = 
    let checkCritLst = [head.RoleOf = Some (Head(inst.ID)) ; monitor.RoleOf = Some (Member(inst.ID))]
    not (List.contains false checkCritLst)

let assignMonitor head monitor inst = 
    if powToAssignMonitor head monitor inst then
        printfn "%s has assigned %s as monitor of %s" head.Name monitor.Name inst.Name
        monitor.RoleOf <- Some (Monitor inst.ID)
    else
        printfn "%s has failed to assign %s as monitor to %s" head.Name monitor.Name inst.Name     

let powToReport monitor agent inst = 
    let checkCritLst = [monitor.RoleOf = Some (Monitor(inst.ID)) ; isAgentInInst agent inst]
    not (List.contains false checkCritLst)

//************************* Principle 5 *********************************/
let reportGreed monitor agent inst = 
    let allocatedR =     
        let rec getAllocatedR lst = 
            match lst with
            | Allocated(mem,x,ins)::_ when mem=agent.ID && ins=inst.ID -> x
            | _::rest -> getAllocatedR rest
            | [] -> 0
        getAllocatedR inst.MessageQueue 
    let appropriatedR =
        let rec getAppropriatedR lst = 
            match lst with
            | Appropriated(mem,x,ins)::_ when mem=agent.ID && ins=inst.ID -> x
            | _::rest -> getAppropriatedR rest
            | [] -> 0
        getAppropriatedR inst.MessageQueue
    if appropriatedR>allocatedR && powToReport monitor agent inst then
        agent.OffenceLevel <- agent.OffenceLevel + 1
        printfn "monitor %s reported member %s in institution %s; offence level increased to %i" monitor.Name agent.Name inst.Name agent.OffenceLevel
    else
        printfn "monitor %s failed to report member %s in institution %s" monitor.Name agent.Name inst.Name

let powToSanction head inst = 
    head.RoleOf = Some (Head(inst.ID))

let sanctionMember head agent inst =
    let sancLvl = agent.OffenceLevel
    if powToSanction head inst then
        agent.SanctionLevel <- sancLvl
        printfn "head %s changed sanction level of %s in inst %s to %i" head.Name agent.Name inst.Name agent.SanctionLevel
    else
        printfn "head %s failed to sanction member %s in inst %s" head.Name agent.Name inst.Name    

//************************* Principle 6 *********************************/
let powToAppeal agent s inst = 
    let checkCritLst = [isAgentInInst agent inst; agent.SanctionLevel = s]
    not (List.contains false checkCritLst)

let appealSanction agent s inst = 
    match powToAppeal agent s inst with
    | true -> 
        printfn "member %s appealed to inst %s for sanction level %i" agent.Name inst.Name s
        inst.MessageQueue <- inst.MessageQueue @ [Appeal(agent.ID,s,inst.ID)]
    | false -> printfn "member %s failed to appeal to inst %s for sanction level %i" agent.Name inst.Name s    
    // let appealRes = 
    //     match powToAppeal agent s inst with
    //     | true -> Some (Appeal(agent.ID,s,inst.ID))
    //     | false -> None
    // sendMessage appealRes inst    

let powToUphold head agent s inst = 
    let checkCritLst = [head.RoleOf = Some (Head inst.ID); checkFromQ inst (Appeal(agent.ID,s,inst.ID)) true]
    not (List.contains false checkCritLst)

let upholdAppeal head agent s inst =
    if powToUphold head agent s inst then
        agent.SanctionLevel <- agent.SanctionLevel - 1
        agent.OffenceLevel <- agent.OffenceLevel - 1
        printfn "head %s in inst %s has decremented the following in member %s:" head.Name inst.Name agent.Name
        printfn "sanctions to %i, offence to %i" agent.SanctionLevel agent.OffenceLevel
    else
        printfn "head %s is not empowered to uphold appeal of member %s in inst %s" head.Name agent.Name inst.Name
        





