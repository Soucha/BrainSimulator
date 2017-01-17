﻿﻿using System;
using System.Reflection;
using VRageMath;
using World.Physics;

namespace World.GameActors.GameObjects
{
    public interface ICharacter : IGameObject, IForwardMovable, IRotatable
    {
        new IForwardMovablePhysicalEntity PhysicalEntity { get; set; }
    }

    public abstract class Character : GameObject, ICharacter
    {
        public float Rotation { get; set; }

        public new IForwardMovablePhysicalEntity PhysicalEntity
        {
            get { return (IForwardMovablePhysicalEntity)base.PhysicalEntity; }
            set { base.PhysicalEntity = value; }
        }

        public float Direction
        {
            get { return PhysicalEntity.Direction; }
            set { PhysicalEntity.Direction = value; }
        }

        public float ForwardSpeed
        {
            get { return PhysicalEntity.ForwardSpeed; }
            set { PhysicalEntity.ForwardSpeed = value; }
        }

        public float RotationSpeed
        {
            get { return PhysicalEntity.RotationSpeed; }
            set { PhysicalEntity.RotationSpeed = value; }
        }

        public bool ElasticCollision
        {
            get { return PhysicalEntity.ElasticCollision; }
            set { PhysicalEntity.ElasticCollision = value; }
        }

        public bool InelasticCollision
        {
            get { return PhysicalEntity.InelasticCollision; }
            set { PhysicalEntity.InelasticCollision = value; }
        }

        public Character(
            string tilesetName,
            int tileId,
            string name,
            Vector2 position,
            Vector2 size,
            float direction,
            Type shapeType = null
            )
            : base(tilesetName, tileId, name)
        {
            shapeType = shapeType ?? typeof(CircleShape);
            ConstructorInfo ctor = shapeType.GetConstructor(new[] {typeof(Vector2), typeof(Vector2) });

            if (ctor == null)
            {
                throw new Exception("Class " + shapeType.FullName + " has no constructor " + shapeType.Name +
                                    "(Vector2 v), " +
                                    "hence Character cannot create his PhysicalEntity Shape.");
            }

            Rotation = direction;

            Shape shape = (Shape)ctor.Invoke(new object[] { position, size });
            PhysicalEntity = new ForwardMovablePhysicalEntity(position, shape, direction: direction);
        }
    }
}
