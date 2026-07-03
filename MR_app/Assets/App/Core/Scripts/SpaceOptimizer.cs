using UnityEngine;

public class SpaceOptimizer : MonoBehaviour
{
    public static SpaceOptimizer Instance;

    [Header("Nastavení Radaru")]
    public LayerMask roomMeshLayer;
    public int maxAttempts = 20;

    void Awake()
    {
        Instance = this;
    }


    public Vector3? GetSafePosition(
        Transform head,
        float minDist,
        float maxDist,
        float minHeight,
        float maxHeight,
        float width,
        float checkRadius = 0.2f)
    {
        Quaternion headRotation = Quaternion.Euler(0, head.eulerAngles.y, 0);

        for (int i = 0; i < maxAttempts; i++)
        {
            float randomX = Random.Range(-width, width);
            float randomY = Random.Range(minHeight, maxHeight);
            float randomZ = Random.Range(minDist, maxDist);

            Vector3 relativeOffset = new Vector3(randomX, randomY - head.position.y, randomZ);
            Vector3 potentialPos = head.position + (headRotation * relativeOffset);

            Vector3 directionToTarget = potentialPos - head.position;
            float distanceToTarget = directionToTarget.magnitude;

            // 1. Projde paprsek èistì?
            if (!Physics.Raycast(head.position, directionToTarget.normalized, distanceToTarget, roomMeshLayer))
            {
                // 2. Je kolem bodu bublina 20 cm (nebo jiná) bez pøekážek?
                if (!Physics.CheckSphere(potentialPos, checkRadius, roomMeshLayer))
                {
                    return potentialPos;
                }
            }
        }
        return null;
    }

    
    public bool IsPositionSafe(Vector3 startPos, Vector3 targetPos, float checkRadius = 0.2f)
    {
        Vector3 direction = targetPos - startPos;
        float distance = direction.magnitude;

        // 1. Projde paprsek èistì?
        if (Physics.Raycast(startPos, direction.normalized, distance, roomMeshLayer))
        {
            return false;
        }

        // 2. Je kolem bodu bublina 20 cm (nebo jiná) bez pøekážek?
        if (Physics.CheckSphere(targetPos, checkRadius, roomMeshLayer))
        {
            return false;
        }

        return true;
    }
}