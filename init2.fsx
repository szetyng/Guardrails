#load "holon.fsx"
#load "platform.fsx"
#load "physical.fsx"
#load "simulation.fsx"
open Holon
open Platform
open Physical
open Simulation

let topCap = 1000000000
let midCap = 1000
let bottomCap = 50

//let refillRateA = [High;High;Low;Low;Low;Low;High;High;High;High;Low;Low]
//let refillRateB = [High;Low;Low;High;High;High;High;Low;Low;Low;Low;High]

let refillRateA = [High;High;Low;Low;Low;Low;High]
let refillRateB = [Low; High; High; Low; Low; Low; Low]

let midGreed = 0.75

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
        WdMethod = Some Plurality;
        MonitoringFreq = 0.5;
        MonitoringCost = 10;
        IssueStatus = false;
        SanctionLimit = 2;
        ResourceCap = 500;
        Greediness = 0.5;
        RiskTolerance = 0.5;
        RefillRate = []
    }

let parksNames = 
    [
        "tom"; "donna" ; "jerry"; "ben"; "andy" ; "chris"; "mark"; "ann"; "jeremy"; "gary";
        "larry"; "ingrid"; "jen"; "bob"; "barb"; "kris"; "bruce"; "peter"; "parker"; "tony"
    ]
let brooklynNames = 
    [
        "jake"; "rosa"; "charles"; "michael"; "norm"; "kevin"; "adrian"; "madeline"; "gina"; "boyle";
        "steve"; "rogers"; "clint"; "scott"; "paul" ; "carol"; "thor"; "korg"; "meek"; "reek"
    ]

// let agentNames = 
//     [
//         "eleanor"; "tahani"; "jason"; "janet"; "gen"; "derek"; "chidi"; "vicky"; "shawn"; "trevor"
//     ]

let createAgent name id resMax = {def with Name=name; ID=id; ResourceCap=resMax}
let createMemberAgent name id resMax inst role = 
    let actRole = 
        match role with
        | "head" -> Some(Head(inst.ID)) 
        | "monitor" -> Some(Monitor(inst.ID))
        | "gatekeeper" -> Some(Gatekeeper(inst.ID))
        | "member" -> Some(Member(inst.ID)) 
        | _ -> None
    {createAgent name id resMax with RoleOf=actRole}
let createOtherAgents nameLst currID resMax = 
    let sz = List.length nameLst
    List.map2 (fun name ind -> createAgent name ind resMax) nameLst [currID..sz+currID-1]
let createInstAgents nameLst inst currID resMax = 
    let sz = List.length nameLst
    List.map2 (fun name ind -> createMemberAgent name ind resMax inst "member") nameLst [currID..sz+currID-1]

//********************************************************************************************************************
let offices = {createAgent "offices" 0 topCap with RaMethod=Some Queue}
let parks = {createMemberAgent "parks" 1 midCap offices "member" with RaMethod=Some Queue; Greediness=midGreed; RefillRate=refillRateA}
let brooklyn = {createMemberAgent "brooklyn" 2 midCap offices "member" with RaMethod=Some Queue; Greediness=midGreed; RefillRate=refillRateB}

let initParksAdmin = 
    let ron = createMemberAgent "ron" 3 bottomCap parks "head"
    let leslie = createMemberAgent "leslie" 4 bottomCap parks "gatekeeper"
    let april = createMemberAgent "april" 5 bottomCap parks "monitor"
    [parks; ron; leslie; april]

let initBrooklynAdmin = 
    let ray = createMemberAgent "ray" 6 bottomCap brooklyn "head"
    let terry = createMemberAgent "terry" 7 bottomCap brooklyn "gatekeeper"
    let amy = createMemberAgent "amy" 8 bottomCap brooklyn "gatekeeper"
    [brooklyn; ray; terry; amy]

let initAllAdmins = [offices] @ initParksAdmin @ initBrooklynAdmin


let initParksOffice = createInstAgents parksNames parks ((getLatestId initAllAdmins) + 1) bottomCap
let initBrooklynOffice = createInstAgents brooklynNames brooklyn ((getLatestId initParksOffice) + 1) bottomCap 
//let initUnemployed = createOtherAgents agentNames ((getLatestId initBrooklynOffice) + 1) bottomCap
let allAgents = initAllAdmins @ initParksOffice @ initBrooklynOffice //@ initUnemployed
