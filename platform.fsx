open System.Collections.Generic
#load "holon.fsx"
open Holon

//********************** Helper functions *******************************/
/// add Msg to Recipient.MessageQueue
let sendMessage msg recipient = 
    match msg with
    | Some m -> 
        recipient.MessageQueue <- recipient.MessageQueue @ [m]
        // printfn "%s has received message %A" recipient.Name m 
    | None -> 
        ()
        //printfn "No message to be sent to %s" recipient.Name

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
    let q = inst.MessageQueue
    let i = inst.ID
    let applicants = []
    let rec getting q lst = 
        match q with
        | Applied(x, iID)::rest -> 
            if iID = i then getting rest (List.append lst [x])
            else getting rest lst
        | _::rest -> getting rest lst
        | [] -> lst
    getting q applicants                
    
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

let isAgentInInst agent inst = 
    match agent.RoleOf, inst.ID with
    | Some (Member (sID)), i| Some (Head (sID)), i | Some (Monitor (sID)), i| Some (Gatekeeper (sID)), i -> sID = i
    | _ -> false

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
    let a = agent.ID
    let i = inst.ID
    // List.contains is not good enough due to anonymous variable r in the message
    let hasAgentDemanded = 
        let rec checkQ q = 
            match q with
            | Demanded (aID, _, iID)::rest -> 
                if (a = aID) && (i = iID) then true
                else checkQ rest         
            | _::rest -> checkQ rest
            | [] -> false
        checkQ inst.MessageQueue        
    let checkCritLst = [isAgentInInst agent inst ; agent.SanctionLevel = 0; not hasAgentDemanded]
    not (List.contains false checkCritLst)

let demandResources agent r inst t = 
    match powToDemand agent inst t with
    | true -> 
        // printfn "member %s demanded %i from inst %s" agent.Name r inst.Name 
        inst.MessageQueue <- inst.MessageQueue @ [Demanded (agent.ID,r,inst.ID)]  
    | false -> printfn "member %s is not empowered to demand %i from inst %s" agent.Name r inst.Name       

let powToAllocate head inst agent =
    let a = agent.ID
    let i = inst.ID
    let demandLst = 
        let rec getDemands q dLst= 
            match q with
            | Demanded(ag,x,ins)::rest -> getDemands rest (dLst @ [Demanded(ag,x,ins)])
            | _::rest -> getDemands rest dLst
            | [] -> dLst
        getDemands inst.MessageQueue [] 
    if head.RoleOf = Some (Head(inst.ID)) then
        match inst.RaMethod, demandLst with
        | Some Queue, Demanded(aID, r, iID)::_ -> 
            if aID=a && iID=i then 
                checkFromQ inst (Demanded(aID, r, iID)) true |> ignore
                if r<=inst.Resources then r
                else if r>inst.Resources then inst.Resources
                else 0
            else 0
        | Some (Ration (Some rPrime)), Demanded(aID, r, iID)::_ ->
            if aID=a && iID=i then
                checkFromQ inst (Demanded(aID, r, iID)) true |> ignore
                if r<=rPrime && r<=inst.Resources then r
                else if r>rPrime && rPrime<=inst.Resources then rPrime
                else if r>rPrime && rPrime>inst.Resources then inst.Resources 
                else 0
            else 0
        | _ , _ -> 0     
    else 0       


//************************* Principle 3 *********************************/
let powToVote agent inst = 
    let a = agent.ID
    let hasAgentVoted = 
        let rec checkQ q =
            match q with
            | VotedRaMeth(aID)::rest -> 
                if aID=a then true
                else checkQ rest
            | _::rest -> checkQ rest
            | [] -> false
        checkQ inst.MessageQueue           

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
    if powToDeclare head inst then
        let i = inst.ID
        let votelist = 
            let rec getVotes q vLst = 
                match q with
                | Vote (ra, iID)::rest -> 
                    if iID=i then 
                        checkFromQ inst (Vote (ra, iID)) true |> ignore
                        getVotes rest (vLst @ [Vote (ra, i)])
                    else getVotes rest vLst
                | _::rest -> getVotes rest vLst
                | [] -> vLst
            getVotes inst.MessageQueue []
        printfn "votelist: %A" votelist        
        let winner =         
            match inst.WdMethod, votelist with
            | _, [] -> 
                printfn "Error: No one voted"
                None
            | Some Plurality, vLst -> Some (countVotesPlurality vLst)     
            | _ -> 
                printfn "Winner declaration method is %A, not usable" inst.WdMethod
                None
        match winner with
        | Some (Vote(ra, _) ) -> 
            printfn "%s has changed raMethod in %s to %A" head.Name inst.Name ra            
            inst.RaMethod <- Some ra                   
        | _ -> printfn "Not a valid issue"    
    else 
        printfn "%s is not empowered to declare winner to raMethod issue in institution %s" head.Name inst.Name                 

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
    let a = agent.ID
    let i = inst.ID
    let allocatedR =     
        let rec getAllocatedR lst = 
            match lst with
            | Allocated(mem,x,ins)::rest ->
                if mem=a && ins=i then x
                else getAllocatedR rest
            | _::rest -> getAllocatedR rest
            | [] -> 0
        getAllocatedR inst.MessageQueue 
    let appropriatedR =
        let rec getAppropriatedR lst = 
            match lst with
            | Appropriated(mem,x,ins)::rest ->
                if mem=a && ins=i then x
                else getAppropriatedR rest
            | _::rest -> getAppropriatedR rest
            | [] -> 0
        getAppropriatedR inst.MessageQueue
    if appropriatedR>allocatedR && powToReport monitor agent inst then
        agent.OffenceLevel <- agent.OffenceLevel + 1
        printfn "Monitor %s reported member %s in institution %s; offence level increased to %i" monitor.Name agent.Name inst.Name agent.OffenceLevel
    else
        printfn "Monitor %s failed to report member %s in institution %s" monitor.Name agent.Name inst.Name

let powToSanction head inst = 
    head.RoleOf = Some (Head(inst.ID))

let sanctionMember head agent inst =
    let sancLvl = agent.OffenceLevel
    if powToSanction head inst then
        agent.SanctionLevel <- sancLvl
        printfn "Head %s changed sanction level of %s in inst %s to %i" head.Name agent.Name inst.Name agent.SanctionLevel
    else
        printfn "Head %s failed to sanction member %s in inst %s" head.Name agent.Name inst.Name    

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
        





