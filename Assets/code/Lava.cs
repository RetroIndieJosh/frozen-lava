using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lava : MonoBehaviour {
    [Header("Heat")]

    [SerializeField]
    Counter m_counter = null;

    [SerializeField]
    private int m_heatPerPull = 1;

    [Header("Graphics")]

    [SerializeField]
    Palette m_palette = null;

    [SerializeField]
    int m_paletteIndexHeated = 0;

    [SerializeField]
    int m_paletteIndexCooled = 1;

    public int PullHeat() {
        if( m_counter.Count <= 0 ) return 0;

        m_counter.Add( -m_heatPerPull );
        if ( m_counter.Count <= 0 ) {
            m_palette.PaletteIndex = m_paletteIndexCooled;
            var animator = m_palette.GetComponent<SpriteAnimator>();
            if ( animator != null ) animator.IsPaused = true;
            m_palette.UpdateColors();
        }
        return m_heatPerPull;
    }

    private void Start() {
        m_palette.PaletteIndex = m_paletteIndexHeated;
    }
}
