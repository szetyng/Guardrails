open System.Collections.Generic
#load "holon.fsx"
#load "platform.fsx"
open Holon
open Platform

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
        IssueStatus = false
    }

let parks = {def with ID=0; Name="parks"; Resources=100; WdMethod = Some Plurality; RaMethod=Some (Ration(Some 10))}
let ron = {def with ID=1; Name="ron"; RoleOf=Some (Head(parks.ID))}
let leslie = {def with ID=2; Name="leslie"; RoleOf=Some(Gatekeeper(parks.ID))}
let tom = {def with ID=3; Name="tom"; RoleOf=Some(Member(parks.ID))}
let april = {def with ID=4; Name="april"; RoleOf=Some(Member(parks.ID))}
let donna = {def with ID=5; Name="donna"}
let jerry = {def with ID=6; Name="jerry"}

let clearParksQueue() =
    parks.MessageQueue <- []

let testGetLatestID() =
    let agentsUnsorted = [parks ; leslie ; tom ; jerry ; april ; donna ; ron]
    let i = getLatestId agentsUnsorted
    i = 6

let testDemandResources() = 
    demandResources tom 20 parks
    demandResources tom 5 parks

let testPowToAllocate() = 
    demandResources tom 20 parks
    demandResources april 40 parks
    let tomGets = powToAllocate ron parks tom 40
    printfn "tom gets %i" tomGets

let testVoting() = 
    doVote tom parks Queue
    parks.IssueStatus <- true
    doVote tom parks Queue
    doVote april parks (Ration(None))
    doVote tom parks Queue

let testDeclareWinner() = 
    parks.IssueStatus <- true
    doVote tom parks Queue
    doVote april parks (Ration(None))
    doVote jerry parks Queue
    doVote leslie parks (Ration(None))
    parks.IssueStatus <- false
    declareWinner ron parks 

let testPowToReport() = 
    let checkPower() = 
        let ans = powToReport april tom parks
        match ans with
        | true -> printfn "april has the power to report tom"
        | false -> printfn "april does not have the power to report tom"

    assignMonitor leslie april parks
    checkPower()

    assignMonitor ron april parks
    checkPower() 

let testSanction() = 
    assignMonitor ron april parks
    let tomWants = 10
    demandResources tom tomWants parks
    let tomGets = powToAllocate ron parks tom tomWants

    // allocate resources to tom
    parks.MessageQueue <- parks.MessageQueue @ [Allocated(tom.ID,tomGets,parks.ID)]
    printfn "tom gets %i" tomGets

    // tom appropriates resources (without removing Allocated message)
    parks.MessageQueue <- parks.MessageQueue @ [Appropriate(tom.ID,tomWants,parks.ID)]

    // monitor and head do their jobs
    reportGreed april tom parks 
    sanctionMember ron tom parks

let testUpholdSanctions() = 
    assignMonitor ron april parks
    let tomWants = 20
    demandResources tom tomWants parks
    let tomGets = powToAllocate ron parks tom tomWants

    // allocate resources to tom
    parks.MessageQueue <- parks.MessageQueue @ [Allocated(tom.ID,tomGets,parks.ID)]
    printfn "tom gets %i" tomGets

    // tom appropriates resources (without removing Allocated message)
    parks.MessageQueue <- parks.MessageQueue @ [Appropriate(tom.ID,tomWants,parks.ID)]

    // monitor and head do their jobs
    reportGreed april tom parks 
    sanctionMember ron tom parks

    // tom appeals
    appealSanction tom 1 parks
    upholdAppeal ron tom 1 parks



// Tests, make them functions so that they are only called here
clearParksQueue()
testGetLatestID()
testDemandResources()
testPowToAllocate()
testVoting()
parks
testDeclareWinner()
testPowToReport()
testSanction()
testUpholdSanctions()