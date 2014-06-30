using UnityEngine;
using System.Collections.Generic;

public class AIAttackCollection : MonoBehaviour
{

    static Dictionary<string, IAttack> attackCollection = new Dictionary<string, IAttack>()
    {
        {"AttackCircleClockwise", new AttackCircleClockwise()},
        {"AttackCircleAntiClockwise", new AttackCircleAntiClockwise()},
        {"AttackStrafeLeft", new AttackStrafeLeft()},
        {"AttackStrafeRight", new AttackStrafeRight()},
        {"AttackFromBehind", new AttackFromBehind()},
        {"AttackShieldShipCircleClockwise", new AttackShieldShipCircleClockwise()},
        {"AttackShieldShipCircleAntiClockwise", new AttackShieldShipCircleAntiClockwise()}
    };

    public AIAttackCollection()
    {

    }

    public static IAttack GetAttack(string name_)
    {
        IAttack returnee;
        attackCollection.TryGetValue(name_, out returnee);
        return returnee;
    }

}
