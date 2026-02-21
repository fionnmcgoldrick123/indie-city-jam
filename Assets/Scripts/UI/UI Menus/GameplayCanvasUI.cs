using UnityEngine;

public class GameplayCanvasUI : GenericSingleton<GameplayCanvasUI>
{
    public JoystickController movementJoystickController;
    public JoystickController atkJoystickController;
    protected override void Awake()
    {
        base.Awake();
    }
}
