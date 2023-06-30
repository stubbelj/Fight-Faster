using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColliderTrapTrigger : MonoBehaviour
{
    public Trap trap;

    void OnTriggerEnter2D() {
        trap.Trigger();
    }
}
