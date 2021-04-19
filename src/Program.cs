using System;
using System.Collections.Generic;

using static ArchetypeEcs;

public static class Program {

  public struct Statistics {
    public float totalHealing;
    public float totalDamage;
  }

  public struct Vec2 {
    public static Vec2 zero = new Vec2(0, 0);
    public static Vec2 one = new Vec2(1, 1);
    public int x;
    public int y;
    public int w { get => x; set => x = value; }
    public int h { get => y; set => y = value; }
    public Vec2(int x, int y) { this.x = x; this.y = y; }
    public static Vec2 operator+(Vec2 a, Vec2 b) => new Vec2(a.x + b.x, a.y + b.y);
    public static Vec2 operator-(Vec2 a, Vec2 b) => new Vec2(a.x - b.x, a.y - b.y);
    public static Vec2 operator/(Vec2 a, float b) => new Vec2((int)(a.x / b), (int)(a.y / b));
    public static bool operator==(Vec2 a, Vec2 b) => a.x == b.x && a.y == b.y;
    public static bool operator!=(Vec2 a, Vec2 b) => !(a == b);
    public override bool Equals(object o) => (o is Vec2 v) ? this == v : false;
    public override int GetHashCode() => (x << 2) ^ y;
  }

  public struct Health {
    public float current;
    public float delta;
    public float max;
  }

  public struct Position {
    public bool changed;
    public Vec2 position;
  }

  public struct Faction {
    public int value;
  }

  static void Main(string[] args) {
    try {
      var ecs = new Ecs();
      var healthComponent = ecs.RegisterDataType<Health>();
      var positionComponent = ecs.RegisterDataType<Position>();
      var factionComponent = ecs.RegisterDataType<Faction>();
      var tagComponent = ecs.CreateEntity();
      Console.WriteLine($"health:   {healthComponent}");
      Console.WriteLine($"position: {positionComponent}");
      Console.WriteLine($"faction:  {factionComponent}");
      Console.WriteLine($"tag:      {tagComponent}");
      var entityA = ecs.CreateEntity();
      var entityB = ecs.CreateEntity();
      var entityC = ecs.CreateEntity();
      Console.WriteLine($"entityA: {entityA}");
      Console.WriteLine($"entityB: {entityB}");
      Console.WriteLine($"entityC: {entityC}");
      ecs.RegisterResource<Statistics>();
      ecs.RegisterSystem(typeof(Program), nameof(DisplayStatistics));
      ecs.RegisterSystem(typeof(Program), nameof(DisplayHealthSystem));
      ecs.RegisterSystem(typeof(Program), nameof(HealthSystem));
      var sb = new System.Text.StringBuilder();
      Action<string> print = msg => {
        sb.Clear();
        sb.Append("===");
        sb.AppendLine(msg);
        var index = ecs.GetInternals().GetIndex();
        for (var i = 0; i < index.archetypes.Count; i++) {
          sb.Append("  ");
          sb.AppendLine(index.archetypes[i].ToString());
        }
        sb.Replace(healthComponent.ToString(), "health");
        sb.Replace(positionComponent.ToString(), "position");
        sb.Replace(factionComponent.ToString(), "faction");
        sb.Replace(tagComponent.ToString(), "tag");
        sb.Replace(entityA.ToString(), "entityA");
        sb.Replace(entityB.ToString(), "entityB");
        sb.Replace(entityC.ToString(), "entityC");
        Console.Write(sb.ToString());
      };
      print("INITIAL");
      ecs.AddComponent(entityA, new Health{
        current = 80,
        max = 100,
        delta = -20,
      });
      ecs.AddComponent(entityC, new Health{
        current = 5,
        max = 120,
        delta = 32,
      });
      print("SETUP A AND C");
      Console.WriteLine("STEP");
      ecs.Step();
      Console.WriteLine("STEP");
      ecs.Step();
      ecs.AddComponent(entityA, tagComponent);
      print("ADD A TAG");
      ecs.AddComponents(entityB,
        new Health{
          current = 100,
          max = 100,
          delta = -5,
        },
        new Position{
          position = new Vec2(10, -3),
        },
        new Faction{
          value = 12,
        }
      );
      print("SETUP B");
      Console.WriteLine("STEP");
      ecs.Step();
      ecs.RemoveComponents(entityB, positionComponent);
      print("REMOVE B POSITION");
      Console.WriteLine("STEP");
      ecs.Step();
      ecs.RemoveComponents(entityB, factionComponent, healthComponent);
      print("REMOVE B FACTION AND HEALTH");
      Console.WriteLine("STEP");
      ecs.Step();
      ecs.DestroyEntity(entityC);
      print("DESTROY C");
      Console.WriteLine("STEP");
      ecs.Step();
    } catch (Exception e) {
      Console.WriteLine(e);
    }
  }

  static void DisplayStatistics(Statistics stats) {
    Console.WriteLine($"  healing: {stats.totalHealing}");
    Console.WriteLine($"  damage: {stats.totalDamage}");
  }

  static void ResetStatistics(ref Statistics stats) {
    stats.totalHealing = 0;
    stats.totalDamage = 0;
  }

  static void DisplayHealthSystem(Health health) {
    Console.WriteLine($"  {health.current}/{health.max} ({health.delta})");
  }

  static void HealthSystem(ref Statistics statistics, ref Health health) {
    if (health.delta > 0) {
      statistics.totalHealing += health.delta;
    }
    if (health.delta < 0) {
      statistics.totalDamage -= health.delta;
    }
    health.current = Math.Max(0, Math.Min(health.current + health.delta, health.max));
    health.delta = 0;
  }

}
