using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterManager : MonoBehaviour
{
    [Header("Water Pick Ups")]
    [Tooltip("Number of waters picked up")]
    public int WaterBalance;

    public LayerMask RaycastLayers;

    [Header("Water Pick Ups")]
    [Tooltip("Water Pick Up prefab")]
    public GameObject WaterPickUpPrefab;

    [Tooltip("Starting scale for both player controler and water pick ups")]
    public float StartScale = 0.1f;

    [Tooltip("The number of available pick ups in the scene")]
    public int PickUpPopulation = 200;

    [Header("Scale")]
    public int XMin = 0;

    public int XMax = 0;
    public int ZMin = 0;
    public int ZMax = 0;

    //private variables
    private GameObject[] _pickUps;

    private int _instantiateIndex = 0;
    private List<int> _pickUpIndices;
    private float Y = 1000;
    private Vector3 _scale;

    // Start is called before the first frame update
    private void Start()
    {
        _pickUps = new GameObject[PickUpPopulation];
        _pickUpIndices = new List<int>();
        //set up instantiation height

        if (!GetComponent<Player>()) return;

        Y = GetComponent<Player>().GetMaxHeight();
        _scale = new Vector3(StartScale, StartScale, StartScale);
        GetComponent<Player>().gameObject.transform.localScale = _scale;
    }

    // Update is called once per frame
    private void Update()
    {
        //scale the player after pickups
        if (transform.localScale != _scale)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, _scale, Time.deltaTime / 2);
        }

        //while the list isn't full we instantiate pickups
        if (_instantiateIndex >= PickUpPopulation) return;
        GameObject pickUp = InstiantiatePickUp();
        if (pickUp == null) return;
        pickUp.GetComponent<WaterPickUp>().SetIndex(_instantiateIndex);
        _pickUps[_instantiateIndex] = pickUp;
        _instantiateIndex++;
    }

    public GameObject InstiantiatePickUp()
    {
        Vector3 position = DropPoint();
        GameObject pickup = Instantiate(WaterPickUpPrefab, position, Quaternion.identity, null);
        pickup.transform.localScale = new Vector3(StartScale, StartScale, StartScale);
        return pickup;
    }

    private void OnCollisionEnter(Collision collision)
    {
        //if it isn't a pick up, we don't care
        if (collision.gameObject.tag != "PickUp") return;
        //if already picked up, don't do anything
        WaterPickUp pickup = collision.gameObject.GetComponent<WaterPickUp>();
        if (pickup.PickUpStatus()) return;

        //pick up droplet
        _pickUpIndices.Add(pickup.GetIndex());
        pickup.PickUp();
        WaterBalance++;

        //increase the scale of the object
        _scale.x += StartScale;
        _scale.y += StartScale;
        _scale.z += StartScale;
    }

    public void DropWater()
    {
        //test if below cloud is appropriate drop position (i.e. it is a piece of terrain)
        Vector3 rayStart = transform.position; //Add ignore masks for future only testing terrain
        rayStart.y = Y;

        RaycastHit hit;
        if (!Physics.Raycast(rayStart, Vector3.down, out hit, RaycastLayers)) return; //if nothing is hit we return zero

        if (hit.transform.gameObject.tag != "Terrain") return; //if it isn't a terrain we also return

        //TODO: create instantiation point checker

        //recycling pick ups
        GameObject pickUp = _pickUps[_pickUpIndices[0]];
        _pickUpIndices.RemoveAt(0); //remove this object from the pickup list
        pickUp.transform.position = hit.point; //move the pickup to a position below the player
        pickUp.GetComponent<WaterPickUp>().ResetPickUp(); //reset the component
        WaterBalance--;

        //decrease the scale of the object
        _scale.x -= StartScale;
        _scale.y -= StartScale;
        _scale.z -= StartScale;

        //if we are out of water, we switch out of cloud
        if (_pickUpIndices.Count == 0) { GetComponent<Player>().ExitCloud(); return; }
    }

    public Vector3 DropPoint()
    {
        Vector3 point = Vector3.zero;
        float radius = 1;
        Vector3 normal = Vector3.zero;
        while (point == Vector3.zero)
        {
            Vector3 rayStart = new Vector3(Random.Range(XMin, XMax), Y, Random.Range(ZMin, ZMax));
            Vector3 rayXMin = new Vector3(rayStart.x - radius, rayStart.y, rayStart.z);
            Vector3 rayXMax = new Vector3(rayStart.x + radius, rayStart.y, rayStart.z);
            Vector3 rayZMin = new Vector3(rayStart.x, rayStart.y, rayStart.z - radius);
            Vector3 rayZMax = new Vector3(rayStart.x, rayStart.y, rayStart.z + radius);
            RaycastHit hit;
            RaycastHit hitXMin;
            RaycastHit hitXMax;
            RaycastHit hitZMin;
            RaycastHit hitZMax;
            if (!Physics.Raycast(rayStart, Vector3.down, out hit, RaycastLayers)) continue; //if nothing is hit we return zero
            if (!Physics.Raycast(rayXMin, Vector3.down, out hitXMin, RaycastLayers)) continue; //if nothing is hit we return zero
            if (!Physics.Raycast(rayXMax, Vector3.down, out hitXMax, RaycastLayers)) continue; //if nothing is hit we return zero
            if (!Physics.Raycast(rayZMin, Vector3.down, out hitZMin, RaycastLayers)) continue; //if nothing is hit we return zero
            if (!Physics.Raycast(rayZMax, Vector3.down, out hitZMax, RaycastLayers)) continue; //if nothing is hit we return zero
            if (hit.transform.gameObject.tag != "Terrain") continue;
            if (hitXMin.transform.gameObject.tag != "Terrain") continue;
            if (hitXMax.transform.gameObject.tag != "Terrain") continue;
            if (hitZMin.transform.gameObject.tag != "Terrain") continue;
            if (hitZMax.transform.gameObject.tag != "Terrain") continue;

            //only allow raindrops on slopes with steamness less than 3 degrees
            if (Vector2.Angle(hit.normal, Vector3.up) > 30) continue;

            Vector3 X = CircleCenter(hit.point, hitXMin.point, hitXMax.point, out normal);
            Vector3 Z = CircleCenter(hit.point, hitZMin.point, hitZMax.point, out normal);

            //test for positive or negative gaussian curvature and only allow negative
            if (X.y < hit.point.y || Z.y < hit.point.y) continue;

            //Debug.DrawLine(X, hit.point, Color.red, 10.0f);
            //Debug.DrawLine(Z, hit.point, Color.green, 10.0f);

            point = hit.point;
        }
        return point;
    }

    /// <summary>
    /// Calculates the center of the circle that passes through the 3 given points
    /// </summary>
    /// <param name="aP0"></param>
    /// <param name="aP1"></param>
    /// <param name="aP2"></param>
    /// <param name="normal">returns the normal of the plane the circle lies in</param>
    /// <returns>The circle center position</returns>
    public static Vector3 CircleCenter(Vector3 aP0, Vector3 aP1, Vector3 aP2, out Vector3 normal)
    {
        // two circle chords
        var v1 = aP1 - aP0;
        var v2 = aP2 - aP0;

        normal = Vector3.Cross(v1, v2);
        if (normal.sqrMagnitude < 0.00001f)
            return Vector3.one * float.NaN;
        normal.Normalize();

        // perpendicular of both chords
        var p1 = Vector3.Cross(v1, normal).normalized;
        var p2 = Vector3.Cross(v2, normal).normalized;
        // distance between the chord midpoints
        var r = (v1 - v2) * 0.5f;
        // center angle between the two perpendiculars
        var c = Vector3.Angle(p1, p2);
        // angle between first perpendicular and chord midpoint vector
        var a = Vector3.Angle(r, p1);
        // law of sine to calculate length of p2
        var d = r.magnitude * Mathf.Sin(a * Mathf.Deg2Rad) / Mathf.Sin(c * Mathf.Deg2Rad);
        if (Vector3.Dot(v1, aP2 - aP1) > 0)
            return aP0 + v2 * 0.5f - p2 * d;
        return aP0 + v2 * 0.5f + p2 * d;
    }
}