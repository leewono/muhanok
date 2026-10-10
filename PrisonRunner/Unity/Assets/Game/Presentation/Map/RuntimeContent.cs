using System;
using System.Collections.Generic;
using Muhanok.Domain;
using UnityEngine;

namespace Muhanok.Presentation.Map
{
    public interface IRunnerAnimated { void Preview(float runnerZ); }

    public sealed class ItemPickup : MonoBehaviour
    {
        public ItemKind Kind;
        public bool Taken { get; private set; }
        public void ResetPickup() { Taken = false; }
        public void Evaluate(RunSession run, float previousZ)
        {
            if (Taken || !gameObject.activeSelf || run.Flow.State != GameFlowState.Running || run.Paused) return;
            float z = transform.position.z;
            if (previousZ > z || run.Runner.Z < z) return;
            RouteSurface.Sample(run.Runner.Z, out float x, out _);
            if (Mathf.Abs(run.Runner.WorldLaneOffset + x - transform.position.x) > 1f) return;
            Taken = true; run.Items.Activate(Kind); gameObject.SetActive(false);
        }
        private void Update() { transform.Rotate(0, Time.deltaTime * 120, 0, Space.Self); }
    }

    // Giraffe neck sweeps down across all lanes (slide under it); elephant trunk lies across one lane (jump it).
    public sealed class ZooHazard : MonoBehaviour, IRunnerAnimated
    {
        public bool Giraffe;
        public Transform Moving;
        public ObstacleController[] Hazards;
        public void Preview(float runnerZ)
        {
            if (Moving == null) return;
            float t = Mathf.InverseLerp(26f, 9f, transform.position.z - runnerZ);
            if (Giraffe) Moving.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(8f, 90f, Mathf.SmoothStep(0, 1, t)));
            else Moving.localRotation = Quaternion.Euler(0, Mathf.Sin(Time.time * 3f) * 10f * (1 - t), Mathf.Lerp(-55f, 0f, Mathf.SmoothStep(0, 1, t)));
        }
    }

    // Places items and zoo animals into walking chunks at runtime, from pools, without touching the authored prefabs.
    public sealed class RuntimeContent : ChunkSpawner.IChunkDecorator
    {
        private readonly Transform root;
        private readonly Func<List<ItemKind>> availableItems;
        private readonly float itemChance;
        private readonly Dictionary<ItemKind, Stack<ItemPickup>> itemPool = new Dictionary<ItemKind, Stack<ItemPickup>>();
        private readonly Stack<ZooHazard> giraffes = new Stack<ZooHazard>(), elephants = new Stack<ZooHazard>();
        private readonly Dictionary<MuhanokMapChunk, List<Component>> placed = new Dictionary<MuhanokMapChunk, List<Component>>();
        private static readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();
        public RuntimeContent(Transform parent, Func<List<ItemKind>> items, float chance)
        {
            root = new GameObject("RuntimeContent").transform; root.SetParent(parent, false);
            availableItems = items; itemChance = chance;
        }

        public void Decorate(MuhanokMapChunk chunk, int sequence, int type, System.Random random)
        {
            bool cart = type == 1 || type == 2;
            if (sequence == 0 || cart) return;
            var list = new List<Component>(); placed[chunk] = list;
            float start = chunk.Entry.position.z;
            // Zoo animals: one per zoo chunk, in a stretch with no authored hazard nearby.
            if (type == 11 || type == 12)
            {
                float z = FreeZ(chunk, start, random, 5f);
                if (!float.IsNaN(z))
                {
                    var animal = random.NextDouble() < .5 ? Giraffe(z) : Elephant(z, random.Next(3) - 1);
                    animal.transform.SetParent(chunk.transform, true);
                    chunk.DynamicObstacles.AddRange(animal.Hazards); chunk.Animated.Add(animal); list.Add(animal);
                }
            }
            var kinds = availableItems();
            if (kinds.Count > 0 && random.NextDouble() < itemChance)
            {
                float z = FreeZ(chunk, start, random, 3f);
                if (!float.IsNaN(z))
                {
                    var pickup = Item(kinds[random.Next(kinds.Count)], z, random.Next(3) - 1);
                    pickup.transform.SetParent(chunk.transform, true);
                    chunk.Pickups.Add(pickup); list.Add(pickup);
                }
            }
        }

        public void Release(MuhanokMapChunk chunk)
        {
            chunk.DynamicObstacles.Clear(); chunk.Pickups.Clear(); chunk.Animated.Clear();
            if (!placed.TryGetValue(chunk, out var list)) return;
            foreach (var item in list)
            {
                item.gameObject.SetActive(false); item.transform.SetParent(root, true);
                if (item is ItemPickup pickup) Pool(pickup.Kind).Push(pickup);
                else if (item is ZooHazard animal) (animal.Giraffe ? giraffes : elephants).Push(animal);
            }
            placed.Remove(chunk);
        }

        // A z inside the chunk at least `clearance` metres from every active authored hazard.
        private static float FreeZ(MuhanokMapChunk chunk, float start, System.Random random, float clearance)
        {
            for (int attempt = 0; attempt < 6; attempt++)
            {
                float z = start + 4f + (float)random.NextDouble() * 16f;
                if (Clear(chunk.Obstacles, z, clearance) && Clear(chunk.ExerciseObstacles, z, clearance) && Clear(chunk.DynamicObstacles, z, clearance)
                    && ClearItems(chunk.FloorItems, z, clearance) && ClearGates(chunk, z, clearance)) return z;
            }
            return float.NaN;
        }
        private static bool Clear(IEnumerable<ObstacleController> hazards, float z, float clearance)
        {
            if (hazards == null) return true;
            foreach (var hazard in hazards) if (hazard != null && hazard.gameObject.activeSelf && Mathf.Abs(hazard.transform.position.z - z) < clearance) return false;
            return true;
        }
        private static bool ClearItems(FloorItemController[] items, float z, float clearance)
        {
            if (items == null) return true;
            foreach (var item in items) if (item != null && item.gameObject.activeSelf && Mathf.Abs(item.transform.position.z - z) < clearance) return false;
            return true;
        }
        private static bool ClearGates(MuhanokMapChunk chunk, float z, float clearance)
        {
            if (chunk.Gates != null) foreach (var gate in chunk.Gates) if (gate != null && Mathf.Abs(gate.transform.position.z - z) < clearance + 2f) return false;
            if (chunk.SlideGates != null) foreach (var gate in chunk.SlideGates) if (gate != null && Mathf.Abs(gate.transform.position.z - z) < clearance + 2f) return false;
            return true;
        }

        private static Vector3 LanePoint(int lane, float z, float height)
        {
            RouteSurface.Sample(z, out float x, out float y);
            return new Vector3(lane * 2.2f + x, y + height, z);
        }

        private Stack<ItemPickup> Pool(ItemKind kind) { if (!itemPool.TryGetValue(kind, out var stack)) itemPool[kind] = stack = new Stack<ItemPickup>(); return stack; }
        private ItemPickup Item(ItemKind kind, float z, int lane)
        {
            var stack = Pool(kind);
            var pickup = stack.Count > 0 ? stack.Pop() : BuildItem(kind);
            pickup.transform.position = LanePoint(lane, z, 1.1f); pickup.ResetPickup(); pickup.gameObject.SetActive(true);
            return pickup;
        }
        private ItemPickup BuildItem(ItemKind kind)
        {
            var node = new GameObject("Item_" + kind); node.transform.SetParent(root, false);
            var pickup = node.AddComponent<ItemPickup>(); pickup.Kind = kind;
            Color color = kind == ItemKind.Magnet ? new Color(.85f, .15f, .15f) : kind == ItemKind.DoubleScore ? new Color(1f, .78f, .1f) : new Color(.2f, .6f, 1f);
            Part(PrimitiveType.Sphere, node.transform, Vector3.zero, Vector3.one * .75f, new Color(color.r, color.g, color.b) * .55f + Color.white * .3f);
            if (kind == ItemKind.Magnet)
            {
                Part(PrimitiveType.Cube, node.transform, new Vector3(-.2f, .05f, 0), new Vector3(.14f, .5f, .14f), color);
                Part(PrimitiveType.Cube, node.transform, new Vector3(.2f, .05f, 0), new Vector3(.14f, .5f, .14f), color);
                Part(PrimitiveType.Cube, node.transform, new Vector3(0, -.22f, 0), new Vector3(.54f, .14f, .14f), color);
            }
            else if (kind == ItemKind.DoubleScore)
            {
                Part(PrimitiveType.Cylinder, node.transform, Vector3.zero, new Vector3(.55f, .06f, .55f), color, Quaternion.Euler(90, 0, 0));
            }
            else Part(PrimitiveType.Cube, node.transform, Vector3.zero, new Vector3(.42f, .52f, .12f), color);
            return pickup;
        }

        private ZooHazard Giraffe(float z)
        {
            var animal = giraffes.Count > 0 ? giraffes.Pop() : BuildGiraffe();
            PlaceAnimal(animal, z, true); return animal;
        }
        private ZooHazard Elephant(float z, int lane)
        {
            var animal = elephants.Count > 0 ? elephants.Pop() : BuildElephant();
            PlaceAnimal(animal, z, false, lane); return animal;
        }
        private static void PlaceAnimal(ZooHazard animal, float z, bool fullRow, int lane = 0)
        {
            RouteSurface.Sample(z, out float x, out float y);
            animal.transform.position = new Vector3(x, y, z);
            for (int i = 0; i < animal.Hazards.Length; i++)
            {
                var hazard = animal.Hazards[i];
                int hazardLane = fullRow ? i - 1 : lane;
                hazard.transform.position = new Vector3(hazardLane * 2.2f + x, y, z);
                hazard.FullExerciseRow = fullRow; hazard.ResetPass(); hazard.gameObject.SetActive(true);
            }
            if (!fullRow && animal.Moving != null)
            {
                // The trunk reaches from the right-hand enclosure into the chosen lane.
                animal.Moving.parent.localPosition = new Vector3(4.6f, 0, 0);
                float length = 4.6f - lane * 2.2f + 1f;
                var trunk = animal.Moving.GetChild(0);
                trunk.localScale = new Vector3(.45f, length * .5f, .45f);
                trunk.localPosition = new Vector3(-length * .5f, 0, 0);
            }
            animal.Preview(-1000f); animal.gameObject.SetActive(true);
        }
        private ZooHazard BuildGiraffe()
        {
            var node = new GameObject("Zoo_Giraffe"); node.transform.SetParent(root, false);
            var animal = node.AddComponent<ZooHazard>(); animal.Giraffe = true;
            var body = new GameObject("Body").transform; body.SetParent(node.transform, false); body.localPosition = new Vector3(5.2f, 0, 0);
            Color yellow = new Color(.95f, .74f, .3f), brown = new Color(.45f, .27f, .12f);
            Part(PrimitiveType.Capsule, body, new Vector3(0, 2.2f, 0), new Vector3(1.3f, 1.1f, 2.4f), yellow, Quaternion.Euler(90, 0, 0));
            for (int i = 0; i < 4; i++) Part(PrimitiveType.Cylinder, body, new Vector3(i < 2 ? -.4f : .4f, 1f, i % 2 == 0 ? -.8f : .8f), new Vector3(.22f, 1f, .22f), yellow);
            for (int i = 0; i < 5; i++) Part(PrimitiveType.Sphere, body, new Vector3(.62f * (i % 2 == 0 ? 1 : -1), 2.3f + (i % 3) * .2f, -.9f + i * .4f), new Vector3(.08f, .35f, .35f), brown);
            // The neck pivots at the shoulder and lowers across all three lanes to slide height.
            var pivot = new GameObject("NeckPivot").transform; pivot.SetParent(body, false); pivot.localPosition = new Vector3(-.4f, 1.45f, 0);
            Part(PrimitiveType.Cylinder, pivot, new Vector3(0, 3.6f, 0), new Vector3(.38f, 3.6f, .38f), yellow);
            Part(PrimitiveType.Cube, pivot, new Vector3(0, 7.4f, 0), new Vector3(.55f, .6f, 1f), yellow);
            Part(PrimitiveType.Cylinder, pivot, new Vector3(0, 7.85f, .2f), new Vector3(.08f, .25f, .08f), brown);
            Part(PrimitiveType.Cylinder, pivot, new Vector3(0, 7.85f, -.2f), new Vector3(.08f, .25f, .08f), brown);
            for (int i = 0; i < 4; i++) Part(PrimitiveType.Sphere, pivot, new Vector3(.2f, 1.5f + i * 1.4f, 0), new Vector3(.1f, .3f, .3f), brown);
            animal.Moving = pivot; animal.Hazards = Hazards(node.transform, ObstacleKind.Pipe, 3);
            return animal;
        }
        private ZooHazard BuildElephant()
        {
            var node = new GameObject("Zoo_Elephant"); node.transform.SetParent(root, false);
            var animal = node.AddComponent<ZooHazard>();
            var body = new GameObject("Body").transform; body.SetParent(node.transform, false); body.localPosition = new Vector3(4.6f, 0, 0);
            Color grey = new Color(.55f, .57f, .6f), tusk = new Color(.95f, .93f, .85f);
            Part(PrimitiveType.Sphere, body, new Vector3(2.4f, 1.9f, 0), new Vector3(3.2f, 2.3f, 2.4f), grey);
            Part(PrimitiveType.Sphere, body, new Vector3(.6f, 2.4f, 0), new Vector3(1.6f, 1.5f, 1.5f), grey);
            Part(PrimitiveType.Cube, body, new Vector3(.9f, 2.5f, .95f), new Vector3(.15f, 1.2f, 1f), grey * .9f);
            Part(PrimitiveType.Cube, body, new Vector3(.9f, 2.5f, -.95f), new Vector3(.15f, 1.2f, 1f), grey * .9f);
            Part(PrimitiveType.Cylinder, body, new Vector3(-.1f, 1.8f, .4f), new Vector3(.12f, .5f, .12f), tusk, Quaternion.Euler(0, 0, 70));
            Part(PrimitiveType.Cylinder, body, new Vector3(-.1f, 1.8f, -.4f), new Vector3(.12f, .5f, .12f), tusk, Quaternion.Euler(0, 0, 70));
            for (int i = 0; i < 4; i++) Part(PrimitiveType.Cylinder, body, new Vector3(i < 2 ? 1.6f : 3.2f, .7f, i % 2 == 0 ? -.7f : .7f), new Vector3(.6f, .7f, .6f), grey);
            // The trunk lies along the ground into the lane: jump over it.
            var pivot = new GameObject("TrunkPivot").transform; pivot.SetParent(body, false); pivot.localPosition = new Vector3(0, .35f, 0);
            var trunk = Part(PrimitiveType.Cylinder, pivot, new Vector3(-2f, 0, 0), new Vector3(.45f, 2f, .45f), grey, Quaternion.Euler(0, 0, 90));
            trunk.name = "Trunk";
            animal.Moving = pivot; animal.Hazards = Hazards(node.transform, ObstacleKind.Crate, 1);
            return animal;
        }
        private static ObstacleController[] Hazards(Transform parent, ObstacleKind kind, int count)
        {
            var result = new ObstacleController[count];
            for (int i = 0; i < count; i++)
            {
                var hazard = new GameObject("Hazard_" + kind + "_" + i).AddComponent<ObstacleController>();
                hazard.transform.SetParent(parent, false); hazard.Kind = kind; result[i] = hazard;
            }
            return result;
        }
        private static Transform Part(PrimitiveType shape, Transform parent, Vector3 position, Vector3 scale, Color color, Quaternion? rotation = null)
        {
            var part = GameObject.CreatePrimitive(shape);
            var collider = part.GetComponent<Collider>(); if (collider != null) UnityEngine.Object.Destroy(collider);
            part.transform.SetParent(parent, false); part.transform.localPosition = position; part.transform.localScale = scale;
            part.transform.localRotation = rotation ?? Quaternion.identity;
            part.GetComponent<Renderer>().sharedMaterial = Material(color);
            return part.transform;
        }
        private static Material Material(Color color)
        {
            if (materials.TryGetValue(color, out var material) && material != null) return material;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader) { color = color };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .25f);
            materials[color] = material; return material;
        }
    }
}
