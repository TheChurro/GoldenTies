using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class WinchableObject : MonoBehaviour
{
    public Collider2D Collider;
    void Start() {
        Collider = GetComponent<Collider2D>();
        var winchProxy = new GameObject();
        var proxyCollider = winchProxy.AddComponent<BoxCollider2D>();
        proxyCollider.isTrigger = true;
        proxyCollider.size = 2f * (Vector2)(Collider.bounds.extents) + new Vector2(0.1f, 0.1f);
        winchProxy.AddComponent<WinchableProxy>().obj = this;
        winchProxy.transform.parent = this.gameObject.transform;
        winchProxy.transform.localPosition = Vector3.zero;
    }
}

public class WinchableProxy : MonoBehaviour {
    public WinchableObject obj;
}