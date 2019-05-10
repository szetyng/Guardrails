open System.Collections.Generic
#load "holon.fsx"
#load "platform.fsx"
#load "physical.fsx"
#load "decisions.fsx"
open Holon
open Platform
open Physical
open Decisions

type HolonState = 
    {
        SupraID : HolonID;
        ResourcesState : int list;
        BenefitState : int list;
    }

let random = System.Random()

let simulate agentLst time tmax tax refillRate = 
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
        
        let rationMember = calculateRationMember tax midHolonLst agentLst topHolon 
        printfn "ration per base member = %i" rationMember

        let calculateRationSupra rationMember supra = 
            let sz = List.length (getBaseMembers agentLst supra)
            printfn "inst %s in total gets %i" supra.Name (rationMember*sz)
            rationMember*sz
        let rationSupraLst = List.map (calculateRationSupra rationMember) midHolonLst    

        let reportTaxSubsidy inst rationSupra = 
            let rProd = inst.Resources
            match rationSupra with
            | rAllocated when rProd>rAllocated -> Some (Tax (inst.ID,rProd-rAllocated))
            | rAllocated when rProd<rAllocated -> Some (Subsidy (inst.ID, rAllocated-rProd))
            | _ -> None 
        let reports = List.map2 reportTaxSubsidy midHolonLst rationSupraLst |> List.choose id
        topHolon.MessageQueue <- topHolon.MessageQueue @ reports

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
            | Subsidy(i, amt) ->
                let inst = List.find (fun agent -> agent.ID=i) members
                inst.Resources <- inst.Resources + amt
                boss.Resources <- boss.Resources - amt
                inst.MessageQueue <- inst.MessageQueue @ [Subsidy(i,amt)]
                printfn "inst %s got SUBSIDY: %i :)" inst.Name amt
                None
            | m -> Some m            
        let qAfterTax inst = 
            inst.MessageQueue
            |> List.map (takeTax midHolonLst topHolon)
            |> List.choose id
            |> List.map (giveSubsidy midHolonLst topHolon)
            |> List.choose id
        topHolon.MessageQueue <- qAfterTax topHolon      

        let satisfactionOfInst inst = 
            let netBenefit = 
                let rec getInfoFromQ q = 
                    match inst.MessageQueue with
                    | Tax(i,amt)::_ when i=inst.ID -> -amt
                    | Subsidy(i,amt)::_ when i=inst.ID -> amt
                    | _::rest -> getInfoFromQ rest
                    | [] -> 0
                getInfoFromQ inst.MessageQueue

            if hasBoss supraHolonLst inst then inst.Resources <- 0  
            else ()
            inst.MessageQueue <- [] // obv don't do this, TODO fix
            netBenefit


            // let getStuff state msg = 
            //     let netBenefit = 
            //         match msg with
            //         | Tax(i,amt) when i=inst.ID -> -amt
            //         | Subsidy(i,amt) when i=inst.ID -> amt
            //         | _ -> 0


            // inst.MessageQueue
            // |> List.fold getStuff []              

        
   

        // newline to separate time slices printing
        printfn ""
        match time with
        | t when t=tmax -> state
        | t -> 
            let updateState insts oldState = 
                let oldRes = oldState.ResourcesState
                let oldBen = oldState.BenefitState
                let rec getS supras = 
                    match supras with
                    | h::_ when h.ID=oldState.SupraID -> 
                        //let accumBen = List.tail 
                        {oldState with ResourcesState=oldRes @ [h.Resources]; BenefitState=oldBen @ [ (satisfactionOfInst h)]}
                    | _::rest -> getS rest
                    | [] -> oldState    
                getS insts                            

            let newState = List.map (updateState supraHolonLst) state             
            runSimulate (t+1) newState
            
    // include ID to be safe
    let createInitState holon = {SupraID=holon.ID; ResourcesState=[holon.Resources]; BenefitState = [0]}
    let initState = List.map createInitState supraHolonLst    
    runSimulate time initState 
