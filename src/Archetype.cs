using System;
using System.Collections.Generic;

public static partial class ArchetypeEcs {

// Classes
///////////////////////////

public class Archetype {

  // Fields
  ///////////////////////////

  public EntityType type;

  public SparseSet<int, EntityId> entities;

  public SparseSet<EntityId, ComponentArray> data;

  public Queue<int> free;

  public SparseSet<EntityId, Archetype> edges;

  // Constructors
  ///////////////////////////

  public Archetype(EntityType type) {
    this.type = type;
    entities = new SparseSet<int, EntityId>(i => i, this.MoveDataRow);
    data = SparseEntitySet<ComponentArray>();
    free = new Queue<int>();
    edges = SparseEntitySet<Archetype>();
  }

  // Implementations
  ///////////////////////////

  public override string ToString() =>  $"Archetype {type} {ArrayToString(entities.values, entities.Size())} {ArrayToString(data.values, data.Size())}";

}

// Public methods
///////////////////////////

public static EntityId GetEntity(this Archetype a, int row) {
  return a.entities.Get(row);
}

public static ref T GetData<T>(this Archetype a, int row, EntityId component) {
  return ref a.data.Get(component).As<T>().Get(row);
}

// Internal methods
///////////////////////////

static ComponentArray GetDataSet(this Archetype a, EntityId component, Type dataType) {
  if (a.data.Has(component)) return a.data.Get(component);
  var arrayType = typeof(ComponentArray<>).MakeGenericType(new Type[] { dataType });
  var componentArray = (ComponentArray)Activator.CreateInstance(arrayType);
  a.data.Add(component, componentArray);
  return componentArray;
}

static void MoveDataRow(this Archetype a, int from, int to) {
  for (var i = 0; i < a.type.Length; i++) {
    if (!a.data.Has(a.type[i])) continue;
    var dataSet = a.data.Get(a.type[i]);
    var obj = dataSet.GetObj(from);
    dataSet.SetObj(to, obj);
  }
}

}
