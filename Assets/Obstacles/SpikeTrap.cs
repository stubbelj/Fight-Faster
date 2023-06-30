using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpikeTrap : Trap
{
    public Sprite disabledSprite;
    public Sprite enabledSprite;
    public GameObject attack;

    SpriteRenderer sr;
    
    void Awake() {
        sr = GetComponent<SpriteRenderer>();
        attack.GetComponent<Attack>().Init(new AttackData("Neutral", 1));
    }

    public override void Trigger() {
        sr.sprite = enabledSprite;
        attack.SetActive(true);
    }

    public override void Reset() {
        sr.sprite = disabledSprite;
        attack.SetActive(false);
    }
}
