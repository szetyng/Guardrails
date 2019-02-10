# Guardrails

## Principle 1: Clearly defined boundaries
Agent `A` can 
- `applyToInstitution` if it wasn't already a member, and if it qualifies as a member

`Gatekeeper` in a supra-holon is empowered to 
- `IncludeMember` if an agent has applied to the supra-holon and it qualifies (and hence is approved) as a member
- `ExcludeMember` if a member agent has exceeded the `sanctionLevel` of 2 (consider making this the job of a `Head`)

## Principle 2: Congruence
Agent `A` is empowered to 
- `Demand` R amount of resources from a supra-holon if `A` is a member of the supra-holon and has not demanded anything in this time slice (?) and it has not received any sanctions

Valid demands are added to the `demandQ`, and the `Head` is empowered to `Allocate` resources based on the resource allocation method. 