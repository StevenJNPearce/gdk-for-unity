using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Improbable.Gdk.Core.NetworkStats;
using Improbable.Worker.CInterop;

namespace Improbable.Gdk.Core
{
    public class MockConnectionHandlerBuilder : IConnectionHandlerBuilder
    {
        public MockConnectionHandler ConnectionHandler;

        public MockConnectionHandlerBuilder()
        {
            ConnectionHandler = new MockConnectionHandler();
        }

        public Task<IConnectionHandler> CreateAsync(CancellationToken? token = null)
        {
            return Task.FromResult((IConnectionHandler) ConnectionHandler);
        }

        public string WorkerType { get; }
    }

    public class MockConnectionHandler : IConnectionHandler
    {
        private uint updateId;

        private ViewDiff currentDiff => diffs[currentDiffIndex];

        private readonly ViewDiff[] diffs = new[]
        {
            new ViewDiff(),
            new ViewDiff()
        };

        private int currentDiffIndex = 0;

        public void CreateEntity(long entityId, EntityTemplate template)
        {
            var handler = new CreateEntityTemplateDynamicHandler(template, entityId, currentDiff);
            Dynamic.ForEachComponent(handler);
        }

        public void ChangeAuthority(long entityId, uint componentId, Authority newAuthority)
        {
            currentDiff.SetAuthority(entityId, componentId, newAuthority);
        }

        public void UpdateComponent<T>(long entityId, uint componentId, T update) where T : ISpatialComponentUpdate
        {
            currentDiff.AddComponentUpdate(update, entityId, componentId, updateId++);
        }

        public void AddEvent<T>(long entityId, uint componentId, T ev) where T : IEvent
        {
            currentDiff.AddEvent(ev, entityId, componentId, updateId++);
        }

        public void RemoveEntity(long entityId)
        {
            currentDiff.RemoveEntity(entityId);
        }

        public void RemoveEntityAndComponents(long entityId, EntityTemplate template)
        {
            var handler = new RemoveEntityTemplateDynamicHandler(template, entityId, currentDiff);
            Dynamic.ForEachComponent(handler);

            RemoveEntity(entityId);
        }

        public void RemoveComponent(long entityId, uint componentId)
        {
            currentDiff.RemoveComponent(entityId, componentId);
        }

        public void AddComponent<T>(long entityId, uint componentId, T component)
            where T : ISpatialComponentUpdate
        {
            currentDiff.AddComponent(component, entityId, componentId);
        }

        public void UpdateComponentAndAddEvents<TUpdate, TEvent>(long entityId, uint componentId, TUpdate update,
            params TEvent[] events)
            where TUpdate : ISpatialComponentUpdate
            where TEvent : IEvent
        {
            var thisUpdateId = updateId++;

            currentDiff.AddComponentUpdate(update, entityId, componentId, thisUpdateId);
            foreach (var ev in events)
            {
                currentDiff.AddEvent(ev, entityId, componentId, thisUpdateId);
            }
        }

        // TODO: Commands

        #region IConnectionHandler implementation

        public string GetWorkerId()
        {
            return "TestWorker";
        }

        public List<string> GetWorkerAttributes()
        {
            return new List<string> { "attribute_the_first", "attribute_the_second" };
        }

        public void GetMessagesReceived(ref ViewDiff viewDiff)
        {
            var diffToReturn = currentDiff;

            currentDiffIndex = (currentDiffIndex + 1) % diffs.Length;
            var nextDiff = currentDiff;
            nextDiff.Clear();

            viewDiff = diffToReturn;
        }

        public MessagesToSend GetMessagesToSendContainer()
        {
            return new MessagesToSend();
        }

        public void PushMessagesToSend(MessagesToSend messages, NetFrameStats netFrameStats)
        {
        }

        public bool IsConnected()
        {
            return true;
        }

        #endregion

        private class CreateEntityTemplateDynamicHandler : Dynamic.IHandler
        {
            private readonly EntityTemplate template;
            private readonly long entityId;
            private readonly ViewDiff viewDiff;

            public CreateEntityTemplateDynamicHandler(EntityTemplate template, long entityId, ViewDiff viewDiff)
            {
                this.template = template;
                this.viewDiff = viewDiff;
                this.entityId = entityId;

                viewDiff.AddEntity(this.entityId);
            }

            public void Accept<TUpdate, TSnapshot>(uint componentId, Dynamic.VTable<TUpdate, TSnapshot> vtable)
                where TUpdate : struct, ISpatialComponentUpdate
                where TSnapshot : struct, ISpatialComponentSnapshot
            {
                var maybeSnapshot = template.GetComponent<TSnapshot>();

                if (!maybeSnapshot.HasValue)
                {
                    return;
                }

                var snapshot = maybeSnapshot.Value;
                viewDiff.AddComponent(vtable.ConvertSnapshotToUpdate(snapshot), entityId, componentId);
            }
        }

        private class RemoveEntityTemplateDynamicHandler : Dynamic.IHandler
        {
            private readonly EntityTemplate template;
            private readonly long entityId;
            private readonly ViewDiff viewDiff;

            public RemoveEntityTemplateDynamicHandler(EntityTemplate template, long entityId, ViewDiff viewDiff)
            {
                this.template = template;
                this.entityId = entityId;
                this.viewDiff = viewDiff;
            }

            public void Accept<TUpdate, TSnapshot>(uint componentId, Dynamic.VTable<TUpdate, TSnapshot> vtable)
                where TUpdate : struct, ISpatialComponentUpdate
                where TSnapshot : struct, ISpatialComponentSnapshot
            {
                if (template.HasComponent<TSnapshot>())
                {
                    viewDiff.RemoveComponent(entityId, componentId);
                }
            }
        }

        public void Dispose()
        {
        }
    }
}
