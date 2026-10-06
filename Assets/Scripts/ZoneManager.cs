using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Checks each zone against tagged player positions and fades the zone material
/// when at least one player is standing inside it.
/// </summary>
public class ZoneManager : MonoBehaviour
{
    public const string PlayerTag = "Player";

    public List<Zone> zones = new List<Zone>();

    [Range(0f, 1f)]
    public float inZoneOpacity = 0.85f;

    [Range(0f, 1f)]
    public float emptyOpacity = 0.15f;

    readonly List<Transform> players = new List<Transform>();

    void Awake()
    {
        for (int i = 0; i < zones.Count; i++)
            zones[i].Initialize(emptyOpacity);
    }

    void Update()
    {
        CollectPlayers();

        for (int i = 0; i < zones.Count; i++)
            zones[i].Refresh(players, inZoneOpacity, emptyOpacity);
    }

    void OnDestroy()
    {
        for (int i = 0; i < zones.Count; i++)
            zones[i].Cleanup();
    }

    void OnValidate()
    {
        if (zones == null)
            return;

        for (int i = 0; i < zones.Count; i++)
        {
            if (zones[i] != null && zones[i].trigger != null)
                zones[i].trigger.isTrigger = true;
        }
    }

    void CollectPlayers()
    {
        players.Clear();
        GameObject[] tagged = GameObject.FindGameObjectsWithTag(PlayerTag);

        for (int i = 0; i < tagged.Length; i++)
        {
            if (tagged[i] != null)
                players.Add(tagged[i].transform);
        }
    }
}

/// <summary>
/// One trigger volume. Active while any Player position is inside the collider.
/// </summary>
[System.Serializable]
public class Zone
{
    public Collider trigger;

    [Range(0f, 1f)]
    public float opacity;

    public bool active;
    public int numPlayers;

    Material runtimeMaterial;
    Color baseColor = Color.white;
    bool ownsRuntimeMaterial;

    public int GetPlayersInZone()
    {
        return numPlayers;
    }

    public void Initialize(float emptyOpacity)
    {
        if (trigger != null)
            trigger.isTrigger = true;

        Renderer renderer = trigger != null ? trigger.GetComponent<Renderer>() : null;
        if (renderer != null && renderer.sharedMaterial != null)
        {
            runtimeMaterial = renderer.material;
            ownsRuntimeMaterial = true;
            baseColor = runtimeMaterial.color;
            MakeTransparent(runtimeMaterial);
        }

        numPlayers = 0;
        active = false;
        opacity = emptyOpacity;
        ApplyOpacity();
    }

    public void Refresh(List<Transform> players, float inZoneOpacity, float emptyOpacity)
    {
        numPlayers = 0;

        if (trigger != null && players != null)
        {
            for (int i = 0; i < players.Count; i++)
            {
                Transform player = players[i];
                if (player != null && Contains(trigger, player.position))
                    numPlayers++;
            }
        }

        active = numPlayers > 0;
        opacity = active ? inZoneOpacity : emptyOpacity;
        ApplyOpacity();
    }

    public void Cleanup()
    {
        if (ownsRuntimeMaterial && runtimeMaterial != null)
        {
            if (Application.isPlaying)
                Object.Destroy(runtimeMaterial);
            else
                Object.DestroyImmediate(runtimeMaterial);
        }

        runtimeMaterial = null;
        ownsRuntimeMaterial = false;
    }

    void ApplyOpacity()
    {
        if (runtimeMaterial == null)
            return;

        Color color = baseColor;
        color.a = opacity;
        runtimeMaterial.color = color;
    }

    static bool Contains(Collider zoneCollider, Vector3 worldPoint)
    {
        BoxCollider box = zoneCollider as BoxCollider;
        if (box != null)
        {
            Vector3 local = box.transform.InverseTransformPoint(worldPoint) - box.center;
            Vector3 half = box.size * 0.5f;
            return Mathf.Abs(local.x) <= half.x
                && Mathf.Abs(local.y) <= half.y
                && Mathf.Abs(local.z) <= half.z;
        }

        return zoneCollider.bounds.Contains(worldPoint);
    }

    static void MakeTransparent(Material mat)
    {
        if (mat == null || !mat.HasProperty("_Mode"))
            return;

        mat.SetFloat("_Mode", 3f);
        mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = (int)RenderQueue.Transparent;
    }
}
