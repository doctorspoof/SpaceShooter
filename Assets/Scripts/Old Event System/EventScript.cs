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
	public OutcomeType m_typeOfOutcome;

	public bool m_outcomeRequiresSpecificPlayer = false;

	//If it's a resource type, designate it here
	public ResourceType m_affectedResource;

	// the magnitude is only used for the resource and damage outcome types
	public int m_outcomeMagnitude;

	//The focus point could be where enemies are spawned, or where the CShip is told to go
	public Transform m_outcomeFocusPoint;

	//Only applicable for enemy spawnage.
	//If create new spawn point, this will be the wave list for the spawner
	//Otherwise, it's the amount of enemies that are immediately created
	public WaveInfo[] m_enemiesAssociated;
}

[System.Serializable]
public class EventOutcomeGroup
{
	public EventOutcome[] m_outcomesInThisGroup;
	public string m_groupOutcomeText = "This is the outcome text";
	public int percentageChance = 100;
}

[System.Serializable]
public class EventRequirement
{
	public ResourceType m_requiredResource;
	public int m_requiredAmount;

	public bool CheckRequirement(GameObject cship, GameObject player)
	{
		switch(m_requiredResource)
		{
			case ResourceType.CShipFuel:
			{
				if(cship.GetComponent<CapitalShipScript>().GetCurrentResourceFuel() >= m_requiredAmount)
					return true;
				else
					return false;
			}
			case ResourceType.CShipWater:
			{
				if(cship.GetComponent<CapitalShipScript>().GetCurrentResourceWater() >= m_requiredAmount)
					return true;
				else
					return false;
			}
			case ResourceType.CShipMass:
			{
				if(cship.GetComponent<CapitalShipScript>().GetCurrentResourceMass() >= m_requiredAmount)
					return true;
				else
					return false;
			}
			case ResourceType.PlayerCash:
			{
				if(player.GetComponent<PlayerControlScript>().GetSpaceBucks() >= m_requiredAmount)
					return true;
				else
					return false;
			}
		}

		Debug.LogWarning ("Couldn't find enum type: " + m_requiredResource.ToString ());
		return false;
	}
}

[System.Serializable]
public class EventOption
{
	public bool m_isHiddenIfNotAvailable = false;
	public EventRequirement[] m_optionRequirement;
	//public EventOutcome[] m_optionOutcome;
	public EventOutcomeGroup[] m_optionGroups;
	public string m_optionText = "This is an option";
	public string m_hoverText = "This option will affect you in these ways: " + System.Environment.NewLine + "- DEATH";
}

public class EventScript : MonoBehaviour 
{
	public string m_EventText;
	public EventOption[] m_possibleOptions;
	public int[] m_optionVotes;
	int m_hostVote = 0;
	int m_previousVote = -1;

	public float m_timer = 30.0f;

	bool hasStarted = false;
	bool hasTriggered = false;

    float timeBetweenWaves = 20.0f;

	/*GameObject player;
	public void SetAffectedPlayer(GameObject play)
	{
		player = play;
	}*/

	// Use this for initialization
	void Start () 
	{
		m_optionVotes = new int[m_possibleOptions.Length];
		for(int i = 0; i < m_optionVotes.Length; i++)
			m_optionVotes[i] = 0;
	}

	bool m_eventShouldSelfDestruct = false;
	// Update is called once per frame
	void Update () 
	{
		if(Network.isServer && hasStarted)
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
		if(!hasTriggered)
		{
			//If the other is a player, then initiate event
			if(other.tag == "Player")
			{
				//TODO: Change this to all connected players
				GameObject.FindGameObjectWithTag("GUIManager").GetComponent<GUIManager>().SetActiveEvent(this.gameObject, other.gameObject.GetComponent<PlayerControlScript>().GetOwner());
				hasStarted = true;
			}
		}
	}
	
