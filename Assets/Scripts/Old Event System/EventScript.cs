using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum ResourceType
{
	PlayerCash = 0,
	CShipWater = 1,
	CShipFuel = 2,
	CShipMass = 3
}

public enum OutcomeType
{
	AffectsCapitalShipMoveTarget = 0,
	AffectsCapitalShipObjective = 1,
	AffectsCapitalShipResource = 2,
	AffectsPlayerCash = 3,
	CreatesNewSpawnPoint = 4,
	ImmediatelySpawnsEnemies = 5,
	CausesCShipDamage = 6,
	CausesPlayerDamage = 7,
}

[System.Serializable]
public class EventOutcome
{
	public OutcomeType typeOfOutcome;

	public bool outcomeRequiresSpecificPlayer = false;

	//If it's a resource type, designate it here
	public ResourceType affectedResource;

	// the magnitude is only used for the resource and damage outcome types
	public int outcomeMagnitude;

	//The focus point could be where enemies are spawned, or where the CShip is told to go
	public Transform outcomeFocusPoint;

	//Only applicable for enemy spawnage.
	//If create new spawn point, this will be the wave list for the spawner
	//Otherwise, it's the amount of enemies that are immediately created
	public WaveInfo[] enemiesAssociated;
}

[System.Serializable]
public class EventOutcomeGroup
{
	public EventOutcome[] outcomesInThisGroup;
	public string groupOutcomeText = "This is the outcome text";
	public int percentageChance = 100;
}

[System.Serializable]
public class EventRequirement
{
	public ResourceType requiredResource;
	public int requiredAmount;

	public bool CheckRequirement(GameObject cship, GameObject player)
	{
		switch(requiredResource)
		{
			case ResourceType.CShipFuel:
			{
				if(cship.GetComponent<CapitalShipScript>().GetCurrentResourceFuel() >= requiredAmount)
					return true;
				else
					return false;
			}
			case ResourceType.CShipWater:
			{
				if(cship.GetComponent<CapitalShipScript>().GetCurrentResourceWater() >= requiredAmount)
					return true;
				else
					return false;
			}
			case ResourceType.CShipMass:
			{
				if(cship.GetComponent<CapitalShipScript>().GetCurrentResourceMass() >= requiredAmount)
					return true;
				else
					return false;
			}
			/*case ResourceType.PlayerCash:
			{
				if(player.GetComponent<Inventory>().GetCurrentCash() >= requiredAmount)
					return true;
				else
					return false;
			}*/
		}

		Debug.LogWarning ("Couldn't find enum type: " + requiredResource.ToString ());
		return false;
	}
}

[System.Serializable]
public class EventOption
{
	public bool isHiddenIfNotAvailable = false;
	public EventRequirement[] optionRequirement;
	//public EventOutcome[] m_optionOutcome;
	public EventOutcomeGroup[] optionGroups;
	public string optionText = "This is an option";
	public string hoverText = "This option will affect you in these ways: " + System.Environment.NewLine + "- DEATH";
}

public class EventScript : MonoBehaviour 
{
	[SerializeField] string m_eventText;
	[SerializeField] EventOption[] m_possibleOptions;
	[SerializeField] int[] m_optionVotes;

	[SerializeField] float m_timer = 30.0f;
    
	[SerializeField] GameObject m_selectedPlayer = null;

    //int m_hostVote = 0;
    int m_previousVote = -1;

	bool m_hasStarted = false;
	bool m_hasTriggered = false;

    //float m_timeBetweenWaves = 20.0f;

    bool m_eventShouldSelfDestruct = false;

    string m_delayedOutcomeText = "";


    #region getset

    public string GetEventText()
    {
        return m_eventText;
    }

    public EventOption[] GetPossibleOptions()
    {
        return m_possibleOptions;
    }

    public int[] GetOptionVotes()
    {
        return m_optionVotes;
    }

    public float GetTimer()
    {
        return m_timer;
    }

    public GameObject GetSelectedPlayer()
    {
        return m_selectedPlayer;
    }

