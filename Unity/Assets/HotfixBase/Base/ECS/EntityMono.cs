using System.ComponentModel;
using UnityEngine;

namespace Ux
{
    public interface IEntityMono
    {
#if UNITY_EDITOR
        void SetEntity(Entity entity, EntityViewer go);
#else
        void SetEntity(Entity entity);
#endif
    }
    public class EntityMono : MonoBehaviour, IEntityMono
    {
        public Entity Entity { get; private set; }

        public T GetEntity<T>() where T : Entity
        {
            if (Entity == null) return null;
            return Entity as T;
        }

#if UNITY_EDITOR
        [SerializeField] EntityViewer Viewer;
        public void SetEntity(Entity entity, EntityViewer viewer)
        {
            Entity = entity;
            Viewer = viewer;
        }
#else
        public void SetEntity(Entity entity)
        {
            Entity = entity;
        }
#endif
    }
}
