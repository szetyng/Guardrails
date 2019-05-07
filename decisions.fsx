#load "holon.fsx"
#load "platform.fsx"
#load "physical.fsx"
open Holon
open Platform
open Physical

type Rate = High | Medium | Low

let rand = System.Random()

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
    let allocatedR =
        let rec getR q = 
            match q with
            | Allocated(ag,r,i)::_ when ag=agent.ID -> r
            | _::rest -> getR rest
            | [] -> 0
        getR inst.MessageQueue
    // let demandedR = decideOnDemandR agent
    // let deficit = demandedR - allocatedR

    // let allocatedOrDemanded deficit = 
    //     let desireToCheat = float(deficit) / (agent.CompliancyDegree * float(agent.Resources))
    //     let fearOfCheating = (inst.MonitoringFreq * float(agent.SanctionLevel)) / float(inst.SanctionLimit)
    //     let risk = desireToCheat - fearOfCheating
    //     match risk with
    //     | r when r<0.0 -> allocatedR
    //     | r -> demandedR

    // match deficit with
    // | 0 -> allocatedR
    // | def -> allocatedOrDemanded def    

    allocatedR

//************************* Principle 3 *********************************/
// TODO: head to change ramethod based on level of resources in the inst
let decideElection tMin tMax inst = 
    let res = float(inst.Resources)
    let minFloat = tMin*(float(inst.ResourceCap))
    let maxFloat = tMax*(float(inst.ResourceCap))
    match res with
    | amt when amt<=minFloat -> 
        printfn "inst %s opened election because resources are too low: %i" inst.Name inst.Resources
        true
    | amt when amt>=maxFloat -> 
        printfn "inst %s opened elections because resources are too high: %i" inst.Name inst.Resources
        true
    | _ -> false

// let decideVote agent inst = 
//     // let rand = System.Random()
//     let vote = rand.NextDouble()
//     match vote with
//     | x when x<0.5 -> 
//         //printfn "%f" x
//         Queue
//     | x -> 
//         //printfn "%f" x
//         Ration(None)

//     // let res = float(agent.Resources)
//     // let safety = 0.4*float(agent.ResourceCap)
//     // match res with
//     // | amt when amt>safety -> Queue
//     // | _ -> Ration(None)

let decideVote agent inst = 
    let res = float(inst.Resources)
    let cap = float(inst.ResourceCap)
    match res with
    | amt when amt<=0.4*cap -> Ration(None)
    | amt -> Queue