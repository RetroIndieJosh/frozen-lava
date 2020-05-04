using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cold : MonoBehaviour {
    [SerializeField]
    private float m_coolPerSec = 0.5f;

    private void OnTriggerStay2D( Collider2D collision ) {
        if ( collision == null ) return;
        if ( collision.transform.parent == null ) return;

        var mover = collision.transform.parent.GetComponentInChildren<BodyMover>();
        mover.SlideDirection = mover.Direction;

        var heatSuit = collision.transform.parent.GetComponentInChildren<HeatSuit>();
        if ( heatSuit == null ) return;

        heatSuit.LoseHeat( Time.deltaTime * m_coolPerSec);
    }
}
