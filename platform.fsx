#load "holon.fsx"
open Holon 

/// Applicant applies to inst
let applyToInstitution (applicant:Holon) (inst:Holon) = 
    let crit = [applicant.SanctionLevel < inst.SanctionLimit ; inst.MembershipLimit > inst.MemberOf.Length]
    let rec checkCrit (supraH:Holon) (h:Holon) lst = 
        match lst with 
        | [] -> true
        | true::rest -> checkCrit supraH h rest
        | false::_ -> false
    let applicationResult = 
        match checkCrit inst applicant crit with
        | true ->
            match inst.Gatekeeper with
            | Some gatekeeper ->
                printfn "%A is applying to Institution %A via Gatekeeper %A" applicant.Name inst.Name gatekeeper.Name
                Some (gatekeeper.IncludeMember inst)
            | None -> None
        | false -> Some (false)

    match applicationResult with
    | Some true ->
        printfn "Result of agent %A applying to %A is %A" applicant.Name inst.Name applicationResult
        inst.AddMember(applicant)
        applicant.JoinHolon(inst)
    | Some false -> printfn "Result of agent %A applying to %A is %A" applicant.Name inst.Name applicationResult
    | None -> printfn "%A does not have a gatekeeper, or gatekeeper did not reply - it cannot admit new member %A" inst.Name applicant.Name  


let createParks(parksLst) =
    let parks = Holon("parks")

    parks.SetMembers parksLst
    List.map (fun (h:Holon) -> h.JoinHolon parks) parksLst |> ignore

    parks.SetGatekeeper (List.head parksLst)

    // Principle 1: Membership criteria
    parks.SetSanctionLimit 2
    parks.SetInstSize 5
    
    parks

