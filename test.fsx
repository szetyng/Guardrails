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
    }

let parks = {def with ID=0; Name="parks"; Resources=100}
let ron = {def with ID=1; Name="ron"; RoleOf=Some (Head(parks.ID))}
let leslie = {def with ID=2; Name="leslie"; RoleOf=Some(Gatekeeper(parks.ID))}
let tom = {def with ID=3; Name="tom"}
let april = {def with ID=4; Name="april"}
let donna = {def with ID=5; Name="donna"}
let jerry = {def with ID=6; Name="jerry"}

let testGetLatestID =
    let agentsUnsorted = [parks ; leslie ; tom ; jerry ; april ; donna ; ron]
    let i = getLatestId agentsUnsorted
    i = 6


// Tests
testGetLatestID