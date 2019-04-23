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
        IssueStatus = false;
        SanctionLimit = 2;
        ResourceCap = 500;
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

let createAgent name id = {def with Name=name; ID=id}
let createMemberAgent name id inst = {def with RoleOf=Some(Member(inst.ID)); Name=name; ID=id}

let initAgents nameLst currID = 
    let sz = List.length nameLst
    List.map2 createAgent nameLst [currID..sz+currID-1]

let initMemberAgents nameLst inst currID = 
    let sz = List.length nameLst
    List.map2 (fun n i -> createMemberAgent n i inst) nameLst [currID..sz+currID-1] 

let offices = {createAgent "offices" 0 with Resources=100}
let parks = {createAgent "parks" 1 with RoleOf=Some (Member(offices.ID)); Resources=100; RaMethod=Some Queue; WdMethod=Some Plurality}
let brooklyn = {createAgent "brooklyn" 5 with RoleOf=Some (Member(offices.ID)); Resources=100; RaMethod=Some (Ration(Some 20)); WdMethod=Some Plurality}

let initParksPositions = 
    let ron = {createAgent "ron" 2 with RoleOf=Some (Head(parks.ID))}
    let leslie = {createAgent "leslie" 3 with RoleOf=Some (Gatekeeper(parks.ID))}
    let april = {createAgent "april" 4 with RoleOf=Some (Monitor(parks.ID))}
    [parks ; ron ; leslie; april]

let initBrooklynPositions = 
    let ray = {createAgent "ray" 6 with RoleOf=Some (Head(brooklyn.ID))}
    let terry = {createAgent "terry" 7 with RoleOf=Some (Gatekeeper(brooklyn.ID))}
    let amy = {createAgent "amy" 8 with RoleOf=Some (Monitor(brooklyn.ID))}
    [brooklyn ; ray ; terry; amy]

let initHier = [offices] @ initParksPositions @ initBrooklynPositions

let initParks = initMemberAgents parksNames parks ((getLatestId initHier) + 1)

let initBrooklyn = initMemberAgents brooklynNames brooklyn ((getLatestId initParks) + 1)
let otherAgents = initAgents agentNames ((getLatestId initBrooklyn) + 1)
let allAgents = initHier @ initParks @ initBrooklyn @ otherAgents

