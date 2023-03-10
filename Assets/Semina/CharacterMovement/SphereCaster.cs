using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SphereCaster : MonoBehaviour
{
	[SerializeField] private LayerMask layerMask;
	[SerializeField] private Transform origin;
	[SerializeField] private float length;

	public bool IsIntersecting()
	{
		return Physics.CheckSphere(origin.position, length, layerMask, QueryTriggerInteraction.Ignore);
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.yellow;
		Gizmos.DrawSphere(origin.position, length);
	}
}
