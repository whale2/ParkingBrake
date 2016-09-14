using System;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using ModuleWheels;

namespace ParkingBrake
{
	public enum State {True, False, Unknown};

	public class ParkingBrake : PartModule
	{
		[KSPField]
		public float bumpVelocityThreshold = 0.1f;
		[KSPField]
		public float partVelocityThreshold = 0.1f;
		[KSPField]
		public float restVelocityThreshold = 0.05f;
		[KSPField]
		public int debugLevel = 0;

		public bool isAvailable = false;

		[KSPField(isPersistant = true)]
		public State isInstalled = State.False;

		[KSPField(isPersistant = true)]
		public State isEngaged = State.Unknown;

		[KSPField(isPersistant = true)]
		private State isMaster = State.Unknown;

		private Part mirrorBrake = null;
		private bool inRest = false;

		public ParkingBrake ()
		{
		}

		public override string GetInfo ()
		{
			return "ParkingBrake - available in mirrored setup";
		}

		public override void OnStart(StartState state)
		{
			switch (state) {
			case StartState.Editor:
				isAvailable = this.part.symmetryCounterparts.Count() > 0;
				Events ["InstallParkingBrake"].active = (isAvailable && isInstalled != State.True);
				Events ["UninstallParkingBrake"].active = (isAvailable && isInstalled == State.True);
				dbg ("Editor mode; available: " + isAvailable + "; installed: " + isInstalled, 1);
				break;

			default:

				Events ["InstallParkingBrake"].active = false;
				Events ["UninstallParkingBrake"].active = false;

				if (isEngaged == State.Unknown) {
					if (isInstalled == State.True)
						Events ["EngageParkingBrake"].active = true;

					Events ["DisengageParkingBrake"].active = false;
				} else {
					//Events ["EngageParkingBrake"].active = engaged == State.True;
					//Events ["DisengageParkingBrake"].active = engaged == State.False;

					if (isMaster != State.True)
						return;
					if (isEngaged == State.True)
						EngageParkingBrake ();
					else
						DisengageParkingBrake ();
				}
				break;
			}
				
		}

		[KSPEvent(guiActive = false, guiActiveEditor = true, name = "InstallParkingBrake", guiName = "Install Parking Brake")]
		public void InstallParkingBrake() {

			isInstalled = State.True;
			Events ["InstallParkingBrake"].active = false;
			Events ["UninstallParkingBrake"].active = true;
			foreach (Part p in this.part.symmetryCounterparts)
				foreach (ParkingBrake b in p.Modules.OfType<ParkingBrake>()) {
					b.isInstalled = State.True;
					b.Events ["InstallParkingBrake"].active = false;
					b.Events ["UninstallParkingBrake"].active = true;
				}
		}

		[KSPEvent(guiActive = false, guiActiveEditor = true, name = "UninstallParkingBrake", guiName = "Uninstall Parking Brake")]
		public void UninstallParkingBrake() {

			isInstalled = State.False;
			Events ["InstallParkingBrake"].active = true;
			Events ["UninstallParkingBrake"].active = false;
			foreach (Part p in this.part.symmetryCounterparts)
				foreach (ParkingBrake b in p.Modules.OfType<ParkingBrake>()) {
					b.isInstalled = State.False;
					b.Events ["InstallParkingBrake"].active = true;
					b.Events ["UninstallParkingBrake"].active = false;
				}
		}

		[KSPEvent(guiActive = true, guiActiveEditor = false, name = "EngageParkingBrake", guiName = "Engage Parking Brake")]
		public void EngageParkingBrake()
		{
			if (isInstalled == State.False) // || isMaster == State.False)
				return;

			isEngaged = State.True;
			isMaster = State.True;
			mirrorBrake = null;

			// Register this parking brake
			ParkedVessel.parkVessel(this.vessel.id, this.part);
			Events ["EngageParkingBrake"].active = false;
			Events ["DisengageParkingBrake"].active = true;
			dbg ("Engaged", 2);

			// saving part positions
			// TODO: Only stabilize after we're settled
			inRest  = false;

			// engage real brakes
			foreach (ModuleWheelBrakes m in this.part.Modules.OfType<ModuleWheelBrakes>()) {
				m.brakeInput = 1;
			}
		}

		[KSPEvent(guiActive = true, guiActiveEditor = false, name = "DisengageParkingBrake", guiName = "Disengage Parking Brake")]
		public void DisengageParkingBrake()
		{
			if (mirrorBrake == null) {
				isEngaged = State.False;
				Events ["EngageParkingBrake"].active = true;
				Events ["DisengageParkingBrake"].active = false;

				ParkedVessel.releaseVessel (this.vessel.id, this.part);
				dbg ("Disengaged", 2);
			} else {
				ParkedVessel.releaseVessel (this.vessel.id, this.part);
			}

			// disengage real brakes
			foreach (ModuleWheelBrakes m in this.part.Modules.OfType<ModuleWheelBrakes>()) {
				m.brakeInput = 0;
			}
		}

