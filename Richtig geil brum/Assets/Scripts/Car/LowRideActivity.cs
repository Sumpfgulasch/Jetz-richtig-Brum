using UnityEngine;

public class LowRideActivity
{
    private float[] values;
    /// <summary>
    /// Front(0), back(1) Set-access via indexer of class-instance.
    /// </summary>
    public float[] Values
    { get { return values; } private set { values = value; } }

    public LowRideActivity() // empty constructor that inits the values.
    {
        values = new float[2] { 0f, 0f};
    }

    /// <summary>
    /// Set the lowRideActivity-variable, which is used to deactivate the magnetPower temporarily.
    /// </summary>
    /// <param name="_lowRideValue"></param>
    public void SetLowRideActivity(Vector2 _lowRideValue, float _lowRideActivityDecreaseSpeed = 0.01f, bool _invertLowRideInput = false)
    {
        // inverted controls
        if (_invertLowRideInput)
            _lowRideValue.y *= -1f;

        // (SCHEI� CODE) Alle 4 Richtungen der lowRideActivity erh�hen oder verringern

        // front
        if (_lowRideValue.y > Values[0])
            Values[0] = _lowRideValue.y;                                    // set
        else
            Values[0] -= _lowRideActivityDecreaseSpeed;                     // decrease

        // back
        if (-_lowRideValue.y > Values[1])
            Values[1] = -_lowRideValue.y;                                   // set
        else
            Values[1] -= _lowRideActivityDecreaseSpeed;                     // decrease
    }

    public bool IsActive
    {
        get
        {
            foreach (float value in Values)
            {
                if (value != 0)
                    return true;
            }
            return false;
        }
    }
    public float HighestValue
    {
        get
        {
            float highestValue = Values[0];
            foreach (float value in Values)
            {
                if (value > highestValue)
                    highestValue = value;
            }
            return highestValue;
        }
    }
    public float this[CarDir _carDir]
    {
        get
        {
            switch (_carDir)
            {
                case CarDir.F:
                    return Values[0];
                case CarDir.B:
                    return Values[1];
            }

            return 0f;
        }
        set
        {
            float clampedVal = Mathf.Clamp01(value); // side-stick-bewegung erstmal ignorieren
            switch (_carDir)
            {
                case CarDir.F:
                    Values[0] = clampedVal;
                    break;
                case CarDir.B:
                    Values[1] = clampedVal;
                    break;
            }
        }
    }
}