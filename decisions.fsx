open System.Collections.Generic
#load "holon.fsx"
#load "platform.fsx"
#load "physical.fsx"
open Holon
open Platform
open Physical

type Rate = High | Medium | Low

//************************* Misc *********************************/
/// refillRate example: [High;High;Medium;Low]
let decideOnRefill inst time refillRate = 
    let nrOfSeasons = List.length refillRate 
    let timeBlock = time/5
    let seasonInd = timeBlock%nrOfSeasons // which season are we in
    let season = refillRate.[seasonInd] 

    let max = inst.ResourceCap
   
    match season with
    | High -> max
    | Medium -> 
        let rFloat = 0.5*float(max)
        int(rFloat)
    | Low -> 
        let rFloat = 0.25*float(max)
        int(rFloat)    

//************************* Principle 2 *********************************/
// TODO: head to change ramethod based on level of resources in the inst

// TODO: agent decides on how to demand for r
let decideOnDemandR agent inst = 2

// TODO: agent decides what r is, from Allocated or from greed
let decideOnAppropriateR agent inst = 
    let max = agent.ResourceCap
    max/2

