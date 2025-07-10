using UnityEngine;
using Cosmos.Entity;

public class PlayerMovementComponent : EntityComponentBase
{
    [SerializeField] private float moveSpeed = 4;
    [SerializeField] private float rotationSpeed = 8;
    
    public void Move(Vector3 direction)
    {
        if (direction == Vector3.zero)
            return;
            
        Transform transform = Entity.transform;
        Vector3 normalizedDirection = direction.normalized;
        
        // 旋转朝向移动方向
        transform.rotation = Quaternion.Lerp(
            transform.rotation, 
            Quaternion.LookRotation(normalizedDirection), 
            rotationSpeed * Time.deltaTime
        );
        
        // 执行移动
        transform.position = Vector3.Lerp(
            transform.position, 
            transform.position + normalizedDirection, 
            Time.deltaTime * moveSpeed
        );
    }
}