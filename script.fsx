open System.Windows.Forms

#load "holon.fsx"
#load "platform.fsx"
#load "simulation.fsx"
#load "init.fsx"
open Holon
open Platform
open Simulation
open Init

let simType = Reasonable
let timeBegin = 0
let timeMax = 100
let taxBracket = 25
let taxRate = 20
let subsidyRate = 10
let refillRateA = [High; High; Low; Low; Low; High; High; Low]//; Low; Low; High]
let refillRateB = [Low; High; High; Low; High; High; Low; Low]//; Low; Low; Low]
let topCap = 1000
let midCap = 1000
let bottomCap = 50


let allAgents = init refillRateA refillRateB topCap midCap bottomCap
let answer = simulate allAgents simType timeBegin timeMax taxBracket taxRate subsidyRate

let transformSatis state = 
    let getSatis acc state = 
        let res,ben = state
        match ben with
        | 0 when res=midCap -> acc// + 10 // get dynamic version
        | amt -> acc + amt
    let lst = List.map2 (fun r b -> r,b) state.ResourcesState state.CurrBenefit
    let satis = List.scan (getSatis) 0 lst |> List.tail
    {state with RunningBenefit=satis}



// let runningTotal = List.scan (+) 0 >> List.tail
// let transformCumulative state = 
//     let newBen = runningTotal state.CurrBenefit
//     {state with RunningBenefit=newBen}
    
let cumAnswer = 
    answer
    |> List.map transformSatis

printfn "%A" cumAnswer
// let parksCurrBen = cumAnswer.[1].CurrBenefit
// let parksRes = cumAnswer.[1].ResourcesState
// let parksRunningBen = cumAnswer.[1].RunningBenefit

// let brookCurrBen = cumAnswer.[2].CurrBenefit
// let brookRes = cumAnswer.[2].ResourcesState
// let brookRunningBen = cumAnswer.[2].RunningBenefit
