using UnityEngine;



/// <summary>
/// What type of mask you're looking to receive from Layers.
/// </summary>
public enum MaskType
{
    Standard = 1,
    Targetting = 2,
    AoE = 3
}



/// <summary>
/// Layer is used to house the constant values for each layer in the game, this improves readability, simplifies function implementation
/// and layer changes are easily fixable.
/// </summary>
public sealed class Layers
{
    #region Individual layers
    
    public const int    player = 8,
                        ignore = 9,
                        enemy = 11,
                        objective = 12,
                        capital = 13,
                        playerBullet = 14,
                        enemyBullet = 15,
                        capitalBullet = 16,
                        asteroid = 17,
                        shop = 18,
                        events = 19,
                        environmentalDamage = 20,
                        planets3D = 21,
                        enemySupportShield = 22,
                        enemyDestructibleBullet = 23,
                        enemyCollide = 24;
    
    #endregion
    
    
    #region Externally obtain layer mask
    
    /// <summary>
    /// Used to get the desired layer mask depending on the objects current layer.
    /// </summary>
    /// <returns>The determined layer mask</returns>
    /// <param name="baseLayer">What layer is the current object?</param>
    /// <param name="type">Do you require any special type of mask?</param>
    public int GetLayerMask (int baseLayer, MaskType type = MaskType.Standard)
    {
        switch (baseLayer)
        {
            case capital:
                return GetCapitalMask (type);
                
            case playerBullet:
                return GetPlayerBulletMask (type);
                
            case enemyBullet:
                return GetEnemyBulletMask (type);
                
            case capitalBullet:
                return GetCapitalBulletMask (type);
                
            default:
                Debug.LogError ("Can't determine LayerMask for layer: " + baseLayer + " (" + type + ")");
                return -1;
        }
    }
    
    #endregion
    
    
    #region Get mask functions
    
    int GetCapitalMask (MaskType type)
    {
        switch (type)
        {
            case MaskType.Standard:
                return (1 << asteroid) | (1 << enemy) | (1 << enemyDestructibleBullet) | (1 << enemyCollide) | (1 << enemySupportShield);
                
            // Remove asteroids from targetting
            case MaskType.Targetting:
                return GetCapitalMask (MaskType.Standard) & ~(1 << asteroid) & ~(1 << enemyDestructibleBullet);
                
            case MaskType.AoE:
                return GetCapitalMask (MaskType.Standard);
        }

        return -1;
    }
    
    
    int GetPlayerBulletMask (MaskType type)
    {
        switch (type)
        {
            case MaskType.Standard:
                return (1 << asteroid) | (1 << enemy) | (1 << enemyDestructibleBullet) | (1 << enemyCollide) | (1 << enemySupportShield);
                
            // Remove the asteroids from targetting
            case MaskType.Targetting:
                return GetPlayerBulletMask (MaskType.Standard) & ~(1 << asteroid);
                
            // Add the player to AoE damage
            case MaskType.AoE:
                return GetPlayerBulletMask (MaskType.Standard) | (1 << player);
        }

        return -1;
    }
    
    
    int GetEnemyBulletMask (MaskType type)
    {
        switch (type)
        {
            case MaskType.Standard:
                return (1 << asteroid) | (1 << player) | (1 << capital);
                
            // Remove asteroids from targetting
            case MaskType.Targetting:
                return GetEnemyBulletMask (MaskType.Standard) & ~(1 << asteroid);
                
            // Allow enemies to hurt enemies and for missiles to chain missiles
            case MaskType.AoE:
                return GetEnemyBulletMask (MaskType.Standard) | (1 << enemy) | (1 << enemyDestructibleBullet) | (1 << enemyCollide) | (1 << enemySupportShield);
        }

        return -1;
    }
    
    
    int GetCapitalBulletMask (MaskType type)
    {
        switch (type)
        {
            case MaskType.Standard:
                return (1 << asteroid) | (1 << enemy) | (1 << enemyDestructibleBullet) | (1 << enemyCollide) | (1 << enemySupportShield);
                
            // Remove asteroids from targetting
            case MaskType.Targetting:
                return GetCapitalBulletMask (MaskType.Standard) & ~(1 << asteroid);
                
            // Just use the standard mask
            case MaskType.AoE:
                return GetCapitalBulletMask (MaskType.Standard);
        }

        return -1;
    }
    
    #endregion
}
