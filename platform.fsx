open System.Collections.Generic
#load "holon.fsx"
open Holon

//********************** Helper functions *******************************/
/// add Msg to Recipient.MessageQueue
let sendMessage msg recipient = 
    match msg with
    | Some m -> 
        recipient.MessageQueue <- recipient.MessageQueue @ [m]
        printfn "%s has received message %A" recipient.Name m 
    | None -> printfn "No message to be sent to %s" recipient.Name

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
let checkFromQ agent fact toRemove = 
    match (List.contains fact agent.MessageQueue), toRemove with
    | true, true -> 
        // TODO: Remove only FIRST occurence of fact (bad for voting otherwise)
        agent.MessageQueue <- List.except [fact] agent.MessageQueue 
        printfn "%A has been removed from %s inbox" fact agent.Name 
        true
    | true, false -> true
    | false, _ -> false    

let isAgentInInst agent inst = 
    match agent.RoleOf, inst.ID with
    | Some (Member (sID)), i| Some (Head (sID)), i | Some (Monitor (sID)), i| Some (Gatekeeper (sID)), i -> sID = i
    | _ -> false

//************************* Principle 1 *********************************/
/// Sends message from agent to inst
let applyToInst agent inst = 
    let applicationRes = 
        match agent.RoleOf with
        | None -> Some (Applied (agent.ID, inst.ID))
        | Some _ -> None
    sendMessage applicationRes inst    
    //applicationRes 

/// SIDE-EFFECT: inst.MessageQueue
/// is gatekeep empowered to include agent into inst -> bool
let powToInclude gatekeep agent inst = 
    // Check against message queue
    let facts = [Applied (agent.ID, inst.ID)]
    // TODO: don't remove the fact yet... maybe not all of it was TRUE
    let factCheck = List.fold (fun state f -> state && checkFromQ inst f true) true facts
    
    // Check others
    let checkCritLst = [factCheck ; (gatekeep.RoleOf = Some (Gatekeeper(inst.ID)))]     
    not (List.contains false checkCritLst) 
             
      
/// SIDE-EFFECT: agent.RoleOf
/// gatekeep includes agent into inst 
let includeToInst gatekeep agent inst = 
    if powToInclude gatekeep agent inst then 
        agent.RoleOf <- Some (Member(inst.ID))
        printfn "%s has included %s as a member of %s" gatekeep.Name agent.Name inst.Name
    else
        printfn "%s has decided to not include %s as a member of %s" gatekeep.Name agent.Name inst.Name
    

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

let demandResources agent r inst = 
    let demandRes = 
    // TODO: time step
        match powToDemand agent inst 0 with
        | true -> Some (Demanded (agent.ID, r, inst.ID))
        | false -> None
    sendMessage demandRes inst

let powToAllocate head inst agent r =
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
    let voteRes = 
        match (powToVote agent inst) with
        | true -> 
            sendMessage (Some (VotedRaMeth(agent.ID))) inst
            Some (Vote(vote, inst.ID))
        | false -> 
            printfn "%s cannot vote on issue" agent.Name 
            None
    sendMessage voteRes inst    

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
        let winner =         
            match inst.WdMethod with
            | Some Plurality -> Some (countVotesPlurality votelist)     
            | _ -> 
                printfn "Winner declaration method is %A, not usable" inst.WdMethod
                None
        match winner with
        | Some (Vote(ra, _) ) -> 
            printfn "%s has changed raMethod in %s to %A" head.Name inst.Name ra            
            inst.RaMethod <- Some ra                   
        | _ -> printfn "Not a valid issue"    
    else 
        printfn "%s has decided not to declare winner to raMethod issue in institution %s" head.Name inst.Name                 


         