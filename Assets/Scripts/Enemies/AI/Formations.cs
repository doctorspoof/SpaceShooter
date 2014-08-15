using UnityEngine;
using System.Collections.Generic;

public class Formations
{

    public static List<Vector2> GenerateCircleFormation(List<Ship> ships_)
    {
        List<Vector2> returnee = new List<Vector2>();
        Ship largestShipInLayer = null;
        float radius = 0;

        if (ships_.Count > 0)
        {
            int pointsCount = ships_.Count;
            largestShipInLayer = GetLargestShip(ships_);

            //special case for the first tier with only one ship to be placed in the middle
            bool skip = false;
            if (radius == 0 && pointsCount == 1)
            {
                skip = true;
                returnee.Add(new Vector2(0, 0));
            }

            float circumferance = ((largestShipInLayer.GetMaxSize()) + 1f) * pointsCount;
            radius += circumferance / (2 * Mathf.PI);

            //skip assigning ships to circle if only only one ship in first tier (i hate special cases like this but i cant think of a way round it)
            if (!skip)
            {
                for (int j = 0; j < pointsCount; ++j)
                {

                    Quaternion finalRotation = Quaternion.AngleAxis(j * (360.0f / pointsCount), Vector3.forward);
                    returnee.Add((Vector2)(finalRotation * new Vector3(radius, 0, 0)));

                }
            }
        }

        return returnee;
    }

    static Ship GetLargestShip(List<Ship> list)
    {
        if (list.Count == 0)
            return null;

        Ship returnee = null;
        float size = 0;

        foreach (Ship ship in list)
        {
            if (returnee == null || ship.GetMaxSize() > size)
            {
                returnee = ship;
                size = ship.GetMaxSize();
            }
        }

        return returnee;
    }

}
