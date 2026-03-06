using System;
using System.Collections.Generic;
using Dq99.Prototype.Domain;
using UnityEngine;

namespace Dq99.Prototype.Unity
{
    public static class PrototypeSceneAuthoring
    {
        public static PrototypeSceneBindings CollectBindings(PrototypeContent content)
        {
            var bindings = new PrototypeSceneBindings();
            foreach (var marker in UnityEngine.Object.FindObjectsOfType<SceneMarker>())
            {
                if (marker == null || string.IsNullOrEmpty(marker.MarkerId))
                {
                    continue;
                }

                bindings.MarkerPositions[marker.MarkerId] = new Float2(marker.transform.position.x, marker.transform.position.z);
            }

            if (!string.IsNullOrEmpty(content.playerSpawnMarkerId) && bindings.MarkerPositions.TryGetValue(content.playerSpawnMarkerId, out var playerSpawn))
            {
                bindings.PlayerSpawn = playerSpawn;
            }
            else
            {
                Debug.LogWarning($"Missing SceneMarker for player spawn id '{content.playerSpawnMarkerId}'. Player will spawn at world origin.");
            }

            return bindings;
        }

        public static Dictionary<string, GameObject> EnsureActorViews(
            PrototypeContent content,
            Func<string, Color, Material> cloneMaterialWithColor)
        {
            var actorViews = new Dictionary<string, GameObject>();

            foreach (var actorMarker in UnityEngine.Object.FindObjectsOfType<ActorMarker>())
            {
                if (actorMarker == null || string.IsNullOrEmpty(actorMarker.ActorId))
                {
                    continue;
                }

                SetAllCollidersTrigger(actorMarker.gameObject);
                EnsurePresentation(actorMarker.gameObject);
                actorViews[actorMarker.ActorId] = actorMarker.gameObject;
            }

            if (content.actors == null)
            {
                return actorViews;
            }

            foreach (var actor in content.actors)
            {
                if (actor == null || string.IsNullOrEmpty(actor.id) || actorViews.ContainsKey(actor.id))
                {
                    continue;
                }

                var actorObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                actorObject.name = actor.id;
                actorObject.AddComponent<ActorMarker>().ActorId = actor.id;
                actorObject.GetComponent<Collider>().isTrigger = true;
                actorObject.GetComponent<Renderer>().sharedMaterial = cloneMaterialWithColor(actor.tint, new Color(0.94f, 0.76f, 0.38f));
                var marker = FindMarkerComponent(actor.markerId);
                if (marker != null)
                {
                    actorObject.transform.position = marker.transform.position + new Vector3(0f, 1f, 0f);
                }

                SetAllCollidersTrigger(actorObject);
                EnsurePresentation(actorObject);
                actorViews[actor.id] = actorObject;
            }

            return actorViews;
        }

        public static Dictionary<string, GameObject> EnsurePortalViews(
            PrototypeContent content,
            Func<string, Color, Material> cloneMaterialWithColor)
        {
            var portalViews = new Dictionary<string, GameObject>();

            foreach (var portalMarker in UnityEngine.Object.FindObjectsOfType<PortalMarker>())
            {
                if (portalMarker == null || string.IsNullOrEmpty(portalMarker.PortalId))
                {
                    continue;
                }

                SetAllCollidersTrigger(portalMarker.gameObject);
                EnsurePortalVisual(portalMarker.gameObject, cloneMaterialWithColor);
                portalViews[portalMarker.PortalId] = portalMarker.gameObject;
            }

            if (content.portals == null)
            {
                return portalViews;
            }

            foreach (var portal in content.portals)
            {
                if (portal == null || string.IsNullOrEmpty(portal.id) || portalViews.ContainsKey(portal.id))
                {
                    continue;
                }

                var marker = FindMarkerComponent(portal.markerId);
                if (marker == null)
                {
                    Debug.LogWarning($"Missing SceneMarker for portal id '{portal.id}' marker '{portal.markerId}'. Portal view will be placed at world origin.");
                }

                var portalObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                portalObject.name = $"portal_{portal.id}";
                portalObject.transform.position = marker != null ? marker.transform.position + new Vector3(0f, 1.1f, 0f) : new Vector3(0f, 1.1f, 0f);
                portalObject.transform.localScale = new Vector3(0.7f, 1.1f, 0.7f);
                portalObject.AddComponent<PortalMarker>().PortalId = portal.id;
                portalObject.GetComponent<Collider>().isTrigger = true;
                portalObject.GetComponent<Renderer>().sharedMaterial = cloneMaterialWithColor("#7DD3FC", new Color(0.49f, 0.83f, 0.99f));
                EnsurePortalVisual(portalObject, cloneMaterialWithColor);
                portalViews[portal.id] = portalObject;
            }

            return portalViews;
        }

        private static void SetAllCollidersTrigger(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            foreach (var collider in root.GetComponentsInChildren<Collider>(true))
            {
                collider.isTrigger = true;
            }
        }

        private static bool FindMarker(string markerId)
        {
            return FindMarkerComponent(markerId) != null;
        }

        private static void EnsurePresentation(GameObject root)
        {
            if (root == null || root.GetComponent<ActorPresentation>() != null)
            {
                return;
            }

            root.AddComponent<ActorPresentation>();
        }

        private static void EnsurePortalVisual(GameObject root, Func<string, Color, Material> cloneMaterialWithColor)
        {
            if (root == null)
            {
                return;
            }

            var visual = root.transform.Find("PortalVisual");
            if (visual != null)
            {
                return;
            }

            var visualObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visualObject.name = "PortalVisual";
            visualObject.transform.SetParent(root.transform, false);
            visualObject.transform.localPosition = Vector3.zero;
            visualObject.transform.localScale = new Vector3(0.7f, 1.1f, 0.7f);

            var collider = visualObject.GetComponent<Collider>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }

            var renderer = visualObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = cloneMaterialWithColor("#7DD3FC", new Color(0.49f, 0.83f, 0.99f));
            }
        }

        private static SceneMarker FindMarkerComponent(string markerId)
        {
            foreach (var marker in UnityEngine.Object.FindObjectsOfType<SceneMarker>())
            {
                if (marker != null && marker.MarkerId == markerId)
                {
                    return marker;
                }
            }

            return null;
        }

    }
}
