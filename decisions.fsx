open System.Collections.Generic
#load "holon.fsx"
#load "platform.fsx"
#load "physical.fsx"
open Holon
open Platform
open Physical

//************************* Principle 2 *********************************/
// TODO: head to change ramethod based on level of resources in the inst


/// TODO: agent decides what r is, from Allocated or from greed
let desireToCheat agent =
    