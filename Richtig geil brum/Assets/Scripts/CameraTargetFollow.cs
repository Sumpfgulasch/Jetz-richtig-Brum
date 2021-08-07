using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class CameraTargetFollow : SerializedMonoBehaviour
{
    [Range(0f,1f)] public float rotationDelay;
    [Range(0f,1f)] public float transformationDelay;
    [RequiredAttribute] public Transform followingTransform;
    void Update()
    {
        this.transform.position = Vector3.Lerp(followingTransform.position, this.transform.position, transformationDelay);
        this.transform.rotation = Quaternion.Slerp(followingTransform.rotation, this.transform.rotation, rotationDelay);
    }


}
