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
		public int resetRate = 60;
		[KSPField]
		public float velocityThreshold = 0.1f;
		[KSPField]
		public float partVelocityThreshold = 0.1f;
		[KSPField]
		public float saveVelocityThreshold = 0.05f;
		[KSPField]
		public int debugLevel = 0;

		public bool isAvailable = false;

		[KSPField(isPersistant = true)]
		public State isInstalled;

		[KSPField(isPersistant = true)]
		public State isEngaged;

		[KSPField(isPersistant = true)]
		private State isMaster;

		private Part mirrorBrake = null;
		private bool needsSave = false;
		private int frameCount;

		private Dictionary<Part, Vector3> partsPosition;

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
			// TODO: Only save after we're settled
			needsSave  = true;
			partsPosition = new Dictionary<Part, Vector3> ();

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

		public override void OnLoad(ConfigNode node)
		{
			this.isInstalled = resolveState (node.GetValue ("isInstalled"));
			this.isEngaged = resolveState (node.GetValue ("isEngaged"));
			this.isMaster = resolveState (node.GetValue ("isMaster"));

			if (this.isInstalled != State.True) {
				Events ["EngageParkingBrake"].active = false;
				Events ["DisengageParkingBrake"].active = false;
			} else if (this.isEngaged == State.True) {
				EngageParkingBrake ();
			}
		}

		public void FixedUpdate()
		{
			if (vessel == null)
				return;
			
			if (shouldStabilize())
			{
				if (needsSave && this.vessel.GetSrfVelocity ().magnitude < saveVelocityThreshold) {
					foreach (Part p in this.vessel.parts)
						partsPosition [p] = p.Rigidbody.position;

					dbg ("Saved parts : " + partsPosition.Count (), 3);
					frameCount = 0;
					needsSave = false;
				}

				frameCount++;
				dbg ("Stabilizing (frameCount=" + frameCount + ")", 3);

				// Ok, let's try to cancel velocity of every part
				int i = 0;
				int j = 0;
				int k = 0;
				foreach (Part p in this.vessel.parts) {
					//if (p.physicalSignificance != Part.PhysicalSignificance.FULL || p.Rigidbody == null)
					if (p.Rigidbody == null)
						continue;
					j++;
					// Eliminating small phantom velocity change
					//print (nm + "Veocity magnitude: " + p.Rigidbody.velocity.magnitude);
					if (p.Rigidbody.velocity.magnitude < partVelocityThreshold) {
						p.Rigidbody.velocity = Vector3.zero;
						k++;
						if (frameCount >= resetRate) {
							try {
								p.Rigidbody.position = partsPosition [p];
								i++;
							} catch (KeyNotFoundException e) {
								dbg ("Part " + p.name + " not found (" + e.ToString () + ")", 2);
								dbg ("Parts Dict=" + partsPosition.ToString (), 2);
								dbg ("First part: " + partsPosition.First ().Key.ToString ()
									+ "; name=" + partsPosition.First ().Key.name, 2);
							}
						}
					}
				}
				if (i > 0)
					frameCount = 0;
				dbg ("Reset velocity for " + k + " parts out of " + j + " with Rigidbody; reset position for " + i + " parts", 3);
				Vector3d sp = vessel.GetSrfVelocity ();
				dbg ("Vessel speed: x=" + sp.x + "; y=" + sp.y + "; z=" + sp.z, 3);
			}
			else dbg ("Not stabilizing", 3);
		}

		private bool shouldStabilize() {

			dbg ("eng=" + isEngaged + "; master=" + isMaster + 
				"; mirror=" + (mirrorBrake == null ? "null" : "not null") + "; landed=" + this.vessel.checkLanded() + 
				"; splashed=" + this.vessel.checkSplashed() + "; partspos=" + (partsPosition == null ? "null" : "not null") + 
				"; srf=" + this.vessel.GetSrfVelocity ().magnitude + "; thr=" + velocityThreshold + "; save thr=" + saveVelocityThreshold, 3);
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
			if (this.partsPosition == null)
				return false;
			if (this.vessel.GetSrfVelocity ().magnitude > velocityThreshold) {
				// trigger saving new vehicle position
				needsSave = true;
				return false;
			}
			
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

