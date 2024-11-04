using NUnit.Framework;
using Matterless.Floorcraft;
using UnityEngine;

namespace Matterless.Test
{
    public class DomainDataAssetsSerializationTest
    {
        /*[Test]
        public void Serialise()
        {
            // prepare test
            var payload = new DomainAssetPayload()
            {
                app_id = "floorcraft",
                id = "123",
                type = "100", // AssetId.Pillar.ToString()
                pose = "[1.2,2.1,3.3,4.4,5.5,6.6,7.7]"
            
            };

            // do the logic
            var asset = new DomainAssetData(payload);

            // assertions
            Assert.AreEqual(true, asset.isValid);
            Assert.AreEqual("123", asset.id);
            Assert.AreEqual("floorcraft", asset.appId);
            Assert.AreEqual(AssetId.Pillar, asset.type);
            Assert.AreEqual(1.2f, asset.pose.position.x);
            Assert.AreEqual(2.1f, asset.pose.position.y);
            Assert.AreEqual(3.3f, asset.pose.position.z);

            Assert.AreEqual(4,4f, asset.pose.rotation.x);
            Assert.AreEqual(5.5f, asset.pose.rotation.y);
            Assert.AreEqual(6.6f, asset.pose.rotation.z);
            Assert.AreEqual(7.7f, asset.pose.rotation.w);
        }

        [Test]
        public void Desirialize()
        {
            var asset = new DomainAssetData(
                "123",
                AssetId.Car0,
                new Pose(new Vector3(1.1f, 2.2f, 3.3f),
                new Quaternion(4.4f, 5.5f, 6.6f, 7.7f)));

            var payload = DomainAssetPayload.Create(asset);

            Assert.AreEqual("123", payload.id);
            Assert.AreEqual("200", payload.type);
            Assert.AreEqual("floorcraft", payload.app_id);
            Assert.AreEqual("[1.1,2.2,3.3,4.4,5.5,6.6,7.7]", payload.pose);

        }*/

    }
}