using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine.VFX;

public class CollectableLocation : Collectable
{
    private bool enable = true;
    public override bool Enable { get => enable; set => enable = value; }
    public float rotationSpeed = 0.5f;

    private void Update()
    {
        //rotate this object randomly.
        this.transform.Rotate(new Vector3(Mathf.PerlinNoise(Time.time, 0f)* rotationSpeed, Mathf.PerlinNoise(0f, Time.time)* rotationSpeed, 0f));
    }
    public override void OnCollect(bool _enable)
    {
        //SPAWN AND KILL PARTICLE.
        SpawnAndKillParticle(this.transform.position, SceneObjectManager.Instance.collectableMechanicVisualEffectAsset, 5f);
    }
}
