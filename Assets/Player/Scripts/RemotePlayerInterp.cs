using UnityEngine;
using System.Collections;

public class NState : System.Object 
{
	public Vector3 p;
	public Vector3 v;
	public Quaternion r;
	public float t;

	public NState(Vector3 pos, Vector3 v, Quaternion rot, float t)
	{
		this.p = pos;
		this.v = v;
		this.r = rot;
		this.t = t;
	}
}

public class RemotePlayerInterp : MonoBehaviour 
{
	public bool simulatePhysics = true;
	public bool updatePosition = true;
	public float physInterp = 0.1f;
	public float netInterp = 0.2f;
	public float ping;
	public float jitter;
	public GameObject localPlayer;
	public bool isResponding = false;
	public string netCode = " (No Connections)";

	int m;
	Vector3 p;
	Vector3 v;
	Quaternion r;
	NState[] states;
	int stateCount;

	// Use this for initialization
	void Start () 
	{
		states = new NState[15];
		for(int i = 0; i < 15; i++)
			states[i] = new NState(Vector3.zero, Vector3.zero, Quaternion.identity, 0.0f);

		networkView.observed = this;
	}
	
	// Update is called once per frame
	void FixedUpdate () 
	{
		//if(!updatePosition || !states[10])
		if(!updatePosition || states == null)
			return;

		//simulatePhysics = (localPlayer != null && Vector3.Distance(localPlayer.transform.position, this.transform.position) < 30);
		simulatePhysics = (localPlayer != null && Vector3.Distance(localPlayer.rigidbody.position, rigidbody.position) < 30);
		jitter = Mathf.Lerp (jitter, Mathf.Abs(ping - ((float)Network.time - states[0].t)), Time.deltaTime * 0.3f);
		ping = Mathf.Lerp (ping, (float)Network.time - states[0].t, Time.deltaTime * 0.3f);

		//Interpolation
		float interpolationTime = (float)Network.time - netInterp;
		if(states[0].t > interpolationTime)
		{
			for(int i = 0; i < stateCount; i++)
			{
				if(states[i] != null && (states[i].t <= interpolationTime  || i == stateCount - 1))
				{
					NState rhs = states[Mathf.Max(i-1, 0)];
					NState lhs = states[i];
					float l = rhs.t - lhs.t;
					float t = 0.0f;
					if(l > 0.0001f)
						t = ((interpolationTime - lhs.t) / 1);

					//this.transform.position = Vector3.Lerp(lhs.p, rhs.p, t);
					rigidbody.position = Vector3.Lerp (lhs.p, rhs.p, t);
					//this.GetComponent<PlayerControlScript>().m_currentVelocity = Vector3.Lerp(lhs.p, rhs.p, t);
					//this.transform.rotation = Quaternion.Slerp(lhs.r, rhs.r, t);
					rigidbody.rotation = Quaternion.Slerp(lhs.r, rhs.r, t);
					rigidbody.velocity = ((rhs.p - states[i + 1].p) / (rhs.t - states[i + 1].t));

					isResponding = true;
					netCode = "";
					return;
				}
			}
		}
		//Extrapolation
		else
		{
			float extrapolationLength = (interpolationTime - states[0].t);
			if(extrapolationLength < 1 && states[0] != null && states[1] != null)
			{
				//this.transform.position = states[0].p + (((states[0].p - states[1].p) / (states[0].t - states[1].t)) * extrapolationLength);
				rigidbody.position = states[0].p + (((states[0].p - states[1].p) / (states[0].t - states[1].t)) * extrapolationLength);
				//this.transform.position = states[0].v;
				//this.transform.rotation = states[0].r;
				rigidbody.rotation = states[0].r;

				isResponding = true;
				if(extrapolationLength < 0.5f)
					netCode = ">";
				else
					netCode = " (Delayed)";
			}
			else
			{
				netCode = " (Not Responding)";
				isResponding = false;
			}
		}

		if(states[0].t > states[2].t)
			rigidbody.velocity = ((states[0].p - states[2].p) / (states[0].t - states[2].t));
	}

	void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info)
	{
		//We're the server, and need to keep track of relaying messages between clients
		if(stream.isWriting)
		{
			if(stateCount == 0)
				return;

			p = states[0].p;
			v = states[0].v;
			r = states[0].r;
			m = (int)(((float)Network.time - states[0].t) * 1000);	//m = milliseconds between player sending and server propagating
			stream.Serialize(ref p);
			stream.Serialize(ref v);
			stream.Serialize(ref r);
			stream.Serialize(ref m);
		}
		//New packet! Add it to state array to be inspected
		else
		{
			stream.Serialize(ref p);
			stream.Serialize(ref v);
			stream.Serialize(ref r);
			stream.Serialize(ref m);
			float fM = (float)m;
			NState newState = new NState(p, v, r, (float)info.timestamp - (fM > 0 ? (fM / 1000) : 0));
			if(stateCount == 0)
				states[0] = newState;
			else if(newState.t > states[0].t)
			{
				for(int k = states.Length - 1; k > 0; k--)
				{
					states[k] = states[k-1];
				}
				states[0] = newState;
			}

			stateCount = Mathf.Min (stateCount + 1, states.Length);
		}
	}
}
