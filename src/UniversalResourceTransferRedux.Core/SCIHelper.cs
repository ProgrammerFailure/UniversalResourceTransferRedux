using UnityEngine;
using System.Collections;

public static class SCIPositionProvider
{
    private static Orbit tempOrbit;
 
    /// <summary>
    /// Gets the Sun-Centered Inertial (SCI) position of a CelestialBody at a specific Universal Time.
    /// </summary>
    public static Vector3d GetPosition(CelestialBody body, double UT)
    {
        if (body == null || Planetarium.fetch == null) return Vector3d.zero;
        if (body == Planetarium.fetch.Sun) return Vector3d.zero;

        Vector3d pos = Vector3d.zero;
        CelestialBody current = body;

        // Traverse up the Keplerian orbit tree to the Sun
        while (current != null && current != Planetarium.fetch.Sun)
        {
            if (current.orbit != null)
            {
                pos += current.orbit.getPositionAtUT(UT);
            }
            current = current.referenceBody;
        }
        return pos;
    }

    /// <summary>
    /// Gets the Sun-Centered Inertial (SCI) position of a Vessel at a specific Universal Time.
    /// </summary>
    public static Vector3d GetPosition(Vessel vessel, double UT, QuaternionD worldToInertialTransformation)
    {
        if (vessel == null || Planetarium.fetch == null) return Vector3d.zero;

        if (vessel.loaded)
        {
            // Loaded

            Vector3d worldVesselPos = vessel.GetWorldPos3D();
            Vector3d worldSunPos = (Vector3d)Planetarium.fetch.Sun.position;
            Vector3d relativeWorldPos = worldVesselPos - worldSunPos;
            return worldToInertialTransformation * relativeWorldPos;
        }
        else
        {
            // Unloaded Vessels
            if (vessel.Landed || vessel.Splashed)
            {
                Vector3d worldPos = vessel.mainBody.GetWorldSurfacePosition(vessel.latitude, vessel.longitude, vessel.altitude);
                Vector3d worldBodyPos = (Vector3d)vessel.mainBody.position;
                Vector3d relativeWorldPos = worldPos - worldBodyPos;

                return (worldToInertialTransformation * relativeWorldPos) + GetPosition(vessel.mainBody, UT);
            }
            else
            {
                Vector3d relativeInertialPos = vessel.orbit.getPositionAtUT(UT);
                return relativeInertialPos + GetPosition(vessel.mainBody, UT);
            }
        }
    }

    /// <summary>
    /// Gets the Sun-Centered Inertial (SCI) position of a ProtoVessel at a specific Universal Time.
    /// </summary>
    public static Vector3d GetPosition(ProtoVessel protoVessel, double UT, QuaternionD worldToInertialTransformation)
    {
        if (protoVessel == null || Planetarium.fetch == null) return Vector3d.zero;

        Vessel v = protoVessel.vesselRef;
        if (v == null)
        {
            v = FlightGlobals.Vessels.Find(x => x.id == protoVessel.vesselID);
        }

        if (v != null)
        {
            return GetPosition(v, UT, worldToInertialTransformation);
        }

        // Fallback: If no runtime Vessel wrapper exists, process manually from the ProtoVessel properties
        CelestialBody mainBody = GetMainBody(protoVessel);
        if (mainBody == null) return Vector3d.zero;

        if (protoVessel.landed || protoVessel.splashed)
        {
            Vector3d worldPos = mainBody.GetWorldSurfacePosition(protoVessel.latitude, protoVessel.longitude, protoVessel.altitude);
            Vector3d worldBodyPos = (Vector3d)mainBody.position;
            Vector3d relativeWorldPos = worldPos - worldBodyPos;

            return (worldToInertialTransformation * relativeWorldPos) + GetPosition(mainBody, UT);
        }
        else
        {
            if (protoVessel.orbitSnapShot != null)
            {
                LoadSnapshotIntoTemp(protoVessel.orbitSnapShot, mainBody);
                if (tempOrbit != null)
                {
                    Vector3d relativeInertialPos = tempOrbit.getPositionAtUT(UT);
                    return relativeInertialPos + GetPosition(mainBody, UT);
                }
            }
            return Vector3d.zero;
        }
    }

    private static void LoadSnapshotIntoTemp(OrbitSnapshot snap, CelestialBody referenceBody)
    {
        tempOrbit.inclination = snap.inclination;
        tempOrbit.eccentricity = snap.eccentricity;
        tempOrbit.semiMajorAxis = snap.semiMajorAxis;
        tempOrbit.LAN = snap.LAN;
        tempOrbit.argumentOfPeriapsis = snap.argOfPeriapsis;
        tempOrbit.meanAnomalyAtEpoch = snap.meanAnomalyAtEpoch;
        tempOrbit.epoch = snap.epoch;
        tempOrbit.referenceBody = referenceBody;

        tempOrbit.Init();
    }

    /// <summary>
    /// Safely resolves the main body reference from a ProtoVessel.
    /// </summary>
    private static CelestialBody GetMainBody(ProtoVessel pv)
    {
        if (pv == null) return null;
        if (pv.vesselRef != null) return pv.vesselRef.mainBody;

        if (pv.orbitSnapShot != null)
        {
            int bodyIndex = pv.orbitSnapShot.ReferenceBodyIndex;
            if (bodyIndex >= 0 && bodyIndex < FlightGlobals.Bodies.Count)
            {
                return FlightGlobals.Bodies[bodyIndex];
            }
        }
        return null;
    }
}