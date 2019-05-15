module Simulation
open Holon
open Platform


type SimType = Strict | Reasonable | Lenient

type HolonState = 
    {
        SupraID : HolonID;
        ResourcesState : int list;
        CurrBenefit : int list;
        RunningBenefit : int list;
    }

let random = System.Random()

let simulate agentLst simType time tmax taxBracket taxRate subsidyRate = 
    let supraHolonLst = 
        List.map getSupraID agentLst
        |> List.distinct
        |> List.choose id
        |> List.map (getHolon agentLst)
        |> List.choose id
    let midHolonLst = 
        supraHolonLst
        |> List.filter (hasBoss agentLst)
    let topHolonLst =
        supraHolonLst
        |> List.except midHolonLst
    if List.length topHolonLst <> 1 then printfn "error: more than one top holon"
    let topHolon = List.exactlyOne topHolonLst
    let bottomHolonLst = 
        agentLst
        |> List.except supraHolonLst        

    printfn "Supra-holons are:"
    List.map (fun h -> printfn "%s" h.Name) supraHolonLst |> ignore
    printfn "Top holon is: \n%s" topHolon.Name
    printfn "Middle holons are:"
    List.map (fun h -> printfn "%s" h.Name) midHolonLst |> ignore
    printfn "Bottom holons are:"
    List.map (fun x -> printf "%s," x.Name) bottomHolonLst |> ignore  
    printfn "\n"

    let rec runSimulate time state =
        printfn "t=%i" time

        let generateResources inst = 
            let r =  getGenerationAmt inst time  
            refillResources inst r 
        midHolonLst
        |> List.map generateResources
        |> ignore

        let needPayTax = 
            match simType with
            | Strict -> true
            | Reasonable ->
                let target = topHolon.ResourceCap
                match topHolon.Resources with
                | r when r<target -> true
                | _ -> false
            | Lenient -> false            

        let reportTax = 
            midHolonLst
            |> List.map (calculateTaxSubsidy taxBracket taxRate needPayTax subsidyRate agentLst) 
            |> List.choose id
        topHolon.MessageQueue <- topHolon.MessageQueue @ reportTax        
               
   
        // If msg is Tax, taxes that member and gives it to boss
        // and Tax msg is removed
        let takeTax members boss msg = 
            match msg with
            | Tax(i, amt) -> 
                let inst = List.find (fun agent -> agent.ID=i) members 
                inst.Resources <- inst.Resources - amt
                boss.Resources <- boss.Resources + amt
                inst.MessageQueue <- inst.MessageQueue @ [Tax(i,amt)]
                printfn "inst %s paid TAX: %i :(" inst.Name amt
                None
            | m -> Some m
        // If msg is Subsidy, subsidises that member by taking from boss
        // and Subsidy msg is removed
        let giveSubsidy members boss msg =
            match msg with
            | Subsidy(i, amt) when amt<=boss.Resources ->
                let inst = List.find (fun agent -> agent.ID=i) members
                inst.Resources <- inst.Resources + amt
                boss.Resources <- boss.Resources - amt
                inst.MessageQueue <- inst.MessageQueue @ [Subsidy(i,amt)]
                printfn "inst %s got SUBSIDY: %i :)" inst.Name amt
                None
            | Subsidy(i,amt) -> 
                let inst = List.find (fun agent -> agent.ID=i) members
                // strict because strict guardrails
                printfn "inst %s's subsidy of %i is refused because bank only has %i" inst.Name amt boss.Resources  
                None          
            | m -> Some m            
        let taxAndClearQ inst = 
            inst.MessageQueue
            |> List.map (takeTax midHolonLst topHolon)
            |> List.choose id
            |> List.map (giveSubsidy midHolonLst topHolon)
            |> List.choose id
        topHolon.MessageQueue <- taxAndClearQ topHolon      

        let satisfactionOfInst inst = 
            let netBenefit = 
                let rec getInfoFromQ q = 
                    match inst.MessageQueue with
                    | Tax(i,amt)::_ when i=inst.ID -> -amt
                    | Subsidy(i,amt)::_ when i=inst.ID -> amt
                    | _::rest -> getInfoFromQ rest
                    | [] -> 0
                getInfoFromQ inst.MessageQueue

            // Consume the resources, but not for topHolon acting as bank
            if hasBoss supraHolonLst inst then inst.Resources <- 0  
            else ()

            let clearMsg msgType msg = 
                match msgType,msg with
                | Tax(_), Tax(_) -> None
                | Subsidy(_), Subsidy(_) -> None
                | _, m -> Some m
            let qWithoutTax = 
                inst.MessageQueue
                |> List.map (clearMsg (Tax(inst.ID,0))) 
                |> List.choose id
                |> List.map (clearMsg (Subsidy(inst.ID,0)))
                |> List.choose id   

            inst.MessageQueue <- qWithoutTax
            netBenefit

            

        
   

        // newline to separate time slices printing
        printfn ""
        match time with
        | t when t=tmax -> state
        | t -> 
            let updateState insts oldState = 
                let oldRes = oldState.ResourcesState
                let oldBen = oldState.CurrBenefit
                let rec getS supras = 
                    match supras with
                    | h::_ when h.ID=oldState.SupraID -> 
                        //let accumBen = List.tail 
                        {oldState with ResourcesState=oldRes @ [h.Resources]; CurrBenefit=oldBen @ [ (satisfactionOfInst h)]}
                    | _::rest -> getS rest
                    | [] -> oldState    
                getS insts                            

            let newState = List.map (updateState supraHolonLst) state             
            runSimulate (t+1) newState
            
    // include ID to be safe
    let createInitState holon = {SupraID=holon.ID; ResourcesState=[holon.Resources]; CurrBenefit = [0]; RunningBenefit=[0]}
    let initState = List.map createInitState supraHolonLst    
    runSimulate time initState 
