// leader of wave
var waveLeader = CreateShip("AISpawnLeader", null);

// group 1
var subLeader1 = CreateShip("LightFighter", waveLeader);
var child11 = CreateShip("LightFighter", subLeader1);
var child12 = CreateShip("LightFighter", subLeader1);
var child13 = CreateShip("LightFighter", subLeader1);
var child14 = CreateShip("LightFighter", subLeader1);

// group 2
var subLeader2 = CreateShip("LightFighter", waveLeader);
var child21 = CreateShip("LightFighter", subLeader2);
var child22 = CreateShip("LightFighter", subLeader2);
var child23 = CreateShip("LightFighter", subLeader2);
var child24 = CreateShip("LightFighter", subLeader2);

// group 3
var subLeader3 = CreateShip("LightFighter", waveLeader);
var child31 = CreateShip("LightFighter", subLeader3);
var child32 = CreateShip("LightFighter", subLeader3);
var child33 = CreateShip("LightFighter", subLeader3);
var child34 = CreateShip("LightFighter", subLeader3);
