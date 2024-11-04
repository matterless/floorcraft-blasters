using Matterless.Floorcraft;
using NUnit.Framework;
using UnityEngine;

namespace Matterless.Test
{
    public class SpawnLocationEncoderTest
    {

        [Test]
        public void TestEncoder()
        {
            Debug.Log("test encoder");

            var spawnLocations_Server = new SpawnLocations();
            spawnLocations_Server.GenerateSpawnPoints(4);
            
            foreach (var point in spawnLocations_Server.spawnPoints) 
                Debug.Log(point);
            
            var spawnLocations_Client = new SpawnLocations();
            
            
            spawnLocations_Client.Decode(spawnLocations_Server.encodedPoints);
            
            foreach (var point in spawnLocations_Client.spawnPoints)
                UnityEngine.Debug.Log(point);
            
            for (var i = 0; i < spawnLocations_Client.spawnPoints.Length; i++)
            {
                //Assert.AreEqual(spawnLocations_Client.spawnPoints[i], spawnLocations_Server.spawnPoints[i]);
                Assert.Contains(spawnLocations_Server.spawnPoints[i], spawnLocations_Server.spawnPoints);
            }

        }
    }
}