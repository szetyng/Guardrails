#load "holon.fsx"
#load "platform.fsx"
open Holon 
open Platform


let leslie = Holon("leslie")
let ron = Holon("ron")
let tom = Holon("tom")
let april = Holon("april")
let parksLst = [leslie; ron; tom; april]
let parks = createParks parksLst

let ben = Holon("ben")
parks.Members
applyToInstitution ben parks

parks.Members.Length
ben.MemberOf

let mark = Holon("mark")
applyToInstitution mark parks

