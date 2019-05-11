#load "holon.fsx"
#load "platform.fsx"
#load "physical.fsx"
#load "decisions.fsx"
#load "strictSim.fsx"
//#load "initialisation.fsx"
#load "strictInit.fsx"
open Holon
open Platform
open Physical
open Decisions
open StrictSim
//open Initialisation
open StrictInit

let answer = simulate allAgents 0 20 25 10 5 [Low;Low;High]

let runningTotal = List.scan (+) 0 >> List.tail
let transformCumulative state = 
    let newBen = runningTotal state.BenefitState
    {state with BenefitState=newBen}
    
let cumAnswer = 
    answer
    |> List.map transformCumulative
let parksBen = cumAnswer.[1].BenefitState
let brookBen = cumAnswer.[2].BenefitState
//List.map2 (+) parksBen brookBen
