using System;
using System.Collections.Generic;

public static partial class ArchetypeEcs {

// Classes
///////////////////////////

public class EntityType : IEquatable<EntityType> {

  // Fields
  ///////////////////////////

  public EntityId[] ids;

  // Constructors
  ///////////////////////////

  public EntityType(params EntityId[] ids) {
    this.ids = ids;
    Array.Sort(this.ids);
  }

  public EntityType(int length) {
    ids = new EntityId[length];
  }

  // Implementations
  ///////////////////////////

  public EntityId this[int i] { get => ids[i]; }

  public int Length { get => ids.Length; }

  public override string ToString() => $"EntityType {ArrayToString(ids)}";

  public bool Equals(EntityType t) {
    if (ids.Length != t.ids.Length) return false;
    for (var i = 0; i < ids.Length; i++) {
      if (ids[i] != t.ids[i]) return false;
    }
    return true;
  }

  public override int GetHashCode() {
    var h = 19;
    for (var i = 0; i < ids.Length; i++) {
      h *= 31;
      h += ids[i].GetHashCode();
    }
    return h;
  }

  public static bool operator==(EntityType a, EntityType b) => a.Equals(b);

  public static bool operator!=(EntityType a, EntityType b) => !a.Equals(b);

  public override bool Equals(object o) => (o is EntityType t) ? Equals(t) : false;

}

// Public methods
///////////////////////////

public static bool Has(this EntityType t, EntityId component) {
  for (var i = 0; i < t.Length; i++) {
    if (t[i] == component) return true;
  }
  return false;
}

public static bool HasAll(this EntityType t, EntityType other) {
  if (other.Length > t.Length) return false;
  var matches = 0;
  var offset = 0;
  for (var i = 0; i < t.ids.Length; i++) {
    if (t[i] == other[i + offset]) {
      matches++;
    } else {
      offset--;
    }
    if (matches == other.Length) return true;
  }
  return false;
}

}
