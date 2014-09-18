using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ShipEnemy : Ship
{

    [SerializeField] int m_bountyAmount = 1;

    [SerializeField] string[] m_allowedAttacksForShip;

    




    
    
    



    IAttack m_currentAttackType = null;
    float m_randomOffsetFromTarget = 0;



    #region getset

    public int GetBountyAmount()
    {
        return m_bountyAmount;
    }

    

    #endregion getset



    protected override void Awake()
    {
        base.Awake();
    }

    // Use this for initialization
    protected override void Start()
    {
        base.Start();
    }

    protected override void Update()
    {
        base.Update();
    }

    public void AlertLowHP(GameObject lastHit)
    {

    }
    public void AlertFirstHit(GameObject shooter)
    {

    }

    public void NotifyEnemyUnderFire(GameObject attacker)
    {
        GetAINode().RequestConsiderationOfOrder(AIHierarchyRelation.Parent, (int)AIShipOrder.Attack, new object[] { attacker });
    }


    

    public void OnPlayerWin()
    {
    }

    public void OnPlayerLoss()
    {
    }

    public void TellEnemyToFreeze()
    {
    }

    public void AlertEnemyUnFreeze()
    {
    }

    

    /// <summary>
    /// Gets the lowest weapon range of all the attached weapons
    /// </summary>
    /// <returns></returns>
    public override float GetMinimumWeaponRange()
    {
        EnemyWeaponScript enemyWeaponScript;
        if ((enemyWeaponScript = GetComponent<EnemyWeaponScript>()) != null)
        {
            m_minWeaponRange = enemyWeaponScript.GetRange();
            return m_minWeaponRange;
        }

        GameObject[] turrets = GetAttachedTurrets();
        foreach (GameObject turret in turrets)
        {
            EnemyTurret turretScript = turret.GetComponent<EnemyTurret>();
            if (turretScript != null && (m_minWeaponRange == 0 || turretScript.GetRange() < m_minWeaponRange))
            {
                m_minWeaponRange = turretScript.GetRange();
            }
        }

        return m_minWeaponRange;
    }

    /// <summary>
    /// Gets the highest weapon range of all the attached weapons
    /// </summary>
    /// <returns></returns>
    public override float GetMaximumWeaponRange()
    {
        EnemyWeaponScript enemyWeaponScript;
        if ((enemyWeaponScript = GetComponent<EnemyWeaponScript>()) != null)
        {
            m_maxWeaponRange = enemyWeaponScript.GetRange();
            return m_maxWeaponRange;
        }

        GameObject[] turrets = GetAttachedTurrets();
        foreach (GameObject turret in turrets)
        {
            EnemyTurret turretScript = turret.GetComponent<EnemyTurret>();
            if (turretScript != null && (m_maxWeaponRange == 0 || turretScript.GetRange() > m_maxWeaponRange))
            {
                m_maxWeaponRange = turretScript.GetRange();
            }
        }

        return m_maxWeaponRange;
    }

    public override bool ReceiveOrder(int orderID_, object[] listOfParameters)
    {
        switch ((AIShipOrder)orderID_)
        {
            case (AIShipOrder.Attack):
                {
                    SetTarget((GameObject)listOfParameters[0]);
                    return true;
                }
            case (AIShipOrder.Move):
                {
                    AddMoveWaypoint((Vector2)listOfParameters[0]);
                    return true;
                }
            case(AIShipOrder.StayInFormation):
                {
                    SetFormationPosition((Vector2)listOfParameters[0]);
                    return true;
                }
            default:
                {
                    return false;
                }
        }
    }

    public override bool GiveOrder(int orderID_, object[] listOfParameters)
    {
        GetAINode().OrderChildren(orderID_, listOfParameters);
        return true;
    }

    public override bool ConsiderOrder(int orderID_, object[] listOfParameters)
    {
        if (base.ConsiderOrder(orderID_, listOfParameters))
        {
            return true;
        }

        switch ((AIShipOrder)orderID_)
        {
            case(AIShipOrder.Attack):
                {
                    // TODO: subordinate is attacked and wants to concentrate fire
                    return false;
                }
            default:
                {
                    return false;
                }
        }
    }

    public override bool RequestOrder(IEntity entity_)
    {
        System.Type entityType = entity_.GetType();

        if (entityType.Equals(typeof(ShipEnemy)))
        {
            List<AINode> children = GetAINode().GetChildren();
            List<Ship> ships = new List<Ship>();
            children.ForEach(
                    x =>
                    {
                        ships.Add((Ship)x.GetEntity());
                    }
                );

            List<Vector2> formationPositions = Formations.GenerateCircleFormation(ships);

            for (int i = 0; i < children.Count; ++i)
            {
                children[i].ReceiveOrder((int)AIShipOrder.StayInFormation, new object[] { formationPositions[i] });
            }

            return true;
        }

        return false;
    }

    public override object[] RequestInformation(int informationID_)
    {
        object[] returnee = null;
        if ((returnee = base.RequestInformation(informationID_)) != null)
        {
            return returnee;
        }

        switch ((AIShipRequestInfo)informationID_)
        {
            default:
                {
                    return returnee;
                }
        }
    }

    public override bool Notify(int informationID_, object[] listOfParameters)
    {
        if (base.Notify(informationID_, listOfParameters))
        {
            return true;
        }

        switch ((AIShipNotifyInfo)informationID_)
        {
            default:
                {
                    return false;
                }
        }
    }

}
