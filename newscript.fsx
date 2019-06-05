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

type Update = 
    | Alpha of float
    | Beta of float


let simType = Reasonable
let topCap = 2000.0
let taxRate = 20.0
let monCost = 10.0
let subsidyRate = 10.0
let alphaGreat = 0.25
let alphaOk = 0.2
let betaHorr = 0.2
let betaOk = 0.1


let timeBegin = 0
let timeMax = 250
let taxBracket = 25.0
let refillRateA = [High; High; Low; Low; Low; High; High; Low]//; Low; Low; High]
let refillRateB = [Low; High; High; Low; High; High; Low; Low]//; Low; Low; Low]
let midCap = 1000.0
let bottomCap = 40.0

let allAgents = init refillRateA refillRateB monCost topCap midCap bottomCap
let answer = simulate allAgents simType timeBegin timeMax taxBracket taxRate subsidyRate
let parksRate = List.map (plotRefillRate midCap refillRateA) [0..timeMax]
let brookRate = List.map (plotRefillRate midCap refillRateB) [0..timeMax]

let transformState state = 
    let updateSigma coeff oldSigma = 
        match coeff with
        | Alpha(a) -> oldSigma + a*(1.0-oldSigma)
        | Beta(b) -> oldSigma - b*oldSigma

    let check oldSigma benCom = 
        let benefit, consumed = benCom
        match benefit with
        | ben when ben>=subsidyRate*25.0 -> updateSigma (Alpha alphaOk) oldSigma // happy, expected sub
        | ben when ben<subsidyRate*25.0 && ben>0.0 -> updateSigma (Beta betaHorr) oldSigma // very unhappy, did not get enough help
        | ben when ben<0.0 -> updateSigma (Beta betaOk) oldSigma // not happy, expected tax
        | ben when ben=0.0 && consumed=midCap -> updateSigma (Alpha alphaGreat) oldSigma // very happy, did not pay tax
        | ben when ben=0.0 -> updateSigma (Beta betaHorr) oldSigma // very unhappy, did not get help
    
    let lst = List.map2 (fun b r -> b,r) state.CurrBenefit state.ResourcesState
    let l = List.scan (check) 0.5 lst |> List.tail
    {state with RunningBenefit=l}

let transAnswer = 
    answer
    |> List.map transformState 
let parksCurrBen = transAnswer.[1].CurrBenefit
let parksRes = transAnswer.[1].ResourcesState
let parksIndRes = List.map (fun i -> i/25.0) parksRes
let parksRunningBen = transAnswer.[1].RunningBenefit

let brookCurrBen = transAnswer.[2].CurrBenefit
let brookRes = transAnswer.[2].ResourcesState
let brookIndRes = List.map (fun i -> i/25.0) brookRes
let brookRunningBen = transAnswer.[2].RunningBenefit

printfn "%A" transAnswer

Chart.Combine ([
    Chart.Line (parksRunningBen, Name="Inst A")
    Chart.Line (brookRunningBen, Name="Inst B")
    //Chart.Line (parksRate, Name="Rate A", Color=Color.PaleTurquoise )
    //Chart.Line (brookRate, Name="Rate B", Color=Color.PaleGoldenrod)
]) 
|> Chart.WithLegend(InsideArea=true) 
|> Chart.WithTitle("Net benefit of the institutions") 
|> Chart.WithXAxis(Title="Time")
|> Chart.WithYAxis(Title="Benefit level", Max=1.0, Min=0.0)//, Max=3000.0, Min=(-4000.0))
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

