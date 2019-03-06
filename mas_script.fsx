open System.Threading
#load "mas_holon.fsx"
#load "mas_platform.fsx"
open Mas_holon
open Mas_platform

let def = 
    {     
        ID = 0;   
        Name = " ";
        Resources = 0;
        MemberOf = None;
        Role = None;
        SanctionLevel = 0;
        OffenceLevel = 0;
        Members = []
    }

let parks = {def with Name = "parks"; Resources = 100}
let ron = {def with ID = 1; Name = "ron"; MemberOf = Some parks; Role = Some Head}
let leslie = {def with ID = 2; Name = "leslie"; MemberOf = Some parks; Role = Some Gatekeeper}

let holonLst = [parks ; ron ; leslie]

let initRoles h = 
    match (h.MemberOf, h.Role) with
    | Some inst, Some Member -> RoleOf (h.ID, Some inst.ID, Some Member)
    | Some inst, Some Head -> RoleOf (h.ID, Some inst.ID, Some Head)
    | Some inst, Some Gatekeeper -> RoleOf (h.ID, Some inst.ID, Some Gatekeeper)
    | Some inst, Some Monitor -> RoleOf (h.ID, Some inst.ID, Some Monitor)
    | _ , _ -> RoleOf (h.ID, None, None)


let initEnv holons = 
    let fLst = []
    let rec initEnv holons fLst = 
        match holons with
        | h::rest -> 
            let f = initRoles h
            List.append [f] (initEnv holons fLst)
        | [] -> fLst
    initEnv holons fLst

