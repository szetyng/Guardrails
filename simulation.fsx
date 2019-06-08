open System.Web.UI.WebControls
#load "holon.fsx"
#load "platform.fsx"
open Holon
open Platform


type SimType = Strict | Reasonable | Lenient

type HolonState = 
    {
        SupraID : HolonID;
        ResourcesState : float list;
        CurrBenefit : float list;
        RunningBenefit : float list;
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

        let workPerSalary =             
            match List.isEmpty topHolon.MessageQueue with 
            | true -> 0.0//5.0*50.0
                //topHolon.SupraResources <- topHolon.SupraResources + (5.0*50.0)
            | false -> 0.0 //()        
        
                
        // If msg is Tax, taxes that member and gives it to boss
        // and Tax msg is removed
        // TODO: take only up to max
        let takeTax optAmt members boss msg = 
            let skimCost = boss.MonitoringCost
            match optAmt, msg with
            | None, Tax(i, amt) -> 
                let inst = List.find (fun agent -> agent.ID=i) members 
                let nr = List.length (getBaseMembers agentLst inst) |> float
                let skim = skimCost * nr
                inst.Resources <- inst.Resources - amt
                boss.Resources <- boss.Resources + amt - skim
                //boss.SupraResources <- boss.SupraResources + skim - (nr*5.0) // monCost - work effort
                inst.MessageQueue <- inst.MessageQueue @ [Tax(i,amt)]
                printfn "inst %s paid TAX: %f :(, upper skimmed %f" inst.Name amt skim
                None, skim// - (nr*5.0) // monCost - work effort
            | Some amt, Tax(i, _) ->
                let inst = List.find (fun agent -> agent.ID=i) members
                let nr = List.length (getBaseMembers agentLst inst) |> float
                let skim = skimCost * nr
                inst.Resources <- inst.Resources - amt
                boss.Resources <- boss.Resources + amt - skim
                //boss.SupraResources <- boss.SupraResources + skim - (nr*5.0) // monCost - work effort
                inst.MessageQueue <- inst.MessageQueue @ [Tax(i,amt)] 
                printfn "inst %s only has to pay %f in tax bc max, upper skimmed %f" inst.Name amt skim 
                None, skim// - (nr*5.0) // monCost - work effort            
            | _, m -> Some m, 0.0
        // If msg is Subsidy, subsidises that member by taking from boss
        // and Subsidy msg is removed
        let giveSubsidy enough members boss msg =
            match enough, msg with
            | true, Subsidy(i, amt) ->
                let inst = List.find (fun agent -> agent.ID=i) members
                let nr = List.length (getBaseMembers agentLst inst) |> float
                inst.Resources <- inst.Resources + amt
                boss.Resources <- boss.Resources - amt
                //boss.SupraResources <- boss.SupraResources - (nr*5.0)
                inst.MessageQueue <- inst.MessageQueue @ [Subsidy(i,amt)]
                printfn "inst %s got SUBSIDY: %f :), upper used up %f to do work" inst.Name amt (nr*6.0)
                None, -(nr*6.0)
            | false, Subsidy(i,_) -> 
                let inst = List.find (fun agent -> agent.ID=i) members
                let bank = boss.Resources
                // why on earth is getPopulation required for topHolon
                let getPopulation acc holon = 
                    let baseMembers = getBaseMembers agentLst holon
                    acc + List.length baseMembers
                let totalMidMembers = getPopulation 0 boss |> float
                let sub = bank/totalMidMembers
                let nr = List.length (getBaseMembers agentLst inst) |> float

                inst.Resources <- inst.Resources + sub
                boss.Resources <- boss.Resources - sub 
                //boss.SupraResources <- boss.SupraResources - (nr*5.0)
                inst.MessageQueue <- inst.MessageQueue @ [Subsidy(i,sub)]
                printfn "inst %s only got subsidy %f because not enough" inst.Name sub 
                None, 0.0// -(nr*5.0)          
            | _, m -> Some m, 0.0  

        // TODO: separate into two -> tax first then figure out if there's enough for subsidy
        let taxNClearQ members inst wpsOld = 
            let max = inst.ResourceCap
            let skimCost = inst.MonitoringCost
            let taxAndSkim acc msg =
                let bank, prevInst, prevSkim = acc 
                match msg with
                | Tax(i,amt) -> 
                    let memInst = List.find (fun agent -> agent.ID=i) members 
                    let nrBaseMems = List.length (getBaseMembers agentLst memInst) |> float
                    let skim = skimCost * nrBaseMems
                    (bank + amt - skim, prevInst + 1.0, prevSkim + skim)
                | _ -> acc
            let afterTax, nrInst, skim = List.fold taxAndSkim (inst.Resources,0.0,0.0) inst.MessageQueue
            let redundantQInfo = 
                match afterTax with
                | newBank when newBank<=max -> 
                    inst.MessageQueue
                    |> List.map (takeTax None midHolonLst inst)             
                | _ ->
                    let taxDiff = max - inst.Resources
                    let needTax = (taxDiff + skim)/nrInst
                    inst.MessageQueue
                    |> List.map (takeTax (Some needTax) midHolonLst inst)         
            let wpsTax =  
                redundantQInfo
                |> List.map snd
                |> List.fold (+) wpsOld  
            let newQ = 
                redundantQInfo  
                |> List.map fst 
                |> List.choose id   
            newQ, wpsTax            
        let taxQ, wpsTax = taxNClearQ midHolonLst topHolon workPerSalary                       
        topHolon.MessageQueue <- taxQ

        let subAndClearQ inst wpsOld = 
            let enoughRes bank msg = 
                match msg with
                | Subsidy(_,amt) -> bank - amt
                | _ -> bank
            let remains = List.fold enoughRes inst.Resources inst.MessageQueue 
            let redundantQInfo = 
                match remains with
                | remainder when remainder>=0.0 ->
                    inst.MessageQueue
                    |> List.map (giveSubsidy true midHolonLst inst)
                | _ ->
                    inst.MessageQueue
                    |> List.map (giveSubsidy false midHolonLst inst)   
            let wps = 
                redundantQInfo 
                |> List.map snd
                |> List.fold (+) wpsOld
            let newQ =
                redundantQInfo
                |> List.map fst
                |> List.choose id
            newQ, wps
        let subQ, wpsSub = subAndClearQ topHolon wpsTax                  
        topHolon.MessageQueue <- subQ    

        topHolon.SupraResources <- wpsSub             
                   
        let satisfactionOfInst inst = 
            let netBenefit = 
                match hasBoss supraHolonLst inst with
                | false -> topHolon.SupraResources 
                | true -> 
                    // midHolons consume resources
                    inst.Resources <- 0.0

                    let rec getInfoFromQ q = 
                        match inst.MessageQueue with
                        | Tax(i,amt)::_ when i=inst.ID -> -amt
                        | Subsidy(i,amt)::_ when i=inst.ID -> amt
                        | _::rest -> getInfoFromQ rest
                        | [] -> 0.0
                    getInfoFromQ inst.MessageQueue

            let clearMsg msgType msg = 
                match msgType,msg with
                | Tax(_), Tax(_) -> None
                | Subsidy(_), Subsidy(_) -> None
                | _, m -> Some m
            let qWithoutTax = 
                inst.MessageQueue
                |> List.map (clearMsg (Tax(inst.ID,0.0))) 
                |> List.choose id
                |> List.map (clearMsg (Subsidy(inst.ID,0.0)))
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
    let createInitState holon = {SupraID=holon.ID; ResourcesState=[holon.Resources]; CurrBenefit = [0.0]; RunningBenefit=[0.0]}
    let initState = List.map createInitState supraHolonLst    
    runSimulate time initState 
