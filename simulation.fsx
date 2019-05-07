open System.Runtime.Hosting
#load "holon.fsx"
#load "platform.fsx"
#load "physical.fsx"
#load "decisions.fsx"
open Holon
open Platform
open Physical
open Decisions

let random = System.Random()

let simulate agentLst time tmax refillRate = 
    let supraHolonLst = 
        List.map getSupraID agentLst
        |> List.distinct
        |> List.choose id
        |> List.map (getHolon agentLst)
        |> List.choose id    
    let headLst = List.filter (fun h -> checkRole h "Head") agentLst    
    let gatekeeperLst = List.filter (fun g -> checkRole g "Gatekeeper") agentLst
    let monitorLst = List.filter (fun m -> checkRole m "Monitor") agentLst
    let baseHolonLst = 
        agentLst
        |> List.except supraHolonLst
        |> List.except headLst
        |> List.except gatekeeperLst
        |> List.except monitorLst

    /// Unsafe version with no Option check
    //let getSupra mem = (getSupraHolon mem supraHolonLst).Value

    printfn "Supra-holons are:"
    List.map (fun h -> printfn "%s" h.Name) supraHolonLst |> ignore
    printfn "Heads are:"
    List.map (fun h -> printfn "%s" h.Name) headLst |> ignore
    printfn "Gatekeepers are:"
    List.map (fun h -> printfn "%s" h.Name) gatekeeperLst |> ignore
    printfn "Monitors are:"
    List.map (fun x -> printfn "%s" x.Name) monitorLst |> ignore  

    let rec runSimulate time state =
        printfn "t=%i" time

        /// Make tuple of mem and the supra-inst it belongs to
        // let doubleMemInst mem = 
        //     let i = getSupra mem
        //     (mem, i)      

        /// Refill only the top most institution
        let refillTopInstitution inst = 
            match hasMembers agentLst inst, hasBoss agentLst inst with 
            | true, false -> 
                let r = decideOnRefill inst time refillRate 
                refillResources inst r
            | _ -> ()   
        
        /// P1: Gatekeeper checks for members to be kicked out
        let boundariesPrinciple agents gatekeeper =
            let inst = getSupraHolon gatekeeper supraHolonLst
            match inst with
            | Some i -> gatekeepChecksExclude gatekeeper i agents 
            | None -> printfn "cannot find supraholon of gatekeeper %s" gatekeeper.Name       

        /// P2: Members of institutions make demands
        let congruencePrinciple heads members isNested = 
            let makeDemand agent = 
                let inst = getSupraHolon agent supraHolonLst
                match checkRole agent "Member", inst with
                | true, Some i -> 
                    demandResources agent (decideOnDemandR agent) i time
                | _ -> ()            
            let allocateDemands head = 
                let inst = getSupraHolon head supraHolonLst
                match inst with
                | Some i when hasBoss agentLst i<>isNested -> 
                    printfn "head %s is allocating resources to members in inst %s according to the protocol" head.Name i.Name
                    allocateAllResources head i members              
                | None -> printfn "cannot find supraholon of head %s" head.Name   
                | _ -> ()         
            let makeAppropriation agent = 
                let inst = getSupraHolon agent supraHolonLst
                match checkRole agent "Member", inst with
                | true, Some i -> 
                    appropriateResources agent i (decideOnAppropriateR agent i)
                | _ -> ()
            List.map makeDemand members |> ignore
            List.map allocateDemands heads |> ignore
            printfn "members are making appropriations"
            List.map makeAppropriation members |> ignore

            supraHolonLst
            |> List.map (fun inst -> printfn "inst %s now has %i amount of resources" inst.Name inst.Resources)
            |> ignore        

        /// P3: Heads decide if they want to open status or not
        /// If status is opened, every member in the inst gets to vote
        let collectiveChoicePrinciple agents heads isNested =            
            let doElections agents head =
                let exerciseVote openInst agent = 
                    let inst = getSupraHolon agent supraHolonLst
                    match inst with
                    | Some i when i=openInst -> doVote agent i (decideVote agent i)
                    | _ -> ()  
                let checkIfNeedRationAmt inst = 
                    match inst.RaMethod with
                    | Some (Ration(None)) when not isNested->
                        let amt = 5
                        //let amt = inst.Resources / 9 //TODO: Make dynamic
                        inst.RaMethod <- Some (Ration (Some amt)) 
                        printfn "inst %s ration is %i" inst.Name amt   
                    | Some (Ration(None)) ->
                        let amt = 100
                        inst.RaMethod <- Some (Ration (Some amt))
                        printfn "inst %s ration is %i" inst.Name amt                    
                    | _ -> ()                               
                let votingProcess inst = 
                    openIssue head inst
                    List.map (exerciseVote inst) agents |> ignore
                    closeIssue head inst
                    declareWinner head inst
                    checkIfNeedRationAmt inst

                let inst = getSupraHolon head supraHolonLst
                match inst with
                | Some i when hasBoss supraHolonLst i<>isNested && decideElection 0.4 0.75 i -> 
                    votingProcess i  
                | None -> printfn "cannot find supraholon of head %s" head.Name
                | _ -> ()
            List.map (doElections agents) heads |> ignore        

        let monitoringPrinciple members monitors isNested = 
            let doMonitoring members monitor = 
                let inst = getSupraHolon monitor supraHolonLst
                match inst with
                | Some i when hasBoss agentLst i<>isNested -> 
                    let rate = i.MonitoringFreq
                    let decision = random.NextDouble()
                    match decision with
                    | x when x<=rate -> 
                        printfn "monitoring. random=%f, rate=%f" x rate                        
                        monitorDoesJob monitor i members   
                    | _ -> 
                        printfn "not monitoring. random=%f, rate=%f" decision rate
                        () 
                | None -> printfn "cannot find supraholon of monitor %s" monitor.Name   
                | _ -> ()                     
            List.map (doMonitoring members) monitors |> ignore                 


        List.map (boundariesPrinciple baseHolonLst) gatekeeperLst |> ignore
        printfn "all members at the base level are making demands"
        congruencePrinciple headLst baseHolonLst false
        monitoringPrinciple baseHolonLst monitorLst false
        collectiveChoicePrinciple baseHolonLst headLst false

              
        // P2 & P8: Holons at the middle hierarchy are making demands
        printfn "supra-holons in the middle hierarchy are making demands"
        congruencePrinciple headLst supraHolonLst true
        collectiveChoicePrinciple supraHolonLst headLst true

        List.map refillTopInstitution supraHolonLst |> ignore
        
        supraHolonLst
        |> List.map deleteAllocatedAndAppr
        |> ignore

   

        // newline to separate time slices printing
        printfn ""
        match time with
        | t when t=tmax -> state
        | t -> 
            let updateState insts old =
                let ind,oldState = old
                let supra = (getHolon insts ind).Value 
                (ind, oldState @ [supra.Resources])
            let newState = List.map (updateState supraHolonLst) state             
            runSimulate (t+1) newState
            
    // include ID to be safe
    let initState = List.map (fun inst -> (inst.ID, [inst.Resources])) supraHolonLst
    runSimulate time initState 
