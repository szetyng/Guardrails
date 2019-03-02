#load "holon.fsx"
open Holon

let sendMessage msg recipient = 
    let msgQ = recipient.MessageQueue
    match msg with
    | Some m -> recipient.MessageQueue <- msgQ @ [m]
    | None -> printfn "No message to be sent to %s" recipient.Name

// apply agent inst -> Applied (agent, inst)
// TODO: send message saying Applied(agent, inst)
let applyToInst agent inst = 
    let applicationRes = 
        // if agent.Role = None
        match agent.RoleOf with
        | None -> Some (Applied (agent.ID, inst.ID))
        | Some _ -> None
    // add Applied(agent, inst) to inst message queue    
    sendMessage applicationRes inst    



// is gatekeep empowered to include agent into inst -> bool
let powToInclude gatekeep agent inst = 
    // check if they applied
    let crit = [Applied (agent.ID, inst.ID)]
    let rec checkPower gatekeep agent inst crit = 
        match crit with
        | c::rest ->
            if List.contains c platformEnv then
                checkPower gatekeep agent inst rest
            else 
                false
        | [] -> true
    // other knowledge         
    if agent.RoleOf = None then    
        checkPower gatekeep agent inst crit 
    else
        false         

        
// gatekeep includes agent into inst -> 
let includeToInst gatekeep agent inst = 
    if powToInclude gatekeep agent inst then 
        agent.RoleOf <- Some (Member(inst.ID))
        // let memLst = inst.Members
        // inst.Members <- memLst @ [agent.ID] // is this nec?

    



