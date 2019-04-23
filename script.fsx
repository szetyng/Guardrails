open System.Runtime.Hosting
#load "holon.fsx"
#load "platform.fsx"
#load "physical.fsx"
#load "simulation.fsx"
#load "initialisation.fsx"
open Holon
open Platform
open Physical
open Simulation
open Initialisation

simulate allAgents 0
printNames allAgents