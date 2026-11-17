using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KiboElOrigen
{
    public static class KiboUtil
    {
        // Busca componentes en las escenas abiertas (también los inactivos). No incluye prefabs del proyecto.
        public static List<T> Buscar<T>() where T : Component
        {
            var lista = new List<T>();
            foreach (var x in Resources.FindObjectsOfTypeAll<T>())
            {
                if (x == null) continue;
                if (!x.gameObject.scene.IsValid()) continue;
                if ((x.gameObject.hideFlags & HideFlags.HideAndDontSave) != 0) continue;
                lista.Add(x);
            }
            return lista;
        }

        public static T BuscarUno<T>() where T : Component
        {
            var l = Buscar<T>();
            return l.Count > 0 ? l[0] : null;
        }

        public static GameObject BuscarPorNombre(string nombre)
        {
            foreach (var t in Buscar<Transform>())
                if (t.name == nombre) return t.gameObject;
            return null;
        }

        // Identificador estable de un objeto en la escena (ruta + posición entre hermanos)
        public static string Ruta(Transform t)
        {
            var partes = new List<string>();
            while (t != null)
            {
                partes.Add(t.name + "#" + t.GetSiblingIndex());
                t = t.parent;
            }
            partes.Reverse();
            return string.Join("/", partes.ToArray());
        }

        static readonly List<RaycastResult> resultadosUI = new List<RaycastResult>();

        // true solo si el mouse está sobre algo VISIBLE de la interfaz (botón, panel, imagen).
        // Los paneles invisibles o transparentes ya no bloquean los clics en el mundo.
        public static bool PunteroSobreUI()
        {
            var es = EventSystem.current;
            if (es == null) return false;
            var datos = new PointerEventData(es) { position = Input.mousePosition };
            resultadosUI.Clear();
            es.RaycastAll(datos, resultadosUI);
            foreach (var r in resultadosUI)
            {
                if (r.gameObject == null) continue;
                if (r.gameObject.GetComponentInParent<Selectable>() != null) return true;
                var g = r.gameObject.GetComponent<Graphic>();
                if (g == null) continue;
                float alfa = g.color.a * (g.canvasRenderer != null ? g.canvasRenderer.GetInheritedAlpha() : 1f);
                if (alfa > 0.05f) return true;
            }
            return false;
        }

        public static Transform Jugador()
        {
            var pm = Object.FindAnyObjectByType<PlayerMovement>();
            if (pm != null) return pm.transform;
            GameObject p = null;
            try { p = GameObject.FindGameObjectWithTag("Player"); } catch { }
            return p != null ? p.transform : null;
        }
    }
}
