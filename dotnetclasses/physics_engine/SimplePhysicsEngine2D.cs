namespace SimplePhysicsEngine2D;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using Godot;

public class PhysicsEngine{
	float tick = 60;

	List<PhysicsObject> physicsObjects;
	List<PhysicsObject> physicsObjectsToRemove;

	public PhysicsEngine()
	{
		physicsObjects = new List<PhysicsObject>();
		physicsObjectsToRemove = new List<PhysicsObject>();
	}

	public PhysicsEngine clone()
	{
		var newPhysicsEngine = new PhysicsEngine();

		physicsObjects.ForEach(physicsObject => newPhysicsEngine.physicsObjects.Add(physicsObject.clone()));

		return newPhysicsEngine;
	}

	public List<PhysicsObject> getFixedObjects()
	{
		return physicsObjects.Where(physicsObject => physicsObject.data.isFixed).ToList();
	}

	public struct PhysicsData
	{
		public float mass;
		public bool isFixed;
		public float damping;

		public PhysicsData(float mass, float damping, bool isFixed=false)
		{
			this.mass = mass;
			this.damping = damping;
			this.isFixed = isFixed;
		}
	}

	public class PhysicsObject
	{

		public PhysicsData data;
		public PhysicsShape shape;

		public int id;


		public Vector2 velocity;
		public Vector2 position;

		public Action<CollisionInfo> collisionOccurred;

		public PhysicsObject(PhysicsData data, PhysicsShape shape, Vector2 position)
		{
			this.data = data;
			this.shape = shape;
			this.position = position;
			this.velocity = Vector2.Zero;
		}

		public PhysicsObject clone()
		{
			var newObject = new PhysicsObject(this.data, this.shape, this.position);
			newObject.velocity = this.velocity;
			return newObject;
		}

	}

	public struct CollisionInfo
	{
		public PhysicsObject other;

		public Vector2 position;

		public Vector2 normal;

	}

	public class Collision
	{
		public PhysicsObject objectA;
		public PhysicsObject objectB;

		public Vector2 position;

		public Vector2 normalToB;

		public float amountOfPenetration;
	}

	public interface PhysicsShape
	{
	}

	public Collision getBallBallCollision(PhysicsObject self, PhysicsObject other, BallShape selfShape, BallShape otherShape)
	{
		var coll = new Collision();

		coll.objectA = self;
		coll.objectB = other;

		//GD.Print($"Distance between balls {other.position.DistanceTo(self.position)} {self.position} {other.position}");
		var penetrationAmount = (selfShape.radius + otherShape.radius) - other.position.DistanceTo(self.position);
		if (penetrationAmount <= 0)
		{
			return null;
		}
		coll.amountOfPenetration = penetrationAmount;
		coll.normalToB = self.position.DirectionTo(other.position);
		coll.position = self.position + coll.normalToB * selfShape.radius;
		return coll;
		
	}

	public Collision getBallSegmentCollision(PhysicsObject self, PhysicsObject other, BallShape selfShape, SegmentShape segmentShape){
		
		var coll = new Collision();

		coll.objectA = self;
		coll.objectB = other;

		var endPointPenetration = selfShape.radius  - self.position.DistanceTo(segmentShape.endPos);
		if (endPointPenetration > 0)
		{
			coll.amountOfPenetration = endPointPenetration;
			coll.position = segmentShape.endPos;
			coll.normalToB = self.position.DirectionTo(segmentShape.endPos);
			return coll;
		} 

		var startPointPenetration = selfShape.radius - self.position.DistanceTo(segmentShape.startPos);
		if (startPointPenetration > 0)
		{
			coll.amountOfPenetration = startPointPenetration;
			coll.position = segmentShape.startPos;
			coll.normalToB = self.position.DirectionTo(segmentShape.startPos);
			return coll;
		} 

		var closestPoint = Geometry2D.GetClosestPointToSegment(
			self.position, segmentShape.startPos, segmentShape.endPos);

		var segmentPenetration = selfShape.radius - closestPoint.DistanceTo(self.position);


		if(segmentPenetration > 0)
		{
			coll.amountOfPenetration = segmentPenetration;
			coll.position = closestPoint;
			coll.normalToB = self.position.DirectionTo(closestPoint);
			return coll;
		}

		return null;
	}

	public class BallShape: PhysicsShape
	{
		public PhysicsData data;
		public float radius;

		public BallShape(float radius)
		{	
			this.radius = radius;
		}

	}
	public class SegmentShape: PhysicsShape
	{
		public Vector2 startPos;
		public Vector2 endPos;

		public SegmentShape(Vector2 startPos, Vector2 endPos)
		{
			this.startPos = startPos;
			this.endPos = endPos;
		}
	}

