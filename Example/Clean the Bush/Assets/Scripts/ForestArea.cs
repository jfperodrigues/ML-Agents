using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForestArea : MonoBehaviour
{
    //Diameter of the are where the agent and the bushes can be, used for relateve distance
    public const float AreaDiameter = 50f;
    public Camera cam;
    public List<Bush> bushes { get; private set; }

    private Dictionary<Collider, Bush> bushDictionary;

    public void ResetBushes()
    {
        foreach (Bush b in bushes)
        {
            bool safePos = false;
            float xPos = 0f, zPos = 0f;
            while (!safePos)
            {
                float limx = transform.position.x;
                float limz = transform.position.z;
                xPos = UnityEngine.Random.Range(limx-018f, limx+018f);
                zPos = UnityEngine.Random.Range(limz-018f, limz+018f);
                Collider[] colls = Physics.OverlapSphere(new Vector3(xPos, 0.56f/4, zPos),2.5f);
                safePos = colls.Length <= 1;
            }
            b.transform.position = new Vector3(xPos, 0.56f / 4, zPos);

            b.ResetBush();
        }
    }

    public Bush GetBushFromColl(Collider collider)
    {
        return bushDictionary[collider];
    }

    private void Awake()
    {
        bushes = new List<Bush>();
        bushDictionary = new Dictionary<Collider, Bush>();
    }

    private void Start()
    {
        FindChildBushes(transform);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
            cam.depth *= -1;
    }
    private void FindChildBushes(Transform parent)
    {
        for(int i = 0; i<parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);

            if (child.CompareTag("Bush"))
            {
                Bush b = child.GetComponent<Bush>();
                if (b != null)
                {
                    bushes.Add(b);
                    bushDictionary.Add(b.bushCollider, b);
                }
                else
                {
                    FindChildBushes(child);
                }
            }
            else
            {
                FindChildBushes(child);
            }
        }
    }
}
