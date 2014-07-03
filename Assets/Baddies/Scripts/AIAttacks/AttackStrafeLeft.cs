﻿using UnityEngine;
using System.Collections;

public class AttackStrafeLeft : IAttack
{

    public override void Attack(EnemyScript ship, GameObject target)
    {
		float timeTakenToTravel = Vector3.Distance(target.transform.position, ship.shipTransform.position) / ship.GetComponent<EnemyWeaponScript>().GetBulletSpeed();
		Vector3 predictedTargetPos = target.transform.position + (target.rigidbody.velocity * timeTakenToTravel);
		ship.RotateTowards(predictedTargetPos);

        EnemyWeaponScript weaponScript = ship.GetComponent<EnemyWeaponScript>();

        float weaponRange = ship.GetMinimumWeaponRange();

        float shipDimension = 0;
        Ship targetShip = target.GetComponent<Ship>();
        if (targetShip != null)
        {
            shipDimension = targetShip.GetCalculatedSizeByPosition(targetShip.shipTransform.position);
        }

        float totalRange = weaponRange <= shipDimension ? weaponRange + shipDimension : weaponRange;

        Vector3 direction = Vector3.Normalize(target.rigidbody.position - ship.shipTransform.position);

        Ray ray = new Ray(ship.shipTransform.position, direction);
        //Debug.DrawLine(ship.shipTransform.position, ship.shipTransform.position + direction * weaponRange);

        RaycastHit hit;
        if (weaponScript != null && target.collider.Raycast(ray, out hit, totalRange))
        {
            ship.rigidbody.AddForce(-ship.shipTransform.right * ship.GetCurrentMomentum() * Time.deltaTime);
            weaponScript.MobRequestsFire();
        }

    }

}
