open System.Drawing

#load "holon.fsx"
#load "platform.fsx"
#load "simulation.fsx"
#load "init.fsx"
open Holon
open Platform
open Simulation
open Init

#load "packages/FSharp.Charting/FSharp.Charting.fsx"
open FSharp.Charting

let simType = Reasonable
let topCap = 1000.0
let taxRate = 20.0
let monCost = 5.0
let subsidyRate = 10.0

let timeBegin = 0
let timeMax = 250
let taxBracket = 25.0
// let refillRateA = [High; High; Low; Low; High; High]//; Low; Low; High]
// let refillRateB = [Low; High; High; High; High; Low]//; Low; Low; Low]
//let refillRateA = [High;High;High;High;High; High;High;High;High;High; Low;Low;Low;Low;Low;      Low; Low;Low;Low;Low;Low;      High;High;High;High;High; High;High;High;High;High; Low]
//let refillRateB = [Low;Low;Low;Low;Low;      High;High;High;High;High; High;High;High;High;High; Low; High;High;High;High;High; High;High;High;High;High; Low;Low;Low;Low;Low;      Low]
// let refillRateA = [High;High;Low;Low;High]
// let refillRateB = [High;Low;High;Low;Low]
let refillRateA = [High; High; Low; Low; Low; High; High; Low]//; Low; Low; High]
let refillRateB = [Low; High; High; Low; High; High; Low; Low]//; Low; Low; Low]
// let refillRateA = [Low;Low;Low;Low;High;High;High;High]
// let refillRateB = [High;High;High;High;Low;Low;Low;Low]
let midCap = 1000.0
let bottomCap = 40.0
//let subsidyRate = taxRate - monCost


let allAgents = init refillRateA refillRateB monCost topCap midCap bottomCap
let answer = simulate allAgents simType timeBegin timeMax taxBracket taxRate subsidyRate

// let transformSatis state = 
//     let getSatis acc state = 
//         let res,ben = state
//         match ben with
//         | 0.0 when res=midCap -> acc + 0.0 // get dynamic version
//         | amt -> acc + amt
//     let lst = List.map2 (fun r b -> r,b) state.ResourcesState state.CurrBenefit
//     let satis = List.scan (getSatis) 0.0 lst |> List.tail
//     {state with RunningBenefit=satis}



let runningTotal = List.scan (+) 0.0 >> List.tail
let transformCumulative state = 
    let newBen = runningTotal state.CurrBenefit
    {state with RunningBenefit=newBen}
    
let cumAnswer = 
    answer
    |> List.map transformCumulative

printfn "%A" cumAnswer
let parksCurrBen = cumAnswer.[1].CurrBenefit
let parksRes = cumAnswer.[1].ResourcesState
let parksIndRes = List.map (fun i -> i/25.0) parksRes
let parksRunningBen = cumAnswer.[1].RunningBenefit

let brookCurrBen = cumAnswer.[2].CurrBenefit
let brookRes = cumAnswer.[2].ResourcesState
let brookIndRes = List.map (fun i -> i/25.0) brookRes
let brookRunningBen = cumAnswer.[2].RunningBenefit

let parksRate = List.map (plotRefillRate midCap refillRateA) [0..timeMax]
let brookRate = List.map (plotRefillRate midCap refillRateB) [0..timeMax]

Chart.Combine ([
    Chart.Line (parksRunningBen, Name="Inst A")
    Chart.Line (brookRunningBen, Name="Inst B")
    Chart.Line (parksRate, Name="Rate A", Color=Color.PaleTurquoise )
    Chart.Line (brookRate, Name="Rate B", Color=Color.PaleGoldenrod)
]) 
|> Chart.WithLegend(InsideArea=true) 
|> Chart.WithTitle("Net benefit of the institutions") 
|> Chart.WithXAxis(Title="Time")
|> Chart.WithYAxis(Title="Benefit level")//, Max=3000.0, Min=(-4000.0))
|> Chart.Show

Chart.Combine ([
    //Chart.Line (cumAnswer.[0].ResourcesState, Name="Top Inst", Color=Color.Black)
    Chart.Line (parksIndRes, Name="Inst A")
    Chart.Line (brookIndRes, Name="Inst B")
    //Chart.Line (parksRate, Name="parksRate", Color=Color.PaleTurquoise )
    //Chart.Line (brookRate, Name="brookRate", Color=Color.PaleGoldenrod)
]) 
|> Chart.WithLegend(InsideArea=true) 
|> Chart.WithTitle("Resources available for base level members")
|> Chart.WithXAxis(Title="Time")
|> Chart.WithYAxis(Title="Resource level", Max=50.0, Min=0.0)
|> Chart.Show

