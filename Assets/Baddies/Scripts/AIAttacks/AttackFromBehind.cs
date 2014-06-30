﻿using UnityEngine;
using System.Collections;

public class AttackFromBehind : IAttack
{

    //[SerializeField]
    //bool clockwise;

    public override void Attack(EnemyScript ship, GameObject target)
    {

        float weaponRange = ship.GetMinimumWeaponRange();
        Vector2 positionBehindTarget = (-target.transform.up * (weaponRange / 2)) + target.transform.position;

        float sqrDirectionToBehindTarget = Vector2.SqrMagnitude(positionBehindTarget - (Vector2)ship.transform.position);
        float sqrDirectionToTarget = Vector2.SqrMagnitude(target.transform.position - ship.transform.position);

        if (sqrDirectionToBehindTarget > sqrDirectionToTarget)
        {
            Vector2 moveTowardsBack = GetPositionForAvoidance(ship, target, positionBehindTarget, weaponRange, 0);

            ship.RotateTowards(moveTowardsBack);
            ship.rigidbody.AddForce(ship.transform.up * ship.GetCurrentMomentum() * Time.deltaTime);
        }
        else
        {

            ship.RotateTowards(target.transform.position);

            if (sqrDirectionToBehindTarget > 9) //9 is sqr(3)
            {
                Vector2 directionToPositionBehind = (positionBehindTarget - (Vector2)ship.transform.position).normalized;
                ship.rigidbody.AddForce(directionToPositionBehind * ship.GetCurrentMomentum() * Time.deltaTime);
            }
            else
            {
                ship.rigidbody.AddForce(new Vector2(Random.Range(-1, 1), Random.Range(-1, 1)) * ship.GetCurrentMomentum() * Time.deltaTime);
            }
            
            EnemyWeaponScript weaponScript = ship.GetComponent<EnemyWeaponScript>();
            weaponScript.MobRequestsFire();
        }


        //Vector2 vectorToGetToPosition = positionBehindTarget - (Vector2)ship.transform.position;

        //float t = Mathf.Clamp(positionBehindTarget.magnitude, 0, 5) / 5.0f;
        //Vector2 directionToMove = ((Vector2)target.transform.position.normalized * (1 - t)) + (vectorToGetToPosition.normalized * t);

        ////Vector2 direction = Vector3.Normalize(target.transform.position - ship.transform.position);

        //Ray ray = new Ray(ship.transform.position, (target.transform.position - ship.transform.position).normalized);
        //Debug.DrawLine(ship.transform.position, positionBehindTarget, Color.yellow);

        //ship.RotateTowards((Vector2)ship.transform.position + directionToMove);

        //if (Vector2.SqrMagnitude(vectorToGetToPosition) < Vector2.SqrMagnitude(target.transform.position - ship.transform.position))
        //{
        //    ship.rigidbody.AddForce(ship.transform.up * ship.GetCurrentMomentum() * Time.deltaTime);
        //}




        //RaycastHit hit;
        //if (target.collider.Raycast(ray, out hit, weaponRange))
        //{
            //    Vector2 directionFromTargetToShip = Vector3.Normalize(ship.transform.position - target.transform.position);

            //    float currentAngle = Vector2.Angle(Vector2.up, directionFromTargetToShip);

            //    Vector3 cross = Vector3.Cross(Vector2.up, directionFromTargetToShip);

            //    if (cross.z > 0)
            //        currentAngle = 360 - currentAngle;

            //    float targetAngle = currentAngle - 10;// (clockwise ? 10 : -10);

            //    float distanceToTarget = Vector2.Distance(ship.transform.position, target.transform.position);

            //    float rangeToCircleAt = Mathf.Clamp(distanceToTarget, weaponRange / 2, weaponRange);

            //    Vector2 newPosition = ((Quaternion.AngleAxis(targetAngle, -Vector3.forward) * Vector2.up) * rangeToCircleAt * 0.8f) + target.transform.position;

            //    ship.RotateTowards(newPosition);
            //    ship.rigidbody.AddForce(ship.transform.up * ship.GetCurrentMomentum() * Time.deltaTime);
        //}
    }

    Vector2 GetPositionForAvoidance(EnemyScript ship, GameObject objectToAvoid, Vector2 targetLocation, float closestDistanceFromGroupToObject, float radiusOfFormation)
    {
        Vector2 directionFromObjectToThis = ship.transform.position - objectToAvoid.transform.position;
        float radiusOfObject = Mathf.Sqrt(Mathf.Pow(objectToAvoid.transform.localScale.x, 2) + Mathf.Pow(objectToAvoid.transform.localScale.y, 2));
        float radius = radiusOfObject + closestDistanceFromGroupToObject + radiusOfFormation;

        Vector2[] returnee = new Vector2[2];
        returnee[0] = new Vector2(radius, 0);
        returnee[1] = new Vector2(-radius, 0);

        Vector2 dir = (targetLocation - (Vector2)ship.transform.position).normalized;
        Quaternion rotation = Quaternion.Euler(new Vector3(0, 0, (Mathf.Atan2(dir.y, dir.x) - Mathf.PI / 2) * Mathf.Rad2Deg));

        returnee[0] = (rotation * returnee[0]) + objectToAvoid.transform.position;
        returnee[1] = (rotation * returnee[1]) + objectToAvoid.transform.position;

        if (Vector2.SqrMagnitude((Vector2)ship.transform.position - returnee[0]) < Vector2.SqrMagnitude((Vector2)ship.transform.position - returnee[1]))
        {
            return returnee[0];
        }
        return returnee[1];

    }

}