open System.Collections.Generic
#load "holon.fsx"
#load "platform.fsx"
#load "physical.fsx"
open Holon
open Platform
open Physical

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
    allocateResources ron parks tom
    allocateResources ron parks april

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
    let tomGets = powToAllocate ron parks tom 

    // allocate resources to tom
    parks.MessageQueue <- parks.MessageQueue @ [Allocated(tom.ID,tomGets,parks.ID)]
    printfn "tom gets %i" tomGets

    // tom appropriates resources (without removing Allocated message)
    parks.MessageQueue <- parks.MessageQueue @ [Appropriated(tom.ID,tomWants,parks.ID)]

    // monitor and head do their jobs
    reportGreed april tom parks 
    sanctionMember ron tom parks

let testUpholdSanctions() = 
    assignMonitor ron april parks
    let tomWants = 20
    demandResources tom tomWants parks
    let tomGets = powToAllocate ron parks tom 

    // allocate resources to tom
    parks.MessageQueue <- parks.MessageQueue @ [Allocated(tom.ID,tomGets,parks.ID)]
    printfn "tom gets %i" tomGets

    // tom appropriates resources (without removing Allocated message)
    parks.MessageQueue <- parks.MessageQueue @ [Appropriated(tom.ID,tomWants,parks.ID)]

    // monitor and head do their jobs
    reportGreed april tom parks 
    sanctionMember ron tom parks

    // tom appeals
    appealSanction tom 1 parks
    upholdAppeal ron tom 1 parks

// april will fail because she is not first in queue
let testPhysicalAppropriate() = 
    demandResources tom 20 parks
    demandResources april 10 parks
    allocateResources ron parks april
    allocateResources ron parks tom
    appropriateResources tom parks 10

let testRefill() = 
    refillResources parks 50

let testPhyDeclareWinner() = 
    openIssue ron parks
    doVote tom parks Queue
    doVote april parks (Ration(None))
    doVote jerry parks Queue
    doVote leslie parks (Ration(None))
    closeIssue ron parks
    declareWinner ron parks

let testPhyMonitor() = 
    assignMonitor ron april parks
    demandResources tom 20 parks
    demandResources leslie 30 parks
    demandResources april 10 parks
    allocateResources ron parks april
    allocateResources ron parks tom
    allocateResources ron parks leslie
    appropriateResources tom parks 10 // ok
    appropriateResources leslie parks 30 // took too much
    appropriateResources april parks 10 // was not allocated any
    monitorDoesJob april parks [parks; ron; april; tom; leslie]

let testPhyHead() = 
    tom.OffenceLevel <- 1
    tom.SanctionLevel <- 0
    april.OffenceLevel <- 1
    april.SanctionLevel <- 1
    headDoesJob ron parks [parks; ron; april; tom; leslie]

let testPhyHeadForgives() = 
    tom.OffenceLevel <- 1
    tom.SanctionLevel <- 1
    april.OffenceLevel <- 2
    april.SanctionLevel <- 2
    leslie.OffenceLevel <- 0
    leslie.SanctionLevel <- 0

    appealSanction tom 1 parks
    appealSanction april 1 parks // wrong -> no message sent
    appealSanction april 2 parks
    appealSanction leslie 0 parks // can't appeal 0

    headFeelsForgiving ron parks [parks; ron; april; tom; leslie]


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
testPhysicalAppropriate()
testRefill()
testPhyDeclareWinner()
testPhyMonitor()
testPhyHead()
testPhyHeadForgives()
