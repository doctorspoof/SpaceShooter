﻿using UnityEngine;
using System.Collections;

public class AttackStrafeLeft : IAttack
{

    public override void Attack(EnemyScript ship, GameObject target)
    {
		float timeTakenToTravel = Vector3.Distance(target.transform.position, ship.transform.position) / ship.GetComponent<EnemyWeaponScript>().GetBulletSpeed();
		Vector3 predictedTargetPos = target.transform.position + (target.rigidbody.velocity * timeTakenToTravel);
		ship.RotateTowards(predictedTargetPos);

        EnemyWeaponScript weaponScript = ship.GetComponent<EnemyWeaponScript>();

        float weaponRange = ship.GetMinimumWeaponRange();
        Vector3 direction = Vector3.Normalize(target.rigidbody.position - ship.transform.position);

        Ray ray = new Ray(ship.transform.position, direction);
        Debug.DrawLine(ship.transform.position, ship.transform.position + direction * weaponRange);

        RaycastHit hit;
        if (weaponScript != null && target.collider.Raycast(ray, out hit, weaponRange))
        {
            ship.rigidbody.AddForce(-ship.transform.right * ship.GetCurrentMomentum() * Time.deltaTime);
            weaponScript.MobRequestsFire();
        }

    }

}