	private Vector2 getUpdatedBallVelocityElastic(PhysicsObject self, PhysicsObject other)
	{
		var massCoeff1 = (self.data.mass - other.data.mass)/(self.data.mass + other.data.mass);
		var massCoeff2 = (2 * other.data.mass)/(self.data.mass + other.data.mass);

		return self.velocity * massCoeff1 + other.velocity * massCoeff2;

	}

	private void reflectOffNormal(PhysicsObject self, Vector2 normal)
	{
		self.velocity = self.velocity.Bounce(normal);
	}

	public PhysicsObject addPhysicsObject(PhysicsObject physicsObject)
	{
		physicsObjects.Add(physicsObject);
		return physicsObject;
	}

	public PhysicsObject addPhysicsObject(Vector2 position, PhysicsShape shape, PhysicsData data)
	{
		var physicsObject = new PhysicsObject(data, shape, position);
		physicsObjects.Add(physicsObject);
		return physicsObject;
	}

	public void removeObject(PhysicsObject physicsObject)
	{
		physicsObjectsToRemove.Add(physicsObject);
	}


	public void update()
	{

		var collisions = new List<Collision>();

		var timeStep = getTimeStep();
		for (int i = 0; i < physicsObjects.Count; i++)
		{
			for (int j = 0; j < physicsObjects.Count; j++)
			{
				if(i>=j)
				{
					continue;
				}
				var physicsObjectA = physicsObjects[i];
				var physicsObjectB = physicsObjects[j];

				if (physicsObjectA.shape is BallShape & physicsObjectB.shape is BallShape)
				{
					var coll = getBallBallCollision(physicsObjectA, physicsObjectB, (BallShape)physicsObjectA.shape, (BallShape)physicsObjectB.shape);
					if (coll != null)
					{
						collisions.Add(coll);
					}
				}

				if (physicsObjectA.shape is BallShape & physicsObjectB.shape is SegmentShape)
				{
					var coll = getBallSegmentCollision(physicsObjectA, physicsObjectB, (BallShape)physicsObjectA.shape, (SegmentShape)physicsObjectB.shape);
					if (coll != null)
					{
						collisions.Add(coll);
					}
				}

				if (physicsObjectA.shape is SegmentShape & physicsObjectB.shape is BallShape)
				{
					var coll = getBallSegmentCollision(physicsObjectB, physicsObjectA, (BallShape)physicsObjectB.shape, (SegmentShape)physicsObjectA.shape);
					if (coll != null)
					{
						collisions.Add(coll);
					}
				}

			}
		}

		foreach (var collision in collisions)
		{
			
			collision.objectA.collisionOccurred?.Invoke(new CollisionInfo {
				other = collision.objectB,
				position = collision.position,
				normal = -collision.normalToB
				});
			collision.objectB.collisionOccurred?.Invoke(new CollisionInfo {
				other = collision.objectA,
				position = collision.position,
				normal = collision.normalToB
				});

			if(collision.objectA.shape is BallShape & collision.objectB.shape is SegmentShape)
			{
				float amountOfPenetration = collision.amountOfPenetration + 0.01f;
				collision.objectA.position += -collision.normalToB * amountOfPenetration;

				reflectOffNormal(collision.objectA, -collision.normalToB);
			}
			if (collision.objectA.shape is BallShape & collision.objectB.shape is BallShape)
			{
				float amountOfPenetration = collision.amountOfPenetration + 0.01f;
				collision.objectA.position += -collision.normalToB * amountOfPenetration/2;
				collision.objectB.position += collision.normalToB * amountOfPenetration/2;

				var newVelocityA = getUpdatedBallVelocityElastic(collision.objectA, collision.objectB);
				var newVelocityB = getUpdatedBallVelocityElastic(collision.objectB, collision.objectA);

				collision.objectA.velocity = newVelocityA;
				collision.objectB.velocity = newVelocityB;

			}
		}

		foreach (var physicsObject in physicsObjects){
			physicsObject.velocity -= physicsObject.velocity * physicsObject.data.damping * timeStep;
			physicsObject.position += physicsObject.velocity * timeStep;
		}

		foreach (var physicsObject in physicsObjectsToRemove)
		{
			physicsObjects.Remove(physicsObject);
		}
		physicsObjectsToRemove.Clear();
	}

	public void resetAllVelocities()
	{
		physicsObjects.ForEach(physicsObject => physicsObject.velocity = Vector2.Zero);
	}

	public bool areAllPhysicsBodiesResting(float minVel=0.1f)
	{
		return physicsObjects.Where(physicsObject => physicsObject.velocity.Length() < minVel).Count() == physicsObjects.Count;
	}

	public float getTimeStep()
	{
		return 1.0f /tick;
	}

}
	