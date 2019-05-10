#load "holon.fsx"
#load "platform.fsx"
#load "physical.fsx"
#load "decisions.fsx"
#load "simulation.fsx"
//#load "initialisation.fsx"
#load "init2.fsx"
open Holon
open Platform
open Physical
open Decisions
open Simulation
//open Initialisation
open Init2

simulate allAgents 0 20 5 [Low;Low;High]

