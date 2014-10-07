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
        PickAugmentsForGroup();
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
        GetAINode().RequestConsiderationOfOrder(AIHierarchyRelation.Parent, (int)AIShipOrder.Attack, new object[] { shooter });
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
    
    void OnTriggerEnter(Collider other)
    {
        if(m_currentOrder == AIShipOrder.Move && other.gameObject.layer == Layers.player)
        {
            GetAINode().RequestConsiderationOfOrder(AIHierarchyRelation.Parent, (int)AIShipOrder.Attack, new object[] { other.attachedRigidbody.gameObject });
        }
    }

    /// <summary>
    /// Gets the lowest weapon range of all the attached weapons
    /// </summary>
    /// <returns></returns>
    public override float GetMinimumWeaponRange()
    {
        EquipmentTypeWeapon enemyWeaponScript;
        if ((enemyWeaponScript = GetComponent<EquipmentTypeWeapon>()) != null)
        {
            m_minWeaponRange = enemyWeaponScript.GetBulletRange();
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
        EquipmentTypeWeapon enemyWeaponScript;
        if ((enemyWeaponScript = GetComponent<EquipmentTypeWeapon>()) != null)
        {
            m_maxWeaponRange = enemyWeaponScript.GetBulletRange();
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
    
    protected override ItemWrapper[] GetAugmentsAttachedToWeapon ()
    {
        EquipmentTypeWeapon weap = GetComponent<EquipmentTypeWeapon>();
        if(weap != null)
        {
            ItemWrapper[] output = new ItemWrapper[weap.GetMaxAugmentNum()];
            for(int i = 0; i < weap.GetMaxAugmentNum(); i++)
            {
                output[i] = weap.GetItemWrapperInSlot(i);
            }
            
            return output;
        }
        else
        {
            List<ItemWrapper> output = new List<ItemWrapper>();
            GameObject[] turrets = GetAttachedTurrets();
            for(int i = 0; i < turrets.Length; i++)
            {
                EquipmentTypeWeapon turrWeap = turrets[i].GetComponent<EquipmentTypeWeapon>();
                for(int j = 0; j < weap.GetMaxAugmentNum(); j++)
                {
                    output.Add(turrWeap.GetItemWrapperInSlot(j));
                }
            }
            
            return output.ToArray();
        }
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

    #region InitialAugmentSet
    public void PickAugmentsForGroup()
    {
        //At most, we'll need two augments for each equipment type, 4 for weapons
        int[] ids = new int[10];
        for(int i = 0; i < ids.Length; i++)
        {
            ids[i] = Random.Range(0, 25);
        }
        
        ItemIDHolder itemHolder = GameObject.FindGameObjectWithTag("ItemManager").GetComponent<ItemIDHolder>();
        ItemWrapper[] weapon = new ItemWrapper[4];
        //Weapons are 0-3
        for(int i = 0; i < weapon.Length; i++)
        {
            if(ids[i] <= 9)
                weapon[i] = itemHolder.GetItemWithID(ids[i]);
            else
                weapon[i] = null;
        }
        
        ItemWrapper[] shield = new ItemWrapper[2];
        //Shields are 4 & 5
        for(int i = 0; i < shield.Length; i++)
        {
            if(ids[i+4] <= 9)
                shield[i] = itemHolder.GetItemWithID(ids[i+4]);
            else
                weapon[i] = null;
        }
        
        ItemWrapper[] plating = new ItemWrapper[2];
        //Platings are 6 & 7
        for(int i = 0; i < plating.Length; i++)
        {
            if(ids[i+6] <= 9)
                plating[i] = itemHolder.GetItemWithID(ids[i+6]);
            else
                weapon[i] = null;
        }
        
        ItemWrapper[] engine = new ItemWrapper[2];
        //Engines are 8 & 9
        for(int i = 0; i < engine.Length; i++)
        {
            if(ids[i+8] <= 9)
                engine[i] = itemHolder.GetItemWithID(ids[i+8]);
            else
                weapon[i] = null;
        }
        
        //Now we have populated item lists, we can attach them onto ourselves and our subjects
        for(int i = 0; i < weapon.Length; i++)
        {
            if(weapon[i] != null)
            {
                AttachItemToSelfAndSubjectsWeapon(i, weapon[i]);
            }
        }
        
        for(int i = 0; i < shield.Length; i++)
        {
            if(shield[i] != null)
            {
                AttachItemToSelfAndSubjectsShield(i, shield[i]);
            }
        }
        
        for(int i = 0; i < plating.Length; i++)
        {
            if(plating[i] != null)
            {
                AttachItemToSelfAndSubjectsPlating(i, plating[i]);
            }
        }
        
        for(int i = 0; i < engine.Length; i++)
        {
            if(engine[i] != null)
            {
                AttachItemToSelfAndSubjectsEngine(i, engine[i]);
            }
        }
        
        Debug.LogWarning ("Assigned augments to group leader.");
    }
    
    void AttachItemToSelfAndSubjectsWeapon(int attachPoint, ItemWrapper item)
    {
        EquipmentTypeWeapon weap = GetComponent<EquipmentTypeWeapon>();
        if(weap == null)
        {
            GameObject[] turrets = GetAttachedTurrets();
            if(attachPoint <= 2)
            {
                weap = turrets[0].GetComponent<EquipmentTypeWeapon>();
            }
            else
            {
                weap = turrets[1].GetComponent<EquipmentTypeWeapon>();
            }
        }
        
        if(attachPoint > 1)
            attachPoint -= 2;
            
        if(weap.GetMaxAugmentNum() >= attachPoint)
        {
            Debug.Log ("Attempting to equip augment: " + item.GetItemName() + " in slot " + attachPoint);
            weap.SetAugmentItemIntoSlot(attachPoint, item);
        }
    }
    void AttachItemToSelfAndSubjectsShield(int attachPoint, ItemWrapper item)
    {
        EquipmentTypeShield shield = GetComponent<EquipmentTypeShield>();
        
        if(shield != null)
        {
            if(shield.GetMaxAugmentNum() >= attachPoint)
            {
                shield.SetAugmentItemIntoSlot(attachPoint, item);
            }
        }
    }
    void AttachItemToSelfAndSubjectsPlating(int attachPoint, ItemWrapper item)
    {
        EquipmentTypePlating plating = GetComponent<EquipmentTypePlating>();
        
        if(plating != null)
        {
            if(plating.GetMaxAugmentNum() >= attachPoint)
            {
                plating.SetAugmentItemIntoSlot(attachPoint, item);
            }
        }
    }
    void AttachItemToSelfAndSubjectsEngine(int attachPoint, ItemWrapper item)
    {
        EquipmentTypeEngine engine = GetComponent<EquipmentTypeEngine>();
        
        if(engine != null)
        {
            if(engine.GetMaxAugmentNum() >= attachPoint)
            {
                engine.SetAugmentItemIntoSlot(attachPoint, item);
            }
        }
    }
    #endregion
}
