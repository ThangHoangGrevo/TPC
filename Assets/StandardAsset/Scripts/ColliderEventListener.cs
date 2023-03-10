using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ColliderEventListener : MonoBehaviour
{
	public event Action<Collider> OnTriggerEnterEvent = _ => { };
	private void OnTriggerEnter(Collider other)
	{
		OnTriggerEnterEvent.Invoke(other);
	}
}
