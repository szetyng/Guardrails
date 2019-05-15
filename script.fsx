open System.Windows.Forms
#load "holon.fs"
#load "platform.fs"
#load "simulation.fs"
#load "init.fsx"
open Holon
open Platform
open Simulation
open Init

let simType = Strict
let timeBegin = 0
let timeMax = 100
let taxBracket = 25
let taxRate = 20
let subsidyRate = 10
let refillRateA = [High; High; Low; Low; Low; High; High; Low]//; Low; Low; High]
let refillRateB = [Low; High; High; Low; High; High; Low; Low]//; Low; Low; Low]


let allAgents = init refillRateA refillRateB
let answer = simulate allAgents simType timeBegin timeMax taxBracket taxRate subsidyRate

let runningTotal = List.scan (+) 0 >> List.tail
let transformCumulative state = 
    let newBen = runningTotal state.CurrBenefit
    {state with RunningBenefit=newBen}
    
let cumAnswer = 
    answer
    |> List.map transformCumulative
let parksCurrBen = cumAnswer.[1].CurrBenefit
let parksRes = cumAnswer.[1].ResourcesState
let parksRunningBen = cumAnswer.[1].RunningBenefit

let brookCurrBen = cumAnswer.[2].CurrBenefit
let brookRes = cumAnswer.[2].ResourcesState
let brookRunningBen = cumAnswer.[2].RunningBenefit




