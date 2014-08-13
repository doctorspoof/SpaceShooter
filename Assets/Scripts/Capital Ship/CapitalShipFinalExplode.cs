using UnityEngine;



/// <summary>
/// An incredibly basic class whose only function is to wait for the OnDestroy() method to be called, at this point it alerts the GUIManager
/// than the CShip has been destroyed.
/// </summary>
public sealed class CapitalShipFinalExplode : MonoBehaviour 
{
    /// <summary>
    /// Alerts the GUIManager that the CShip has been destroyed so that the game may end.
    /// </summary>
	void OnDestroy()
	{
		//Tell gui to display loss popup
        GameObject.FindGameObjectWithTag("GameController").GetComponent<GameStateController>().SwitchToGameOver();
    }
}
