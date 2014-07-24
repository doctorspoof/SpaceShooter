using UnityEngine;
using System.Collections.Generic;

public enum GroupBehaviour
{
    Attack = 0,
    StayWithSlowest = 1,
    Roam = 2,
    Explore = 3,
    Regroup = 4
}

public enum Formation
{
    Charge = 0,
    Arrow = 1,
    Box = 2,
    Circle = 3,
    Line = 4
}

public class EnemyGroup : MonoBehaviour
{

    bool waitForUnitsUntilCheckingToDestroy = true;

    List<AIOrder<EnemyGroup>> orderQueue = new List<AIOrder<EnemyGroup>>();
    List<AIOrder<EnemyGroup>> defaultOrders = new List<AIOrder<EnemyGroup>>();

    //An array of lists with indexes based on ShipSize eg. m_children[ShipSize.Medium]
    List<EnemyScript>[] m_children;
    public List<EnemyScript>[] Children
    {
        get { return m_children; }
    }

    [SerializeField]
    int m_shipCount = 0;

    [SerializeField]
    int m_bountyTotal = 0;

    [SerializeField]
    GroupBehaviour m_behaviour;
    public GroupBehaviour GroupCurrentBehaviour
    {
        get { return m_behaviour; }
    }

    [SerializeField]
    Formation m_formation;
    public Formation GroupCurrentFormation
    {
        get { return m_formation; }
    }

    //bool groupReforming = true;
    //public bool GroupReforming
    //{
    //    get { return groupReforming; }
    //}

    [SerializeField]
    float rangeToMoveIntoBeforeAttackCommand;

    void Awake()
    {
        m_children = new List<EnemyScript>[4];
        for (int i = 0; i < m_children.Length; ++i)
        {
            m_children[i] = new List<EnemyScript>();
        }

        m_behaviour = GroupBehaviour.StayWithSlowest;
        m_formation = (Formation)Random.Range(1, 5);

    }

    // Use this for initialization
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (m_shipCount <= 0 && !waitForUnitsUntilCheckingToDestroy)
        {
            Destroy(this.gameObject);
            return;
        }

        //if (m_bountyTotal > 300)
        //{
        //    int splitTimes = Mathf.CeilToInt(m_bountyTotal / 300.0f);
        //    SplitGroup(splitTimes);
        //    return;
        //}

        Debug.DrawRay(transform.position, transform.up, Color.red);

        //EnemyScript script;
        //groupReforming = !InFormation(out script);

        CheckAndAttackPlayersInRange(50);

        SetShipSpeeds();

        //if (orderQueue.Count > 0)
        //{
        //    Debug.DrawLine(transform.position, orderQueue[0].PositionOfInterest);

        //    if(orderQueue[0].ObjectOfInterest != null)
        //    {
        //        Debug.Log("objectOfInterest = " + orderQueue[0].ObjectOfInterest.name);
        //    }
        //}

        if (orderQueue.Count == 0 && defaultOrders.Count > 0 && !GroupStillHasMembersWithOrder())
        {
            orderQueue.Add(defaultOrders[Random.Range(0, defaultOrders.Count)]);
        }

