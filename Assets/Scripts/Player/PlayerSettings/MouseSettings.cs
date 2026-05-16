using UnityEngine;

public class MouseSettings
{
    public float MouseSensitivity;
    public bool EnableSmoothness;
    public float SmoothnessSpeed;
    public bool EnableAcceleration;
    public float AccelerationThreshold;
    public float AccelerationMultiplier;
    public float MaxAcceleration;

    public MouseSettings(
            float mouseSensitivity, 
            bool enableSmoothness, 
            float smoothnessSpeed, 
            bool enableAcceleration, 
            float accelerationThreshold, 
            float accelerationMultiplier, 
            float maxAcceleration)
    {
        MouseSensitivity = mouseSensitivity;
        EnableSmoothness = enableSmoothness;
        SmoothnessSpeed = smoothnessSpeed;
        EnableAcceleration = enableAcceleration;
        AccelerationThreshold = accelerationThreshold;
        AccelerationMultiplier = accelerationMultiplier;
        MaxAcceleration = maxAcceleration;
    }
}
