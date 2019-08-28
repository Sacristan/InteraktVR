using UnityEngine;

public static class LayerUtils
{
    public static string PlayerLayer = "Player";
    public static string EnvironmentLayer = "Environment";

    public static bool IsPlayerLayer(this GameObject go)
    {
        return IsGameObjectLayer(go, PlayerLayer);
    }

    public static bool IsInLayerMask(this GameObject go, LayerMask layermask)
    {
        return IsInLayerMask(go.layer, layermask);
    }

    public static bool IsInLayerMask(int layer, LayerMask layermask)
    {
        return layermask == (layermask | (1 << layer));
    }

    private static bool IsGameObjectLayer(GameObject go, string layerName)
    {
        return go.layer == LayerMask.NameToLayer(layerName);
    }

    public static void SetLayerRecursively(this GameObject go, int layer)
    {
        if (go == null) return;

        go.layer = layer;

        for (int i = 0; i < go.transform.childCount; i++)
        {
            Transform child = go.transform.GetChild(i);
            if (child != null) SetLayerRecursively(child.gameObject, layer);
        }
    }

}

public static class GameUtils
{
    private const string PlayerTag = "Player";

    public static bool IsPlayer(this GameObject go)
    {
        return go.CompareTag(PlayerTag);
    }
}