    public void SetSelectedPlayer(GameObject player_)
    {
        m_selectedPlayer = player_;
    }

    #endregion getset


    // Use this for initialization
	void Start () 
	{
		m_optionVotes = new int[m_possibleOptions.Length];
		for(int i = 0; i < m_optionVotes.Length; i++)
			m_optionVotes[i] = 0;
	}

	
	// Update is called once per frame
	void Update () 
	{
		if(Network.isServer && m_hasStarted)
		{
			m_timer -= Time.deltaTime;

			if(m_timer <= 0)
			{
				//Do highest voted option
				PerformHighestVoted();
			}

			if(m_eventShouldSelfDestruct)
				Network.Destroy(this.gameObject);
		}
	}

	void OnTriggerEnter(Collider other)
	{
		if(!m_hasTriggered)
		{
			//If the other is a player, then initiate event
			if(other.tag == "Player")
			{
				//TODO: Change this to all connected players
				//GameObject.FindGameObjectWithTag("GUIManager").GetComponent<GUIIn>().SetActiveEvent(this.gameObject, other.gameObject.GetComponent<PlayerControlScript>().GetOwner());
				m_hasStarted = true;
			}
		}
	}
	
	public void VoteForOption(int optionNum)
	{
		if(Network.isServer)
		{
			//Debug.Log ("Received host vote for option #" + optionNum);
			//m_hostVote = optionNum;
			if(m_previousVote != -1)
				m_optionVotes[m_previousVote]--;
			m_previousVote = optionNum;
			m_optionVotes[optionNum]++;

			//Check for >50% here
			float numPlayers = (float)GameObject.FindGameObjectWithTag("GameController").GetComponent<GameStateController>().GetConnectedPlayers().Count;
			float rawHalf = numPlayers / 2.0f;
			
			for(int i = 0; i < m_optionVotes.Length; i++)
			{
				//Debug.Log ("Checking if no. votes #" + m_optionVotes[i] + " is greater than half players (" + rawHalf + ")");
				if(m_optionVotes[i] > rawHalf)
				{
					//This option has over half the votes, it has passed

					ActivateOption(i);
					break;
				}
			}

			//Propagate current votes to all clients
			for(int i = 0; i < m_optionVotes.Length; i++)
			{
				networkView.RPC ("PropagateVotes", RPCMode.Others, i, m_optionVotes[i]);
			}
		}
		else
		{
			networkView.RPC ("PropagateClientVote", RPCMode.Server, optionNum, m_previousVote);
			m_previousVote = optionNum;
		}
	}

	[RPC] void PropagateClientVote(int optionNum, int previousVote)
	{
		Debug.Log ("Received client vote for option #" + optionNum);
		if(previousVote != -1)
			m_optionVotes[previousVote]--;

		m_optionVotes[optionNum]++;

		//Check for >50% here
		int numPlayers = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameStateController>().GetConnectedPlayers().Count;
		float rawHalf = (float)numPlayers / 2.0f;

		for(int i = 0; i < m_optionVotes.Length; i++)
		{
			if(m_optionVotes[i] > rawHalf)
			{
				//This option has over half the votes, it will pass
				ActivateOption(i);
				break;
			}
		}

		//Propagate current votes to all clients
		for(int i = 0; i < m_optionVotes.Length; i++)
		{
			networkView.RPC ("PropagateVotes", RPCMode.Others, i, m_optionVotes[i]);
		}
	}

	[RPC] void PropagateVotes(int location, int votes)
	{
		m_optionVotes[location] = votes;
	}

	void PerformHighestVoted()
	{
		int highest = -1;

		bool isTie = false;
		for(int i = 0; i < m_optionVotes.Length; i++)
		{
			if(highest == -1 || m_optionVotes[i] > m_optionVotes[highest])
				highest = i;
			else if(m_optionVotes[i] == m_optionVotes[highest])
			{
				//Tie!
				//Alert GUI that host needs to select an option
				//GameObject.FindGameObjectWithTag("GUIManager").GetComponent<GUIManager>().HostShouldTieBreak();
				isTie = true;
				break;
			}
		}

		if(!isTie)
			ActivateOption(highest);
		//Tell all clients which one happened
	}

