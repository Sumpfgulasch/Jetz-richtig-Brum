using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class RotationTestings : SerializedMonoBehaviour
{
    public GameObject targetRotationObject;
    Quaternion newRot;
    Quaternion originalRot;

    public float angleX = 0f;
    public float angleY = 0f;
    public float angleZ = 0f;

    private void Awake()
    {
        originalRot = this.transform.rotation;
    }

    private void Update()
    {
        angleX = Vector3.Angle(this.transform.right, targetRotationObject.transform.right);
        angleY = Vector3.Angle(this.transform.up, targetRotationObject.transform.up);
        angleZ = Vector3.Angle(this.transform.forward, targetRotationObject.transform.forward);
    }

    [Button]
    public void RotateTowardsTargetZ()
    {
        Quaternion rot = Quaternion.FromToRotation(this.transform.forward, targetRotationObject.transform.forward);
        StartCoroutine(RotateToTarget(rot));
    }

    [Button]
    public void RotateTowardsOriginal()
    {
        StartCoroutine(RotateToTarget(originalRot));
    }

    [Button]
    public void RotateToMinimizeY()
    {
        Quaternion rotA = Quaternion.FromToRotation(this.transform.forward, targetRotationObject.transform.forward);

        Vector3 thisYAxis = rotA * this.transform.up;
        float angle = Vector3.SignedAngle(thisYAxis, targetRotationObject.transform.up, Vector3.Cross(thisYAxis, targetRotationObject.transform.up));


        Quaternion rot = Quaternion.AngleAxis(angle, this.transform.forward);

        StartCoroutine(RotateToTarget(rot));
    }

    public IEnumerator RotateToTarget(Quaternion _newRot)
    {
        //Vector3 axis = Vector3.Cross(this.transform.forward, targetRotationObject.transform.forward);
        //Debug.DrawRay(this.transform.position, axis, Color.red,10f);

        Quaternion currentRot = this.transform.rotation;

        float t = 0f;

        while (t < 1f)
        {
            t += 0.01f;
            this.transform.rotation = Quaternion.Slerp(currentRot, _newRot, t);
            yield return new WaitForEndOfFrame();
        }
    }
}
