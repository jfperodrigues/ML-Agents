using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bush : MonoBehaviour
{
    [Tooltip("The color when the bush is polluted")]
    public Color fullColor = new Color(0.5f, 0f, 0.9f);

    [Tooltip("The color when the bush is empty")]
    public Color emptyColor = new Color(0.1f, 1f, 0.1f);

    /// <summary>
    /// The trigger collider representing the bush
    /// </summary>
    [HideInInspector]
    public Collider bushCollider;

    //The bush's material
    private Material bushMaterial;

    /// <summary>
    /// The center position of the bush collider
    /// </summary>
    public Vector3 BushCenterPosition
    {
        get
        {
            return bushCollider.transform.position;
        }
    }

    /// <summary>
    /// The amount of dirt remaining in the bush
    /// </summary>
    public float DirtAmount { get; private set; }

    public bool HasDirt
    {
        get
        {
            return DirtAmount > 0;
        }
    }

    /// <summary>
    /// Atempts to remove pollution from the bush
    /// </summary>
    /// <param name="amount">The ammount of pollution to remove</param>
    /// <returns>The amount successfully removed</returns>
    public float Clean(float amount, NPCAgent agent)
    {
        float dirtCleaned = Mathf.Clamp(amount, 0f, DirtAmount);

        DirtAmount -= amount;
        if (DirtAmount <= 0)
        {
            DirtAmount = 0;

            bushCollider.enabled = false;
            agent.onBushExit();

            bushMaterial.SetColor("_BaseColor", emptyColor);
        }

        return dirtCleaned;
    }

    /// <summary>
    /// Resets the bush
    /// </summary>
    public void ResetBush()
    {
        //Refill the dirt
        DirtAmount = 2f;

        //Enable the colliders
        bushCollider.gameObject.SetActive(true);
        bushCollider.enabled = true;

        //change the bush color to indicate that it is polluted
        bushMaterial.SetColor("_BaseColor", fullColor);
    }

    /// <summary>
    /// Called when the bush wakes up
    /// </summary>
    private void Awake()
    {
        //Find the bush's mesh renderer and get main material
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        bushMaterial = meshRenderer.material;
        bushCollider = transform.GetComponent<Collider>();
        DirtAmount = 2f;
    }
}
