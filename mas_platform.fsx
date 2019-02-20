#load "mas_holon.fsx"
open Mas_holon

let platformEnv:InstFacts list = []

let applyToInst agent inst = 
    match agent.Role with
    | None -> Some (Applied (agent.ID, inst.ID))
    | Some _ -> None

let powToInclude gatekeep agent inst = 
    // make a list of requirements
    let crit = [Applied (agent.ID, inst.ID) ; RoleOf (gatekeep.ID, Some inst.ID, Some Gatekeeper)]
    // check them all against knowledge base
    let rec checkPower gatekeep agent inst crit = 
        match crit with
        | c::rest -> 
            if List.contains c platformEnv then
                checkPower gatekeep agent inst rest
            else
                false            
        | [] -> true
    checkPower gatekeep agent inst crit              
    
        

let includeToInst gatekeep agent inst = 
    if powToInclude gatekeep agent inst then
        let memLst = inst.Members
        agent.MemberOf <- Some inst
        inst.Members <- memLst @ [agent] 



