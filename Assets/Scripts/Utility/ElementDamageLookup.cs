using UnityEngine;
using System.Collections;

public static class ElementDamageLookup 
{
    // First id is firer, second id is target
                                    //Vs. Fire      Ice     Earth   Ligtn.  Light   Dark    Spirit  Grav    Air     Nature 
    static float[,] m_percentageTable = { {0.5f,    1.25f,  0.75f,  0.9f,   1f,     1.25f,  1.1f,   1f,     0.85f,  1.5f},                // Firing fire
                                          {1.25f,   0.5f,   1f,     1.1f,   1f,     0.85f,  0.85f,  1f,     0.85f,  1.25f},               // Firing ice
                                          {0.75f,   1f,     0.5f,   1f,     1f,     1f,     1f,     0.85f,  1.5f,   0.95f},               // Firing earth
                                          {0.9f,    1.1f,   1f,     0.5f,   1f,     1.25f,  1.1f,   1f,     1f,     1.2f},               // Firing lightning
                                          {1f,      1f,     0.75f,  0.9f,   0.5f,   1.5f,   1.1f,   1f,     1f,     0.6f},               // Firing light
                                          {0.9f,    1f,     1f,     1.1f,   1.5f,   0.5f,   1f,     1.1f,   1f,     1.1f},               // Firing dark
                                          {0.95f,   0.9f,   1.2f,   1f,     0.85f,  0.95f,  0.5f,   1f,     0.95f,  0.6f},               // Firing spirit
                                          {1f,      1f,     1f,     1f,     1f,     1.1f,   1f,     0.5f,   1f,     0.85f},               // Firing gravity
                                          {0.8f,    1f,     0.9f,   1f,     1f,     1f,     1.05f,  1.1f,   0.5f,   0.75f},               // Firing air
                                          {1.5f,    1.15f,  1f,     0.85f,  1.1f,   0.85f,  0.95f,  1f,     0.95f,  0.5f},               // Firing organic
                                        };
                          
    // Returns a decimal percentage              
    public static float GetDamagePercentage(Element firer, Element victim)
    {
        if(firer == Element.NULL || victim == Element.NULL)
            return 1f;
        else
            return m_percentageTable[(int)firer, (int)victim];
    }
    public static float GetDamagePercentage(int firer, int victim)
    {
        if(firer == 0 || victim == 0)
            return 1f;
        else
            return m_percentageTable[firer, victim];
    }
}
