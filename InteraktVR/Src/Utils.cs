using UnityEngine;

namespace InteraktVR
{
    public static class Utils
    {
        public static bool LookupComponent<T>(this GameObject go)
        {
            T t = go.GetComponent<T>();
            if (t == null) go.GetComponentInChildren<T>();
            return t != null;
        }
    }
}

