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
        IssueRaMethStatus = false
    }

let parks = {def with ID=0; Name="parks"; Resources=100; WdMethod = Some Plurality; RaMethod=Some (Ration(10))}
let ron = {def with ID=1; Name="ron"; RoleOf=Some (Head(parks.ID))}
let leslie = {def with ID=2; Name="leslie"; RoleOf=Some(Gatekeeper(parks.ID))}
let tom = {def with ID=3; Name="tom"; RoleOf=Some(Member(parks.ID))}
let april = {def with ID=4; Name="april"; RoleOf=Some(Member(parks.ID))}
let donna = {def with ID=5; Name="donna"}
let jerry = {def with ID=6; Name="jerry"}

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
    doVote tom parks IssueRaMeth (RaMeth(Queue))
    parks.IssueRaMethStatus <- true
    doVote tom parks IssueRaMeth (RaMeth(Queue))
    doVote april parks IssueRaMeth (RaMeth(Ration(20)))
    doVote tom parks IssueRaMeth (RaMeth(Queue))

let testDeclareWinner() = 
    parks.IssueRaMethStatus <- true
    doVote tom parks IssueRaMeth (RaMeth(Queue))
    doVote april parks IssueRaMeth (RaMeth(Ration(20)))
    doVote jerry parks IssueRaMeth (RaMeth(Queue))
    doVote leslie parks IssueRaMeth (RaMeth(Ration(20)))
    parks.IssueRaMethStatus <- false
    declareWinner ron parks IssueRaMeth

// Tests, make them functions so that they are only called here
testGetLatestID()
testDemandResources()
testPowToAllocate()
testVoting()
parks
testDeclareWinner()