using System;
using System.Collections.Generic;

public static partial class ArchetypeEcs {

// Constants
///////////////////////////

const int IDX_BITS = 24;
const uint IDX_MASK = (1 << IDX_BITS) - 1;
const int GEN_BITS = 8;
const uint GEN_MASK = (1 << GEN_BITS) - 1;

// Structs
///////////////////////////

public struct EntityId : IEquatable<EntityId>, IComparable<EntityId> {

  // Fields
  ///////////////////////////

  public uint id;

  // Implementations
  ///////////////////////////

  public override string ToString() => $"EntityId {id}";

  public int CompareTo(EntityId e) => id.CompareTo(e.id);

  public bool Equals(EntityId e) => id == e.id;

  public override int GetHashCode() => id.GetHashCode();

  public static bool operator==(EntityId a, EntityId b) => a.Equals(b);

  public static bool operator!=(EntityId a, EntityId b) => !a.Equals(b);

  public override bool Equals(object o) => (o is EntityId e) ? Equals(e) : false;

}

// Public methods
///////////////////////////

public static SparseSet<EntityId, V> SparseEntitySet<V>() {
  return new SparseSet<EntityId, V>(Index);
}

// Internal methods
///////////////////////////

static EntityId NewEntityId(int index, uint generation) {
  return new EntityId{ id = ~(((uint)index << IDX_BITS) | generation) };
}

static int Index(this EntityId e) {
  return (int)(( ~e.id >> IDX_BITS) & IDX_MASK);
}

static uint Generation(this EntityId e) {
  return ~e.id & GEN_MASK;
}

}
