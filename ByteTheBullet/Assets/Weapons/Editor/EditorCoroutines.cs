using UnityEngine;
using UnityEditor;

namespace Weapons.Editor
{
    /// <summary>
    /// Simple implementation of coroutines for editor mode
    /// </summary>
    public static class EditorCoroutines
    {
        public static void StartCoroutine(System.Collections.IEnumerator routine, Object owner)
        {
            EditorApplication.CallbackFunction update = null;
            update = () =>
            {
                try
                {
                    if (!routine.MoveNext())
                    {
                        EditorApplication.update -= update;
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogException(ex);
                    EditorApplication.update -= update;
                }
            };
            
            EditorApplication.update += update;
        }
    }
}