	public string ActivateOption(int optionNum)
	{
		//We should assume the requirements are already met, since the gui shouldn't let them click it if they aren't met
		networkView.RPC ("PropagateTriggered", RPCMode.All, true);

		//Get the selected option
		EventOption selectedOption = m_possibleOptions[optionNum];

		//Generate a number from 1 to the total percentage of all outcome groups
		int totalPercentage = 0;
		foreach(EventOutcomeGroup group in selectedOption.optionGroups)
			totalPercentage += group.percentageChance;


		int rand = Random.Range(0, totalPercentage);
		int previous = 0;
		//Debug.Log ("Total Percentage = " + totalPercentage + ", Rand = " + rand + ".");


		//Find the appropiate outcome group to be triggered
		EventOutcomeGroup groupToBeTriggered = null;
		foreach(EventOutcomeGroup group in selectedOption.optionGroups)
		{
			if(rand < (group.percentageChance + previous))
			{
				groupToBeTriggered = group;
				break;
			}
			else
				previous += group.percentageChance;
		}

		//Get all outcomes of this group and trigger them
		//If any of the outcomes need a player, then pause
		bool playerReqd = false;
		EventOutcome outcomePlayerReqdFor = null;
		if(groupToBeTriggered != null)
		{
			foreach(EventOutcome outcome in groupToBeTriggered.outcomesInThisGroup)
			{
				if(outcome.outcomeRequiresSpecificPlayer)
				{
					playerReqd = true;
					outcomePlayerReqdFor = outcome;
				}
				else
					FireEventOutcome(outcome);
			}
		}
		else
			Debug.LogError ("An outcome group was not selected!");

		//foreach(EventOutcomeGroup
		/*foreach(EventOutcome outcome in selectedOption.m_optionOutcome)
		{
			FireEventOutcome(outcome);
		}*/

		if(playerReqd)
		{
			m_selectedPlayer = null;
			StartCoroutine(ListenForPlayerSelection(outcomePlayerReqdFor));
			m_delayedOutcomeText = groupToBeTriggered.groupOutcomeText;
			//GameObject.FindGameObjectWithTag("GUIManager").GetComponent<GUIManager>().RecievePlayerRequiresSelectingForEvent("A player selection is required");
			return "A player selection is required";
		}
		else
		{
			//Return the string so the GUI can draw it
			string text = "YOU SHOULDN'T SEE THIS";
			if(groupToBeTriggered != null)
				text = groupToBeTriggered.groupOutcomeText;
			//GameObject.FindGameObjectWithTag("GUIManager").GetComponent<GUIManager>().RecieveEventTextFromEventCompletion(text);
			m_eventShouldSelfDestruct = true;
			return text;
		}
	}

	[RPC] void PropagateTriggered(bool triggered)
	{
		m_hasTriggered = triggered;
	}

