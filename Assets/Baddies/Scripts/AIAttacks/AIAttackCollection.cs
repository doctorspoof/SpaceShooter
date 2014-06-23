using UnityEngine;
using System.Collections;

public class AIAttackCollection
{
    //public static AIAttackCollection instance = new AIAttackCollection();

    public static IAttack[][] attackLists = 
    {
            new IAttack[] //large ships
            {
                new AttackCircleClockwise(),
                new AttackCircleAntiClockwise()
            }, 
            new IAttack[] //medium ships
            { 
                new AttackStrafeLeft(),
                new AttackStrafeRight()
            }, 
            new IAttack[] //small ships
            { 
                new AttackStrafeLeft(),
                new AttackStrafeRight()
            } 
    };

    public AIAttackCollection()
    {

    }

    public static IAttack GetRandomAttack(int shipSize)
    {
        return attackLists[shipSize][UnityEngine.Random.Range(0, attackLists[shipSize].Length)];
    }

}