	public void VoteForOption(int optionNum)
	{
		if(Network.isServer)
		{
			//Debug.Log ("Received host vote for option #" + optionNum);
			m_hostVote = optionNum;
			if(m_previousVote != -1)
				m_optionVotes[m_previousVote]--;
			m_previousVote = optionNum;
			m_optionVotes[optionNum]++;

			//Check for >50% here
			float numPlayers = (float)GameObject.FindGameObjectWithTag("GameController").GetComponent<GameStateController>().m_connectedPlayers.Count;
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
	[RPC]
	void PropagateClientVote(int optionNum, int previousVote)
	{
		Debug.Log ("Received client vote for option #" + optionNum);
		if(previousVote != -1)
			m_optionVotes[previousVote]--;

		m_optionVotes[optionNum]++;

		//Check for >50% here
		int numPlayers = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameStateController>().m_connectedPlayers.Count;
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

	[RPC]
	void PropagateVotes(int location, int votes)
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
				GameObject.FindGameObjectWithTag("GUIManager").GetComponent<GUIManager>().HostShouldTieBreak();
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
		foreach(EventOutcomeGroup group in selectedOption.m_optionGroups)
			totalPercentage += group.percentageChance;


		int rand = Random.Range(0, totalPercentage);
		int previous = 0;
		//Debug.Log ("Total Percentage = " + totalPercentage + ", Rand = " + rand + ".");


		//Find the appropiate outcome group to be triggered
		EventOutcomeGroup groupToBeTriggered = null;
		foreach(EventOutcomeGroup group in selectedOption.m_optionGroups)
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
			foreach(EventOutcome outcome in groupToBeTriggered.m_outcomesInThisGroup)
			{
				if(outcome.m_outcomeRequiresSpecificPlayer)
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
			selectedPlayer = null;
			StartCoroutine(ListenForPlayerSelection(outcomePlayerReqdFor));
			delayedOutcomeText = groupToBeTriggered.m_groupOutcomeText;
			GameObject.FindGameObjectWithTag("GUIManager").GetComponent<GUIManager>().RecievePlayerRequiresSelectingForEvent("A player selection is required");
			return "A player selection is required";
		}
		else
		{
			//Return the string so the GUI can draw it
			string text = "YOU SHOULDN'T SEE THIS";
			if(groupToBeTriggered != null)
				text = groupToBeTriggered.m_groupOutcomeText;
			GameObject.FindGameObjectWithTag("GUIManager").GetComponent<GUIManager>().RecieveEventTextFromEventCompletion(text);
			m_eventShouldSelfDestruct = true;
			return text;
		}
	}
	[RPC]
	void PropagateTriggered(bool triggered)
	{
		hasTriggered = triggered;
	}

	void FireEventOutcome(EventOutcome outcome)
	{
		switch(outcome.m_typeOfOutcome)
		{
			case OutcomeType.AffectsCapitalShipMoveTarget:
			{
				//Set CShip target
				GameObject.FindGameObjectWithTag("Capital").GetComponent<CapitalShipScript>().SetTargetPoint(outcome.m_outcomeFocusPoint);
				break;
			}
			case OutcomeType.AffectsCapitalShipObjective:
			{
				//TODO: Add this in after the CShip objective system
				break;
			}
			case OutcomeType.AffectsCapitalShipResource:
			{
				switch(outcome.m_affectedResource)
				{
					case ResourceType.CShipFuel:
					{
						if(outcome.m_outcomeMagnitude < 0)
							GameObject.FindGameObjectWithTag("Capital").GetComponent<CapitalShipScript>().ReduceResourceFuel(-outcome.m_outcomeMagnitude);
						else
							GameObject.FindGameObjectWithTag("Capital").GetComponent<CapitalShipScript>().IncreaseResourceFuel(outcome.m_outcomeMagnitude);
						break;
					}
					case ResourceType.CShipWater:
					{
						if(outcome.m_outcomeMagnitude < 0)
							GameObject.FindGameObjectWithTag("Capital").GetComponent<CapitalShipScript>().ReduceResourceWater(-outcome.m_outcomeMagnitude);
						else
							GameObject.FindGameObjectWithTag("Capital").GetComponent<CapitalShipScript>().IncreaseResourceWater(outcome.m_outcomeMagnitude);
						break;
					}
					case ResourceType.CShipMass:
					{
						if(outcome.m_outcomeMagnitude < 0)
							GameObject.FindGameObjectWithTag("Capital").GetComponent<CapitalShipScript>().ReduceResourceMass(-outcome.m_outcomeMagnitude);
						else
							GameObject.FindGameObjectWithTag("Capital").GetComponent<CapitalShipScript>().IncreaseResourceMass(outcome.m_outcomeMagnitude);
						break;
					}
					default:
					{
						Debug.Log ("Couldn't find appropriate capital resource " + outcome.m_affectedResource.ToString());
						break;
					}
				}
				break;
			}
			case OutcomeType.AffectsPlayerCash:
			{
				//We should be passed a player that will be affected, so we'll store that in a temp for now
				if(outcome.m_outcomeMagnitude < 0)
					selectedPlayer.GetComponent<PlayerControlScript>().RemoveSpaceBucks(-outcome.m_outcomeMagnitude);
				else
					selectedPlayer.GetComponent<PlayerControlScript>().AddSpaceBucks(outcome.m_outcomeMagnitude);
				break;
			}
			case OutcomeType.CreatesNewSpawnPoint:
			{
				GameObject newSpawnPoint = new GameObject();
				newSpawnPoint.transform.position = outcome.m_outcomeFocusPoint.position;
				newSpawnPoint.AddComponent<EnemySpawnPointScript>();
				newSpawnPoint.tag = "SpawnPoint";

				//Generate list of GOs to spawn
                List<WaveInfo> enemyWaves = new List<WaveInfo>();
                foreach (WaveInfo wave in outcome.m_enemiesAssociated)
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
				if(outcome.m_outcomeFocusPoint != null)
				{
					//Immediately spawn the enemies at the focus point
					List<GameObject> enemiesToSpawn = new List<GameObject>();
					foreach(WaveInfo wave in outcome.m_enemiesAssociated)
					{
						foreach(GameObject enemy in wave.GetRawWave())
						{
							enemiesToSpawn.Add (enemy);
						}
					}

					foreach(GameObject enemy in enemiesToSpawn)
					{
						Network.Instantiate(enemy, outcome.m_outcomeFocusPoint.position, outcome.m_outcomeFocusPoint.rotation, 0);
					}
					
					break;
				}
				else
				{
					//Otherwise spawn it where the event object is
					List<GameObject> enemiesToSpawn = new List<GameObject>();
					foreach(WaveInfo wave in outcome.m_enemiesAssociated)
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
				GameObject.FindGameObjectWithTag("Capital").GetComponent<HealthScript>().DamageMobHullDirectly(outcome.m_outcomeMagnitude);
				break;
			}
			case OutcomeType.CausesPlayerDamage:
			{
				//selectedPlayer.GetComponent<HealthScript>().DamageMob(outcome.m_outcomeMagnitude, null);
				selectedPlayer.GetComponent<HealthScript>().DamageMobHullDirectly(outcome.m_outcomeMagnitude);
				break;
			}
		}
	}

	string delayedOutcomeText = "";
	public GameObject selectedPlayer = null;
	IEnumerator ListenForPlayerSelection(EventOutcome outcome)
	{
		while(selectedPlayer == null)
		{
			yield return 0;
		}

		//We've been given a player, now fire the outcome
		FireEventOutcome(outcome);
		GameObject.FindGameObjectWithTag("GUIManager").GetComponent<GUIManager>().RecieveEventTextFromEventCompletion(delayedOutcomeText);
		m_eventShouldSelfDestruct = true;
	}
}