        if (!GroupStillHasMembersWithOrder() || (orderQueue.Count > 0 && orderQueue[0].Completed()))
        {
            NextOrder();
        }
    }

    private void NextOrder()
    {
        if (orderQueue[0].Completed())
        {
            orderQueue.RemoveAt(0);
        }

        if (orderQueue.Count > 0)
        {
            orderQueue[0].Activate();
        }
    }

    void LateUpdate()
    {
        transform.position = GetGroupAverageCentre();
        //UpdateLastPositions();

        if (orderQueue.Count > 0)
        {
            RotateTowards(orderQueue[0].PositionOfInterest);
        }
    }

    public void AddOrder(AIOrder<EnemyGroup> order)
    {
        orderQueue.Add(order);
    }

    public void AddDefaultOrder(AIOrder<EnemyGroup> order)
    {
        defaultOrders.Add(order);
    }

    /// <summary>
    /// Orders a move to position
    /// </summary>
    /// <param name="position">Position to move to</param>
    public void OrderMove(Vector2 position)
    {
        float radiusOfFormation = GetRadius();

        Vector2 fromPosition = transform.position;
        GameObject collidedObject;
        bool pathFound = CheckCanMoveTo(fromPosition, position, out collidedObject);

        List<Vector2> moveOrderPositions = new List<Vector2>();

        int count = 0;

        while (!pathFound)
        {
            fromPosition = GetPositionForAvoidance(collidedObject, position, fromPosition, 10.0f, radiusOfFormation);

            moveOrderPositions.Add(fromPosition);
            pathFound = CheckCanMoveTo(fromPosition, position, out collidedObject);

            count++;
            if (count > 20)
                break;
        }

        moveOrderPositions.Add(position);

        // uncomment to show movement paths
            //Debug.DrawLine(transform.position, moveOrderPositions[0], Color.red, 999);

            //for (int i = 0; i < moveOrderPositions.Count - 1; ++i)
            //{
            //    Debug.DrawLine(moveOrderPositions[i], moveOrderPositions[i + 1], Color.red, 999);
            //}

        foreach (Vector2 orderPositions in moveOrderPositions)
        {
            AIOrder<EnemyGroup> order = new AIOrder<EnemyGroup> { Orderee = this, PositionOfInterest = orderPositions };
            order.AttachAction(delegate(EnemyGroup group, GameObject objectOfInterest, Vector3 pointOfInterest)
                               {

                                   group.RotateTowards(pointOfInterest);

                                   group.SetBehaviour(GroupBehaviour.StayWithSlowest);
                                   foreach (List<EnemyScript> tier in group.Children)
                                   {
                                       foreach (EnemyScript ship in tier)
                                       {
                                           //int indexOfShip = group.Children[(int)ship.ShipSize].IndexOf(ship);

                                           ship.SetMoveTarget((Vector2)pointOfInterest + ship.GetLocalFormationPosition());

                                       }
                                   }
                               });

            order.AttachCondition(delegate(EnemyGroup group, GameObject objectOfInterest, Vector3 pointOfInterest)
                                  {
                                      return Vector2.Distance((Vector2)group.transform.position, (Vector2)pointOfInterest) < 0.8f;
                                  });

            AddOrder(order);
        }
    }

    public bool CheckCanMoveTo(Vector2 from, Vector2 target, out GameObject collidedObject)
    {
        collidedObject = null;

        float distanceToTarget = Vector2.Distance(from, target) + 10;
        RaycastHit hit;

        bool collidedWithSomething = Physics.Raycast(new Ray(from, (target - from).normalized), out hit, distanceToTarget);

        if (collidedWithSomething)
            collidedObject = hit.collider.gameObject;

        return !collidedWithSomething;
    }

    public Vector2 GetPositionForAvoidance(GameObject objectToAvoid, Vector2 targetLocation, Vector2 currentLocation, float closestDistanceFromGroupToObject, float radiusOfFormation)
    {

        //Vector2 directionFromObjectToThis = currentLocation - (Vector2)objectToAvoid.transform.position;
        float radiusOfObject = Mathf.Sqrt(Mathf.Pow(objectToAvoid.transform.localScale.x, 2) + Mathf.Pow(objectToAvoid.transform.localScale.y, 2)) * objectToAvoid.GetComponent<SphereCollider>().radius;
        float radius = radiusOfObject + closestDistanceFromGroupToObject + radiusOfFormation;

        Vector2[] returnee = new Vector2[2];
        returnee[0] = new Vector2(radius, 0);
        returnee[1] = new Vector2(-radius, 0);

        Vector2 dir = (targetLocation - currentLocation).normalized;
        Quaternion rotation = Quaternion.Euler(new Vector3(0, 0, (Mathf.Atan2(dir.y, dir.x) - Mathf.PI / 2) * Mathf.Rad2Deg));

        returnee[0] = (rotation * returnee[0]) + objectToAvoid.transform.position;
        returnee[1] = (rotation * returnee[1]) + objectToAvoid.transform.position;

        if (Vector2.SqrMagnitude(currentLocation - returnee[0]) < Vector2.SqrMagnitude(currentLocation - returnee[1]))
        {
            return returnee[0];
        }
        return returnee[1];

    }

    /// <summary>
    /// Orders an attack on the specified object
    /// </summary>
    /// <param name="attackableObject">Object to be attacked</param>
    public void OrderAttack(GameObject attackableObject)
    {
        if (Vector2.Distance(this.transform.position, attackableObject.rigidbody.transform.position) > GetMinimumRangeOfGroup() * 2)
        {
            Vector2 closerPosition = Vector2.MoveTowards(attackableObject.rigidbody.transform.position, this.transform.position, GetMinimumRangeOfGroup() * 2);
            OrderMove(closerPosition);
        }

        AIOrder<EnemyGroup> order = new AIOrder<EnemyGroup> { Orderee = this, ObjectOfInterest = attackableObject };
        order.AttachAction(delegate(EnemyGroup group, GameObject objectOfInterest, Vector3 pointOfInterest)
                           {
                               group.RotateTowards(pointOfInterest);

                               group.SetBehaviour(GroupBehaviour.Attack);
                               foreach (List<EnemyScript> tier in group.Children)
                               {
                                   foreach (EnemyScript ship in tier)
                                   {

                                       ship.SetTarget(attackableObject);
                                       ship.ResetShipSpeed();
                                   }
                               }

                           });

        order.AttachCondition(delegate(EnemyGroup group, GameObject objectOfInterest, Vector3 pointOfInterest)
                              {
                                  return (objectOfInterest == null) || (Vector2.Distance(objectOfInterest.transform.position, group.transform.position) > 50);
                              });

        AddOrder(order);
    }

    /// <summary>
    /// Cancels all orders given to the group
    /// </summary>
    public void CancelAllOrders()
    {
        CancelCurrentOrder();
        orderQueue.Clear();
    }

    /// <summary>
    /// Cancels the current order of the group
    /// </summary>
    public void CancelCurrentOrder()
    {
        if (orderQueue.Count > 0)
        {
            orderQueue.RemoveAt(0);
            foreach (List<EnemyScript> tier in m_children)
            {
                foreach (EnemyScript ship in tier)
                {
                    CancelShipCurrentOrder(ship);
                }
            }
        }
    }

    /// <summary>
    /// Cancels the order of a specific ship
    /// </summary>
    /// <param name="ship"></param>
    public void CancelShipCurrentOrder(EnemyScript ship)
    {
        ship.CancelOrder();
    }

    /// <summary>
    /// Checks to see if any members are still completing the order
    /// </summary>
    /// <returns>Returns true if members are still completing orders</returns>
    public bool GroupStillHasMembersWithOrder()
    {
        foreach (List<EnemyScript> tier in m_children)
        {
            foreach (EnemyScript ship in tier)
            {
                if (ship.CurrentOrder != Order.Idle)
                    return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Adds an enemy to the groups entity list
    /// </summary>
    /// <param name="enemyShip">Enemy ship to be added</param>
    /// <returns>Returns true if successful (if object is not null)</returns>
    public bool AddEnemyToGroup(EnemyScript enemyShip)
    {
        if (null == enemyShip)
        {
            return false;
        }

        waitForUnitsUntilCheckingToDestroy = false;

        m_children[(int)enemyShip.ShipSize].Add(enemyShip);
        enemyShip.FormationPosition = new Vector2();
        enemyShip.SetParentGroup(this);
        //enemyShip.transform.parent = transform;
        m_shipCount++;
        m_bountyTotal += enemyShip.BountyAmount;

        if (slowestShip == null || enemyShip.GetMaxShipSpeed() < slowestShip.GetMaxShipSpeed())
        {
            slowestShip = enemyShip;
        }

        if (orderQueue.Count > 0)
            orderQueue[0].Activate();

        transform.position = GetGroupAverageCentre();
        ResetAllFormationsPositions();

        return true;
    }

    /// <summary>
    /// Removes the enemy ship from the group
    /// </summary>
    /// <param name="enemyShip">Ship to be removed</param>
    /// <returns>Returns true if successfully removed</returns>
    public bool RemoveEnemyFromGroup(EnemyScript enemyShip)
    {
        if (null == enemyShip)
        {
            return false;
        }

        bool succeeded = m_children[(int)enemyShip.ShipSize].Remove(enemyShip);

        enemyShip.transform.parent = null;
        m_shipCount--;
        m_bountyTotal -= enemyShip.BountyAmount;

        if (enemyShip == slowestShip)
        {
            slowestShip = CalculateSlowestShip();
        }

        if (orderQueue.Count > 0)
            orderQueue[0].Activate();

        if (m_shipCount > 0)
        {
            transform.position = GetGroupAverageCentre();
            ResetAllFormationsPositions();
        }

        return succeeded;
    }

    /// <summary>
    /// Splits the groups *evenly* and returns how many you asked for.
    /// May not return fully evenly if the amount of ships cannot be spread evenly. The last group will always have the lowest amount of ships.
    /// 
    /// TODO change it so it alternates back and forth so that the last group does not have the least amount of ships for each shipsize.
    /// </summary>
    /// <param name="groupCount">Amount of groups to be created</param>
    /// <returns>All groups asked for.</returns>
    public EnemyGroup[] SplitGroup(int groupCount)
    {
        EnemyGroup[] groups = new EnemyGroup[groupCount];

        for (int i = 0; i < groupCount; ++i)
        {
            groups[i] = new GameObject("EnemyGroup").AddComponent<EnemyGroup>();
        }

        // loop through each ships type list and add to groups
        for (int i = 0; i < m_children.Length; ++i)
        {
            int shipsCount = m_children[i].Count;

            //loop and add ships from this type to group
            for (int a = 0; a < groupCount; ++a)
            {
                //make sure we dont try to assign too many ships to the groups eg. if there are 8 ships being split between 3 groups, the last group should only receive 2 ships.
                int shipsPerGroup = Mathf.Min(Mathf.CeilToInt(shipsCount / (float)groupCount), shipsCount - a * Mathf.CeilToInt(shipsCount / (float)groupCount));
                for (int b = 0; b < shipsPerGroup; ++b)
                {
                    groups[a].AddEnemyToGroup(m_children[i][0]);
                    m_children[i].RemoveAt(0);
                }
            }
        }

        Destroy(this.gameObject);

        return groups;
    }

    /// <summary>
    /// Use to merge groups with the group its being called on.
    /// </summary>
    /// <param name="groups">The groups to be merged into this one</param>
    /// <returns>Returns false if groups is empty</returns>
    public bool MergeWith(EnemyGroup[] groups)
    {
        if (0 == groups.Length)
            return false;

        //foreach group to be merged with this one add all ships to their respective groups
        foreach (EnemyGroup group in groups)
        {

            foreach (List<EnemyScript> listTier in group.Children)
            {
                foreach (EnemyScript script in listTier)
                {
                    m_children[(int)script.ShipSize].Add(script);
                }
            }

        }

        return true;

    }

    public void SetBehaviour(GroupBehaviour behaviour)
    {
        m_behaviour = behaviour;
    }

    public void SetFormation(Formation formation)
    {
        m_formation = formation;
        ResetAllFormationsPositions();
    }

    EnemyScript slowestShip = null;

    /// <summary>
    /// Returns the slowest ship in the group
    /// </summary>
    /// <returns>Speed of the slowest ship</returns>
    public float GetSlowestShipSpeed()
    {
        return slowestShip.GetMaxShipSpeed();

        //float slowestSpeed = -1;
        //foreach (List<EnemyScript> shipTier in m_children)
        //{
        //    foreach (EnemyScript enemy in shipTier)
        //    {
        //        if (-1 == slowestSpeed || enemy.ShipSpeed < slowestSpeed)
        //        {
        //            slowestSpeed = enemy.ShipSpeed;
        //        }
        //    }
        //}
        //return slowestSpeed;
    }

    /// <summary>
    /// Returns the slowest ship of the group
    /// </summary>
    /// <returns></returns>
    public EnemyScript GetSlowestShip()
    {
        return slowestShip;
        //float slowestSpeed = -1;
        //EnemyScript ship = null;
        //foreach (List<EnemyScript> shipTier in m_children)
        //{
        //    foreach (EnemyScript enemy in shipTier)
        //    {
        //        if (-1 == slowestSpeed || enemy.ShipSpeed < slowestSpeed)
        //        {
        //            slowestSpeed = enemy.ShipSpeed;
        //            ship = enemy;
        //        }
        //    }
        //}
        //return ship;
    }

    public EnemyScript CalculateSlowestShip()
    {
        float slowestSpeed = -1;
        EnemyScript ship = null;
        foreach (List<EnemyScript> shipTier in m_children)
        {
            foreach (EnemyScript enemy in shipTier)
            {
                if (-1 == slowestSpeed || enemy.GetMaxShipSpeed() < slowestSpeed)
                {
                    slowestSpeed = enemy.GetMaxShipSpeed();
                    ship = enemy;
                }
            }
        }
        return ship;
    }

    //public void UpdateGroupCentre()
    //{
    //    Vector2 returnee = new Vector2(0, 0);
    //    foreach (List<EnemyScript> shipTier in m_children)
    //    {
    //        foreach (EnemyScript enemy in shipTier)
    //        {
    //            returnee += (Vector2)(enemy.rigidbody.transform.position - enemy.LastFramePosition);
    //        }
    //    }
    //    transform.position = (Vector2)transform.position + (returnee / m_shipCount);
    //}

    /// <summary>
    /// used when you want to add an array of ships translation to the group centre
    /// </summary>
    /// <param name="ship"></param>
    //public void AddToGroupCentre(EnemyScript[] ships)
    //{
    //    Vector2 returnee = new Vector2(0, 0);
    //    foreach (EnemyScript ship in ships)
    //    {
    //        returnee += (Vector2)(ship.rigidbody.transform.position - ship.LastFramePosition);
    //    }
    //    transform.position = (Vector2)transform.position + (returnee / ships.Length);
    //}

    /// <summary>
    /// This needed to be seperate from UpdateGroupCentre as it caused problems when update group centre stopped being called
    /// and then called at a later frame when the ships have reformed. It would cause the centre to jump.
    /// </summary>
    //private void UpdateLastPositions()
    //{
    //    foreach (List<EnemyScript> shipTier in m_children)
    //    {
    //        foreach (EnemyScript enemy in shipTier)
    //        {
    //            enemy.LastFramePosition = enemy.rigidbody.transform.position;
    //        }
    //    }
    //}

    public Vector2 GetGroupAverageCentre()
    {
        Vector2 returnee = new Vector2();

        if (m_shipCount == 0)
            return returnee;

        foreach (List<EnemyScript> shipTier in m_children)
        {
            foreach (EnemyScript enemy in shipTier)
            {
                returnee += (Vector2)enemy.transform.position;
            }
        }
        return returnee / m_shipCount;
    }


    /// <summary>
    /// Returns the smallest range of every member of the group
    /// </summary>
    /// <returns></returns>
    public float GetMinimumRangeOfGroup()
    {
        float minRange = 0;
        foreach (List<EnemyScript> shipTier in m_children)
        {
            foreach (EnemyScript enemy in shipTier)
            {
                if (0 == minRange || enemy.GetMinimumWeaponRange() < minRange)
                {
                    minRange = enemy.GetMinimumWeaponRange();
                }
            }
        }

        return minRange;
    }

    public float GetMaximumRangeOfGroup()
    {
        float maxRange = 0;
        foreach (List<EnemyScript> shipTier in m_children)
        {
            foreach (EnemyScript enemy in shipTier)
            {
                if (0 == maxRange || enemy.GetMinimumWeaponRange() > maxRange)
                {
                    maxRange = enemy.GetMaximumWeaponRange();
                }
            }
        }
        return maxRange;
    }

    /// <summary>
    /// Gets all the ships that are currently in the formation
    /// </summary>
    /// <returns></returns>
    public void SortShipsWithRegardsToFormation(out List<EnemyScript> inFormation, out List<EnemyScript> outOfFormation)
    {
        inFormation = new List<EnemyScript>();
        outOfFormation = new List<EnemyScript>();
        foreach (List<EnemyScript> shipTier in m_children)
        {
            foreach (EnemyScript ship in shipTier)
            {
                if (ship.InFormation(0.8f))
                {
                    inFormation.Add(ship);
                }
                else
                {
                    outOfFormation.Add(ship);
                }
            }
        }
    }

    /// <summary>
    /// TEMPORARY TEST METHOD FOR DETERMINING WHETHER GENERATING THE WHOLE FORMATION IS BETTER THAN 1 BY 1
    /// </summary>
    void ResetFormationPositions()
    {
        switch (m_formation)
        {
            case Formation.Circle:
                {
                    GenerateCircleFormation();
                    break;
                }
            case Formation.Arrow:
                {
                    GenerateArrowheadFormation();
                    break;
                }
            case Formation.Box:
                {
                    GenerateBoxFormation();
                    break;
                }
            case Formation.Line:
                {
                    GenerateLineFormation();
                    break;
                }
        }

    }

    /// <summary>
    /// Call this method when you want the formation to be recalculated
    /// </summary>
    public void ResetAllFormationsPositions()
    {
        ResetFormationPositions();
        //foreach (List<EnemyScript> shipTier in m_children)
        //{
        //    foreach (EnemyScript enemy in shipTier)
        //    {
        //        SetShipFormationPosition(enemy);
        //    }
        //}
    }

    public bool HasGroupArrivedAtLocation()
    {
        if (Vector3.Distance(transform.position, orderQueue[0].PositionOfInterest) < 1.0f)
        {
            return true;
        }
        return false;
    }

    void GenerateCircleFormation()
    {
        //EnemyScript largestShip = GetLargestShipInGroup();
        EnemyScript largestShipInLayer = null, largestShipInLastLayer = null;
        float radius = 0;

        for (int i = 0; i < m_children.Length; ++i)
        {
            if (m_children[i].Count > 0)
            {
                int pointsCount = m_children[i].Count;
                largestShipInLayer = GetLargestShip(m_children[i]);

                //special case for the first tier with only one ship to be placed in the middle
                bool skip = false;
                if (radius == 0 && pointsCount == 1)
                {
                    skip = true;
                    m_children[i][0].FormationPosition = new Vector2(0, 0);
                }

                if (largestShipInLastLayer == null)
                {
                    float circumferance = ((largestShipInLayer.GetMaxSize()) + 1f) * pointsCount;
                    radius += circumferance / (2 * Mathf.PI);
                }
                else
                {
                    radius += ((largestShipInLastLayer.GetMaxSize() + largestShipInLayer.GetMaxSize()) / 2.0f) + 1.0f;
                }

                //skip assigning ships to circle if only only one ship in first tier (i hate special cases like this but i cant think of a way round it)
                if (!skip)
                {
                    for (int j = 0; j < pointsCount; ++j)
                    {

                        Quaternion finalRotation = Quaternion.AngleAxis(j * (360.0f / pointsCount), Vector3.forward);
                        m_children[i][j].FormationPosition = (Vector2)(finalRotation * new Vector3(radius, 0, 0));

                    }
                }


                largestShipInLayer = largestShipInLastLayer;
            }

        }

    }

    void GenerateBoxFormation()
    {

        //  int middleLayer = Mathf.CeilToInt(m_children.Length / 2.0f);

        float width = 0, height = 0;

        {
            EnemyScript largestShipInLayer = null, largestShipInLastLayer = null;

            for (int i = 0; i < m_children.Length; ++i)
            {
                // only the longest ship in each layer needs adding
                if (m_children[i].Count > 0)
                {
                    largestShipInLayer = GetLongestShip(m_children[i]);

                    if (largestShipInLastLayer != null)
                    {
                        //Debug.Log("added height");
                        height += ((largestShipInLayer.GetShipHeight() + largestShipInLastLayer.GetShipHeight()) / 2.0f) + 1;
                    }


                    //each ship in a layer needs to be checked individually due to potentially having different sizes in same shipSizeClass
                    float layerWidth = 0;
                    foreach (EnemyScript ship in m_children[i])
                    {
                        layerWidth += ship.GetShipWidth() + 1;
                    }
                    if (layerWidth > width)
                    {
                        width = layerWidth;
                    }

                    largestShipInLastLayer = largestShipInLayer;
                }
            }
            ////take 1 away since there should be no spacing after the last layer
            width -= 1;

            if (largestShipInLastLayer && largestShipInLastLayer != largestShipInLayer)
                height -= 1;
        }



        {
            EnemyScript largestShipInLayer = null, largestShipInLastLayer = null;

            float spacingBetweenLayers = 0;

            for (int i = 0; i < m_children.Length; ++i)
            {

                if (m_children[i].Count > 0)
                {
                    int pointsCount = m_children[i].Count;
                    largestShipInLayer = GetLargestShip(m_children[i]);

                    if (largestShipInLastLayer)
                    {
                        spacingBetweenLayers += ((largestShipInLayer.GetShipHeight() + largestShipInLastLayer.GetShipHeight()) / 2.0f) + 1.0f;
                    }

                    //float layerY = spacingBetweenLayers;// -(height / 2) + (spacingBetweenLayers / 2);

                    if (pointsCount == 1)
                    {
                        m_children[i][0].FormationPosition = new Vector2(0, spacingBetweenLayers - (height / 2.0f) + (spacingBetweenLayers / 2));
                    }
                    else
                    {

                        float spacingBetweenShipsInLayer = width / m_children[i].Count;

                        for (int j = 0; j < m_children[i].Count; ++j)
                        {
                            //float layerX = spacingBetweenShipsInLayer;

                            m_children[i][j].FormationPosition = new Vector2((spacingBetweenShipsInLayer * j) - (width / 2.0f) + (spacingBetweenShipsInLayer / 2), spacingBetweenLayers - (height / 2.0f) + (spacingBetweenLayers / 2));
                        }
                    }



                    largestShipInLastLayer = largestShipInLayer;
                }
            }

        }

        Vector2 centre = new Vector2(0, 0);
        // offset by centre
        for (int i = 0; i < m_children.Length; ++i)
        {
            for (int j = 0; j < m_children[i].Count; ++j)
            {
                centre += m_children[i][j].FormationPosition;
            }
        }

        centre /= m_shipCount;

        for (int i = 0; i < m_children.Length; ++i)
        {
            for (int j = 0; j < m_children[i].Count; ++j)
            {
                m_children[i][j].FormationPosition -= centre;
            }
        }
    }

    void GenerateLineFormation()
    {

        List<EnemyScript> firstHalf = new List<EnemyScript>(), secondHalf = new List<EnemyScript>();
        float widthOfLine = 0;

        // go from smaller ships to largest as the smaller ships are the ones that start on the ends with larger ships in middle
        for (int i = m_children.Length - 1; i > -1; --i)
        {
            int shipTierCount = m_children[i].Count;
            int splitCount = Mathf.FloorToInt(shipTierCount / 2.0f);
            for (int j = 0; j < m_children[i].Count; ++j)
            {
                widthOfLine += m_children[i][j].GetShipWidth() + 1.0f;

                //since we are going from smallest -> largest, we split the line into two seperate lists which we will merga later
                //we add the ships to the end of the front list, or the front of the end list
                if (j < splitCount)
                {
                    //add ship at end of first half
                    firstHalf.Add(m_children[i][j]);
                }
                else
                {
                    // add ship at front of second half
                    secondHalf.Insert(0, m_children[i][j]);
                }
            }
        }

        widthOfLine -= 1.0f; //remove 1 since we dont want an extra spacing on the end of the line

        // add the two lists together
        firstHalf.AddRange(secondHalf);

        float currentX = 0;

        for (int i = 0; i < firstHalf.Count; ++i)
        {
            EnemyScript ship = firstHalf[i];

            if (i > 0)
            {
                currentX += ((firstHalf[i].GetShipWidth() + firstHalf[i - 1].GetShipWidth()) / 2.0f) + 0.5f;
            }

            ship.FormationPosition = new Vector2(currentX - (widthOfLine / 2.0f), 0);

        }

        Vector2 centre = new Vector2(0, 0);
        // offset by centre
        for (int i = 0; i < m_children.Length; ++i)
        {
            for (int j = 0; j < m_children[i].Count; ++j)
            {
                centre += m_children[i][j].FormationPosition;
            }
        }

        centre /= m_shipCount;

        for (int i = 0; i < m_children.Length; ++i)
        {
            for (int j = 0; j < m_children[i].Count; ++j)
            {
                m_children[i][j].FormationPosition -= centre;
            }
        }

    }

    void GenerateArrowheadFormation()
    {

        //generate the box formation for this one to base it off. We can just take each row from the box formation and rotate the individuals of the row
        //by having the centre point at the centre of the row
        GenerateBoxFormation();

        Vector2[] centreOfRows = new Vector2[m_children.Length];

        for (int i = 0; i < m_children.Length; ++i)
        {
            if (m_children[i].Count > 0)
            {

                foreach (EnemyScript ship in m_children[i])
                {
                    centreOfRows[i] += ship.FormationPosition;
                }
                centreOfRows[i] /= m_children[i].Count;

            }
        }

        for (int i = 0; i < m_children.Length; ++i)
        {
            for (int j = 0; j < m_children[i].Count; ++j)
            {
                //EnemyScript ship = m_children[i][j];

                float arrowheadAngle = 90.0f;

                int pointOffsetFromCentre = (j - (m_children[i].Count / 2));

                if (pointOffsetFromCentre < 0)
                    arrowheadAngle *= -1;

                Vector2 originalBoxPosition = m_children[i][j].FormationPosition;
                Vector2 localisedPoint = originalBoxPosition - centreOfRows[i];

                m_children[i][j].FormationPosition = (Vector2)(Quaternion.AngleAxis(-arrowheadAngle / 2.0f, Vector3.forward) * localisedPoint);

                Vector2 tempPoint = m_children[i][j].FormationPosition;
                tempPoint.y += originalBoxPosition.y;
                m_children[i][j].FormationPosition = tempPoint;
            }
        }

        Vector2 centre = new Vector2(0, 0);
        // offset by centre
        for (int i = 0; i < m_children.Length; ++i)
        {
            for (int j = 0; j < m_children[i].Count; ++j)
            {
                centre += m_children[i][j].FormationPosition;
            }
        }

        centre /= m_shipCount;

        for (int i = 0; i < m_children.Length; ++i)
        {
            for (int j = 0; j < m_children[i].Count; ++j)
            {
                m_children[i][j].FormationPosition -= centre;
            }
        }

    }

    public void RotateTowards(Vector2 target)
    {
        Vector2 dir = (target - (Vector2)this.transform.position).normalized;
        transform.rotation = Quaternion.Euler(new Vector3(0, 0, (Mathf.Atan2(dir.y, dir.x) - Mathf.PI / 2) * Mathf.Rad2Deg));
    }

    public bool InFormation()
    {
        foreach (List<EnemyScript> shipTier in m_children)
        {
            foreach (EnemyScript enemy in shipTier)
            {
                if (!enemy.InFormation(0.8f))
                {
                    return false;
                }
            }
        }
        return true;
    }

    public EnemyScript GetShipFurthestOutOfFormation()
    {
        EnemyScript shipFurthestOutOfFormation = null;
        float shipFurthestFromFormationDistance = 0;
        foreach (List<EnemyScript> shipTier in m_children)
        {
            foreach (EnemyScript enemy in shipTier)
            {
                if (shipFurthestOutOfFormation == null || shipFurthestFromFormationDistance < enemy.GetDistanceFromFormation())
                {
                    shipFurthestFromFormationDistance = enemy.GetDistanceFromFormation();
                    shipFurthestOutOfFormation = enemy;
                }
            }
        }
        return shipFurthestOutOfFormation;
    }

    public void CheckAndAttackPlayersInRange(float range)
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        GameObject closestPlayer = null;
        float closestDistance = 0;
        foreach (GameObject obj in players)
        {
            float distance = Vector2.Distance(obj.transform.position, transform.position);
            if (distance < range && (closestDistance == 0 || distance < closestDistance))
            {
                closestPlayer = obj;
                closestDistance = distance;
            }
        }

        if (closestPlayer != null)
        {
            CancelAllOrders();
            OrderAttack(closestPlayer);
        }
    }

    public EnemyScript GetLargestShip(List<EnemyScript> list)
    {
        if (list.Count == 0)
            return null;

        EnemyScript returnee = null;
        float size = 0;

        foreach (EnemyScript ship in list)
        {
            if (returnee == null || ship.GetMaxSize() > size)
            {
                returnee = ship;
                size = ship.GetMaxSize();
            }
        }

        return returnee;
    }

    public EnemyScript GetLargestShipInGroup()
    {
        if (m_shipCount == 0)
        {
            return null;
        }

        EnemyScript largestShip = null;
        float size = 0;

        foreach (List<EnemyScript> list in m_children)
        {
            EnemyScript largestShipInList = GetLargestShip(list);
            if (largestShip == null || largestShipInList.GetMaxSize() > size)
            {
                largestShip = largestShipInList;
                size = largestShipInList.GetMaxSize();
            }
        }

        return largestShip;
    }

    public EnemyScript GetWidestShip(List<EnemyScript> list)
    {
        if (list.Count == 0)
            return null;

        EnemyScript returnee = null;
        float size = 0;

        foreach (EnemyScript ship in list)
        {
            if (returnee == null || ship.GetShipWidth() > size)
            {
                returnee = ship;
                size = ship.GetShipWidth();
            }
        }

        return returnee;
    }

    public EnemyScript GetLongestShip(List<EnemyScript> list)
    {
        if (list.Count == 0)
            return null;

        EnemyScript returnee = null;
        float size = 0;

        foreach (EnemyScript ship in list)
        {
            if (returnee == null || ship.GetShipHeight() > size)
            {
                returnee = ship;
                size = ship.GetShipHeight();
            }
        }

        return returnee;
    }

    public float GetRadius()
    {
        float radius = 0;

        foreach (List<EnemyScript> list in m_children)
        {
            foreach (EnemyScript ship in list)
            {
                if (Vector2.Distance(transform.position, ship.FormationPosition) > radius)
                {
                    radius = Vector2.Distance(new Vector2(0, 0), ship.FormationPosition);
                }
            }
        }

        return radius;
    }

    void SetShipSpeeds()
    {

        if (m_shipCount == 0)
            return;

        // sort the ships into who is inFormation and outOfFormation
        List<EnemyScript> inFormation, outOfFormation;
        SortShipsWithRegardsToFormation(out inFormation, out outOfFormation);

        // grab the slowest ship so we can find the maximum speed of the group when inFormation
        EnemyScript slowestShip = GetSlowestShip();
        float maxSpeedOfShipsInFormation = slowestShip.GetMaxShipSpeed();

        if (outOfFormation.Count > 0)
        {

            //EnemyScript shipFurthestOutOfFormation = GetShipFurthestOutOfFormation();
            List<EnemyScript> shipsAhead = new List<EnemyScript>();
            foreach (EnemyScript ship in outOfFormation)
            {

                float dotAheadOfGroup = Vector2.Dot(Vector3.Normalize((Vector2)ship.transform.position - ship.GetWorldCoordinatesOfFormationPosition(transform.position)),
                                                    Vector3.Normalize(ship.GetMoveTarget() - ship.GetWorldCoordinatesOfFormationPosition(transform.position))) * -1;

                if (dotAheadOfGroup <= 0 && ship.GetDistanceFromClosestFormation() < 1.0f)
                {
                    shipsAhead.Add(ship);
                    ship.SetCurrentShipSpeed(ship.GetMaxShipSpeed() * 0.1f);
                }
                else
                {
                    ship.ResetShipSpeed();
                }
            }

            if (shipsAhead.Count == 0)
            {
                maxSpeedOfShipsInFormation *= 0.1f;
            }

        }

        foreach (EnemyScript ship in inFormation)
        {
            ship.SetCurrentShipSpeed(maxSpeedOfShipsInFormation);
        }

    }

}
