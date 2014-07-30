using UnityEngine;
using System.Collections;



/// <summary>
/// The barrel script is used entirely to perform the recoil action of gun barrels on each ship.
/// </summary>
public class Barrel : MonoBehaviour 
{
    #region Unity modifiable values

	[SerializeField] float m_minY = 0f;  // The minimum position on the Y axis the barrel can move to
	[SerializeField] float m_idleY = 0f; // The position on the Y axis the barrel should be when idle

    #endregion


    #region Recoil functionality

	public void Recoil (float timeToRecoil)
	{
		StartCoroutine (BeginRecoilAnimation (timeToRecoil));
	}
	
    /// <summary>
    /// Handles the animation sequence for the barrel of the weapon.
    /// Will automatically calculate the time required to animate by looking at the time to fire and number of barrels.
    /// </summary>
	IEnumerator BeginRecoilAnimation (float timeToRecoil)
	{
        // Cache variables instead of recreating them every frame
        float recoilSequenceTimer = 0f, t = 0f, newY = 0f;

        // Perform the recoil loop
        while (recoilSequenceTimer < timeToRecoil)
		{
            recoilSequenceTimer += Time.deltaTime;

            if (recoilSequenceTimer <= (timeToRecoil * 0.25f))
			{
				// Recoiling, get the 0->(1/4TTR) to 0->1
                t = recoilSequenceTimer / (timeToRecoil * 0.25f);
				
                newY = Mathf.Lerp (m_idleY, m_minY, t);
			}

			else
			{
				// Extending, get the 1/4TTR->TTR to 0->1
                t = (recoilSequenceTimer - (timeToRecoil * 0.25f)) / (timeToRecoil / 3f);
				
                newY = Mathf.Lerp (m_minY, m_idleY, t);
			}

            // Update the position
            transform.localPosition = new Vector3 (transform.localPosition.x, newY, transform.localPosition.z);

			yield return null;
		}
	}

    #endregion
}
