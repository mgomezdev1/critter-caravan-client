using UnityEditor.UIElements;
using System.Collections.Generic;
using Newtonsoft.Json;

public class ListConverter<T> : UxmlAttributeConverter<List<T>>
{
    public override List<T> FromString(string value)
    {
        return JsonConvert.DeserializeObject<List<T>>(value);
    }
    public override string ToString(List<T> value)
    {
        return JsonConvert.SerializeObject(value);
    }
}