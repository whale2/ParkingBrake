# ParkingBrake
Kerbal Space Program mod - duct-tape solution for slowly sliding vehicles 

I've found that sometimes my planes in KSP 1.1.3 slowly slide from their 
position ignoring any brakes and anchors. That makes really annoying 
performing airport-like activities - boarding and de-boarding crew, refueling, 
loading and unloading cargo, you name it. Either this will be probably fixed in 
upcoming (as of now) KSP 1.2, chances are that some first 1.2 releases will 
not nail it or will contain some other problems, so until 1.1.3 is around, one
can park their vehicles using Parking Brake. 

The mod itself is not perfect and really is a kind of duct-tape solution - it 
tries to 'freeze' every part of the vehicle by calling Sleep() on Unity's Rigidbody
object of every vessel part.
However, parked vehicle is not permanently fixed by divine power - if you bump your
heavy airport truck into parked plane, it will move due to the impulse from the truck,
so only small phantom speed, usually under 0.1 m/s, is eliminated.
(See Brake.cfg for some tweakable parameters)

Parking brake can be engaged on any wheel that has brakes and attached in mirror setup 
and when engaged, it also activates standard wheel brake, however, whole brake action 
group is not toggled (so, for instance, airbrakes are not activated)
Parking brake should be 'installed' (enabled) in editor to be available in flight mode.
Activating parking brake on any wheel in turn activates all other brakes where parking 
brake is installed.

(This is development version, expect tons of bugs)

