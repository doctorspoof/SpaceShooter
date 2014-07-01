using UnityEngine;
using System.Collections;

public class AttackCircleClockwise : IAttack
{

    //[SerializeField]
    //bool clockwise;

    public override void Attack(EnemyScript ship, GameObject target)
    {
        float weaponRange = ship.GetMinimumWeaponRange();

        float shipDimension = 0;
        Ship targetShip = target.GetComponent<Ship>();
        if (targetShip != null)
        {
            shipDimension = targetShip.GetMaxSize();
        }

        float totalRange = weaponRange + shipDimension;

        Vector2 direction = Vector3.Normalize(target.transform.position - ship.transform.position);

        Ray ray = new Ray(ship.transform.position, direction);

        RaycastHit hit;
        if (target.collider.Raycast(ray, out hit, totalRange))
        {
            Vector2 directionFromTargetToShip = Vector3.Normalize(ship.transform.position - target.transform.position);

            float currentAngle = Vector2.Angle(Vector2.up, directionFromTargetToShip);

            Vector3 cross = Vector3.Cross(Vector2.up, directionFromTargetToShip);

            if (cross.z > 0)
                currentAngle = 360 - currentAngle;

            float targetAngle = currentAngle - 10;// (clockwise ? 10 : -10);

            float distanceToTarget = Vector2.Distance(ship.transform.position, target.transform.position);

            float rangeToCircleAt = Mathf.Clamp(distanceToTarget, (weaponRange / 2) + shipDimension, totalRange);

            Vector2 newPosition = ((Quaternion.AngleAxis(targetAngle, -Vector3.forward) * Vector2.up) * rangeToCircleAt * 0.8f) + target.transform.position;

            ship.RotateTowards(newPosition);
            ship.rigidbody.AddForce(ship.transform.up * ship.GetCurrentMomentum() * Time.deltaTime);
        }
    }

}