	void FireEventOutcome(EventOutcome outcome)
	{
		switch(outcome.typeOfOutcome)
		{
			case OutcomeType.AffectsCapitalShipMoveTarget:
			{
				//Set CShip target
				GameObject.FindGameObjectWithTag("Capital").GetComponent<CapitalShipScript>().SetTargetPoint(outcome.outcomeFocusPoint);
				break;
			}
			case OutcomeType.AffectsCapitalShipObjective:
			{
				//TODO: Add this in after the CShip objective system
				break;
			}
			case OutcomeType.AffectsCapitalShipResource:
			{
				switch(outcome.affectedResource)
				{
					case ResourceType.CShipFuel:
					{						
						GameObject.FindGameObjectWithTag("Capital").GetComponent<CapitalShipScript>().AlterCurrentResourceFuel(outcome.outcomeMagnitude);
						break;
					}
					case ResourceType.CShipWater:
					{
                        GameObject.FindGameObjectWithTag("Capital").GetComponent<CapitalShipScript>().AlterCurrentResourceWater(outcome.outcomeMagnitude);
						break;
					}
					case ResourceType.CShipMass:
					{
                        GameObject.FindGameObjectWithTag("Capital").GetComponent<CapitalShipScript>().AlterCurrentResourceMass(outcome.outcomeMagnitude);
						break;
					}
					default:
					{
						Debug.Log ("Couldn't find appropriate capital resource " + outcome.affectedResource.ToString());
						break;
					}
				}
				break;
			}
			case OutcomeType.AffectsPlayerCash:
			{
				//We should be passed a player that will be affected, so we'll store that in a temp for now
				/*if(outcome.outcomeMagnitude < 0)
					m_selectedPlayer.GetComponent<PlayerControlScript>().RemoveCash(-outcome.outcomeMagnitude);
				else
					m_selectedPlayer.GetComponent<PlayerControlScript>().AddCash(outcome.outcomeMagnitude);*/
				break;
			}
			case OutcomeType.CreatesNewSpawnPoint:
			{
				GameObject newSpawnPoint = new GameObject();
				newSpawnPoint.transform.position = outcome.outcomeFocusPoint.position;
				newSpawnPoint.AddComponent<EnemySpawnPointScript>();
				newSpawnPoint.tag = "SpawnPoint";

				//Generate list of GOs to spawn
                List<WaveInfo> enemyWaves = new List<WaveInfo>();
                foreach (WaveInfo wave in outcome.enemiesAssociated)
                {
                    enemyWaves.Add(wave);
                }

                //EnemySpawnPointScript spawnPoint = newSpawnPoint.GetComponent<EnemySpawnPointScript>();
                //spawnPoint.SetSpawnList(enemyWaves, timeBetweenWaves);
                //spawnPoint.m_shouldStartSpawning = true;
				break;
			}
			case OutcomeType.ImmediatelySpawnsEnemies:
			{
				if(outcome.outcomeFocusPoint != null)
				{
					//Immediately spawn the enemies at the focus point
					List<GameObject> enemiesToSpawn = new List<GameObject>();
					foreach(WaveInfo wave in outcome.enemiesAssociated)
					{
						foreach(GameObject enemy in wave.GetRawWave())
						{
							enemiesToSpawn.Add (enemy);
						}
					}

					foreach(GameObject enemy in enemiesToSpawn)
					{
						Network.Instantiate(enemy, outcome.outcomeFocusPoint.position, outcome.outcomeFocusPoint.rotation, 0);
					}
					
					break;
				}
				else
				{
					//Otherwise spawn it where the event object is
					List<GameObject> enemiesToSpawn = new List<GameObject>();
					foreach(WaveInfo wave in outcome.enemiesAssociated)
					{
						foreach(GameObject enemy in wave.GetRawWave())
						{
							enemiesToSpawn.Add (enemy);
						}
					}
					
					foreach(GameObject enemy in enemiesToSpawn)
					{
						Network.Instantiate(enemy, this.transform.position, this.transform.rotation, 0);
					}
					
					break;
				}
			}
			case OutcomeType.CausesCShipDamage:
			{
				GameObject.FindGameObjectWithTag("Capital").GetComponent<HealthScript>().DamageMobHullDirectly(outcome.outcomeMagnitude);
				break;
			}
			case OutcomeType.CausesPlayerDamage:
			{
				//selectedPlayer.GetComponent<HealthScript>().DamageMob(outcome.m_outcomeMagnitude, null);
				m_selectedPlayer.GetComponent<HealthScript>().DamageMobHullDirectly(outcome.outcomeMagnitude);
				break;
			}
		}
	}

	
	IEnumerator ListenForPlayerSelection(EventOutcome outcome)
	{
		while(m_selectedPlayer == null)
		{
			yield return 0;
		}

		//We've been given a player, now fire the outcome
		FireEventOutcome(outcome);
		//GameObject.FindGameObjectWithTag("GUIManager").GetComponent<GUIManager>().RecieveEventTextFromEventCompletion(m_delayedOutcomeText);
		m_eventShouldSelfDestruct = true;
	}
}
