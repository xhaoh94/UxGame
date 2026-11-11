using UnityEngine;

namespace Ux
{
    public interface IEntityMono
    {
        void SetEntity(Entity entity);
    }

    public class EntityViewer : MonoBehaviour, IEntityMono
    {
        public Entity Entity { get; private set; }

        public T GetEntity<T>() where T : Entity
        {
            if (Entity == null) return null;
            return Entity as T;
        }

        public void Release()
        {
            Entity = null;
        }

        public void SetEntity(Entity entity)
        {
            Entity = entity;
        }
    }
}
