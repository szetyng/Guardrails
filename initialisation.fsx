open System.Runtime.Hosting
open Simulation
#load "holon.fsx"
#load "platform.fsx"
#load "physical.fsx"
#load "simulation.fsx"
open Holon
open Platform
open Physical
open Simulation

// default agent
let def = 
    {     
        ID = 0;   
        Name = " ";
        Resources = 0.0;
        MessageQueue = [];
        RoleOf = None;
        CompliancyDegree = 1.0;
        SanctionLevel = 0;
        OffenceLevel = 0;
        RaMethod = None;
        WdMethod = None;
        MonitoringFreq = 0.5;
        MonitoringCost = 10.0;
        IssueStatus = false;
        SanctionLimit = 2;
        ResourceCap = 500.0;
    }

let parksNames = 
    [
        "tom"; "donna" ; "jerry"; "ben"; "andy" ; "chris"; "mark"; "ann"; "jeremy"
    ]
let brooklynNames = 
    [
        "jake"; "rosa"; "charles"; "michael"; "norm"; "kevin"; "adrian"; "madeline"; "gina"
    ]

let agentNames = 
    [
        "eleanor"; "tahani"; "jason"; "janet"; "gen"; "derek"; "chidi"; "vicky"; "shawn"; "trevor"
    ]

let createAgent name id initRes resMax = {def with Name=name; ID=id; Resources=initRes; ResourceCap=resMax}
let createMemberAgent name id inst initRes resMax = {def with RoleOf=Some(Member(inst.ID)); Name=name; ID=id; Resources=initRes; ResourceCap=resMax}

let initAgents nameLst currID initRes resMax = 
    let sz = List.length nameLst
    List.map2 (fun name ind -> createAgent name ind initRes resMax)nameLst [currID..sz+currID-1]

let initMemberAgents nameLst inst currID initRes resMax = 
    let sz = List.length nameLst
    List.map2 (fun n i -> createMemberAgent n i inst initRes resMax) nameLst [currID..sz+currID-1] 

let offices = {createAgent "offices" 0 500.0 500.0 with RaMethod=Some Queue; WdMethod=Some Plurality}
let mike = {createAgent "mike" 1 0.0 200.0 with RoleOf=Some(Head(offices.ID))}
let dan = {createAgent "dan" 2 0.0 200.0 with RoleOf=Some(Monitor(offices.ID))}
let parks = {createAgent "parks" 3 200.0 200.0 with RoleOf=Some (Member(offices.ID)); RaMethod=Some Queue; WdMethod=Some Plurality}
let brooklyn = {createAgent "brooklyn" 4 200.0 200.0 with RoleOf=Some (Member(offices.ID)); RaMethod=Some (Ration(Some 20.0)); WdMethod=Some Plurality}

let initParksPositions = 
    let ron = {createAgent "ron" 5 0.0 20.0 with RoleOf=Some (Head(parks.ID))}
    let leslie = {createAgent "leslie" 6 0.0 20.0 with RoleOf=Some (Gatekeeper(parks.ID))}
    let april = {createAgent "april" 7 0.0 20.0 with RoleOf=Some (Monitor(parks.ID))}
    [parks ; ron ; leslie; april]

let initBrooklynPositions = 
    let ray = {createAgent "ray" 8 0.0 20.0 with RoleOf=Some (Head(brooklyn.ID))}
    let terry = {createAgent "terry" 9 0.0 20.0 with RoleOf=Some (Gatekeeper(brooklyn.ID))}
    let amy = {createAgent "amy" 10 0.0 20.0 with RoleOf=Some (Monitor(brooklyn.ID))}
    [brooklyn ; ray ; terry; amy]

let initHier = [offices; mike; dan] @ initParksPositions @ initBrooklynPositions

let initParks = initMemberAgents parksNames parks ((getLatestId initHier) + 1) 0.0 20.0
let initBrooklyn = initMemberAgents brooklynNames brooklyn ((getLatestId initParks) + 1) 0.0 20.0
let otherAgents = initAgents agentNames ((getLatestId initBrooklyn) + 1) 0.0 20.0
let allAgents = initHier @ initParks @ initBrooklyn @ otherAgents

