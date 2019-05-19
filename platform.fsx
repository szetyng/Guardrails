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
    | "Member", Some (Member (_)) -> true
    | "Null", None | "None", None -> true
    | _, _ -> false

/// get holonID of the supra-institution that Agent belongs to
/// TODO: list might not be sorted
let getSupraID agent = 
    match agent.RoleOf with
    | Some (Member supraID) -> Some supraID
    | None -> None

/// get holon record from the list of Agents based on its HolonID
let getHolon agents holonID = List.tryFind (fun a -> a.ID = holonID) agents

let getSupraHolon agent agents= 
    match agent.RoleOf with
    | Some (Member supraID) -> getHolon agents supraID
    | None -> None

let deleteFromQ agent msg = 
    let rec removeFirstMsg lst m =
        match lst with
        | h::rest when h=m -> rest // no recursion, won't remove other occurences
        | h::rest -> h::(removeFirstMsg rest m)
        | _ -> []
    agent.MessageQueue <- removeFirstMsg agent.MessageQueue msg  

let isAgentInInst agent inst = 
    match agent.RoleOf with
    | Some (Member (sID)) -> sID = inst.ID
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

//********************** Platform-related *******************************/
let refillResources inst r = 
    let max = inst.ResourceCap
    let newTotal = 
        match inst.Resources+r with
        | newTot when newTot<=max ->    
            printfn "inst %s has refilled its resources by %i to %i" inst.Name r newTot
            newTot
        | _ -> 
            printfn "inst %s has refilled its resources to the max: %i" inst.Name max
            max
    inst.Resources <- newTotal 

/// refillRate example: [High;High;Medium;Low]
let decideOnRefill inst time refillRate = 
    let nrOfSeasons = List.length refillRate 
    let max = float(inst.ResourceCap)

    let amtFloat = 
        match nrOfSeasons with
        | 0 -> 
            printfn "did not set a refill rate for %s, will refill to max" inst.Name
            max
        | nr ->        
            let timeBlock = time/1
            let seasonInd = timeBlock%nr // which season are we in
            let season = refillRate.[seasonInd] 
       
            match season with
                | High -> max
                | Medium -> 0.5*max
                | Low -> 0.25*max
    int(amtFloat)        
    
let getGenerationAmt inst time = 
    let refillRate = inst.RefillRate
    decideOnRefill inst time refillRate

/// inst is a midHolon
let calculateTaxSubsidy taxBracket taxRate needPayTax subsidyRate agentLst inst = 
    let getPopulation acc holon =  
        let baseMembers = getBaseMembers agentLst holon
        acc + List.length baseMembers

    let totalMembers = getPopulation 0 inst
    let resPerMember = inst.Resources/totalMembers
    match resPerMember, needPayTax with
    | (xPerMem, true) when xPerMem > taxBracket -> 
        printfn "%s's members get %i each" inst.Name (xPerMem - taxRate)
        Some (Tax(inst.ID, taxRate*totalMembers))
    | (xPerMem, false) when xPerMem > taxBracket ->
        printfn "%s's members get %i each" inst.Name (xPerMem)      
        None  
    | (xPerMem, _) when xPerMem < taxBracket ->
        // TODO: wrong to print it here, bc subsidy might get rejected 
        printfn "%s's members get %i each" inst.Name (xPerMem + subsidyRate)
        Some (Subsidy(inst.ID, subsidyRate*totalMembers))
    | x, _ -> 
        printfn "%s's members get %i each" inst.Name x
        None
