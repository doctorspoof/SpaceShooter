using UnityEngine;
using System.Collections.Generic;
using System;

public class AIAttackCollection : MonoBehaviour
{

    static Dictionary<string, System.Type> attackCollection = new Dictionary<string, System.Type>()
    {
        {"AttackCircleClockwise", typeof(AttackCircleClockwise)},
        {"AttackCircleAntiClockwise", typeof(AttackCircleAntiClockwise)},
        {"AttackStrafeLeft", typeof(AttackStrafeLeft)},
        {"AttackStrafeRight", typeof(AttackStrafeRight)},
        {"AttackFromBehind", typeof(AttackFromBehind)},
        {"AttackShieldShipCircleClockwise", typeof(AttackShieldShipCircleClockwise)},
        {"AttackShieldShipCircleAntiClockwise", typeof(AttackShieldShipCircleAntiClockwise)},
        {"AttackRam", typeof(AttackRam)}
    };

    public AIAttackCollection()
    {
    }

    public static IAttack GetAttack(string name_)
    {
        System.Type returnee;
        attackCollection.TryGetValue(name_, out returnee);
        //return (IAttack)returnee.GetConstructor(new Type[]{}).Invoke(new object[]{});
        return null;
    }

}
