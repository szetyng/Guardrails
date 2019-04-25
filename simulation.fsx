open System.Runtime.Hosting
#load "holon.fsx"
#load "platform.fsx"
#load "physical.fsx"
#load "decisions.fsx"
open Holon
open Platform
open Physical
open Decisions

let simulate agents time = 
    printfn "t=%i" time

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

    // P1: Gatekeeper checks for members to be kicked out
    gatekeepers
    |> List.map (fun g -> gatekeepChecksExclude g (getSupra g) agents)
    |> ignore
  
    let tripleMemInstR mem = 
        let i = getSupra mem
        let r = decideOnR mem i
        (mem, i, r)

    // P2: Members of institutions make demands
    regHolons
    |> List.filter (fun h -> checkRole h "Member")
    |> List.map (tripleMemInstR >> (fun (h,i,r) -> demandResources h r i time))
    |> ignore

    let pairHeadInst head = 
        let i = getSupra head
        printfn "Head: %s, inst: %s" head.Name i.Name
        (head, i)

    // P2: Heads of institutions allocate demands
    heads
    |> List.map (pairHeadInst >> (fun (h,i) -> allocateAllResources h i agents))
    |> ignore
