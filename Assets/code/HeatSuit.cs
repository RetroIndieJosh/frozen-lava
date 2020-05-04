using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeatSuit : MonoBehaviour {
    [SerializeField]
    Counter m_ammoCounter = null;

    [SerializeField]
    Jumper m_jumper = null;

    [SerializeField]
    Health m_health = null;

    [SerializeField]
    float m_timePerPullSec = 1.0f;

    [SerializeField]
    Palette m_palette;

    private bool m_isCharging = false;
    private float m_timeElapsed = 0.0f;

    public void LoseHeat(float a_coolAmount ) {
        m_ammoCounter.Add( -a_coolAmount );
    }

    public void MaxCharge() {
        m_ammoCounter.ResetToMaximum();
        TryCharge();
    }

    public void StartCharge() {
        GetComponent<AudioSource>().Play();

        m_isCharging = true;
        m_timeElapsed = 0.0f;

        // HACK force a drain on the first press so we can use switch weapon logic
        TryCharge();
    }

    public void StopCharge() {
        m_isCharging = false;
        GetComponent<AudioSource>().Stop();
    }

    private void Update() {
        if ( GodMode ) m_ammoCounter.ResetToMaximum();

        UpdatePalette();
        if ( !m_isCharging ) return;

        m_timeElapsed += Time.deltaTime;
        if ( m_timeElapsed < m_timePerPullSec ) return;

        m_timeElapsed = 0.0f;

        TryCharge();
    }

    public bool GodMode = false;

    private void UpdatePalette() {
        if( GodMode ) {
            m_palette[0] = Color.yellow;
            m_palette[1] = Color.red;
            return;
        }

        if( m_ammoCounter.Count > 0 ) {
            m_palette[0] = Color.white;
            m_palette[1] = Color.red;
        } else {
            m_palette[0] = Color.grey;
            m_palette[1] = Color.green;
        }
    }

    private void TryCharge() {
        var middle = m_jumper.MiddleGroundHit;
        if ( middle ) {
            PullHeat( middle.collider.gameObject );
            return;
        }

        var left = m_jumper.RightGroundHit;
        if ( left ) {
            PullHeat( left.collider.gameObject );
            return;
        }

        var right = m_jumper.RightGroundHit;
        if ( right ) PullHeat( right.collider.gameObject );
        else StopCharge();
    }

    private void PullHeat( GameObject a_target ) {
        if ( m_health.Count == m_health.Maximum
            && m_ammoCounter.Count == m_ammoCounter.Maximum ) {

            StopCharge();
            return;
        }

        var lava = a_target.GetComponent<Lava>();
        if ( lava == null ) {
            StopCharge();
            return;
        }

        var heat = lava.PullHeat();
        if( heat == 0 ) {
            StopCharge();
            return;
        }
        if ( m_health.Count < m_health.Maximum ) m_health.Add( heat * 3 );
        else m_ammoCounter.Add( heat );
    }
}
