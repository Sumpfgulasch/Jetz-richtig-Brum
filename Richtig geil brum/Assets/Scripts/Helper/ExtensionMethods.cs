using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


public static class ExtensionMethods
{
    public static float Remap(this float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }

    public static float CutNumberAtDecimalPlace(this float _f, int _decimalPlaces)
    {
        _decimalPlaces = Mathf.Clamp(_decimalPlaces, 0, 10); // make sure no false input gets put in. maxed it to 10, because more than 10 decimal places seems to not happen

        if (_decimalPlaces == 0)
        {
            return Mathf.Floor(_f); // cut off all decimal places
        }
        float mult = _decimalPlaces * 10f; 
        _f = Mathf.Floor(_f * mult); // offset the number to the left by the number of decimal places bsp: (decimal places = 1) (float = 5,63)  -> 5,63 * (1*10) = 56,3  then cutoff the decimal places. -> 56
        return _f / mult; // offset the number to the right by the number of decimal places it was put to the left:   56 -> 5,6
    }

    public static void ClampAngularVelocity(this Rigidbody _rB, float _max)
    {
        if (_rB.angularVelocity.magnitude > _max)
        {
            _rB.angularVelocity = _rB.angularVelocity.normalized * _max;
        }
    }
    public static void BrakeVelocity(this Rigidbody _rB, float _factor)
    {
        _factor = Mathf.Clamp01(_factor);
        _rB.velocity *= _factor;
    }
    public static void BrakeAngularVelocity(this Rigidbody _rB, float _factor)
    {
        _factor = Mathf.Clamp01(_factor);
        _rB.angularVelocity *= _factor;
    }

    //public static Vector3 WorldFromLocal(Transform _transform, Vector3 _localPosition)
    //{
    //    return _transform.position + (_transform.rotation * _localPosition);
    //}

    public static bool IsGenericList(this object o)
    {
        var oType = o.GetType();
        return (oType.IsGenericType && (oType.GetGenericTypeDefinition() == typeof(List<>)));
    }
}