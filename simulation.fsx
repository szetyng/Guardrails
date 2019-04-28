open System.Runtime.Hosting
#load "holon.fsx"
#load "platform.fsx"
#load "physical.fsx"
#load "decisions.fsx"
open Holon
open Platform
open Physical
open Decisions

let simulate agents time tmax refillRate = 
    let supraHolons = 
        List.map getSupraID agents
        |> List.distinct
        |> List.choose id
        |> List.map (getHolon agents)
        |> List.choose id    
    let heads = List.filter (fun h -> checkRole h "Head") agents    
    let gatekeepers = List.filter (fun g -> checkRole g "Gatekeeper") agents
    let monitors = List.filter (fun m -> checkRole m "Monitor") agents
    let regHolons = 
        agents
        |> List.except supraHolons
        |> List.except heads
        |> List.except gatekeepers
        |> List.except monitors

    let getSupra mem = (getSupraHolon mem supraHolons).Value

    printfn "Supra-holons are:"
    List.map (fun h -> printfn "%s" h.Name) supraHolons |> ignore
    printfn "Heads are:"
    List.map (fun h -> printfn "%s" h.Name) heads |> ignore
    printfn "Gatekeepers are:"
    List.map (fun h -> printfn "%s" h.Name) gatekeepers |> ignore
    printfn "Monitors are:"
    List.map (fun x -> printfn "%s" x.Name) monitors |> ignore 

    let rec runSimulate time state =
        printfn "t=%i" time

        /// Make tuple of mem and the supra-inst it belongs to
        let doubleMemInst mem = 
            let i = getSupra mem
            (mem, i)      

        /// Make tuple of inst and the amount to refill
        let doubleInstRefill inst = 
            let r = decideOnRefill inst time refillRate
            (inst,r)      

        let allMembersMakeDemands memberLst = 
            memberLst   
            |> List.filter (fun h -> checkRole h "Member")
            |> List.map (doubleMemInst >> (fun (h,i) -> demandResources h (decideOnDemandR h i) i time))
            |> ignore
          
        let allMembersAppropriate memberLst = 
            memberLst
            |> List.filter (fun h -> checkRole h "Member")
            |> List.map (doubleMemInst >> (fun (m,i) -> appropriateResources m i (decideOnAppropriateR m i)))
            |> ignore   

        let allHeadsAllocate heads = 
            let headAllocatesToInst headInst =
                let head, inst = headInst
                printfn "head %s is allocating resources to members in inst %s according to the protocol" head.Name inst.Name
                allocateAllResources head inst agents
            heads
            |> List.map (doubleMemInst >> headAllocatesToInst)       
            |> ignore     


        let principle2 memberLst heads = 
            allMembersMakeDemands memberLst
            allHeadsAllocate heads

            printfn "members are making appropriations"
            allMembersAppropriate memberLst

            supraHolons
            |> List.map (fun inst -> printfn "inst %s now has %f amount of resources" inst.Name inst.Resources)
            |> ignore
                    

        // P1: Gatekeeper checks for members to be kicked out
        gatekeepers
        |> List.map (fun g -> gatekeepChecksExclude g (getSupra g) agents)
        |> ignore

        // P2: Members of institutions make demands
        printfn "all members at the base level are making demands"
        principle2 regHolons heads

        // P2 & P8: Holons at the middle hierarchy are making demands
        printfn "supra-holons in the middle hierarchy are making demands"
        principle2 supraHolons heads
  
        // Refill top institution
        supraHolons
        |> List.filter (fun h -> checkRole h "None")
        |> List.map (doubleInstRefill >> (fun (inst,r) -> refillResources inst r))
        |> ignore



        // P3: Heads decide if they want to open vote or not
        // AFTER refill is done


        // newline to separate time slices printing
        printfn ""
        match time with
        | t when t=tmax -> state
        | t -> 
            let updateState insts old =
                let ind,oldState = old
                let supra = (getHolon insts ind).Value 
                (ind, oldState @ [supra.Resources])
            let newState = List.map (updateState supraHolons) state             
            runSimulate (t+1) newState
            
    // include ID to be safe
    let initState = List.map (fun inst -> (inst.ID, [inst.Resources])) supraHolons
    runSimulate time initState 
