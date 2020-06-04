using Improbable.Gdk.Core;
using Improbable.Gdk.Core.Representation;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.TestUtils;
using NUnit.Framework;
using UnityEngine;

namespace Improbable.Gdk.GameObjectCreation.EditmodeTests
{
    public class LinkedGameObjectMapSubscriptionTests : MockBase
    {
        private EntityId entityId = new EntityId(100);

        [Test]
        public void Subscribe_to_LinkedGameObjectMap_should_not_be_available_if_GameObjectCreation_systems_are_not_present()
        {
            World
                .Step((world) =>
                {
                    var subscriptionSystem = world.GetSystem<SubscriptionSystem>();
                    var goMapSubscription = subscriptionSystem.Subscribe<LinkedGameObjectMap>(entityId);

                    return goMapSubscription;
                })
                .Step((world, goMapSubscription) =>
                {
                    Assert.IsFalse(goMapSubscription.HasValue);
                });
        }

        [Test]
        public void Subscribe_to_LinkedGameObjectMap_should_be_available_if_GameObjectCreation_systems_are_added()
        {
            World
                .Step((world) =>
                {
                    // TODO: Hacked in EntityLinkerDatabase
                    GameObjectCreationHelper.EnableStandardGameObjectCreation(world.Worker.World, new MockGameObjectCreator(), ScriptableObject.CreateInstance<EntityLinkerDatabase>());
                    var subscriptionSystem = world.GetSystem<SubscriptionSystem>();
                    var goMapSubscription = subscriptionSystem.Subscribe<LinkedGameObjectMap>(entityId);

                    return goMapSubscription;
                })
                .Step((world, goMapSubscription) =>
                {
                    Assert.IsTrue(goMapSubscription.HasValue);
                    Assert.IsNotNull(goMapSubscription.Value);
                });
        }
    }
}
