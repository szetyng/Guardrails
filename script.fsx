open System.Threading
#load "holon.fsx"
#load "platform.fsx"
open Holon
open Platform

let def = 
    {     
        ID = 0;   
        Name = " ";
        Resources = 0;
        RoleOf = None;
        //Role = None;
        SanctionLevel = 0;
        OffenceLevel = 0;
        MessageQueue = []
    }

let parks = {def with Name = "parks"; Resources = 100}
let ron = {def with ID = 1; Name = "ron"; RoleOf = Some (Head(parks.ID))}
let leslie = {def with ID = 2; Name = "leslie"; RoleOf = Some (Gatekeeper(parks.ID))}

let holonLst = [parks ; ron ; leslie]

// let initRoles h = 
//     match (h.MemberOf, h.Role) with
//     | Some inst, Some Member -> RoleOf (h.ID, Some inst, Some Member)
//     | Some inst, Some Head -> RoleOf (h.ID, Some inst, Some Head)
//     | Some inst, Some Gatekeeper -> RoleOf (h.ID, Some inst, Some Gatekeeper)
//     | Some inst, Some Monitor -> RoleOf (h.ID, Some inst, Some Monitor)
//     | _ , _ -> RoleOf (h.ID, None, None)


// let initEnv holons = 
//     let fLst = []
//     let rec initEnv holons fLst = 
//         match holons with
//         | h::rest -> 
//             let f = initRoles h
//             List.append [f] (initEnv rest fLst)
//         | [] -> fLst
//     initEnv holons fLst

// initEnv holonLst

let ben = {def with ID = 3; Name = "ben"}
applyToInst ben parks
parks
