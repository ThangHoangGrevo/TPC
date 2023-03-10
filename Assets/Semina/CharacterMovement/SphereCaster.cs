using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class SphereCaster : MonoBehaviour
{
	[SerializeField] private LayerMask layerMask;
	[SerializeField] private Transform origin;
	[SerializeField] private float radius;

	public bool IsIntersecting()
	{
		return Physics.CheckSphere(origin.position, radius, layerMask, QueryTriggerInteraction.Ignore);
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.yellow;
		Gizmos.DrawSphere(origin.position, radius);
	}
}
