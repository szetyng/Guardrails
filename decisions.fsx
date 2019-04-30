open System.Collections.Generic
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
    let max = float(inst.ResourceCap)
   
    let amtFloat = 
        match season with
            | High -> max
            | Medium -> 0.5*max
            | Low -> 0.25*max
    int(amtFloat)        
    

//************************* Principle 2 *********************************/
// agent decides on how to demand for r
let decideOnDemandR agent = 
    let greed = agent.Greediness
    let cap = float(agent.ResourceCap)
    let amtFloat = 
        match greed with
        | g when g>0.5 -> 0.75*cap
        | _ -> 0.5*cap
    int(amtFloat)
    

// TODO: agent decides what r is, from Allocated or from greed
let decideOnAppropriateR agent inst = 
    let max = agent.ResourceCap
    max/2

//************************* Principle 2 *********************************/
// TODO: head to change ramethod based on level of resources in the inst
let decideElection tMin tMax inst = 
    let res = float(inst.Resources)
    let minFloat = tMin*(float(inst.ResourceCap))
    let maxFloat = tMax*(float(inst.ResourceCap))
    match res with
    | amt when amt<=minFloat -> true
    | amt when amt>=maxFloat -> true
    | _ -> false

let decideVote agent = 
    let res = float(agent.Resources)
    let safety = 0.4*float(agent.ResourceCap)
    match res with
    | amt when amt>safety -> Queue
    | _ -> Ration(None)
