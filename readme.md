# Guardrails

Multi-agent simulation platform for a three-tier hierarchy to investigate Elinor Ostrom's minimal recognition of rights to self-organise. Constraints on the rights to self-organise are implemented using guardrails---the space where any action is allowed to be taken. Self-organisation is defined as the ability to make choices for itself and not having everything be dictated for you.

To run the visualisations in `script.fsx`, the FSharp.Charting library is required. At the time of writing, the full library is not available on Mac OS yet, so either run the script on Windows or remove the parts of the code calling it. The output is otherwise a list of values of the satisfaction of the institutions over time. 

## Dependency
```
holon.fsx
|
platform.fsx
|
simulation.fsx
|
init.fsx
|
script.fsx
```
`init.fsx` and `script.fsx` are used to run experiments on the platform. The satisfaction metrics are defined in `script.fsx`, and `init.fsx` has all the holon initialisations. These two files can be rewritten to define your own experiments, with your own holon population.
