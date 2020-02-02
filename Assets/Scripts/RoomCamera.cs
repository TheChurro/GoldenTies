using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomCamera : MonoBehaviour
{
    public RectTransform UIContainer;
    public new Camera camera;
    // Screen Space boundary of where the play space should be rendered. This accounts
    // for all the area the UI occludes.
    public Bounds ViewBounds;
    private Bounds ViewWorldBounds;
    // Boundaries of the play space in world bounds.
    public Bounds WorldBounds;
    private Bounds CameraWorldPositionBounds;
    private Vector3 CameraOffset;
    public GameObject TrackingObject;
    private Vector2 LastPosition;
    private bool isRestoring;
    public Vector2 cameraStaySize;
    public Vector2 cameraRestoreSize;
    public Vector2 cameraJumpSize;
    public float cameraMovementRate;
    public void SetWorldBounds(Bounds NewBounds) {
        WorldBounds = NewBounds;
        print(WorldBounds);
        RecomputeCameraPositionBounds();
    }

    public void SetViewBounds(Bounds NewBounds) {
        ViewBounds = NewBounds;
        var min = camera.ScreenToWorldPoint(ViewBounds.min);
        var max = camera.ScreenToWorldPoint(ViewBounds.max);
        ViewWorldBounds.SetMinMax(min, max);
        CameraOffset = ViewWorldBounds.center - camera.transform.position;
        RecomputeCameraPositionBounds();
    }

    private void RecomputeCameraPositionBounds() {
        CameraWorldPositionBounds.center = WorldBounds.center;
        var extents = WorldBounds.extents - ViewWorldBounds.extents;
        extents.x = Mathf.Max(0, extents.x);
        extents.y = Mathf.Max(0, extents.y);
        extents.z = Mathf.Max(0, extents.z);
        CameraWorldPositionBounds.extents = extents;
    }

    private Vector2 GetClosestPositionInBoundsTo(Vector3 point) {
        Vector2 pos = Vector2.zero;
        pos.x = Mathf.Min(Mathf.Max(CameraWorldPositionBounds.min.x, point.x), CameraWorldPositionBounds.max.x);
        pos.y = Mathf.Min(Mathf.Max(CameraWorldPositionBounds.min.y, point.y), CameraWorldPositionBounds.max.y);
        return pos;
    }

    public void Track(GameObject newObject) {
        TrackingObject = newObject;
        LastPosition = GetClosestPositionInBoundsTo(TrackingObject.transform.position);
        UpdatePosition(LastPosition);
        isRestoring = false;
    }

    void Start() {
        var canvasRect = UIContainer.rect;
        var cameraRect = camera.pixelRect;
        ViewBounds.SetMinMax(
            new Vector3(cameraRect.min.x, canvasRect.max.y, 10),
            new Vector3(cameraRect.max.x, cameraRect.max.y, 10)
        );
        SetViewBounds(ViewBounds);
    }

    void UpdatePosition(Vector2 pos) {
        this.transform.position = new Vector3(pos.x - CameraOffset.x, pos.y - CameraOffset.y, this.transform.position.z);
    }

    // Update is called once per frame
    void Update()
    {
        if (TrackingObject != null) {
            Vector2 newPosition = GetClosestPositionInBoundsTo(TrackingObject.transform.position);
            Vector2 offset = newPosition - LastPosition;
            Vector2 absOffset = new Vector2(Mathf.Abs(offset.x), Mathf.Abs(offset.y));
            if (isRestoring) {
                LastPosition = LastPosition + offset.normalized * Time.deltaTime * cameraMovementRate;
                UpdatePosition(LastPosition);
            }
            if (absOffset.x > cameraStaySize.x || absOffset.y > cameraStaySize.y) {
                isRestoring = true;
            } else if (absOffset.x < cameraRestoreSize.x && absOffset.y < cameraRestoreSize.y) {
                isRestoring = false;
            } else if (absOffset.x > cameraJumpSize.x || absOffset.y > cameraJumpSize.y) {
                UpdatePosition(newPosition);
                LastPosition = newPosition;
                isRestoring = false;
            }
        }
    }

    void OnDrawGizmos() {
        // Gizmos.color = Color.red;
        // Gizmos.DrawWireCube(ViewWorldBounds.center, 2 * ViewWorldBounds.extents);
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(WorldBounds.center, 2 * WorldBounds.extents);
        // Gizmos.color = Color.blue;
        // Gizmos.DrawWireCube(CameraWorldPositionBounds.center, 2 * CameraWorldPositionBounds.extents);
        // Gizmos.color = Color.yellow;
        // if (TrackingObject != null) {
        //     Gizmos.DrawWireCube(GetClosestPositionInBoundsTo(TrackingObject.transform.position), new Vector3(0.5f, 0.5f, 0.5f));
        // }
    }
}
