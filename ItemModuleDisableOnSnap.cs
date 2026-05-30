using ThunderRoad;

namespace BaronsBounties
{
    public class ItemModuleDisableOnSnap : ItemModule
    {
        #if !SDK
        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            item.gameObject.GetOrAddComponent<DisableOnSnap>().Init(item);
        }

        public class DisableOnSnap : ThunderBehaviour
        {
            public Item item;

            public void Init(Item item)
            {
                item.OnSnapEvent += OnSnapEvent;
                item.OnUnSnapEvent += OnUnsnapEvent;
                item.OnDespawnEvent += OnDespawn;
            }

            private void OnDespawn(EventTime eventTime)
            {
                if (eventTime == EventTime.OnEnd) return;
                item.OnSnapEvent -= OnSnapEvent;
                item.OnUnSnapEvent -= OnUnsnapEvent;
                item.OnDespawnEvent -= OnDespawn;
            }

            private void OnSnapEvent(Holder holder) => item.SetColliders(false);

            private void OnUnsnapEvent(Holder holder) => item.SetColliders(true);
        }
        #endif
    }
}