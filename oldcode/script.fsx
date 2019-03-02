open System.Collections
open System.Collections.Generic
open System.Collections.Generic
open System.Collections.Generic
#load "holon.fsx"
#load "platform.fsx"
open Holon 
open Platform

// Instantiate holons
let leslie = Holon("leslie")
let ron = Holon("ron")
let tom = Holon("tom")
let april = Holon("april")

// Instantiate supra-holon
let parksLst = [ron; leslie; tom; april]
let parks = createInstitution "parks" parksLst 5


// Principle 1: clearly defined boundaries
let p1Crit (applicant:Holon) (inst:Holon) = 
    [applicant.SanctionLevel < 2 ; inst.MembershipLimit > inst.Members.Length]

let ben = Holon("ben")
applyToInstitution ben parks p1Crit // should work

let mark = Holon("mark")
applyToInstitution mark parks p1Crit // should fail


// Principle 2: congruence to local conditions
demandResources leslie parks 50
demandResources ron parks 20
demandResources tom parks 100
demandResources april parks 10
demandResources ben parks 5

allocateResources ron parks 

// Principle 3: collective choice arrangements
parks.Issue <- true
voting leslie parks Ration
voting ron parks Queue
voting ben parks Ration
voting tom parks Queue
voting april parks Queue
voting mark parks Ration

parks.Issue <- false
declareWinner ron parks 
