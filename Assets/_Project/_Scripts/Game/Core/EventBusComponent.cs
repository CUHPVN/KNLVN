using UnityEngine;

namespace KNLVN.Game
{
    /// <summary>
    /// MonoBehaviour wrapper that exposes <see cref="EventBus"/> as a Unity component.
    /// Attach to a persistent GameObject so all other scripts can reference it via Inspector.
    /// </summary>
    public class EventBusComponent : PersistentSingleton<EventBusComponent>
    {
        private EventBus _bus;
        private EventBus Bus => _bus ??= new EventBus();

        protected override void Awake()
        {
            base.Awake();
            // Initialize here to ensure it's created early, but lazy init handles earlier accesses
            if (_bus == null) _bus = new EventBus();
        }

        public void Subscribe<T>(System.Action<T> handler)   => Bus.Subscribe(handler);
        public void Unsubscribe<T>(System.Action<T> handler) => Bus.Unsubscribe(handler);
        public void Publish<T>(T evt)                        => Bus.Publish(evt);

        private void OnDestroy() => _bus?.Dispose();
    }
}
