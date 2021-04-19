using System;
using System.Collections.Generic;

public static partial class ArchetypeEcs {

// Classes
///////////////////////////

public class EntityIndex {

  // Fields
  ///////////////////////////

  public Archetype root;

  public List<Archetype> archetypes;

  public SparseSet<EntityId, Record> index;

  public SparseSet<EntityId, Type> dataTypes;

  // Constructors
  ///////////////////////////

  public EntityIndex() {
    root = new Archetype(new EntityType());
    archetypes = new List<Archetype>();
    index = SparseEntitySet<Record>();
    dataTypes = SparseEntitySet<Type>();
    archetypes.Add(root);
  }

}

public class Record {

  // Fields
  ///////////////////////////

  public Archetype archetype;

  public int row;

  // Constructors
  ///////////////////////////

  public Record() {
    archetype = null;
    row = -1;
  }

  // Implementations
  ///////////////////////////

  public override string ToString() =>  $"Record {archetype} {row}";

}

// Public methods
///////////////////////////

public static void Register(this EntityIndex ei, EntityId id, Type dataType) {
  ei.dataTypes.Add(id, dataType);
}

public static void Add(
  this EntityIndex ei,
  EntityId id,
  params (EntityId, object)[] components
) {
  // Look up the entity record (or start tracking it)
  var record = default(Record);
  if (ei.index.Has(id)) {
    record = ei.index.Get(id);
  } else {
    record = new Record();
    record.archetype = ei.root;
    ei.index.Add(id, record);
  }
  // Follow the archetype graph to the new archetype
  var newArchetype = record.archetype;
  for (var i = 0; i < components.Length; i++) {
    var (component, _) = components[i];
    if (newArchetype.edges.Has(component)) {
      newArchetype = newArchetype.edges.Get(component);
    } else {
      var types = new EntityId[newArchetype.type.Length + 1];
      for (var j = 0; j < newArchetype.type.Length; j++) {
        types[j] = newArchetype.type[j];
      }
      types[types.Length - 1] = component;
      var newType = new EntityType(types);
      var nextArchetype = ei.FindArchetype(newType);
      newArchetype.edges.Add(component, nextArchetype);
      newArchetype = nextArchetype;
    }
  }
  // Move existing components to new archetype
  var newRow = ei.MoveRecord(id, record, newArchetype);
  // Add new components to new archetype
  for (var i = 0; i < components.Length; i++) {
    var (component, data) = components[i];
    if (data == null) {
      if (ei.dataTypes.Has(component)) {
        throw new Exception($"component should have had data of type {ei.dataTypes.Get(component).FullName} but was null");
      }
      continue;
    }
    var dataType = ei.dataTypes.Get(component);
    if (!dataType.IsInstanceOfType(data)) {
        throw new Exception($"component should have had data of type {dataType.FullName} but was {data.GetType().FullName}");
    }
    newArchetype.GetDataSet(component, dataType).SetObj(newRow, data);
  }
  // Update the record to point at the new archetype
  record.archetype = newArchetype;
  record.row = newRow;
}

public static void Remove(
  this EntityIndex ei,
  EntityId id,
  params EntityId[] components
) {
  var record = ei.index.Get(id);
  // Follow the archetype graph to the new archetype
  var newArchetype = record.archetype;
  for (var i = 0; i < components.Length; i++) {
    var component = components[i];
    if (newArchetype.edges.Has(component)) {
      newArchetype = newArchetype.edges.Get(component);
    } else {
      var types = new EntityId[newArchetype.type.Length - 1];
      var offset = 0;
      for (var j = 0; j < newArchetype.type.Length; j++) {
        var type = newArchetype.type[j];
        if (type == component) {
          offset--;
          continue;
        }
        types[j + offset] = type;
      }
      var newType = new EntityType(types);
      var nextArchetype = ei.FindArchetype(newType);
      newArchetype.edges.Add(component, nextArchetype);
      newArchetype = nextArchetype;
    }
  }
  // Move existing components to new archetype
  var newRow = ei.MoveRecord(id, record, newArchetype);
  // Update the record to point at the new archetype
  record.archetype = newArchetype;
  record.row = newRow;
}

public static void RemoveEntity(
  this EntityIndex ei,
  EntityId id
) {
  var record = ei.index.Get(id);
  // Move to the root archetype to clear component data
  var newRow = ei.MoveRecord(id, record, ei.root);
  // Remove the record from the root archetype
  ei.root.free.Enqueue(newRow);
  ei.root.entities.Remove(newRow);
}

public static ref T Get<T>(
  this EntityIndex ei,
  EntityId id,
  EntityId component
) {
  var record = ei.index.Get(id);
  var dataSet = record.archetype.data.Get(component).As<T>();
  return ref dataSet.Get(record.row);
}

public static void Each(
  this EntityIndex ei,
  EntityType type,
  Action<Archetype, int> action
) {
  for (var i = 0; i < ei.archetypes.Count; i++) {
    var archetype = ei.archetypes[i];
    if (!archetype.type.HasAll(type)) continue;
    var size = archetype.entities.Size();
    for (var j=0; j < size; j++) {
      action(archetype, archetype.entities.At(j));
    }
  }
}

public static IEnumerable<(Archetype, int)> All(
  this EntityIndex ei,
  EntityType type
) {
  for (var i = 0; i < ei.archetypes.Count; i++) {
    var archetype = ei.archetypes[i];
    if (!archetype.type.HasAll(type)) continue;
    var size = archetype.entities.Size();
    for (var j=0; j < size; j++) {
      yield return (archetype, archetype.entities.At(j));
    }
  }
}

// Internal methods
///////////////////////////

static Archetype FindArchetype(this EntityIndex ei, EntityType type) {
  if (type.Length == 0) {
    return ei.root;
  }
  for (var i=0; i < ei.archetypes.Count; i++) {
    var archetype = ei.archetypes[i];
    if (archetype.type == type) {
      return archetype;
    }
  }
  var newArchetype = new Archetype(type);
  ei.archetypes.Add(newArchetype);
  return newArchetype;
}

static int MoveRecord(
  this EntityIndex ei,
  EntityId id,
  Record record,
  Archetype to
) {
  var newRow = -1;
  if (to.free.Count > 0) {
    newRow = to.free.Dequeue();
  } else {
    newRow = to.entities.Size();
  }
  to.entities.Add(newRow, id);
  if (record.archetype != ei.root) {
    for (var i = 0; i < record.archetype.type.Length; i++) {
      var type = record.archetype.type[i];
      if (!to.type.Has(type)) continue;
      var dataType = ei.dataTypes.Get(type);
      var dataSet = record.archetype.GetDataSet(type, dataType);
      var data = dataSet.GetObj(record.row);
      to.GetDataSet(type, dataType).SetObj(newRow, data);
    }
    record.archetype.free.Enqueue(record.row);
    record.archetype.entities.Remove(record.row);
  }
  return newRow;
}

}
