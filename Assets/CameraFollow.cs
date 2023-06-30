using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField]
    public GameObject target;

    [SerializeField]
    float smoothNess;

    [Header("Offset")]
    [SerializeField]
    float x;
    [SerializeField]
    float y;

    void LateUpdate() {
        transform.position = Vector3.Lerp(transform.position, target.transform.position + new Vector3(x, y, -10), smoothNess);
    }
}
