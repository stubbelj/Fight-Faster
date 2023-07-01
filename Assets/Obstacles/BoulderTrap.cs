using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoulderTrap : Trap
{
    public Sprite disabledSprite;
    public Sprite enabledSprite;
    public GameObject attack;
    public bool boulderSpawned = false;

    SpriteRenderer sr;
    
    void Awake() {
        sr = GetComponent<SpriteRenderer>();
    }

    public override void Trigger() {
        if (boulderSpawned) {
            return;
        }
        sr.sprite = enabledSprite;
        attack.SetActive(true);
        attack.GetComponent<BoulderSpawn>().Init();
        boulderSpawned = true;
    }

    public override void Reset() {
        sr.sprite = disabledSprite;
        attack.SetActive(false);
    }
}
