#load "holon.fsx"
#load "platform.fsx"
open Holon
open Platform

// default agent
let def = 
    {     
        ID = 0;   
        Name = " ";
        Resources = 0;
        RoleOf = None;
        SanctionLevel = 0;
        OffenceLevel = 0;
        MessageQueue = []
    }
let agentNames = 
    [
        "tom"; "april"; "donna"; "jerry"; "amy"; "rosa"; "terry";
        "ben"; "mark" ; "andy"; "chris"; "ann"; "jeremy"; "jennifer"; "eleanor";
        "michael"; "tahani"; "jason"; "janet"; "gen"; "derek"; "chidi"
    ]

let createAgent name id = {def with Name=name; ID=id}

let initAgents nameLst currID = 
    let sz = List.length nameLst
    List.map2 createAgent nameLst [currID..sz+currID-1]

let initParksAgents = 
    let parks = {createAgent "parks" 0 with Resources=100}
    let ron = {createAgent "ron" 1 with RoleOf=Some (Head(parks.ID))}
    let leslie = {createAgent "leslie" 2 with RoleOf=Some (Gatekeeper(parks.ID))}
    [parks ; ron ; leslie]

let parksAgents = initParksAgents
let otherAgents = initAgents agentNames ((getLatestId parksAgents) + 1)
let allAgents = parksAgents @ otherAgents


let simulate agents timestep =
    let supraHolons =
        List.map getSupraID agents // get all the supra-holon IDs of all the agents, if one exists - could also be repeated
        |> List.distinct 
        |> List.choose id // get the elements from the options
        |> List.map (getHolon agents) // get holon from ID
        |> List.choose id
    List.map (fun x -> printfn "Supra holons are %s" x.Name) supraHolons |> ignore 
    let heads = List.filter (fun h -> checkRole h "Head") agents
    List.map (fun x -> printfn "Heads are %s" x.Name) heads |> ignore 
    let gatekeepers = List.filter (fun g -> checkRole g "Gatekeeper") agents
    List.map (fun x -> printfn "Gatekeepers are %s" x.Name) gatekeepers |> ignore 
    // let monitors = List.filter (fun m -> checkRole m "Monitor") agents
    // List.map (fun x -> printfn "Monitors are %s" x.Name) monitors |> ignore 

    // TODO: what to do with supra institution?
    let getRoles inst = 
        let gatekeeper = List.find (fun g -> g.RoleOf = Some(Gatekeeper (inst.ID))) gatekeepers 
        let head = List.find (fun h -> h.RoleOf = Some(Head (inst.ID))) heads 
        //let monitor = List.find (fun m -> m.RoleOf = Some(Monitor (inst.ID))) monitors
        {Self = inst; Head = head; Gatekeeper = gatekeeper} //; Monitor = monitor}

    let includeMembers inst = 
        let applicantIDLst = findApplicants inst.Self
        let applicants = 
            applicantIDLst
            |> List.map (getHolon agents)
            |> List.choose id
        printfn "including members"        
        applicants
        |> List.map (fun a -> includeToInst inst.Gatekeeper a inst.Self)      
    
    supraHolons 
    |> List.map getRoles 
    |> List.map includeMembers
 

let x = List.last allAgents
applyToInst x (List.head allAgents) 


simulate allAgents 0
List.head allAgents


// let ben = {def with ID = 3; Name = "ben"}
// applyToInst ben parks
// parks
// includeToInst leslie ben parks
