// leader of wave
var waveLeader = CreateShip("AISpawnLeader", null);
waveLeader.AddTargetTag("Capital");
waveLeader.AddTargetTag("Player");

// group 1
var subLeader1 = CreateShip("EnemyNormal", waveLeader);
var child11 = CreateShip("EnemyNormal", subLeader1);
var child12 = CreateShip("EnemyNormal", subLeader1);

// group 2
var subLeader2 = CreateShip("EnemyNormal", waveLeader);
var child21 = CreateShip("EnemyNormal", subLeader2);
var child22 = CreateShip("EnemyNormal", subLeader2);

// group 3
var subLeader3 = CreateShip("EnemyNormal", waveLeader);
var child31 = CreateShip("EnemyNormal", subLeader3);
var child32 = CreateShip("EnemyNormal", subLeader3);

SetWave("EnemyNormal", waveLeader);