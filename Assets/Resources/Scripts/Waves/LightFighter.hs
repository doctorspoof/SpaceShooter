// leader of wave
var waveLeader = CreateShip("AISpawnLeader", null);
waveLeader.AddTargetTag("Capital");
waveLeader.AddTargetTag("Player");

// group 1
var subLeader1 = CreateShip("EnemyFast", waveLeader);
var child11 = CreateShip("EnemyFast", subLeader1);
var child12 = CreateShip("EnemyFast", subLeader1);
var child13 = CreateShip("EnemyFast", subLeader1);
var child14 = CreateShip("EnemyFast", subLeader1);

// group 2
var subLeader2 = CreateShip("EnemyFast", waveLeader);
var child21 = CreateShip("EnemyFast", subLeader2);
var child22 = CreateShip("EnemyFast", subLeader2);
var child23 = CreateShip("EnemyFast", subLeader2);
var child24 = CreateShip("EnemyFast", subLeader2);

// group 3
var subLeader3 = CreateShip("EnemyFast", waveLeader);
var child31 = CreateShip("EnemyFast", subLeader3);
var child32 = CreateShip("EnemyFast", subLeader3);
var child33 = CreateShip("EnemyFast", subLeader3);
var child34 = CreateShip("EnemyFast", subLeader3);

SetWave("EnemyFast", waveLeader);