		public void EngageMirroredParkingBrake(Part mirror)
		{
			dbg ("Engaging by mirrored part; my id=" + this.part.GetInstanceID () + "; other id=" + mirror.GetInstanceID(), 2);

			isMaster = State.False;
			mirrorBrake = mirror;
			isEngaged = State.True;
			Events ["EngageParkingBrake"].active = false;
			Events ["DisengageParkingBrake"].active = true;

			// engage real brakes
			foreach (ModuleWheelBrakes m in this.part.Modules.OfType<ModuleWheelBrakes>()) {
				m.brakeInput = 1;
			}
		}

		public void DisengageMirroredParkingBrake()
		{
			isMaster = State.Unknown;
			mirrorBrake = null;
			isEngaged = State.False;
			Events ["EngageParkingBrake"].active = true;
			Events ["DisengageParkingBrake"].active = false;

			// disengage real brakes
			foreach (ModuleWheelBrakes m in this.part.Modules.OfType<ModuleWheelBrakes>()) {
				m.brakeInput = 0;
			}

			dbg ("Disengaged mirrored parking brake", 2);
		}

		public override void OnLoad(ConfigNode node) {
			
			this.isInstalled = resolveState (node.GetValue ("isInstalled"));
			this.isEngaged = resolveState (node.GetValue ("isEngaged"));
			this.isMaster = resolveState (node.GetValue ("isMaster"));

			dbg ("OnLoad: part name: " + this.part.name + "; installed: " + isInstalled + "; engaged: " + isEngaged + "; master: " + isMaster, 2);
			if (this.isInstalled != State.True) {
				Events ["EngageParkingBrake"].active = false;
				Events ["DisengageParkingBrake"].active = false;
			} else if (this.isEngaged == State.True) {
				EngageParkingBrake ();
			} else if (this.isEngaged == State.False) {
				DisengageParkingBrake ();
			}
		}

		/*public override void OnSave(ConfigNode node) {

			node.AddValue("isInstalled", this.isInstalled);
			node.AddValue("isEngaged", this.isEngaged);
			node.AddValue("isMaster", this.isMaster);
		}*/

		public void FixedUpdate()
		{
			if (vessel == null)
				return;

			float srfSpeed = this.vessel.GetSrfVelocity ().magnitude;
			if (srfSpeed > bumpVelocityThreshold)
				inRest = false;
			
			else if (srfSpeed < restVelocityThreshold)
				inRest = true;

			if (inRest && shouldStabilize())
			{
				dbg ("Stabilizing", 3);

				// Ok, let's cast some sleep magic
				int j = 0;
				int k = 0;
				foreach (Part p in this.vessel.parts) {
					//if (p.physicalSignificance != Part.PhysicalSignificance.FULL || p.Rigidbody == null)
					if (p.Rigidbody == null)
						continue;
					j++;

					//print (nm + "Veocity magnitude: " + p.Rigidbody.velocity.magnitude);
					if (p.Rigidbody.velocity.magnitude < partVelocityThreshold) {
						//p.Rigidbody.velocity = Vector3.zero;
						p.Rigidbody.Sleep ();
						k++;
					}
				}

				dbg ("Put to sleep " + k + " parts out of " + j + " with Rigidbody", 3);
			}
			else dbg ("Not stabilizing", 3);

			Vector3 sp = this.vessel.GetSrfVelocity ();
			dbg ("Vessel speed: x=" + sp.x + "; y=" + sp.y + "; z=" + sp.z, 3);
			dbg ("In rest = " + inRest, 3);
		}

		private bool shouldStabilize() {

			dbg ("eng=" + isEngaged + "; master=" + isMaster + 
				"; mirror=" + (mirrorBrake == null ? "null" : "not null") + "; landed=" + this.vessel.checkLanded() + 
				"; splashed=" + this.vessel.checkSplashed() + "; srf=" + this.vessel.GetSrfVelocity ().magnitude + 
				"; thr=" + bumpVelocityThreshold + "; save thr=" + restVelocityThreshold, 3);
			if (this.isEngaged != State.True)
				return false;
			if (this.isMaster != State.True)
				return false;
			if (this.vessel == null)
				return false; 
			if (!this.vessel.checkLanded())
				return false;
			if (this.vessel.checkSplashed ())
				return false;
			
			return true;
		}

		private State resolveState(String state) {
			switch (state) {
			case "True":
				return State.True;
			case "False":
				return State.False;
			default:
				return State.Unknown;
			}
		}

		private void dbg(String msg, int lvl) {

			if (lvl <= debugLevel) {
				String id = (vessel != null ? vessel.GetName () : "-")  + "(" + this.part.GetInstanceID() + "): ";

				print ("ParkingBrake: " + id + msg);
			}
		}
	}
}

