using System.Collections.Generic;
using UnityEngine;

namespace KiboElOrigen
{
    [System.Serializable]
    public class DatosPartida
    {
        public int aciertos;
        public int fallos;
        public int recolectados;
        public List<string> ocultos = new List<string>();      // residuos que ya no están en el mapa
        public List<string> barra = new List<string>();        // residuos en la barra de inventario
        public List<string> mochila = new List<string>();      // residuos en la mochila
        public List<int> mochilaCantidad = new List<int>();
        public float jugadorX, jugadorY, jugadorZ;
        public bool tienePosicion;
    }

    // Guardar / continuar partida (se guarda en PlayerPrefs)
    public static class KiboGuardado
    {
        const string CLAVE = "Kibo_Partida";

        // Lo activa el botón CONTINUAR del menú para que MainScene cargue la partida
        public static bool CargarAlIniciar;

        public static bool HayPartida { get { return PlayerPrefs.HasKey(CLAVE); } }

        public static void Guardar(DatosPartida d)
        {
            PlayerPrefs.SetString(CLAVE, JsonUtility.ToJson(d));
            PlayerPrefs.Save();
        }

        public static DatosPartida Leer()
        {
            if (!HayPartida) return null;
            try { return JsonUtility.FromJson<DatosPartida>(PlayerPrefs.GetString(CLAVE)); }
            catch { return null; }
        }

        public static void Borrar()
        {
            PlayerPrefs.DeleteKey(CLAVE);
            PlayerPrefs.Save();
        }

        public static int MejorPuntaje
        {
            get { return PlayerPrefs.GetInt("Kibo_Mejor", 0); }
            set { PlayerPrefs.SetInt("Kibo_Mejor", value); PlayerPrefs.Save(); }
        }
    }
}
