#load "holon.fsx"
#load "platform.fsx"
#load "simulation.fsx"
open Holon
open Platform
open Simulation

// let topCap = 1000
// let midCap = 1000
// let bottomCap = 50

//let refillRateA = [High;High;Low;Low;Low;Low;High;High;High;High;Low;Low]
//let refillRateB = [High;Low;Low;High;High;High;High;Low;Low;Low;Low;High]



// default agent
let def = 
    {     
        ID = 0;   
        Name = " ";
        Resources = 0;
        MessageQueue = [];
        RoleOf = None;
        ResourceCap = 500;
        RefillRate = []
    }

let parksNames = 
    [
        "tom"; "donna" ; "jerry"; "ben"; "andy" ; "chris"; "mark"; "ann"; "jeremy"; "gary";
        "larry"; "ingrid"; "jen"; "bob"; "barb"; "kris"; "bruce"; "peter"; "parker"; "tony";
        "abba"; "bsb"; "nsync"; "blue" ; "westlife"
    ]
let brooklynNames = 
    [
        "jake"; "rosa"; "charles"; "michael"; "norm"; "kevin"; "adrian"; "madeline"; "gina"; "boyle";
        "steve"; "rogers"; "clint"; "scott"; "paul" ; "carol"; "thor"; "korg"; "meek"; "reek";
        "sansa"; "arya"; "jon"; "dany"; "tyrion"
    ]

// let agentNames = 
//     [
//         "eleanor"; "tahani"; "jason"; "janet"; "gen"; "derek"; "chidi"; "vicky"; "shawn"; "trevor"
//     ]

let createAgent name id resMax = {def with Name=name; ID=id; ResourceCap=resMax}
let createMemberAgent name id resMax inst role = 
    let actRole = 
        match role with
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

let init refillRateA refillRateB topCap midCap bottomCap = 
    let offices = createAgent "offices" 0 topCap
    let parks = {createMemberAgent "parks" 1 midCap offices "member" with RefillRate=refillRateA}
    let brooklyn = {createMemberAgent "brooklyn" 2 midCap offices "member" with RefillRate=refillRateB}

    let initParksAdmin = 
        [parks]

    let initBrooklynAdmin = 
        [brooklyn]

    let initAllAdmins = [offices] @ initParksAdmin @ initBrooklynAdmin


    let initParksOffice = createInstAgents parksNames parks ((getLatestId initAllAdmins) + 1) bottomCap
    let initBrooklynOffice = createInstAgents brooklynNames brooklyn ((getLatestId initParksOffice) + 1) bottomCap 
    //let initUnemployed = createOtherAgents agentNames ((getLatestId initBrooklynOffice) + 1) bottomCap
    let allAgents = initAllAdmins @ initParksOffice @ initBrooklynOffice //@ initUnemployed

    allAgents