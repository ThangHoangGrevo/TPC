using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset;
    [SerializeField] private Vector3 angleOffset;

    private void LateUpdate()
    {
        this.transform.rotation = target.rotation *  Quaternion.Euler(angleOffset);
        this.transform.position = target.position + target.rotation * offset;

    }

#if UNITY_EDITOR
    [ContextMenu("CacheCurrentOffset")]
    private void CacheCurrentOffset()
    {
        offset = this.transform.position - target.position;
        angleOffset = Quaternion.FromToRotation(target.forward, transform.forward).eulerAngles;
    }
#endif
}
