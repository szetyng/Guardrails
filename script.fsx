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
        MessageQueue = [];
        RoleOf = None;
        CompliancyDegree = 1.0;
        SanctionLevel = 0;
        OffenceLevel = 0;
        RaMethod = None;
        WdMethod = None;
        MonitoringFreq = 0.5;
        MonitoringCost = 10;
    }

let parksNames = 
    [
        "tom"; "april"; "donna" ; "jerry"; "ben"; "andy" ; "chris"; "mark"; "ann"; "jeremy"
    ]
let brooklynNames = 
    [
        "amy"; "jake"; "rosa"; "charles"; "michael"; "norm"; "kevin"; "adrian"; "madeline"; "gina"
    ]

let agentNames = 
    [
        "eleanor"; "tahani"; "jason"; "janet"; "gen"; "derek"; "chidi"; "vicky"; "shawn"; "trevor"
    ]

let createAgent name id = {def with Name=name; ID=id}

let initAgents nameLst currID = 
    let sz = List.length nameLst
    List.map2 createAgent nameLst [currID..sz+currID-1]

let initParksPositions = 
    let parks = {createAgent "parks" 1 with Resources=100; RaMethod=Some Queue; WdMethod=Some Plurality}
    let ron = {createAgent "ron" 2 with RoleOf=Some (Head(parks.ID))}
    let leslie = {createAgent "leslie" 3 with RoleOf=Some (Gatekeeper(parks.ID))}
    [parks ; ron ; leslie]

let initBrooklynPositions = 
    let brooklyn = {createAgent "brooklyn" 4 with Resources=100; RaMethod=Some (Ration(20)); WdMethod=Some Plurality}
    let ray = {createAgent "ray" 5 with RoleOf=Some (Head(brooklyn.ID))}
    let terry = {createAgent "terry" 6 with RoleOf=Some (Gatekeeper(brooklyn.ID))}
    [brooklyn ; ray ; terry]

let offices = {createAgent "offices" 0 with Resources=100}
let initHier = [offices] @ initParksPositions @ initBrooklynPositions

let initParks = initAgents parksNames ((getLatestId initHier) + 1)
let initBrooklyn = initAgents brooklynNames ((getLatestId initParks) + 1)
let otherAgents = initAgents agentNames ((getLatestId initBrooklyn) + 1)
let allAgents = initHier @ initParks @ initBrooklyn @ otherAgents


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
    let baseHolons = List.except supraHolons agents 


    // let includeMembers inst = 
    //     let applicantIDLst = findApplicants inst.Self
    //     let applicants = 
    //         applicantIDLst
    //         |> List.map (getHolon agents)
    //         |> List.choose id
    //     printfn "including members"        
    //     applicants
    //     |> List.map (fun a -> includeToInst inst.Gatekeeper a inst.Self)      
    
    supraHolons 

 

let x = List.last allAgents
applyToInst x (List.head allAgents) 


simulate allAgents 0
List.head allAgents


// let ben = {def with ID = 3; Name = "ben"}
// applyToInst ben parks
// parks
// includeToInst leslie ben parks
