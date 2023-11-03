using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ux
{
    public interface IEntityMono
    {
        void SetEntity(Entity entity);
    }
    public class EntityMono : MonoBehaviour, IEntityMono
    {
        public Entity Entity { get; private set; }

        public T GetEntity<T>() where T : Entity
        {
            if (Entity == null) return null;
            return Entity as T;
        }
        public void SetEntity(Entity entity)
        {
            Entity = entity;
        }
    }
}
