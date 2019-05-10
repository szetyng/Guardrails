#load "holon.fsx"
#load "platform.fsx"
#load "physical.fsx"
#load "decisions.fsx"
#load "simulation.fsx"
//#load "initialisation.fsx"
#load "init2.fsx"
open Holon
open Platform
open Physical
open Decisions
open Simulation
//open Initialisation
open Init2

let answer = simulate allAgents 0 20 5 [Low;Low;High]

let runningTotal = List.scan (+) 0 >> List.tail
let transformCumulative state = 
    let newBen = runningTotal state.BenefitState
    {state with BenefitState=newBen}
    
let cumAnswer = 
    answer
    |> List.map transformCumulative
let parksBen = cumAnswer.[1].BenefitState
let brookBen = cumAnswer.[2].BenefitState

List.map2 (+) parksBen brookBen
