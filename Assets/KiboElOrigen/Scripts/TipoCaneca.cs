using System.Text;
using UnityEngine;

namespace KiboElOrigen
{
    // Código de colores de Colombia (Resolución 2184 de 2019)
    public enum TipoCaneca { Blanca, Negra, Verde }

    public static class InfoCaneca
    {
        public static readonly TipoCaneca[] Todas = { TipoCaneca.Blanca, TipoCaneca.Negra, TipoCaneca.Verde };

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
                case TipoCaneca.Blanca: return "Plástico, vidrio, metal, papel y cartón limpios";
                case TipoCaneca.Negra: return "Servilletas, empaques metalizados y cartón sucio";
                default: return "Restos de comida, cáscaras y frutas";
            }
        }

        // Color del plástico de la caneca 3D
        public static Color ColorModelo(TipoCaneca t)
        {
            switch (t)
            {
                case TipoCaneca.Blanca: return new Color(0.93f, 0.93f, 0.91f);
                case TipoCaneca.Negra: return new Color(0.16f, 0.16f, 0.18f);
                default: return new Color(0.18f, 0.62f, 0.30f);
            }
        }

        // Color para resaltar el nombre de la caneca en los textos
        public static string ColorHex(TipoCaneca t)
        {
            switch (t)
            {
                case TipoCaneca.Blanca: return "#FFFFFF";
                case TipoCaneca.Negra: return "#2E2E33";
                default: return "#46D16B";
            }
        }

        public static string Pintado(TipoCaneca t)
        {
            return "<color=" + ColorHex(t) + ">" + NombreColor(t) + "</color>";
        }

        public static string Sprite(TipoCaneca t) { return "caneca_" + Titulo(t).ToLowerInvariant(); }
        public static string Icono(TipoCaneca t) { return "icono_" + Titulo(t).ToLowerInvariant(); }
    }

    // Decide en qué caneca va cada residuo del inventario (ItemData).
    // Si en el ItemData marcas "Caneca Manual", se usa lo que elegiste ahí.
    public static class KiboClasificador
    {
        static readonly string[] clavesNegra = { "papitas", "paquete", "pizza", "vaso", "servilleta", "higienico", "icopor", "tapabocas", "colilla", "chicle", "panal", "jeringa", "encendedor", "sucio" };
        static readonly string[] clavesVerde = { "cascara", "banano", "pescado", "espina", "manzana", "comida", "fruta", "organic", "hueso", "cafe", "verdura" };

        public static TipoCaneca Tipo(ItemData item)
        {
            if (item == null) return TipoCaneca.Blanca;
            if (item.canecaManual) return item.caneca;
            return TipoPorNombre(Clave(item));
        }

        public static TipoCaneca TipoPorNombre(string nombre)
        {
            string n = Normalizar(nombre);
            if (Contiene(n, clavesNegra)) return TipoCaneca.Negra;
            if (Contiene(n, clavesVerde)) return TipoCaneca.Verde;
            return TipoCaneca.Blanca;
        }

        public static string Nombre(ItemData item)
        {
            if (item == null) return "Residuo";
            if (!string.IsNullOrEmpty(item.nombreVisible)) return item.nombreVisible;
            return NombrePorClave(Clave(item));
        }

        public static string Explicacion(ItemData item)
        {
            if (item == null) return "";
            if (!string.IsNullOrEmpty(item.explicacion)) return item.explicacion;
            return ExplicacionPorClave(Clave(item));
        }

        static string Clave(ItemData item)
        {
            return (item.nombreItem ?? "") + " " + item.name;
        }

        public static string NombrePorClave(string clave)
        {
            string n = Normalizar(clave);
            if (n.Contains("papitas") || n.Contains("paquete")) return "Paquete de papitas";
            if (n.Contains("pizza")) return "Caja de pizza";
            if (n.Contains("cereal")) return "Caja de cereal";
            if (n.Contains("caja")) return "Caja de cartón";
            if (n.Contains("vidrio")) return "Botella de vidrio";
            if (n.Contains("botella")) return "Botella de plástico";
            if (n.Contains("bolsa")) return "Bolsa plástica";
            if (n.Contains("craft")) return "Papel craft";
            if (n.Contains("rayado")) return "Papel rayado";
            if (n.Contains("papel")) return "Papel blanco";
            if (n.Contains("vaso")) return "Vaso desechable";
            if (n.Contains("cascara") || n.Contains("banano")) return "Cáscara de banano";
            if (n.Contains("pescado") || n.Contains("espina")) return "Espinas de pescado";
            if (n.Contains("madera")) return "Pedazo de madera";
            if (n.Contains("lata")) return "Lata";
            return Bonito(clave);
        }

        public static string ExplicacionPorClave(string clave)
        {
            string n = Normalizar(clave);
            if (n.Contains("papitas") || n.Contains("paquete")) return "Los empaques metalizados no se pueden reciclar.";
            if (n.Contains("pizza")) return "El cartón con grasa o comida ya no se puede reciclar.";
            if (n.Contains("vaso")) return "Los vasos desechables usados están sucios y mezclan materiales.";
            if (n.Contains("cascara") || n.Contains("banano")) return "Las cáscaras son orgánicas: ¡se convierten en abono!";
            if (n.Contains("pescado") || n.Contains("espina")) return "Los restos de comida son orgánicos.";
            if (n.Contains("vidrio")) return "El vidrio se puede reciclar muchas veces.";
            if (n.Contains("botella") || n.Contains("bolsa")) return "El plástico limpio se puede reciclar.";
            if (n.Contains("papel") || n.Contains("caja") || n.Contains("cereal")) return "El papel y el cartón limpios y secos se reciclan.";
            if (n.Contains("madera")) return "La madera limpia se puede aprovechar.";
            if (n.Contains("lata")) return "El metal se recicla por completo.";
            return "";
        }

        static bool Contiene(string n, string[] claves)
        {
            for (int i = 0; i < claves.Length; i++) if (n.Contains(claves[i])) return true;
            return false;
        }

        public static string Normalizar(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            s = s.ToLowerInvariant();
            return s.Replace('á', 'a').Replace('é', 'e').Replace('í', 'i').Replace('ó', 'o').Replace('ú', 'u').Replace('ñ', 'n');
        }

        static string Bonito(string s)
        {
            s = s.Trim();
            int sp = s.IndexOf(' ');
            if (sp > 0) s = s.Substring(0, sp);
            if (s.StartsWith("IN_")) s = s.Substring(3);
            if (s.StartsWith("Item_")) s = s.Substring(5);
            var sb = new StringBuilder();
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '_') { sb.Append(' '); continue; }
                if (i > 0 && char.IsUpper(c) && char.IsLower(s[i - 1])) sb.Append(' ');
                sb.Append(c);
            }
            string r = sb.ToString().Trim();
            if (r.Length == 0) return "Residuo";
            return char.ToUpper(r[0]) + r.Substring(1).ToLowerInvariant();
        }
    }
}
