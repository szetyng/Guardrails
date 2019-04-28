open System.Runtime.Hosting
#load "holon.fsx"
#load "platform.fsx"
#load "physical.fsx"
#load "decisions.fsx"
#load "simulation.fsx"
#load "initialisation.fsx"
open Holon
open Platform
open Physical
open Decisions
open Simulation
open Initialisation

simulate allAgents 0 5 [High;Medium;Low]

