open System.Collections.Generic
#load "holon.fsx"
#load "platform.fsx"
#load "physical.fsx"
open Holon
open Platform
open Physical

//************************* Misc *********************************/
let decideOnRefill inst = 100

//************************* Principle 2 *********************************/
// TODO: head to change ramethod based on level of resources in the inst

// TODO: agent decides on how to demand for r
let decideOnDemandR agent inst = 2

// TODO: agent decides what r is, from Allocated or from greed
let decideOnAppropriateR agent inst = 
    let max = agent.ResourceCap
    max/2

    