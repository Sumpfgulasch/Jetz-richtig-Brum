using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trajectory
{
    private Vector3[] positions;
    public Vector3[] Positions { get { return positions;} }


    private bool hasHit;
    public bool HasHit { get { return hasHit;} }

    private Vector3 hitPoint;
    public Vector3 HitPoint { get { if (hasHit) { return hitPoint; } else { return Vector3.zero; } } }

    private Vector3 hitNormal;
    public Vector3 HitNormal { get { if (hasHit) { return hitNormal; } else { return Vector3.zero;}}}

    private GameObject hitObject;
    public GameObject HitObject { get { if (hasHit) { return hitObject; } else { return null;}}}

    public Trajectory(Vector3[] _positions, bool _hasHit = false, GameObject _hitObject = null ,Vector3 _hitNormal = new Vector3())
    {
        positions = _positions;
        hasHit = _hasHit;
        hitObject = _hitObject;
        hitNormal = _hitNormal;

        if (_hasHit == true)
        {
            if (_positions.Length > 0)
            {
                hitPoint = _positions[_positions.Length - 1];
            }
        }
        else
        {
            hitPoint = Vector3.zero;
        }
    }
}
