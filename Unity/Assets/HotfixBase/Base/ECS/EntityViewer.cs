using UnityEngine;

namespace Ux
{
    public interface IEntityMono
    {
#if UNITY_EDITOR
        void SetEntity(Entity entity, EntityHierarchy go);
#else
        void SetEntity(Entity entity);
#endif
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
#if UNITY_EDITOR
            _hierarchy = null;
#endif
        }
#if UNITY_EDITOR
        [SerializeField] EntityHierarchy _hierarchy;
        public void SetEntity(Entity entity, EntityHierarchy hierarchy)
        {
            Entity = entity;
            _hierarchy = hierarchy;
        }
#else
        public void SetEntity(Entity entity)
        {
            Entity = entity;
        }
#endif
    }
}
