open System.Runtime.Hosting
#load "holon.fsx"
#load "platform.fsx"
#load "physical.fsx"
#load "decisions.fsx"
open Holon
open Platform
open Physical
open Decisions

let simulate agents time tmax = 
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

    let rec runSimulate time =
        printfn "t=%i" time
        // P1: Gatekeeper checks for members to be kicked out
        gatekeepers
        |> List.map (fun g -> gatekeepChecksExclude g (getSupra g) agents)
        |> ignore
      
        let tripleMemInstR mem = 
            let i = getSupra mem
            let r = decideOnR mem i
            (mem, i, r)

        // P2: Members of institutions make demands
        printfn "members of institutions are making their demands"
        regHolons
        |> List.filter (fun h -> checkRole h "Member")
        |> List.map (tripleMemInstR >> (fun (h,i,r) -> demandResources h r i time))
        |> ignore

        let pairHeadInst head = 
            let i = getSupra head
            (head, i)

        // P2: Heads of institutions allocate demands
        heads
        |> List.map (pairHeadInst >> (fun (h,i) -> allocateAllResources h i agents))
        |> ignore

        let doubleMemInst mem = 
            let i = getSupra mem
            (mem, i)

        // P2: Members of institutions make appropriations
        printfn "all members are making appropriations"
        regHolons
        |> List.filter (fun h -> checkRole h "Member")
        |> List.map (doubleMemInst >> (fun (m,i) -> (m,i,(decideOnR m i))) >> (fun (m,i,r) -> appropriateResources m i r))
        |> ignore

        supraHolons
        |> List.map (fun inst -> printfn "inst %s now has %i amount of resources" inst.Name inst.Resources)
        |> ignore
        // P3: Heads decide if they want to open vote or not


        // REFILL
        supraHolons
        |> List.map (fun inst -> inst.Resources <- inst.Resources + 300)
        |> ignore

        printfn ""

        match time with
        | t when t=tmax -> ()
        | t -> runSimulate (t+1)
    runSimulate time    
