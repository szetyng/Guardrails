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
// lenient = 250. reasonable = 3000. strict approx 70000
// monCost =  [5,10], tax = 20, subsidy = 5
let topCap = 70000.0
let monCost = 10.0

let taxRate = 20.0
let subsidyRate = 5.0
let betaOk = 0.15
let betaHorr = 0.2
let alphaOk = 0.1
let alphaGreat = 0.2

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

let updateSigma coeff oldSigma = 
    match coeff with
    | Alpha(a) -> oldSigma + a*(1.0-oldSigma)
    | Beta(b) -> oldSigma - b*oldSigma

let transformUpperState state = 
    let update oldSigma benefit = 
        let mapToRange inputRange outputRange x = 
            let in1,in2 = inputRange
            let out1,out2 = outputRange
            out1 + ((x-in1)*(out2-out1))/(in2-in1)
        let mapToLinear =  mapToRange (-250.0,500.0) (-1.0,1.0) 
        let mapToGreek alphaRange betaRange x = 
            let linear = mapToLinear x
            let threshold = mapToLinear 0.0 // let 0 in CurrBenefit be the threshold
            //let threshold = 0.0
            match linear with
            | l when l<threshold -> 
                let b1,b2 = betaRange 
                let neg = mapToRange (-1.0,threshold) (-b2,-b1) l  
                Beta -(neg)
            | l when l>=threshold-> 
                let alf = mapToRange (threshold,1.0) alphaRange  l
                Alpha alf  
            | _ -> Alpha 0.0            
        let coeff = mapToGreek (alphaOk,alphaGreat) (betaOk,betaHorr) benefit
        updateSigma coeff oldSigma            
    let l = List.scan (update) 0.5 state.CurrBenefit |> List.tail
    {state with RunningBenefit=l}

let transformMidState state = 
    let update oldSigma benCom = 
        let benefit, consumed = benCom
        match benefit with
        | ben when ben>=subsidyRate*25.0 -> updateSigma (Alpha alphaOk) oldSigma // happy, expected sub
        | ben when ben<subsidyRate*25.0 && ben>0.0 -> updateSigma (Beta betaHorr) oldSigma // very unhappy, did not get enough help
        | ben when ben<0.0 -> updateSigma (Beta betaOk) oldSigma // not happy, expected tax
        | ben when ben=0.0 && consumed=midCap -> updateSigma (Alpha alphaGreat) oldSigma // very happy, did not pay tax
        | ben when ben=0.0 -> updateSigma (Beta betaHorr) oldSigma // very unhappy, did not get help
    let lst = List.map2 (fun b r -> b,r) state.CurrBenefit state.ResourcesState
    let l = List.scan (update) 0.5 lst |> List.tail
    {state with RunningBenefit=l}

let transAnswer = 
    let midStates = 
        answer
        |> List.tail
        |> List.map transformMidState 
    let upperState = 
        answer
        |> List.head
        |> transformUpperState
    [upperState] @ midStates     

let officesRunningBen = transAnswer.[0].RunningBenefit
let officesCurrBen = transAnswer.[0].CurrBenefit

let parksCurrBen = transAnswer.[1].CurrBenefit
let parksRes = transAnswer.[1].ResourcesState
let parksIndRes = List.map (fun i -> i/25.0) parksRes
let parksRunningBen = transAnswer.[1].RunningBenefit

let brookCurrBen = transAnswer.[2].CurrBenefit
let brookRes = transAnswer.[2].ResourcesState
let brookIndRes = List.map (fun i -> i/25.0) brookRes
let brookRunningBen = transAnswer.[2].RunningBenefit

printfn "%A" transAnswer

let allInThisTgt threshold buffer tMax lstOfLst = 
    let quitIfAngry threshold buffer satisLst = 
        let isLessThanThres thres buff accTuple x = 
            let _,acc = accTuple
            match x<=thres, acc<buff with
            | true, true -> Some x, acc+1
            | false, true -> Some x, acc
            | _, false -> None, acc
            //| _, false -> Some 0.0, acc

        satisLst
        |> List.scan (isLessThanThres threshold buffer) (None,0)
        |> List.map fst
        |> List.choose id // Head is None
    let padWithZeros zLst lst = lst @ zLst
           
    let thresholdedLsts = List.map (quitIfAngry threshold buffer) lstOfLst
    let lstOfLengths = List.map List.length thresholdedLsts 
    let t = List.min lstOfLengths
    let zeroLst = List.init (tMax+1-t) (fun _ -> 0.0) 
    
    thresholdedLsts
    |> List.map ((List.take t) >> (padWithZeros zeroLst))

let quitLst = allInThisTgt 0.2 15 timeMax [parksRunningBen;brookRunningBen;officesRunningBen]
let parksQuitBen = quitLst.[0]
let brookQuitBen = quitLst.[1]
let officesQuitBen = quitLst.[2]



Chart.Combine ([
    Chart.Line (parksQuitBen, Name="dCES A")
    Chart.Line (brookQuitBen, Name="dCES B")
    Chart.Line (officesQuitBen, Name="Aggregated dCES")
    //Chart.Line (parksRate, Name="Rate A", Color=Color.PaleTurquoise )
    //Chart.Line (brookRate, Name="Rate B", Color=Color.PaleGoldenrod)
]) 
|> Chart.WithLegend(InsideArea=true) 
|> Chart.WithTitle("Satisfaction of the institutions", InsideArea=false) 
|> Chart.WithXAxis(Title="Time", Min=0.0, Max=float(timeMax))
|> Chart.WithYAxis(Title="Satisfaction level", Max=1.0, Min=0.0)//, Max=3000.0, Min=(-4000.0))
|> Chart.Show

Chart.Combine ([
    //Chart.Line (cumAnswer.[0].ResourcesState, Name="Top Inst", Color=Color.Black)
    Chart.Line (parksIndRes, Name="dCES A")
    Chart.Line (brookIndRes, Name="dCES B")
    //Chart.Line (parksRate, Name="parksRate", Color=Color.PaleTurquoise )
    //Chart.Line (brookRate, Name="brookRate", Color=Color.PaleGoldenrod)
]) 
|> Chart.WithLegend(InsideArea=true) 
|> Chart.WithTitle("Resources available for a member Smart House", InsideArea=false)
|> Chart.WithXAxis(Title="Time")
|> Chart.WithYAxis(Title="Resource level", Max=50.0, Min=0.0)
|> Chart.Show


