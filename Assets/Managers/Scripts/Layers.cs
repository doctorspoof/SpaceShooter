/// <summary>
/// Layer is used to house the constant values for each layer in the game, this improves readability, simplifies function implementation
/// and layer changes are easily fixable.
/// </summary>
public sealed class Layers
{
	public const int	player = 8,
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
						enemyDestructibleBullet = 23;
}
