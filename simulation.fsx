#load "holon.fsx"
#load "platform.fsx"
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
        let giveSubsidy enough members boss msg =
            match enough, msg with
            | true, Subsidy(i, amt) ->
                let inst = List.find (fun agent -> agent.ID=i) members
                inst.Resources <- inst.Resources + amt
                boss.Resources <- boss.Resources - amt
                inst.MessageQueue <- inst.MessageQueue @ [Subsidy(i,amt)]
                printfn "inst %s got SUBSIDY: %i :)" inst.Name amt
                None
            | false, Subsidy(i,_) -> 
                let inst = List.find (fun agent -> agent.ID=i) members
                let bank = boss.Resources
                let getPopulation acc holon = 
                    let baseMembers = getBaseMembers agentLst holon
                    acc + List.length baseMembers
                let totalMembers = getPopulation 0 boss
                let sub = bank/totalMembers

                inst.Resources <- inst.Resources + sub
                boss.Resources <- boss.Resources - sub 
                inst.MessageQueue <- inst.MessageQueue @ [Subsidy(i,sub)]
                printfn "inst %s only got subsidy %i because not enough" inst.Name sub
                None          
            | _, m -> Some m  

        // TODO: separate into two -> tax first then figure out if there's enough for subsidy
        let taxNClearQ inst = 
            inst.MessageQueue
            |> List.map (takeTax midHolonLst inst) 
            |> List.choose id
        topHolon.MessageQueue <- taxNClearQ topHolon

        let subAndClearQ inst = 
            let enoughRes bank msg = 
                match msg with
                | Subsidy(_,amt) -> bank - amt
                | _ -> bank
            let remains = List.fold enoughRes inst.Resources inst.MessageQueue 
            match remains with
            | remainder when remainder>=0 ->
                inst.MessageQueue
                |> List.map (giveSubsidy true midHolonLst inst)
                |> List.choose id
            | _ ->
                inst.MessageQueue
                |> List.map (giveSubsidy false midHolonLst inst)   
                |> List.choose id    
        topHolon.MessageQueue <- subAndClearQ topHolon                 
                   
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
