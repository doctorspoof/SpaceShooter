using UnityEngine;
using System.Collections;

public enum LightningZapType
{
    Small = 1,
    Big = 2
}

public class EffectsManager : MonoBehaviour 
{
    [SerializeField]        GameObject      m_lightningBigZap;
    [SerializeField]        GameObject      m_lightningSmallZap;
    
    [SerializeField]        GameObject      m_fireAoEExplosion;
    
    [SerializeField]        GameObject      m_lightCleansePulse;
    
    [SerializeField]        GameObject      m_dotDripEffect;

    #region Accessfuncs
    public void SpawnLightningEffect(Vector3 startPos, Vector3 endPos, LightningZapType type)
    {
        Vector3 direction = (endPos - startPos);
        
        GameObject effect = null;
        
        Quaternion rotation = Quaternion.LookRotation(direction.normalized, -Vector3.forward);
        rotation.x = 0;
        rotation.y = 0;
        //First work out how many points we need
        int numPoints = (int)direction.magnitude;
        
        //if we have x points, we need x-2 lines
        int numLines = numPoints - 1;
        float lineLength = direction.magnitude / (float)numLines;
        
        //Generate a list of points for the lines to use when they spawn
        Vector3[] points = new Vector3[numPoints];
        points[0] = startPos;
        points[points.Length - 1] = endPos;
        
        //TODO: Change these points to be more random
        for(int i = 1; i < (points.Length - 1); i++)
        {
            points[i] = points[i - 1] + (direction.normalized * lineLength);
        }
        
        //Iterate through the array of points and move them a +ve or -ve amount in the left/right vector
        Vector3 rightVec = Vector3.Cross(-Camera.main.transform.forward.normalized, direction.normalized);
        for(int i = 1; i < (points.Length - 1); i++)
        {
            float randMag = Random.Range(lineLength * -0.5f, lineLength * 0.5f);
            
            points[i] += rightVec * randMag;
        }
        
        //Use the list of points to spawn many line renderers
        for(int i = 0; i < numLines; i++)
        {
            if(type == LightningZapType.Big)
                effect = Instantiate(m_lightningBigZap, startPos + (direction.normalized * (direction.magnitude * 0.5f)), rotation) as GameObject;
            else
                effect = Instantiate(m_lightningSmallZap, startPos + (direction.normalized * (direction.magnitude * 0.5f)), rotation) as GameObject;
            
            LineRenderer line = effect.GetComponent<LineRenderer>();
            
            line.SetVertexCount(2);
            line.SetPosition(0, points[i]);
            line.SetPosition(1, points[i + 1]);
        }
            
        //float visualMag = direction.magnitude * 2.0f;
        //effect.transform.localScale = new Vector3(2.0f, visualMag, 2.0f);
    }
    
    public void SpawnExplosionEffect(Vector3 position, float range)
    {
        GameObject explode = Instantiate(m_fireAoEExplosion, position, Quaternion.identity) as GameObject;
        explode.transform.localScale = new Vector3(range, range, 1.0f);
    }
    
    public void SpawnDotEffect(Vector3 position, float duration, GameObject parent = null)
    {
        GameObject drip = Instantiate(m_dotDripEffect, position + new Vector3(0, 0, -1f), Quaternion.identity) as GameObject;
        Destroy(drip, duration);
        
        if(parent != null)
            drip.transform.parent = parent.transform;
    }
    #endregion
}
