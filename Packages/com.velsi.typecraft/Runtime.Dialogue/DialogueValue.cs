using System;
using System.Globalization;
using UnityEngine;

namespace Velsi.Typecraft.Dialogue
{
    [Serializable]
    public struct DialogueValue : IEquatable<DialogueValue>
    {
        [SerializeField] private DialogueValueType _type;
        [SerializeField] private bool _bool;
        [SerializeField] private int _int;
        [SerializeField] private float _float;
        [SerializeField] private string _string;

        private DialogueValue(DialogueValueType type, bool boolValue, int intValue, float floatValue, string stringValue)
        {
            _type = type;
            _bool = boolValue;
            _int = intValue;
            _float = floatValue;
            _string = stringValue;
        }

        public DialogueValueType Type => _type;
        public bool IsNumber => _type == DialogueValueType.Int || _type == DialogueValueType.Float;

        public bool AsBool => _type switch
        {
            DialogueValueType.Bool => _bool,
            DialogueValueType.Int => _int != 0,
            DialogueValueType.Float => _float != 0f,
            _ => !string.IsNullOrEmpty(_string)
        };

        public int AsInt => _type switch
        {
            DialogueValueType.Bool => _bool ? 1 : 0,
            DialogueValueType.Int => _int,
            DialogueValueType.Float => (int)_float,
            _ => int.TryParse(_string, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed) ? parsed : 0
        };

        public float AsFloat => _type switch
        {
            DialogueValueType.Bool => _bool ? 1f : 0f,
            DialogueValueType.Int => _int,
            DialogueValueType.Float => _float,
            _ => float.TryParse(_string, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) ? parsed : 0f
        };

        public string AsString => ToString();

        public static DialogueValue Bool(bool value) => new DialogueValue(DialogueValueType.Bool, value, 0, 0f, null);
        public static DialogueValue Int(int value) => new DialogueValue(DialogueValueType.Int, false, value, 0f, null);
        public static DialogueValue Float(float value) => new DialogueValue(DialogueValueType.Float, false, 0, value, null);
        public static DialogueValue String(string value) => new DialogueValue(DialogueValueType.String, false, 0, 0f, value ?? string.Empty);

        public static DialogueValue DefaultOf(DialogueValueType type) => type switch
        {
            DialogueValueType.Bool => Bool(false),
            DialogueValueType.Int => Int(0),
            DialogueValueType.Float => Float(0f),
            _ => String(string.Empty)
        };

        public DialogueValue ConvertTo(DialogueValueType type) => type switch
        {
            DialogueValueType.Bool => Bool(AsBool),
            DialogueValueType.Int => Int(AsInt),
            DialogueValueType.Float => Float(AsFloat),
            _ => String(AsString)
        };

        public static DialogueValue Parse(string text)
        {
            if (string.IsNullOrEmpty(text))
                return String(string.Empty);

            if (bool.TryParse(text, out bool boolValue))
                return Bool(boolValue);

            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intValue))
                return Int(intValue);

            if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float floatValue))
                return Float(floatValue);

            return String(text);
        }

        public static DialogueValue Parse(string text, DialogueValueType type) => type switch
        {
            DialogueValueType.String => String(text),
            DialogueValueType.Bool when bool.TryParse(text, out bool boolValue) => Bool(boolValue),
            _ => Parse(text).ConvertTo(type)
        };

        public static DialogueValue Add(DialogueValue left, DialogueValue right)
        {
            if (left._type == DialogueValueType.String || right._type == DialogueValueType.String)
                return String(left.AsString + right.AsString);

            if (left._type == DialogueValueType.Float || right._type == DialogueValueType.Float)
                return Float(left.AsFloat + right.AsFloat);

            return Int(left.AsInt + right.AsInt);
        }

        public static int Compare(DialogueValue left, DialogueValue right)
        {
            if (left.IsNumber && right.IsNumber)
            {
                if (left._type == DialogueValueType.Int && right._type == DialogueValueType.Int)
                    return left._int.CompareTo(right._int);
                return left.AsFloat.CompareTo(right.AsFloat);
            }

            if (left._type == DialogueValueType.Bool || right._type == DialogueValueType.Bool)
                return left.AsBool.CompareTo(right.AsBool);

            if (left.IsNumber || right.IsNumber)
                return left.AsFloat.CompareTo(right.AsFloat);

            return string.CompareOrdinal(left.AsString, right.AsString);
        }

        public bool Equals(DialogueValue other) => Compare(this, other) == 0;

        public override bool Equals(object obj) => obj is DialogueValue other && Equals(other);

        public override int GetHashCode() => _type switch
        {
            DialogueValueType.Bool => _bool.GetHashCode(),
            DialogueValueType.Int => ((float)_int).GetHashCode(),
            DialogueValueType.Float => _float.GetHashCode(),
            _ => (_string ?? string.Empty).GetHashCode()
        };

        public static bool operator ==(DialogueValue left, DialogueValue right) => left.Equals(right);
        public static bool operator !=(DialogueValue left, DialogueValue right) => !left.Equals(right);

        public override string ToString() => _type switch
        {
            DialogueValueType.Bool => _bool ? "true" : "false",
            DialogueValueType.Int => _int.ToString(CultureInfo.InvariantCulture),
            DialogueValueType.Float => _float.ToString("R", CultureInfo.InvariantCulture),
            _ => _string ?? string.Empty
        };
    }

    public enum DialogueValueType
    {
        Bool,
        Int,
        Float,
        String
    }
}
