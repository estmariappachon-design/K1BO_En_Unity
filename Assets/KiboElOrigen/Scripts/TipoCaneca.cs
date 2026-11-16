using UnityEngine;

namespace KiboElOrigen
{
    // Código de colores de Colombia (Resolución 2184 de 2019)
    public enum TipoCaneca { Blanca, Negra, Verde }

    public static class InfoCaneca
    {
        public static string NombreColor(TipoCaneca t)
        {
            switch (t)
            {
                case TipoCaneca.Blanca: return "BLANCA";
                case TipoCaneca.Negra: return "NEGRA";
                default: return "VERDE";
            }
        }

        public static string Titulo(TipoCaneca t)
        {
            switch (t)
            {
                case TipoCaneca.Blanca: return "Blanca";
                case TipoCaneca.Negra: return "Negra";
                default: return "Verde";
            }
        }

        public static string Categoria(TipoCaneca t)
        {
            switch (t)
            {
                case TipoCaneca.Blanca: return "APROVECHABLES";
                case TipoCaneca.Negra: return "NO APROVECHABLES";
                default: return "ORGÁNICOS";
            }
        }

        public static string Ejemplos(TipoCaneca t)
        {
            switch (t)
            {
                case TipoCaneca.Blanca: return "Plástico · Vidrio · Metal · Papel · Cartón";
                case TipoCaneca.Negra: return "Servilletas · Papel higiénico · Residuos sucios";
                default: return "Restos de comida · Cáscaras · Frutas";
            }
        }

        public static Color ColorModelo(TipoCaneca t)
        {
            switch (t)
            {
                case TipoCaneca.Blanca: return new Color(0.93f, 0.93f, 0.92f);
                case TipoCaneca.Negra: return new Color(0.10f, 0.10f, 0.11f);
                default: return new Color(0.12f, 0.55f, 0.22f);
            }
        }

        // Color del texto en los mensajes de pantalla
        public static string ColorHex(TipoCaneca t)
        {
            switch (t)
            {
                case TipoCaneca.Blanca: return "#FFFFFF";
                case TipoCaneca.Negra: return "#B5B5B5";
                default: return "#5BE37A";
            }
        }

        public static Color ColorEtiqueta(TipoCaneca t)
        {
            switch (t)
            {
                case TipoCaneca.Blanca: return new Color(0.20f, 0.22f, 0.25f);
                case TipoCaneca.Negra: return new Color(0.95f, 0.95f, 0.95f);
                default: return new Color(0.96f, 0.98f, 0.94f);
            }
        }

        public static Color ColorTextoEtiqueta(TipoCaneca t)
        {
            switch (t)
            {
                case TipoCaneca.Blanca: return Color.white;
                case TipoCaneca.Negra: return new Color(0.08f, 0.08f, 0.08f);
                default: return new Color(0.05f, 0.35f, 0.12f);
            }
        }

        public static string Pintado(TipoCaneca t)
        {
            return "<b><color=" + ColorHex(t) + ">" + NombreColor(t) + "</color></b>";
        }
    }
}
