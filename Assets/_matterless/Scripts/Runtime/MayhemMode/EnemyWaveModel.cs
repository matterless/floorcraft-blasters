using UnityEngine;
using System;
using System.Collections.Generic;

namespace Matterless.Floorcraft
{
    public class EnemyWaveModel
    {
        public float spawnFrequencyMax { get; set; }
        public float spawnFrequencyMin { get; set; }
        public int maxNumberOfEnemies { get; set; }
        public int spawnPointCount { get; set; }
        public List<Enemy> onlyTheseEnemyTypes { get; set; }
        public Action onWaveCompleted { get; set; }
        public Action spawnEnemy { get; set; }
    }
}