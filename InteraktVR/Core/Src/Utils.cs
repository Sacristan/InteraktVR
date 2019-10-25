using UnityEngine;

namespace InteraktVR.Core
{
    public static class Utils
    {
        public static T LookupComponent<T>(this GameObject go)
        {
            T t = go.GetComponent<T>();
            if (t == null) go.GetComponentInChildren<T>();
            return t;
        }
    }
}

