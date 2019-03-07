#load "holon.fsx"
open Holon

//********************** Helper functions *******************************/
let sendMessage msg recipient = 
    // let msgQ = recipient.MessageQueue
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
let checkAndRemoveFromQ agent fact = 
    if List.contains fact agent.MessageQueue then 
        agent.MessageQueue <- List.except [fact] agent.MessageQueue 
        true
    else
        false    

let isAgentInInst agent inst = 
    match agent.RoleOf, inst.ID with
    | Some (Member (sID)), i| Some (Head (sID)), i | Some (Monitor (sID)), i| Some (Gatekeeper (sID)), i -> sID = i
    | _ -> false

//************************* Principle 1 *********************************/

// apply agent inst -> Applied (agent, inst)
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
    let factCheck = List.fold (fun state f -> state && checkAndRemoveFromQ inst f) true facts
    
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
    let hasAgentDemanded = 
        let rec checkQ q = 
            match q with
            | Demanded (aID, _, iID)::_ -> (a = aID) && (i = iID)
            | _::rest -> checkQ rest
            | [] -> false
        checkQ inst.MessageQueue        
    let checkCritLst = [isAgentInInst agent inst ; agent.SanctionLevel = 0; not hasAgentDemanded]
    not (List.contains false checkCritLst)

let demandResources agent r inst = 
    // TODO: time step
    if powToDemand agent inst 0 then 
        inst.MessageQueue <- inst.MessageQueue @ [Demanded (agent.ID, r, inst.ID)]
        Some (Demanded (agent.ID, r, inst.ID))
    else
        None 
